using System.Collections.Immutable;
using Hocon;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Plugins;
using Mutagen.Bethesda.Plugins.Binary;
using Mutagen.Bethesda.Plugins.Order;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.Synthesis;
using Noggog;
using Reqtificator;
using Reqtificator.Configuration;
using Reqtificator.Events;
using Reqtificator.StaticReferences;
using Serilog.Events;

namespace SynthesisTest
{
    public record Settings
    {
        public string PluginName { get; set; } = "Patches for the Indifferent";

        public List<ModKey> Mods { get; set; } = new();
    }

    public class Program
    {
        private static Lazy<Settings> formSettings = null!;

        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetAutogeneratedSettings("Settings", "Settings.json", out formSettings)
                .SetTypicalOpen(GameRelease.SkyrimSE, "YourPatcher.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            if (formSettings.Value.Mods.Count == 0)
            {
                Console.WriteLine("No mods were selected for patching. Skipping.");
                return;
            }
            ModKey PatchModKey = new(formSettings.Value.PluginName, ModType.Plugin);
            var _version = new RequiemVersion(50203);
            var masterSet = new HashSet<ModKey>();
            var masterQueue = new Queue<ModKey>();
            masterQueue.Enqueue(new("Requiem", ModType.Plugin));
            // There's probably some smart way to detect all present Requiem's addons.
            // I'll just list them here manually for now.
            masterQueue.Enqueue(new("Requiem - Special Feats", ModType.Plugin));
            masterQueue.Enqueue(new("Requiem - Expanded Grimoire", ModType.Plugin));
            masterQueue.Enqueue(formSettings.Value.Mods);
            do
            {
                var modKey = masterQueue.Dequeue();
                if (state.LoadOrder.TryGetIfEnabledAndExists(modKey, out var mod))
                {
                    _ = masterSet.Add(modKey);
                    foreach (var masterRef in mod.MasterReferences)
                    {
                        masterQueue.Enqueue(masterRef.Master);
                    }
                }
            } while (masterQueue.Count > 0);
            var _context = new GameContext
            (
                state.LoadOrder.ListedOrder
                    .OnlyEnabled().Where(it => masterSet.Contains(it.ModKey))
                    .TakeWhile(it => it.ModKey != PatchModKey)
                    .Select(it => it as IModListingGetter).ToImmutableList(),
                state.DataFolderPath,
                state.GameRelease
            );
            var configFolder = Path.Combine(_context.DataFolder, "Reqtificator", "Config");
            Console.WriteLine($"Configuration folder: {configFolder}");
            var reqtificatorConfig = ReqtificatorConfig.ParseConfig
            (
                _context.ActiveMods
                    .Select(m => Path.Combine(configFolder, m.ModKey.FileName.NameWithoutExtension, "Reqtificator.conf"))
                    .Where(File.Exists)
                    .WithIndex()
                    .Tap(f => Console.WriteLine($"loading Reqtificator config (priority {f.Index + 1}) from '{f.Item}'"))
                    .Select(f => HoconConfigurationFactory.FromFile(f.Item))
                    .ToImmutableList()
            );
            var _events = new InternalEvents();
            _events.StateChanged += (sender, args) => Console.WriteLine(args.Readable);
            var _executor = new MainLogicExecutor(_events, _context, reqtificatorConfig, _version);
            string userConfigFile = Path.Combine(_context.DataFolder, "Reqtificator", "UserSettings.json");
            var userConfig = UserSettings.LoadUserSettings(userConfigFile);
            var readyToPatchState = ReqtificatorState.ReadyToPatch(userConfig, _context.ActiveMods.Select(x => x.ModKey));
            Console.WriteLine($"Ready to patch: userConfig detected\r\n{userConfig}");
            var loadOrder = LoadOrder.Import<ISkyrimModGetter>(_context.DataFolder, _context.ActiveMods, _context.Release);
            int pluginVersion = (int)((IGlobalIntGetter)loadOrder.ToImmutableLinkCache()
                .Resolve<IGlobalGetter>(GlobalVariables.VersionStampPlugin.FormKey)).Data!;
            if (_version.AsNumeric() != pluginVersion)
            {
                Console.WriteLine($@"
                There's a version mismatch between your Reqtificator and your Requiem.esp!
                **Reqtificator Version:** {_version}
                **Requiem.esp Version:** {new RequiemVersion(pluginVersion)}");
                return;
            }
            var logLevel = userConfig.VerboseLogging ? LogEventLevel.Debug : LogEventLevel.Information;
            var logContext = new ReqtificatorLogContext(LogUtils.DefaultLogFileName);
            logContext.LogLevel.MinimumLevel = logLevel;
            Console.WriteLine("start patching");
            var generatedPatch = _executor.GeneratePatch(loadOrder, userConfig, PatchModKey) switch
            {
                Success<SkyrimMod> s => s.Value,
                Failed<SkyrimMod> f => throw f.Error,
                _ => throw new NotImplementedException()
            };
            // Remove the records that are unrelated to selected mods.
            var modsLinkCache = formSettings.Value.Mods.Select(it => state.LoadOrder.TryGetValue(it))
                .Where(it => it != null && it.Enabled && it.Mod != null).Select(it => it!)
                .ToImmutableLinkCache();
            foreach (var majorRecordCtx in generatedPatch.EnumerateMajorRecords(typeof(ISkyrimMajorRecordGetter)))
            {
                if (!modsLinkCache.TryResolve(majorRecordCtx.FormKey, typeof(ISkyrimMajorRecordGetter), out _))
                {
                    generatedPatch.Remove(majorRecordCtx.FormKey);
                }
            }
            Console.WriteLine("done patching, now exporting to disk");
            generatedPatch.WriteToBinaryParallel(Path.Combine(_context.DataFolder, generatedPatch.ModKey.FileName), new BinaryWriteParameters
            {
                MastersListOrdering = new BinaryWriteParameters.MastersListOrderingByLoadOrder(loadOrder.Keys)
            });
            Console.WriteLine("done exporting");
        }
    }
}
