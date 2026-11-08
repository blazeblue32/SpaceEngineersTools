# Ore Redistribution: how it works, the ore count limits, and a Better Stone + Better Resources mod pack

Written 2026-11-09 for the Linux port (`OreRedistLinux`). Everything about the two mods comes from reading their files in the Steam workshop folder
(Better Stone 406244471, Better Resources 2841960221) and the game's own `VoxelMaterials_*.sbc`; nothing here was tested in the game.

## 1. Short version

* The tool writes **one ore number per (ore, depth layer)** into the blue channel of each planet's `*_mat.png`. A byte holds 0-255; the tool uses 1-254 for ores and fills
  empty ground with 255 (like vanilla EarthLike). So **one planet can have at most 254 enabled (ore, layer) pairs.** This is the limit that decides how many ores you can have.
* Each ore has 10 depth layers by default (370 numbers for 37 ores), so a pack of 30+ ores has to **switch layers off**: 37 ores fit if they average **6.9 layers** or less.
  A budget that fits is in section 5 (209 numbers for 37 ores, 45 spare).
* The limit is **per planet**. A different planet can use a different ore list, with its own 254.
* Separate from the 254 is the **voxel material count** (every ore type is a voxel material). Vanilla has 62, Better Stone adds 25 new ones, Better Resources 8 new ones: **95** before any
  planet mod. That is under the 128 of the shader limit, but planet mods add their own; your Voxel Material Limit plugin covers that.
* **Correction to the assumption about Better Stone:** it does **not** remove the vanilla ores from its planets. Only **Iron** is replaced (it redefines the materials `Iron_01` and `Iron_02`).
  Nickel, Cobalt, Magnesium, Silicon, Silver, Gold (and Platinum on Moon, Europa, Titan and the test map) and Uraninite (vanilla uranium) are still placed on every Better Stone planet, with 1-3 layers each
  (details in section 4). For your pack the vanilla ores simply stay out of the ore list; their materials remain installed but unused.
* One **conflict**: both mods define the voxel material `Galena_01` (Better Stone: silver ore "Galena (Ag)", Better Resources: lead ore "Galena"). Only one definition can be active, the one from the mod that loads later.

## 2. How the tool works

**Input.** A planet definition (`.sbc`) and the planet's maps (`up/down/front/back/left/right` height maps and `*_mat.png`). The red channel of `_mat.png` is the biome / complex material,
green is environment items, blue is **ore**. The tool never changes red or green.

**Ore values.** `FinalizeList` numbers every enabled layer of every ore in list order, starting at 1. Rules: a layer can be switched off by its checkbox; layer 10 ("very deep ore")
only counts if the ore has *Deep* ticked; *Depth min / max* cut the layers an ore may use. The number of values is shown ("Values Count (max: 254)"), and Confirm is disabled above 254.
Value 0 is not used and 255 is the "no ore" fill. (Planets such as Teal leave empty ground at 0 instead; the game treats both as no ore because no mapping uses them.)

**Depth layers.** Layer 1-3 are surface ore (3-15 m), layer 4-6 medium veins (40-102 m), layer 7-9 deep veins (140-372 m), layer 10 very deep (370-450 m). Start and Depth are absolute
meters below the terrain surface, they do not scale with the planet. Ore is only detectable from the surface down to the detector range (50 / 150 m).

**Placement.** For every cube face the tool walks over the pixels. A pixel starts an ore *event* with a probability of one in *N* (the spawn distance, tied to planet size, about one per 1.3 km at
the default). An event picks an ore by its **Weight**, a depth layer (preferred depth or random), then draws a **vein**: a random walk of *Size* pixels (scaled with the planet size so a vein
covers the same ground on every planet), written with the layer's number. The **share of an ore is Weight x Size** (the Ratio column). The result per cube face is deterministic for a seed.

**Optional layers on top, all off by default so the output equals the original tool:**

| Feature | What it does |
|---|---|
| Water rules (Water Mod) | keeps ore layers out of the water, per ore: land only / land + seabed / seabed only / anywhere; keeps ore ratios and density with a reference pass, diversion to the seabed and a top-up pass |
| Globbing | per-ore strength (Glob %), region size (Glob km), coverage (Cov %): ores clump into regions |
| Geology | simulated rock types and regions (land, seabed, uplift, volcanic painted by hand), per-ore rock weights; a vein is placed where its rock suits, and regions (globs) follow the rock |
| Data packs | XML files that fill in rock weights, abundance-based weights, region values and vein sizes by ore name |
| Auto patch scale | vein pixels and spawn distance follow planet radius and tile width |

