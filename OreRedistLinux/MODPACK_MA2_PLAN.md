# Mod plan with Mod Adjuster 2: Better Stone + Better Resources pack

Plan, 2026-11-09. Nothing here is built yet. Everything about Mod Adjuster 2 (MA2) below was read from its code in the workshop folder (mod `3017795356`, namespace `ModAdjusterV2`); things I could not verify are marked.

## 1. What MA2 does (from its code)

* MA2 is a session component. In `LoadData` it goes through **every mod of the world**, in load order, and looks for `Data/ModAdjuster/ModAdjusterFiles.txt` (one file name per line). Each listed file `Data/ModAdjuster/<name>` is an XML `<Definitions>` document.
* The document can contain `VoxelMaterials`, `PlanetGeneratorDefinitions`, `Blueprints`, `BlueprintClasses`, `BlueprintClassEntries`, `PhysicalItems`, `Components`, `CubeBlocks` and more. For each entry MA2 **looks up the existing definition with the same `Id` and patches it**; fields that are left out stay as they are. If the id does not exist it logs "Failed to find definition" and does nothing.
* So MA2 **cannot create** definitions (new ore items, new blueprints, new materials go in normal `.sbc` files of the mod; they do not override anything) and **cannot change textures** (the texture fields of the voxel material adjuster are commented out in its code).
* **Voxel materials** that can be patched: `MinedOre`, `MinedOreRatio`, `IsRare`, `CanBeHarvested`, `MaterialTypeName`, `MinVersion`, `MaxVersion`, `SpawnsInAsteroids`, `SpawnsFromMeteorites`, `AsteroidGeneratorSpawnProbabilityMultiplier`, `DamagedMaterial`, friction and a few more. **Planets**: `OreMappings` (the **whole list is replaced** when given), `FolderName` (the folder with the planet maps), `PlanetMaps`, `HillParams` and the rest of the planet definition. **Blueprints**: `Prerequisites` and `Results` (replaced as lists), production time. **Physical items**: `DisplayName`, `Description`, `Icon`, mass, volume, size and more.
* Patching a blueprint with a prerequisite it did not have makes MA2 add that item to the input constraint of the production blocks that use the blueprint (`AdjustInputConstraints`).
* **Danger:** `BlueprintClassEntries` **clears** the class the first time the mod mentions it (`ClearBlueprints`) and then adds only what is listed. Do not use it for refinery classes; add new blueprints to the `Ingots` class the normal way (a `BlueprintClassEntries` block in a plain `.sbc`, as Better Stone does).
* Because MA2 runs after all definitions are loaded, **the last mod in the load order wins** if two mods patch the same field. MA2 writes `ModAdjuster0.log` ... `ModAdjuster4.log` into the mod's local storage (the world's Storage folder); it says which definitions were loaded or not found.

## 2. Mod layout

```
BetterStoneResources/            (the pack mod; requires Better Stone, Better Resources, Mod Adjuster 2; load order: after all three)
  Data/
    ModAdjuster/
      ModAdjusterFiles.txt         VoxelMaterials.xml, PhysicalItems.xml, Blueprints.xml, Planets.xml
      VoxelMaterials.xml           section 3
      PhysicalItems.xml            section 4
      Blueprints.xml               section 5
      Planets.xml                  section 6
    PhysicalItems_BSBR.sbc         4 new ore items (section 7)
    Blueprints_BSBR.sbc            4 new refining recipes + their class entries (section 7)
    PlanetDataFiles/<Planet>_BSBR/ *_mat.png and the height maps (section 6)
  Textures/GUI/Icons/              icons for the 4 new ore items
```

## 3. VoxelMaterials.xml: ore, asteroid spawn and weight

