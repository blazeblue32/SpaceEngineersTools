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

* **Galena_01 (decided):** real galena is PbS and usually carries silver, so the two mods' Galena are one ore. Keep Better Stone's `Galena_01` material (drop Better Resources' definition and its
  `Galena` ore item from the pack) and give it Better Resources' refining recipe: **1 ore -> 0.3 Lead + 0.1 Silver + 0.1 Zinc** (Better Stone's own recipe gives only 0.035 Silver). This needs the Lead and Zinc
  ingots from Better Resources, which the pack has anyway.
* **Better Resources also overrides two vanilla recipes**: Magnesium ore -> 0.4 Magnesium (vanilla 0.007) and Uranium ore -> 0.1 Uranium + 0.01 Plutonium (vanilla 0.01). Decide which of them applies in the pack (they only matter for the pure vanilla ore items).
* **Iron:** Better Stone's Iron_01 ("Iron") and Iron_02 ("Dense Iron") are both materials of the pack; decide whether Iron_01 should be on planets (Better Stone uses only Iron_02 on its planets).
* **Uranium:** Better Stone keeps vanilla `Uraninite_01` next to Autunite, Carnotite and Uraniaurite. Leave it out of your list if you want the new minerals only.
* **Refining:** both mods have their own blueprint files (Better Stone `Mod-Blueprints.sbc`, Better Resources `Blueprints.sbc`), presumably the ore-to-ingot recipes; the tool does not touch them. Check that every ore in the pack can be refined, in particular Sperrylite
  (defined by Better Stone but not on its planets) and Sulfur (defined by Better Resources but not on its planets).
* **Vanilla materials stay loaded.** The unused vanilla ores still take a voxel material slot each.

## 9. Native forms: which ores are native on Earth, which only elsewhere

