﻿# Manifest Manipulation

|                         | Untyped Merges | Merges  | Replacements | Deletions | MDDB Update |
|-------------------------|----------------|---------|--------------|-----------|-------------|
| Custom Streaming Assets | x              | x       | x            | -         | -           |
| StreamingAssets         | x              | x       | x            | -         | x           |
| Content Packs           | x              | x       | x            | -         | x           |
| New Mod Content         | -              | x       | x            | - (1)     | x           |
| Custom Resources        | -              | - (1)   | x            | - (1)     | -           |

1) Support was dropped in ModTek v2, mostly to support content packs.

Resources:
- Custom Streaming Assets: DebugSettings and GameTip found in the StreamingAssets folder but not in `VersionManifest.csv`.

- StreamingAssets: Vanilla content found in BTRL.

- Content Packs: DLC content provided by HBS. Content pack ids:
  shadowhawkdlc, flashpoint, urbanwarfare, heavymetal

- New Mod Content: Content that is not from HBS. but still a BattleTechResourceType.

- Custom Resources: Content that is not of type BattleTechResourceType,
  useful to provide completely new or not yet exposed data.
  Examples are SoundBank(Def) and Video in ModTek.
  DLL mods can introduce processing of new resource types that other mods can then provide content for.

Resource Modification Types:
- Untyped Merges: All merges reference an id and type,
  if the type of a resource can be auto-detected,
  since there is only one id, auto-merging can happen.
  All base game files that are mergable only have one type per id.

- Merges: Resources with of vanilla type (BattleTechResourceType)
  and file type csv, txt and json can be merged.
  JSON merges are done with Newtonsoft JSON.NET.
  Txt and CSV merges simply append any text to the existing file.

- Replacements: Instead of merging, one can specify a type,
  id and filepath to overwrite a file completely.
  This is recommended for large mod packs to make sure there are not several
  merges interfering which each other differently based on load order.
  For smaller mods, you don't want to overwrite changes made by other mods,
  there is a good chance using merges instead of replacements is better in that case.

- Deletions: Not supported at all (anymore) since base types that would be in MDDB can't be removed anyway yet.
  Bigger mod packs disable instead of removing content, so deletion doesn't make much sense to keep around.

- MDDB Update: MDDB is used by the game to index and then find certain types of data:
  ContractOverride, LanceDef, PilotDef, SimGameEventDef, MechDef, WeaponDef, TurretDef, VehicleDef, UpgradeDef.