`MinedOre` is the **subtype of the ore item** the material gives. The 44 ore materials of the pack (no new material is created). `SpawnsInAsteroids` is false for planet-only ores; the weight is the asteroid weight of `MODPACK_DISTRIBUTION.md` section 4; `IsRare` stays **true** for all of them (the game's deposit query, the ore detector, uses it; checked in the game code: `MyDepositQuery.ProcessCell`).

| Material id | Mineral | MinedOre item | Item from | Asteroids | Weight |
|---|---|---|---|---|---|
| `Iron_02` | Magnetite (Dense Iron) | `[CM] Dense Iron (Fe+)` | Better Stone | yes | 4 |
| `Iron_01` | Hematite (Iron) | `[CM] Iron (Fe)` | Better Stone | yes | 20 |
| `Hapkeite_01` | Hapkeite | `[S] Hapkeite (Fe,Si)` | Better Stone | yes | 4 |
| `Heazlewoodite_01` | Heazlewoodite | `[CM] Heazlewoodite (Ni)` | Better Stone | yes | 4 |
| `Porphyry_01` | Porphyry | `[PM] Porphyry (Au)` | Better Stone | no | - |
| `Dolomite_01` | Dolomite | `[S] Dolomite (Mg)` | Better Stone | yes | 8 |
| `Cattierite_01` | Cattierite | `[CM] Cattierite (Co)` | Better Stone | no | - |
| `Niggliite_01` | Niggliite | `[PM] Niggliite (Pt)` | Better Stone | yes | 2 |
| `Carnotite_01` | Carnotite | `[EI] Carnotite (U)` | Better Stone | no | - |
| `Glaucodot_01` | Glaucodot | `[CM] Glaucodot (Fe,Co)` | Better Stone | no | - |
| `Sinoite_01` | Sinoite | `[S] Sinoite (Si)` | Better Stone | yes | 6 |
| `Pyrite_01` | Pyrite | `[CM] Pyrite (Fe,Au)` | Better Stone | yes | 12 |
| `Olivine_01` | Olivine | `[S] Olivine (Si,Mg)` | Better Stone | yes | 16 |
| `Cooperite_01` | Cooperite | `[PM] Cooperite (Ni,Pt)` | Better Stone | yes | 3 |
| `Quartz_01` | Quartz | `[S] Quartz (Si)` | Better Stone | yes | 1 |
| `Galena_01` | Galena | `[PM] Galena (Ag)` | Better Stone | no | - |
| `Chlorargyrite_01` | Chlorargyrite | `[PM] Chlorargyrite (Ag)` | Better Stone | no | - |
| `Electrum_01` | Electrum | `[PM] Electrum (Au,Ag)` | Better Stone | no | - |
| `Sperrylite_01` | Sperrylite | `[PM] Sperrylite (Pt)` | Better Stone | yes | 2 |
| `Autunite_01` | Autunite | `[EI] Autunite (U)` | Better Stone | no | - |
| `Akimotoite_01` | Akimotoite | `[S] Akimotoite (Si,Mg)` | Better Stone | yes | 4 |
| `Wadsleyite_01` | Wadsleyite | `[S] Wadsleyite (Si,Mg)` | Better Stone | yes | 4 |
| `Taenite_01` | Taenite | `[CM] Taenite (Fe,Ni)` | Better Stone | yes | 14 |
| `Cohenite_01` | Cohenite | `[CM] Cohenite (Ni,Co)` | Better Stone | yes | 6 |
| `Kamacite_01` | Kamacite | `[CM] Kamacite (Fe,Ni,Co)` | Better Stone | yes | 16 |
| `Uraniaurite_01` | Uraniaurite | `[EI] Uraniaurite (U,Au)` | Better Stone | no | - |
| `Petzite_01` | Petzite | `[PM] Petzite (Ag,Au)` | Better Stone | no | - |
| `Gold_01` | Gold (native) | `Gold` | vanilla | yes | 2 |
| `Silver_01` | Silver (native) | `Silver` | vanilla | yes | 2 |
| `Platinum_01` | Platinum (native) | `Platinum` | vanilla | yes | 2 |
| `Uraninite_01` | Uraninite | `Uranium` | vanilla | yes | 2 |
| `Ice_02` | Ice (asteroids) | Ice | vanilla | yes | 24 |
| `Ice_01` | Ice (planets) | Ice | vanilla | no (vanilla) | - |
| `Cobalt_01` | Azurite | `Azurite` | new | no | - |
| `Nickel_01` | Sphalerite | `Sphalerite` | new | no | - |
| `Magnesium_01` | Ilmenite | `Ilmenite` | new | no | - |
| `Silicon_01` | Spodumene | `Spodumene` | new | no | - |
| `Aluminium_01` | Bauxite | `Bauxite` | Better Resources | no | - |
| `Copper_01` | Native Copper | `Copper` | Better Resources | no | - |
| `Titanium_01` | Rutile | `Titanium` | Better Resources | no | - |
| `Lithium_01` | Lepidolite | `Lithium` | Better Resources | no | - |
| `Wolframite_01` | Wolframite | `Wolframite` | Better Resources | no | - |
| `Nitre_01` | Niter | `Nitre` | Better Resources | no | - |
| `Sulfur_01` | Sulfur (native) | `Sulfur` | Better Resources | no | - |
| `Petroleum_01` | Oil shale (Petroleum) | `Shale` | Better Resources | no | - |

The rest of the Better Stone and vanilla materials (stone, snow...) are not touched. `Galena_01` is defined by both mods: the patch sets `MinedOre` to Better Stone's Galena item, so whichever definition wins the load order, it mines the same ore. Iron_01 and Iron_02 get Better Stone's two iron items.

Example (`VoxelMaterials.xml`):

```xml
<Definitions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">
  <VoxelMaterials>
    <VoxelMaterial>
      <Id><TypeId>VoxelMaterialDefinition</TypeId><SubtypeId>Cobalt_01</SubtypeId></Id>
      <MinedOre>Azurite</MinedOre>
      <MinedOreRatio>5</MinedOreRatio>
      <IsRare>true</IsRare>
      <SpawnsInAsteroids>false</SpawnsInAsteroids>
    </VoxelMaterial>
    <VoxelMaterial>
      <Id><TypeId>VoxelMaterialDefinition</TypeId><SubtypeId>Iron_01</SubtypeId></Id>
      <AsteroidGeneratorSpawnProbabilityMultiplier>20</AsteroidGeneratorSpawnProbabilityMultiplier>
    </VoxelMaterial>
  </VoxelMaterials>
</Definitions>
```

(The type element `TypeId` for voxel materials is `VoxelMaterialDefinition`, as in the game's own file; MA2 strips `MyObjectBuilder_` from `xsi:type` lines itself. The exact element form MA2 expects for each section is checked by its log on the first test.)

## 4. PhysicalItems.xml: names of the minerals

Only `DisplayName` (and `Description`) change; the item ids stay (Better Stone's item ids contain the old formula, e.g. `[CM] Iron (Fe)`, and the recipes refer to them).

| Item id (Type Ore) | New display name |
|---|---|
| `[CM] Iron (Fe)` | Hematite (Fe, Ti) |
| `[CM] Dense Iron (Fe+)` | Magnetite (Fe, Ti) |
| `[CM] Heazlewoodite (Ni)` | Heazlewoodite (Ni, S) |
| `[PM] Porphyry (Au)` | Porphyry (Au, Cu, Ag) |
| `[S] Dolomite (Mg)` | Dolomite (Mg, C) |
| `[CM] Cattierite (Co)` | Cattierite (Co, S) |
| `[PM] Niggliite (Pt)` | Niggliite (Pt, Sn) |
| `[EI] Carnotite (U)` | Carnotite (U, K) |
| `[CM] Glaucodot (Fe,Co)` | Glaucodot (Fe, Co, S) |
| `[S] Sinoite (Si)` | Sinoite (Si, N) |
| `[CM] Pyrite (Fe,Au)` | Pyrite (Fe, S) |
| `[S] Olivine (Si,Mg)` | Olivine (Si, Mg, Fe) |
| `[PM] Cooperite (Ni,Pt)` | Cooperite (Ni, Pt, S) |
| `[PM] Galena (Ag)` | Galena (Pb, Ag, Zn) |
| `[PM] Chlorargyrite (Ag)` | Chlorargyrite (Ag, Cl) |
| `[S] Akimotoite (Si,Mg)` | Akimotoite (Si, Mg, Fe) |
| `[S] Wadsleyite (Si,Mg)` | Wadsleyite (Si, Mg, Fe) |
| `[CM] Cohenite (Ni,Co)` | Cohenite (Fe, Ni, Co, C) |
| `Titanium (Better Resources)` | Rutile (Ti, Nb) |
| `Lithium (Better Resources)` | Lepidolite (Li, Al, K, Nb) |
| `Copper (Better Resources)` | Native Copper (Cu, Ag) |
| `Nitre (Better Resources)` | Niter (K, N) |
| `Shale (Better Resources)` | Oil Shale |
| `Bauxite (Better Resources)` | Bauxite (Al, Ti, Ga) |

## 5. Blueprints.xml: the added ingots

Each patch **replaces `Results`**, so the old results are repeated. Amounts per 1 ore; Ice stays an `Ore` result as in Better Stone. Starting values (to tune).

| Blueprint | Results after the patch |
|---|---|
| `BS_IcyIronOreToIngot` | 0.4 Iron, 0.02 Titanium, 0.5 Ice |
| `BS_DenseIronOreToIngot` | 0.9 Iron, 0.03 Titanium |
| `HeazlewooditeOreToIngot` | 0.4 Nickel, 0.15 Sulfur, 0.15 Ice |
| `PorphyryOreToIngot` | 0.006 Gold, 0.10 Copper, 0.005 Silver |
| `DolomiteOreToIngot` | 0.007 Magnesium, 0.10 Carbon |
| `CattieriteOreToIngot` | 0.27 Cobalt, 0.15 Sulfur |
| `NiggliiteOreToIngot` | 0.003 Platinum, 0.10 Tin |
| `CarnotiteOreToIngot` | 0.001 Uranium, 0.05 Nitre, 0.572 Ice |
| `GlaucodotOreToIngot` | 0.234 Iron, 0.4 Cobalt, 0.15 Sulfur |
| `SinoiteOreToIngot` | 0.62 Silicon, 0.15 Nitrogen, 0.44 Ice |
| `PyriteOreToIngot` | 0.55 Iron, 0.35 Sulfur |
| `OlivineOreToIngot` | 0.18 Silicon, 0.008 Magnesium, 0.10 Iron, 0.35 Ice |
| `CooperiteOreToIngot` | 0.36 Nickel, 0.002 Platinum, 0.15 Sulfur |
| `GalenaOreToIngot (Better Stone)` | 0.3 Lead, 0.1 Silver, 0.1 Zinc |
| `ChlorargyriteOreToIngot` | 0.064 Silver, 0.03 Perchlorate, 0.002 Ice |
| `AkimotoiteOreToIngot` | 0.22 Silicon, 0.005 Magnesium, 0.10 Iron |
| `WadsleyiteOreToIngot` | 0.30 Silicon, 0.003 Magnesium, 0.10 Iron |
| `CoheniteOreToIngot` | 0.21 Nickel, 0.18 Cobalt, 0.15 Iron, 0.08 Carbon |
| `TitaniumOreToIngot (Better Resources)` | 0.35 Titanium, 0.01 Niobium |
| `LithiumOreToIngot (Better Resources)` | 0.15 Lithium, 0.10 Aluminium, 0.05 Nitre, 0.05 Niobium |
| `AluminiumOreToIngot (Better Resources)` | 0.4 Aluminium, 0.05 Gallium, 0.03 Titanium |
| `CopperOreToIngot (Better Resources)` | 0.5 Copper, 0.005 Silver |
| `WolframiteOreToIngot (Better Resources)` | 0.3 Tungsten, 0.2 Tin, 0.10 Iron, 0.01 Niobium |
| `NitreOreToIngot (Better Resources)` | 0.5 Nitre, 0.10 Nitrogen, 0.02 Perchlorate |

Unchanged: Hapkeite, Quartz, Electrum, Sperrylite, Autunite, Taenite, Kamacite, Uraniaurite, Petzite (Better Stone) and `SulfurOreToIngot` (Better Resources). Oil shale already has `ShaleToCarbon` and `ShaleToOil` (Oil, Sulfur, Nitrogen) in Better Resources, so the "+C +S" of `MODPACK_COMPOSITES.md` is not needed. The galena recipe of Better Resources (`GalenaOreToIngot` for its own `Galena` item) is left alone; the item is simply not mined any more.

**Open:** Better Resources overrides two vanilla recipes (Magnesium ore 0.4 Mg, Uranium ore 0.1 U + 0.01 Pu); `Uraninite_01` mines `Uranium`, so decide whether `UraniumOreToIngot` keeps 0.1 U + 0.01 Pu (Better Resources) or goes back to 0.01 U (vanilla and the Better Stone balance). I would patch it to 0.01 U to keep the uranium rate of section 8 of `MODPACK_DISTRIBUTION.md`. Better Resources' blueprint ids for the ores above are the ones its recipes use; their exact subtypes are checked against its `Blueprints.sbc` when the file is written.

## 6. Planets.xml: the ore maps of the eight planets

For each of EarthLike, Alien, Mars, Moon, Europa, Titan, Triton, Pertam: one `PlanetGeneratorDefinition` patch with the new `OreMappings` (the list of the `Ore` elements the tool writes into `oremappings.xml`) and a `FolderName` that points to the planet's maps in the mod.

```xml
<PlanetGeneratorDefinitions>
  <PlanetGeneratorDefinition>
    <Id><TypeId>PlanetGeneratorDefinition</TypeId><SubtypeId>EarthLike</SubtypeId></Id>
    <FolderName>EarthLike_BSBR</FolderName>
    <OreMappings>
      <Ore Value="1" Type="Quartz_01" Start="3" Depth="3" ColorInfluence="15" TargetColor="#616c83" />
      ... (the 254 mappings of the tool)
    </OreMappings>
  </PlanetGeneratorDefinition>
</PlanetGeneratorDefinitions>
```

**Maps.** The tool writes six `*_mat.png` per planet (red and green copied from the vanilla map, blue = ore). A planet reads all its maps from one folder, so `FolderName` needs the six height maps too: copies of the vanilla `up.png`...`right.png` (the vanilla `*_mat.png` of a planet are 25-60 MB, the tool's output about 1 MB each). Sizes of the height maps per planet: EarthLike 31 MB, Alien 31, Mars 25, Moon 34, Europa 27, Titan 26, Triton 17, Pertam 33 = **about 224 MB** for the eight, plus about 50 MB of ore maps. Alternative: put only the six `*_mat.png` of a planet at the vanilla path `Data/PlanetDataFiles/<Planet>/` (an old-style file override, about 6 MB per planet, but another mod that does the same for that planet conflicts). My recommendation: the `FolderName` route, or ship only the planets you really use.

**Not verified:** that setting `FolderName` in MA2's `LoadData` is early enough for the planet generator to read the new folder (the planet maps are read when a planet is created, which is after `LoadData`, so it should be). The first test should load a new world and check the log and the ore under the surface.

Per-planet ore lists and layer budgets (from the planet profiles of `bs_br_merged.xml`):

| Planet | Ores | Values (max 254) | Largest shares |
|---|---|---|---|
| EarthLike | 33 | 254 | Olivine_01 16.6 %, Quartz_01 10.6 %, Heazlewoodite_01 9.3 % |
| Alien | 37 | 254 | Olivine_01 16.4 %, Quartz_01 9.5 %, Heazlewoodite_01 9.5 % |
| Mars | 25 | 243 | Quartz_01 17.2 %, Olivine_01 12.2 %, Sulfur_01 10.7 % |
| Moon | 22 | 213 | Hapkeite_01 11.8 %, Magnesium_01 9.6 %, Olivine_01 9.3 % |
| Europa | 19 | 183 | Heazlewoodite_01 17.7 %, Dolomite_01 16.0 %, Quartz_01 11.1 % |
| Titan | 17 | 163 | Olivine_01 20.9 %, Heazlewoodite_01 15.2 %, Petroleum_01 15.1 % |
| Pertam | 30 | 254 | Quartz_01 21.4 %, Iron_02 20.2 %, Heazlewoodite_01 17.8 % |
| Triton | 16 | 160 | Quartz_01 23.1 %, Dolomite_01 15.8 %, Heazlewoodite_01 14.5 % |

Build steps per planet in the tool: open the planet definition, *Redistribute Ore*, *Data packs...*: tick all four packs, **Planet profile** = the planet, **Apply profile** (replaces the ore list, sets weights and layers, rock types, regions and vein sizes), choose *Geology* placement and the planet diameter, Confirm; copy the six `*_mat.png` and the `oremappings.xml` content into the mod.

## 7. New content in plain `.sbc` files (not overrides)

Four new ore items, four new refining recipes and their entries in the `Ingots` class; no new voxel material and no override of an existing definition:

| Ore item | Recipe (blueprint id) | Results |
|---|---|---|
| `Azurite` (Ore) | `AzuriteOreToIngot` | 0.35 Copper, 0.10 Carbon |
| `Sphalerite` (Ore) | `SphaleriteOreToIngot` | 0.30 Zinc, 0.15 Sulfur, 0.02 Gallium |
| `Ilmenite` (Ore) | `IlmeniteOreToIngot` | 0.30 Iron, 0.25 Titanium |
| `Spodumene` (Ore) | `SpodumeneOreToIngot` | 0.12 Lithium, 0.20 Aluminium, 0.30 Silicon |

Icons: reuse the game's ore icons (e.g. the copper, iron, silicon icon files of the base game) or ship four small icons; a `<BlueprintClassEntries><Entry Class="Ingots" BlueprintSubtypeId="..."/></BlueprintClassEntries>` block adds the recipes. Because the entries are in a normal `.sbc`, MA2 is not involved and no class is cleared.

## 8. Order, dependencies and risks

1. **Load order:** Better Stone, Better Resources, Mod Adjuster 2, then this mod, and the planets mods after it if they should not be touched. Do not load *Better Stone - Rebalanced Asteroids* (3511204395; it sets the asteroid weights of Better Stone's materials that this pack patches itself, and its Steam page shows a removal notice).
2. **Vanilla slots renamed** (`Cobalt_01`, `Nickel_01`, `Magnesium_01`, `Silicon_01`) change in every planet that uses them, also other mods' planets in the same save (a Typhoon or Teal map that places `Cobalt_01` would then place azurite). Only use the pack with the eight planets it covers, or patch those other planets' ore mappings too.
3. **Existing saves:** ore already generated stays, voxel materials are stored by id, so existing voxel storage shows the new name for the same material, and refined recipes change at once. New planets or untouched areas use the new maps; use a new world for a clean test.
4. **Asteroids** pick their ore at world creation from the weights of section 3; existing asteroids keep their ore.
5. **Ore detector and drills:** `IsRare` true on all 44 ore materials; ore items need an entry in the refinery input constraint (the `Ingots` class does it for the new ones).
6. **Better Stone version:** the item ids and blueprint ids above are those of Better Stone v7.1.0 (the companion mod says it requires that version); a later version may rename them, which MA2 reports as "Failed to find definition".
7. **Check after the first start:** open the MA2 log in the world's Storage folder: every definition of sections 3-6 must say "Loaded definition"; any "Failed" line is a wrong id.

## 9. What the tool could still do for this

* Write the `PlanetGeneratorDefinition` patch (id, `FolderName`, `OreMappings`) directly next to `oremappings.xml`, ready for `Planets.xml`.
* Write the `VoxelMaterials.xml` asteroid weights from the pack data (add `AsteroidWeight` to the packs).