"Native" = the element itself (or a natural metal alloy), not a compound. Earth sources are from general geology; the extraterrestrial ones from meteorites, lunar samples and probe data.
This is what decides which body gets which ore (each planet has its own 254, so a body's set can also be smaller and use more layers).

| Ore | Native on Earth? | Extraterrestrial | Put on |
|---|---|---|---|
| Iron | very rare (Disko Island basalt); compounds: hematite, magnetite, banded iron formations | abundant as Fe-Ni metal: kamacite and taenite in iron meteorites and metal asteroids; metal grains in lunar soil | Earth-likes: Iron_01/02 (oxides). Moon, asteroids, Mars scatter: Kamacite, Taenite, Cohenite |
| Nickel | essentially none (awaruite Ni3Fe in serpentinite); sulfides (Heazlewoodite, pentlandite), laterites | in Fe-Ni metal (kamacite 5-7 % Ni, taenite 25-50 %); asteroids | Earth-likes: Heazlewoodite, Cooperite; Moon, asteroids: Taenite, Kamacite, Cohenite |
| Cobalt | none | traces in meteoritic Fe-Ni (about 0.5 %) | Earth-likes: Cattierite, Glaucodot; others: only via Kamacite/Cohenite |
| Magnesium | none (too reactive); dolomite, magnesite, silicates | olivine and pyroxene everywhere (Moon, Mars, asteroids); Mg sulfates on Mars and Europa; carbonates in carbonaceous asteroids | Dolomite on Earth-likes, Mars (carbonates), C-type asteroids; Olivine, Akimotoite, Wadsleyite almost everywhere |
| Silicon | practically none (traces in fulgurites) | traces of metallic Si in lunar soil and enstatite chondrites; silicates everywhere | Quartz on Earth-likes; Olivine, Sinoite, Hapkeite, Wadsleyite on Moon, asteroids |
| Silver | yes, common native silver and electrum (Kongsberg, Ontario); Galena, Chlorargyrite | only traces (meteorites; silver detected in the LCROSS Cabeus plume) | Earth-likes (hydrothermal, volcanic); not on airless bodies except as a trace |
| Gold | yes, the main form (placers, veins, electrum) | ppb levels in iron meteorites, no nuggets known | Earth-likes only; elsewhere only as a trace in Pyrite, Uraniaurite (shock / impact) |
| Platinum | yes, native platinum and Pt-Fe alloys in ultramafic rocks and placers; Sperrylite, Cooperite | PGE alloy grains in chondrites, enriched in iron meteorite metal | Earth-likes (large intrusions); asteroids and impact-rich Moon: Niggliite, Cooperite, Sperrylite |
| Uranium | none; Uraninite (UO2), Autunite, Carnotite are compounds | traces (KREEP on the Moon, chondrites), never ore-grade | Earth-likes (granite, sandstone, unconformity); Moon only as a weak KREEP spot |
| Ice | glaciers, polar | everywhere: Moon poles, Mars poles, Europa shell, Titan bedrock, comets, C-type asteroids | all cold bodies; not hot dry Earth-likes except poles |

Better Resources ores and Better Stone's rare minerals:

| Ore | Native on Earth? | Extraterrestrial | Put on |
|---|---|---|---|
| Copper | yes, native copper (Keweenaw), also sulfides | trace native copper in lunar basalt | Earth-likes; Mars/Moon only a trace |
| Aluminium (bauxite) | native Al practically none; bauxite is a tropical weathering product | Al in anorthite (lunar highlands), no bauxite | bauxite on wet Earth-likes; on the Moon use an Al silicate source (the highlands) |
| Titanium | native none; ilmenite, rutile | ilmenite is the lunar mare titanium ore (up to about 15-20 %) | Moon (ilmenite), Earth-likes (beach sands, layered intrusions), Mars minor |
| Lithium | none; spodumene (pegmatite), brines | traces | Earth-likes only |
| Tungsten (Wolframite) | none; granite-related veins | ppm in meteoritic metal | Earth-likes only |
| Galena (Pb, Ag) | PbS, the main lead ore, argentiferous | traces | Earth-likes (hydrothermal); not airless bodies |
| Sulfur | yes, native sulfur (volcanic, salt domes) | troilite FeS in meteorites, sulfates on Mars, sulfur and SO2 on Io | Earth-likes (volcanic), Mars (sulfates), Europa-like (Io influence) |
| Nitre (nitrates) | evaporite, arid regions (Atacama) | nitrates found by Curiosity on Mars | dry Earth-likes, Mars |
| Petroleum | biogenic oil basins | none biogenic; **Titan**: liquid hydrocarbons, tholins; organics in C-type asteroids | Earth-likes (basins) and **Titan** (hydrocarbon lakes, sand), carbonaceous asteroids |
| Kamacite, Taenite, Cohenite, Hapkeite, Sinoite | Kamacite/Taenite/Cohenite only in meteorites and rare native iron | meteorites; Hapkeite found in lunar meteorites and soil | Moon, asteroids; on Earth-likes only in meteorite craters (tiny regions) |
| Akimotoite, Wadsleyite | not at the surface (mantle, shocked meteorites) | impact melt in meteorites | impact craters on any body, asteroids |
| Heazlewoodite, Glaucodot, Cattierite, Porphyry, Electrum, Petzite, Chlorargyrite | hydrothermal, serpentinite, porphyry, epithermal | not found | Earth-likes only (Mars: some hydrothermal) |
| Pyrite | common | in meteorites and Mars (iron sulfides) | everywhere with a crust |

**Suggested sets per body (ores, then the layer budget goes up as the set gets smaller):**

* **Earth-like / Alien:** all of the compound minerals and native Au, Ag, Cu, S, plus Iron_02/Iron_01, Aluminium, Titanium, Lithium, Wolframite, Nitre, Petroleum, Galena. About 30 ores.
* **Mars:** Iron_02 (oxide), Olivine, Dolomite, Pyrite, Quartz, Sulfur, Nitre, Magnesium minerals, a little Kamacite/Taenite, Uraninite (traces), Ice (poles), Titanium (minor). About 12-16 ores.
* **Moon:** Titanium (ilmenite), Olivine, Hapkeite, an aluminium source, Kamacite/Taenite/Cohenite, Cooperite/Niggliite (impact), Akimotoite/Wadsleyite (shock), Ice (poles), Uraninite as a weak KREEP spot. About 12-14 ores.
* **Europa / icy moons:** Ice, Dolomite or Mg salts, Sulfur, Olivine, Pyrite, a little metal. About 6-8 ores.
* **Titan:** Ice, Petroleum, Sulfur, Olivine, a little Kamacite. About 5-6 ores.
* **Asteroids** (not part of the map tool): Kamacite, Taenite, Cohenite, Hapkeite, Sinoite, Akimotoite, Wadsleyite, PGE minerals, Olivine, Ice (C-type), Petroleum organics.

## 10. Where Better Stone lacks a real mineral that Better Resources has

Better Stone has no copper, aluminium, titanium, lithium, tungsten, lead (apart from Galena), sulfur, nitrate or petroleum ore. Better Resources fills exactly these gaps, which is why its ores stay. A few
entries could be renamed to Better Stone's style (mineral names with the element in brackets) if you edit the materials: Copper -> native copper / chalcopyrite (Cu), Aluminium -> bauxite (Al), Titanium -> ilmenite (Fe,Ti)
or rutile (Ti), Lithium -> spodumene (Li), Wolframite (W, Sn), Nitre -> niter (N), Sulfur -> native sulfur (S). Petroleum stays (not a mineral). Where both mods overlap, use the Better Stone ore and
carry the better refining recipe over (Galena above). The only voxel-material overlap is `Galena_01`; `Iron_01`/`Iron_02` are Better Stone's.

## 11. Repurposing existing slots instead of adding materials

Goal: no new voxel materials. A Better Resources ore takes over the slot (voxel material id) of an ore with a **similar texture** that the pack does not need, and only gets a new name (MinedOre / display name and refining recipe).
Better Stone already reuses vanilla textures for its minerals (Pyrite and Electrum use the gold texture, Quartz the ice texture, Olivine the alien ice texture...), so the textures were compared by eye and by mean color / contrast
(Lab) of the `ColorMetalXZnY` textures.

**Which vanilla ores can be given up:** the ones with no native form: Cobalt, Nickel, Magnesium, Silicon, Uranium (and Iron, except meteoritic metal). The ones that *do* occur natively stay as pure ores, so the
earlier advice to drop all pure vanilla ores is refined: **keep Gold_01, Silver_01 and Platinum_01** (native gold, silver, platinum), 3 more ores for the budget (about 12 layers).

**Proposed assignment (8 Better Resources ores, none of them a new material):**

| Better Resources ore | Takes over the slot of | Why this slot | Elements still covered |
|---|---|---|---|
| Copper | **Cobalt_01** (vanilla, cyan-blue veins) | nearest hue to copper's teal verdigris; native copper exists, native cobalt does not | Co: Cattierite, Glaucodot, Kamacite |
| Titanium | **Silicon_01** (vanilla, light grey) | light grey speckled, like the Titanium texture | Si: Quartz, Olivine, Akimotoite, Wadsleyite |
| Wolframite | **Nickel_01** (vanilla, tan-brown on dark) | brown / orange dark ore | Ni: Heazlewoodite, Cooperite, Taenite, Kamacite |
| Petroleum | **Magnesium_01** (vanilla, dark blue-grey) | dark rough; Better Stone's Dolomite and Olivine carry the magnesium | Mg: Dolomite, Olivine, Akimotoite, Wadsleyite |
| Sulfur | **Carnotite_01** (Better Stone, yellow-brown) | the only yellow texture; uranium stays via Uraninite, Autunite, Uraniaurite | U: Uraninite_01, Autunite, Uraniaurite |
| Aluminium (bauxite) | **Cohenite_01** (Better Stone, tan-pink) | red-brown / pink-orange like bauxite; Cohenite duplicates Kamacite and Taenite | Fe, Ni, Co via Kamacite, Taenite |
| Lithium | **Hapkeite_01** (Better Stone, lilac-grey) | mauve-grey, the closest to lepidolite pink; a rare Fe-Si mineral | Fe, Si via Taenite, Quartz... |
| Nitre | **Petzite_01** (Better Stone, pale grey) | the palest texture; Petzite duplicates Electrum and Chlorargyrite | Ag: Galena, Chlorargyrite, Electrum; Au: Porphyry, Pyrite, Electrum, Uraniaurite |

Other close matches if you want to swap: Copper <- Nickel_01 or BS Cohenite; Lithium <- Cattierite (same texture as Hapkeite); Titanium <- BS Sperrylite (white-grey, and unused on Better Stone's planets);
Petroleum <- BS Porphyry or Kamacite (black rough, the nearest of all, but they carry gold / meteoritic iron); Aluminium <- BS Uraniaurite or Chlorargyrite (orange).
`Galena_01` stays Better Stone's (lead + silver recipe).

**Effect on the numbers:** voxel materials stay at vanilla 62 + Better Stone's 25 = **87** (Better Resources adds none). The ore count for the tool is unchanged (about 36-40 ores with Gold, Silver, Platinum back in).

**Things to watch:**
* Renaming a **vanilla** material changes it in every planet that uses it, also other mods' planets in the same save (a Typhoon or Teal map that places `Cobalt_01` would then place copper). Better Stone's own
  slots are safe; vanilla slots are only safe if every planet you play on uses your pack.
* The tool and the data packs refer to the **material id**, not the new name: the ore the tool lists as `Cobalt_01` is your copper. The built-in Vanilla pack still has Cobalt data under that name, so a merged
  pack (a fourth data pack with a higher Order) must give `Cobalt_01`, `Silicon_01`, `Nickel_01`, `Magnesium_01`, `Carnotite_01`, `Cohenite_01`, `Hapkeite_01` and `Petzite_01` the data of the ore that now uses them
  (an exact id in a later pack overrides the base-name entry of an earlier one).
* The vanilla ore items (Cobalt ore, Silicon ore...) and their refining recipes stay in the game; only the voxel material is renamed. The refining recipes for the renamed ores come from Better Resources.
* Textures stay those of the donor slot, so the renamed ores look like the old ones (a "Copper" that looks like cobalt). Better Resources' own textures would match better but are new texture files, not new materials: you
  can keep the donor's id and use Better Resources' texture files in the override if you want that.

## 8. Tool changes made while writing this

* The Vanilla data pack now also has an `Uraninite` entry: the vanilla uranium voxel material is `Uraninite_01`, so the old `Uranium` entry never matched a planet's ore name.
* The Better Stone pack has `Sperrylite`, the Better Resources pack `Sulfur`, so all 36 ore types of both mods are covered (25 + 9 new, 2 shared).
* A built-in pack that already exists in `~/.config/OreRedistLinux/DataPacks/` is never overwritten; delete a pack file to get the new version.
