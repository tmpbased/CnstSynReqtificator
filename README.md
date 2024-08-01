# CnstSynReqtificator
Run Reqtificator via Synthesis on "Constellations - A true RPG" collection.

# Installation
1. Download https://github.com/ProbablyManuel/requiem/archive/refs/tags/v5.2.3.zip or check out v5.2.3 branch of https://github.com/ProbablyManuel/requiem
2. Copy files from this repository to components/mutagen-reqtificator folder, so that SynReqtificator.sln is in the same folder as mutagen-reqtificator.sln
3. Run Synthesis -> Local Solution -> Existing -> set Solution Path to a path to SynReqtificator.sln & set Patcher Projects to SynReqtificator.csproj (it'll likely be preselected)
4. Go to User Settings and choose mods you want to patch.

# Known Issues
Issue: _System.IO.IOException: The process cannot access the file '...\Skyrim\Data\Patches for the Indifferent.esp' because it is being used by another process._

Solution: exit Synthesis -> remove Patches for the Indifferent.esp -> start Synthesis.