**Output.** Six `*_mat.png`, `oremappings.xml` (the `<Ore .../>` list to paste into the planet definition's `OreMappings`) and `placement_report.txt`. You copy the pngs into the planet's data folder and
replace the `OreMappings` of the definition. The tool does not edit the game or the mod.

## 3. The limits that matter for the ore count

1. **254 values per planet map.** The ore channel is one byte. 255 is reserved as empty. This is a hard limit of the map format, not of the tool. Each enabled layer of each ore costs one.
2. **Layers per ore.** Default 10. Better Stone itself uses only **1-3 mappings per ore** (EarthLike: 51 mappings for 26 ore types). A pack with many ores must use few layers per ore.
3. **Voxel materials.** Every ore is a voxel material. Planets, asteroids and mods all share one table. The game's C# side allows 256, the renderer's shader has 128 (what your Voxel Material Limit
   plugin lifts). Counting the materials of the game files: vanilla 62 (43 planetary + 19 asteroid), Better Stone +25, Better Resources +8 (Galena is shared), so 95 before the planet mods.
4. **Per planet.** Nothing stops different planets using different ores: the oil can be on one planet only. Each planet needs its own `_mat.png` set and `OreMappings`.
5. **Names.** The ore name in the tool must equal the voxel material subtype id (`Quartz_01`, not "Quartz"). The data packs match by base name (`Quartz` matches `Quartz_01`).
6. **Density.** More ores means less ore of each. At the default (a 120 km planet) there are about 25,000 events per planet; with 37 ores that is about 680 per ore on average, a few dozen for the
   rarest. Rarity weights are applied by pixel share, not by event count, so a rare ore with big veins gets few events.
7. **Asteroids are not touched.** Asteroid ore comes from each material's own settings (`SpawnsInAsteroids`, `IsRare`...), not from the ore map. I saw `SpawnsInAsteroids` set in Better Stone's materials; I did not check Better Resources'.

## 4. What the two mods contain

**Better Stone (406244471).** `Mod-VoxelMaterials_asteroids.sbc` defines 27 materials, of which `Iron_01` and `Iron_02` replace the vanilla ones ("Iron (Fe)", "Dense Iron (Fe+)", ratio 5).
The other 25 are new minerals; their MinedOre names carry the category (`[CM]` common metals, `[PM]` precious metals, `[S]` silicates, `[EI]` energy isotopes). Planet definitions are in
`Override-PlanetGeneratorDefinitions.sbc` (11 planets):

| Planet | Mappings | Ore types | of which vanilla-named material types |
|---|---|---|---|
| Alien | 48 | 30 | Cobalt, Gold, Iron, Magnesium, Nickel, Silicon, Silver, Uraninite |
| EarthLike | 51 | 26 | Cobalt, Gold, Iron, Magnesium, Nickel, Silicon, Silver |
| EarthLikeTutorial | 51 | 30 | same + Uraninite |
| Mars, MarsTutorial | 52 | 31 | same + Uraninite + Ice |
| Moon | 57 | 33 | same + Platinum + Uraninite |
| Europa | 54 | 33 | same + Platinum + Uraninite |
| Titan | 105 | 33 | same + Platinum (several layers) |
| MoonTutorial | 109 | 33 | same |
| SystemTestMap | 58 | 34 | same + Platinum + Ice |
| EarthLikeModExample | 11 | 8 | only vanilla-named ores |

So the vanilla ores (apart from Iron) are still placed, 2-3 layers each, next to 19-24 Better Stone minerals. Layers are shallow: Start 3-112 m, Depth 5-55 m on EarthLike. Better Stone does not use
`Sperrylite_01` on any planet (the material exists). `Override-PhysicalItems.sbc` re-declares the vanilla ore items (Iron ... Uranium); the mod also has blueprint files (`Mod-Blueprints.sbc`, `IOCompat-Blueprints_CrushOres.sbc`) that I did not read in detail.

**Better Resources (2841960221).** `VoxelMaterials_asteroids.sbc` defines 9 materials: `Aluminium_01` (Bauxite), `Copper_01`, `Galena_01` (lead), `Lithium_01`, `Nitre_01`,
`Petroleum_01` (Shale), `Sulfur_01` (not used on its planets), `Titanium_01`, `Wolframite_01`. `PlanetGeneratorDefinitions.sbc` redefines 6 planets (EarthLike, Alien, Mars, Moon, Europa, Titan)
with **16 ore types and exactly one mapping per ore** (16 values per planet), the 9 new ones plus Cobalt, Gold, Ice_01, Iron_02, Magnesium, Nickel, Silicon, Silver. It changes no vanilla material.

**Overlap between the two:** `Galena_01` (both define it), `Iron_02` (Better Stone redefines it, Better Resources just uses it). Everything else is separate.

## 5. A pack of both mods: ore list and layer budget

Idea: Better Stone minerals replace the vanilla ores; Better Resources ores that are not minerals stay as they are (oil, nitre, sulfur, bauxite...).

| Source | Ores | Count |
|---|---|---|
| Better Stone (replaces vanilla) | Akimotoite, Autunite, Carnotite, Cattierite, Chlorargyrite, Cohenite, Cooperite, Dolomite, Electrum, Galena, Glaucodot, Hapkeite, Heazlewoodite, Kamacite, Niggliite, Olivine, Petzite, Porphyry, Pyrite, Quartz, Sinoite, Taenite, Uraniaurite, Wadsleyite, Sperrylite, plus Iron_01 and Iron_02 | 27 |
| Better Resources (own ores) | Aluminium, Copper, Lithium, Nitre, Petroleum, Sulfur, Titanium, Wolframite | 8 |
| Vanilla kept | Uraninite_01 (the only uranium besides the new uranium minerals; leave it out if you want only the Better Stone ones), Ice | 1-2 |

That is **36-37 ores**. 254 / 37 = 6.9 layers on average. A budget that fits (rows are the number of enabled layers):

| Class | Ores | Layers | Sum |
|---|---|---|---|
| Bulk, all depths | Iron_02, Iron_01, Quartz, Olivine, Dolomite, Pyrite, Aluminium, Titanium, Petroleum (9) | 8 | 72 |
| Medium | Copper, Galena, Nitre, Lithium, Wolframite, Sulfur, Kamacite, Taenite, Cohenite, Heazlewoodite, Cattierite, Porphyry, Uraninite (13) | 6 | 78 |
| Rare | Akimotoite, Wadsleyite, Hapkeite, Sinoite, Cooperite, Niggliite, Sperrylite, Uraniaurite, Electrum, Petzite, Chlorargyrite, Glaucodot, Autunite, Carnotite (14) | 4 | 56 |
| Ice | Ice | 3 (surface) | 3 |
| **Total** | **37 ores** | | **209 of 254** |

45 numbers are spare: give them to the ores you want to appear at more depths, or to a second Iron class. Switch layers off with the checkboxes in the ore list (or Depth min / max). Which layers to keep: for a
shallow mining game keep layers 1-6 (surface and medium); the deep layers 7-10 matter only for ores meant to be found with deep drilling.

If you also want Better Stone's own style (2 mappings per ore), 37 ores would need only about 75 numbers; there is room for 100+ ores in principle, the practical limit is then the voxel material count.

## 6. Suggested workflow in the tool

1. Open the planet definition (or any definition that has the planet), then **Redistribute Ore**.
2. In the ore list use **Add** for each ore and type the voxel material name exactly (`Quartz_01`...). The list is not limited to what the definition already contains.
3. **Data packs...**: tick Vanilla, Better Resources, Better Stone (order: later wins, Better Stone is last, so its `Galena` and `Uraninite` values are used). Tick all options and Apply: rock types, regions,
   vein sizes and weights from abundance (strength 0.25 by default). Check the Ratio column.
4. Set the layer checkboxes to the budget above; watch "Values Count" stay at 254 or less.
5. Choose the Ore placement mode (Geology uses the rock weights and regions), set the planet diameter, Confirm. Check `placement_report.txt`.
6. Copy the six `_mat.png` and paste `oremappings.xml` into the `OreMappings` of your planet override. Do this once per planet that has its own ore set.

## 7. Open points to decide before building the pack

* **Galena_01:** pick which mod's definition you want (Better Stone: silver; Better Resources: lead). If you keep both mods enabled, the later one in the load order wins. A cleaner way is to give one of
  them a new id in your pack.
* **Iron:** Better Stone's Iron_01 ("Iron") and Iron_02 ("Dense Iron") are both materials of the pack; decide whether Iron_01 should be on planets (Better Stone uses only Iron_02 on its planets).
* **Uranium:** Better Stone keeps vanilla `Uraninite_01` next to Autunite, Carnotite and Uraniaurite. Leave it out of your list if you want the new minerals only.
* **Refining:** both mods have their own blueprint files (Better Stone `Mod-Blueprints.sbc`, Better Resources `Blueprints.sbc`), presumably the ore-to-ingot recipes; the tool does not touch them. Check that every ore in the pack can be refined, in particular Sperrylite
  (defined by Better Stone but not on its planets) and Sulfur (defined by Better Resources but not on its planets).
* **Vanilla materials stay loaded.** The unused vanilla ores still take a voxel material slot each.

## 8. Tool changes made while writing this

* The Vanilla data pack now also has an `Uraninite` entry: the vanilla uranium voxel material is `Uraninite_01`, so the old `Uranium` entry never matched a planet's ore name.
* The Better Stone pack has `Sperrylite`, the Better Resources pack `Sulfur`, so all 36 ore types of both mods are covered (25 + 9 new, 2 shared).
* A built-in pack that already exists in `~/.config/OreRedistLinux/DataPacks/` is never overwritten; delete a pack file to get the new version.
