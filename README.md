# CnstSynReqtificator
Run Reqtificator via [Synthesis](https://github.com/Mutagen-Modding/Synthesis) on [Constellations - A true RPG](https://next.nexusmods.com/skyrimspecialedition/collections/9zfscf) collection.

# Note
Only v5.2.3 branch of [Requiem](https://github.com/ProbablyManuel/requiem) is supported for now.
Other versions may require some changes to compile/work.

# Installation
1. Code -> Download ZIP.
2. Install it like a typical mod.

   SynReqtificator.csproj should end up in the same folder as Reqtificator.csproj.
3. Run Synthesis and go to Local Solution -> Existing:

   Set Solution Path to ...\Skyrim\Data\Source\Reqtificator\SynReqtificator.sln
   
   Set Patcher Projects to SynReqtificator.csproj (it'll likely be preselected)
4. Go to User Settings and choose mods you want to reqtify.
5. Run the patcher.

# Known Issues
Issue: _System.IO.IOException: The process cannot access the file '...\Skyrim\Data\Patches for the Indifferent.esp' because it is being used by another process._

Solution: exit Synthesis -> remove Patches for the Indifferent.esp -> start Synthesis.

Basically, Synthesis uses the esp to build a Load Order (or Link Cache?), so Reqtificator can't overwrite it.
I didn't investigate much in terms of what could be done.

Issue: _Editing Reqtificator settings_

Solution: Copy components\mutagen-reqtificator\Reqtificator\Resources\DefaultUserConfig.json to ...\Skyrim\Data\Reqtificator -> rename it to UserSettings.json -> edit the file as needed.

Was too lazy to add those to the interface...
