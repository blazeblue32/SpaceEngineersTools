# Where each mineral appears, and how rare it is

Proposal, 2026-11-09, for the Better Stone + Better Resources pack with the composite minerals of `MODPACK_COMPOSITES.md`. Planet shares are **estimates** of the share of each mineral in the ore pixels of a planet, computed with the tool's own rule (share ~ abundance^0.25 within the planet's ore list, times a per-body factor); Ice is a fixed share. **Gold, silver, platinum and uranium are scaled to Better Stone's ingot rate per ore (section 8); everything else follows realism.** Nothing is built yet.

## 1. Principles

* **Real geology decides first.** A body gets a mineral only if it forms there (native metals and sulfides on wet Earth-likes, Fe-Ni metal and impact minerals on airless bodies, hydrocarbons on Titan...). Where a planet would otherwise lack an ingot completely, a very rare trace entry keeps it obtainable (marked as trace).
* **Rarity tiers (share of the ore on that body):** Abundant 8 % and up, Common 3-8 %, Uncommon 1-3 %, Rare 0.3-1 %, Very rare under 0.3 %.
* **Host rock, vein size and regions come from the data packs** (`Rocks`, `Size`, `Glob`, `GlobSize`, `GlobCoverage`); the table in section 6 lists them per mineral. Regions are in km on the planet, so they scale to each body's size; change Glob km per planet if a moon is much smaller.
* **Per body ore lists**, each with its own 254-number budget; the lists here have 15-40 ores, so 6-12 layers per ore is possible on the small ones.

## 2. Vanilla progression (checked against the game and Better Stone)

Better Stone says it respects vanilla progression, and the files confirm it: for every planet the set of metals it can give equals vanilla's, with one exception (uranium). Compared from the vanilla `PlanetGeneratorDefinitions.sbc` and Better Stone's `Override-PlanetGeneratorDefinitions.sbc` (through the refining recipes of the minerals):

| Planet | Gold, silver | Platinum | Uranium |
|---|---|---|---|
| EarthLike | vanilla yes, Better Stone yes | no, no | no, no |
| Alien | yes, yes | no, no | no, **Better Stone adds it** |
| Mars | yes, yes | no, no | no, **adds it** |
| Moon | yes, yes | yes, yes | no, **adds it** |
| Europa | yes, yes | yes, yes | no, **adds it** |
| Titan | yes, yes | yes, yes | no, **adds it** |
| Triton, Pertam (vanilla only) | yes | no | no |

So **gold and silver occur on every planet and moon in vanilla**, including the airless Moon, Europa and Titan, and **platinum only on the Moon, Europa and Titan**. The trace amounts of gold and silver proposed for the airless bodies stay, and the plan below follows Better Stone: no platinum on Earth-like or Mars (so no Cooperite, Niggliite, Sperrylite or native platinum there), no uranium on EarthLike (Alien, Mars, Moon, Europa and Titan get the uranium minerals). Vanilla asteroids have both platinum and uranium, kept in section 4.

## 3. Planets and moons

Vanilla planets: EarthLike, Alien, Mars, Moon, Europa, Titan, Triton, Pertam. Vanilla places only 8 ore types on each (Iron_02, Nickel, Cobalt, Magnesium, Silicon, Silver, Gold, Ice; Platinum only on Moon, Europa, Titan; Uranium nowhere, only in asteroids).

### Earth-like (EarthLike, Alien)

Everything forms here (wet crust, hydrothermal and sedimentary systems, pegmatites, laterites, evaporites), except the **platinum and uranium minerals: vanilla EarthLike has neither, and Better Stone keeps it that way** (section 2). Meteoritic and impact minerals only in crater regions (tiny regions). Ice only at the poles (3 %). **Alien** uses the same list plus the uranium minerals (as in Better Stone, but at 0.5, 0.3, 0.3 and 0.2 of the Earth-like weight; still no platinum), and Oil shale at 0.5 (life, then drier); make Niter and Sulfur ×2 (volcanic / arid) and Petroleum ×0.5 if you want it to feel different. **Pertam** (arid, no ice in vanilla; plausibly had life before it dried out): the Earth-like list without Ice, Spodumene and Lepidolite, Oil shale at 0.3, Niter ×3 and Sulfur ×2, no uranium or platinum.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Quartz | `Quartz_01` | 12.33 | Abundant | Si |
| Hematite (Iron) | `Iron_01` | 9.37 | Abundant | Fe, Ti |
| Bauxite | `Aluminium_01` | 8.72 | Abundant | Al, Ti, Ga |
| Dolomite | `Dolomite_01` | 7.88 | Common | Mg, C |
| Magnetite (Dense Iron) | `Iron_02` | 7.45 | Common | Fe, Ti |
| Porphyry | `Porphyry_01` | 5.99 | Common | Au, Cu, Ag |
| Ilmenite | `Magnesium_01` | 4.90 | Common | Fe, Ti |
| Gold (native) | `Gold_01` | 4.77 | Common | Au |
| Olivine | `Olivine_01` | 4.36 | Common | Si, Mg, Fe |
| Pyrite | `Pyrite_01` | 3.73 | Common | Fe, S |
| Silver (native) | `Silver_01` | 3.29 | Common | Ag |
| Ice | `Ice_01 / Ice_02` | 3.00 | Common | Ice |
| Rutile | `Titanium_01` | 2.96 | Uncommon | Ti, Nb |
| Oil shale (Petroleum) | `Petroleum_01` | 2.10 | Uncommon | C, S, Oil |
| Sulfur (native) | `Sulfur_01` | 2.01 | Uncommon | S |
| Chlorargyrite | `Chlorargyrite_01` | 1.99 | Uncommon | Ag, Perchlorate |
| Sphalerite | `Nickel_01` | 1.92 | Uncommon | Zn, S, Ga |
| Spodumene | `Silicon_01` | 1.62 | Uncommon | Li, Al, Si |
| Native Copper | `Copper_01` | 1.55 | Uncommon | Cu, Ag |
| Azurite | `Cobalt_01` | 1.55 | Uncommon | Cu, C |
| Electrum | `Electrum_01` | 1.33 | Uncommon | Ag, Au |
| Galena | `Galena_01` | 1.33 | Uncommon | Pb, Ag, Zn |
| Lepidolite | `Lithium_01` | 1.29 | Uncommon | Li, Al, Nitre, Nb |
| Petzite | `Petzite_01` | 1.12 | Uncommon | Ag, Au |
| Wolframite | `Wolframite_01` | 0.79 | Rare | W, Sn, Fe, Nb |
| Niter | `Nitre_01` | 0.78 | Rare | Nitre, N, Perchlorate |
| Heazlewoodite | `Heazlewoodite_01` | 0.66 | Rare | Ni, S |
| Glaucodot | `Glaucodot_01` | 0.56 | Rare | Fe, Co, S |
| Cattierite | `Cattierite_01` | 0.37 | Rare | Co, S |
| Kamacite | `Kamacite_01` | 0.11 | Very rare (trace entry) | Fe, Ni, Co |
| Taenite | `Taenite_01` | 0.10 | Very rare (trace entry) | Fe, Ni |
| Akimotoite | `Akimotoite_01` | 0.04 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.04 | Very rare (trace entry) | Si, Mg, Fe |

33 ores.

### Mars

Basaltic crust, iron oxides (hematite) and sulfate / carbonate salts; hydrothermal ore only near volcanoes and craters (trace). **No platinum** (vanilla Mars has none); uranium as in Better Stone but weak (Uraninite 0.3, Carnotite 0.15). Polar Ice 6 %. Aluminium and lithium minerals are absent (no laterites or pegmatites).

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Hematite (Iron) | `Iron_01` | 28.07 | Abundant | Fe, Ti |
| Olivine | `Olivine_01` | 26.12 | Abundant | Si, Mg, Fe |
| Ice | `Ice_01 / Ice_02` | 6.00 | Common | Ice |
| Sulfur (native) | `Sulfur_01` | 5.72 | Common | S |
| Gold (native) | `Gold_01` | 4.56 | Common | Au |
| Dolomite | `Dolomite_01` | 3.93 | Common | Mg, C |
| Silver (native) | `Silver_01` | 3.90 | Common | Ag |
| Magnetite (Dense Iron) | `Iron_02` | 3.72 | Common | Fe, Ti |
| Pyrite | `Pyrite_01` | 3.72 | Common | Fe, S |
| Quartz | `Quartz_01` | 3.69 | Common (trace entry) | Si |
| Ilmenite | `Magnesium_01` | 2.45 | Uncommon | Fe, Ti |
| Uraninite | `Uraninite_01` | 1.91 | Uncommon | U, Pb |
| Niter | `Nitre_01` | 1.55 | Uncommon | Nitre, N, Perchlorate |
| Electrum | `Electrum_01` | 1.05 | Uncommon | Ag, Au |
| Carnotite | `Carnotite_01` | 0.71 | Rare | U, Nitre |
| Sphalerite | `Nickel_01` | 0.57 | Rare (trace entry) | Zn, S, Ga |
| Kamacite | `Kamacite_01` | 0.45 | Rare | Fe, Ni, Co |
| Galena | `Galena_01` | 0.40 | Rare (trace entry) | Pb, Ag, Zn |
| Taenite | `Taenite_01` | 0.39 | Rare | Fe, Ni |
| Heazlewoodite | `Heazlewoodite_01` | 0.33 | Rare | Ni, S |
| Native Copper | `Copper_01` | 0.31 | Rare (trace entry) | Cu, Ag |
| Glaucodot | `Glaucodot_01` | 0.17 | Very rare (trace entry) | Fe, Co, S |
| Cohenite | `Cohenite_01` | 0.15 | Very rare | Fe, Ni, Co, C |
| Akimotoite | `Akimotoite_01` | 0.06 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.06 | Very rare (trace entry) | Si, Mg, Fe |

25 ores.

### Moon

Anorthosite highlands and mare basalt: ilmenite is the titanium ore, olivine and the high-pressure impact minerals, meteoritic Fe-Ni metal and PGE minerals from impactors, troilite (Pyrite) and KREEP uranium (weak). Polar Ice 5 %. Uranium is a weak KREEP spot (0.5). **Gap:** no aluminium mineral (anorthite is the real one; see section 7). Gold and silver are trace only, kept so the ingots stay obtainable.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ilmenite | `Magnesium_01` | 27.63 | Abundant | Fe, Ti |
| Olivine | `Olivine_01` | 24.57 | Abundant | Si, Mg, Fe |
| Hematite (Iron) | `Iron_01` | 13.20 | Abundant | Fe, Ti |
| Pyrite | `Pyrite_01` | 5.25 | Common | Fe, S |
| Ice | `Ice_01 / Ice_02` | 5.00 | Common | Ice |
| Silver (native) | `Silver_01` | 4.18 | Common | Ag |
| Gold (native) | `Gold_01` | 4.11 | Common | Au |
| Cooperite | `Cooperite_01` | 2.46 | Uncommon | Ni, Pt, S |
| Niggliite | `Niggliite_01` | 2.06 | Uncommon | Pt, Sn |
| Uraninite | `Uraninite_01` | 1.90 | Uncommon | U, Pb |
| Electrum | `Electrum_01` | 1.69 | Uncommon | Ag, Au |
| Kamacite | `Kamacite_01` | 1.57 | Uncommon | Fe, Ni, Co |
| Platinum (native) | `Platinum_01` | 1.54 | Uncommon | Pt |
| Taenite | `Taenite_01` | 1.38 | Uncommon | Fe, Ni |
| Sperrylite | `Sperrylite_01` | 1.23 | Uncommon | Pt |
| Cohenite | `Cohenite_01` | 0.52 | Rare | Fe, Ni, Co, C |
| Heazlewoodite | `Heazlewoodite_01` | 0.47 | Rare | Ni, S |
| Hapkeite | `Hapkeite_01` | 0.33 | Rare | Fe, Si |
| Akimotoite | `Akimotoite_01` | 0.30 | Very rare | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.30 | Very rare | Si, Mg, Fe |
| Glaucodot | `Glaucodot_01` | 0.24 | Very rare (trace entry) | Fe, Co, S |
| Sinoite | `Sinoite_01` | 0.08 | Very rare | Si, N |

22 ores.

### Europa

Ice shell over a silicate floor: Ice dominates (35 %), salts and sulfur compounds, sulfide and iron minerals from hydrothermal vents on the sea floor; the rest are traces (uranium at 0.1).

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ice | `Ice_01 / Ice_02` | 35.00 | Abundant | Ice |
| Dolomite | `Dolomite_01` | 16.33 | Abundant | Mg, C |
| Olivine | `Olivine_01` | 9.04 | Abundant | Si, Mg, Fe |
| Sulfur (native) | `Sulfur_01` | 8.91 | Abundant | S |
| Gold (native) | `Gold_01` | 4.79 | Common | Au |
| Silver (native) | `Silver_01` | 4.53 | Common | Ag |
| Pyrite | `Pyrite_01` | 3.86 | Common | Fe, S |
| Magnetite (Dense Iron) | `Iron_02` | 3.86 | Common | Fe, Ti |
| Quartz | `Quartz_01` | 3.83 | Common (trace entry) | Si |
| Platinum (native) | `Platinum_01` | 3.42 | Common | Pt |
| Cooperite | `Cooperite_01` | 2.72 | Uncommon | Ni, Pt, S |
| Uraninite | `Uraninite_01` | 1.91 | Uncommon | U, Pb |
| Sphalerite | `Nickel_01` | 0.60 | Rare (trace entry) | Zn, S, Ga |
| Galena | `Galena_01` | 0.41 | Rare (trace entry) | Pb, Ag, Zn |
| Heazlewoodite | `Heazlewoodite_01` | 0.34 | Rare | Ni, S |
| Kamacite | `Kamacite_01` | 0.29 | Very rare | Fe, Ni, Co |
| Glaucodot | `Glaucodot_01` | 0.17 | Very rare (trace entry) | Fe, Co, S |

17 ores.

### Titan

Ice bedrock with hydrocarbon lakes and dunes: **Petroleum is the main mineral** (6 times its normal weight), Ice 30 %, a few silicate and metal traces, uranium only as a trace. Oil shale stays on Titan because it has real hydrocarbons, even though it is not a rock-forming mineral there; drop it if you want oil on Earth-likes only. **Triton** (vanilla has no platinum, uranium or ice ore there): like Europa without the platinum and uranium minerals, with Sinoite (nitrogen) added at 1.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ice | `Ice_01 / Ice_02` | 30.00 | Abundant | Ice |
| Oil shale (Petroleum) | `Petroleum_01` | 22.31 | Abundant | C, S, Oil |
| Olivine | `Olivine_01` | 7.74 | Common | Si, Mg, Fe |
| Dolomite | `Dolomite_01` | 6.99 | Common | Mg, C |
| Magnetite (Dense Iron) | `Iron_02` | 6.61 | Common | Fe, Ti |
| Sulfur (native) | `Sulfur_01` | 5.09 | Common | S |
| Silver (native) | `Silver_01` | 4.94 | Common | Ag |
| Gold (native) | `Gold_01` | 4.79 | Common | Au |
| Platinum (native) | `Platinum_01` | 4.50 | Common | Pt |
| Pyrite | `Pyrite_01` | 3.31 | Common | Fe, S |
| Uraninite | `Uraninite_01` | 1.91 | Uncommon | U, Pb |
| Sphalerite | `Nickel_01` | 0.68 | Rare (trace entry) | Zn, S, Ga |
| Kamacite | `Kamacite_01` | 0.49 | Rare | Fe, Ni, Co |
| Heazlewoodite | `Heazlewoodite_01` | 0.35 | Rare (trace entry) | Ni, S |
| Glaucodot | `Glaucodot_01` | 0.30 | Very rare (trace entry) | Fe, Co, S |

15 ores.

## 4. Asteroids

Asteroids pick their ore from **every material with `SpawnsInAsteroids` true**, repeated `AsteroidGeneratorSpawnProbabilityMultiplier` times (checked in the game code, see MODPACK_REPORT section 12). A mod can replace these weights; **every mineral not listed below is set to `SpawnsInAsteroids` false**, so the planet-only ores stay off asteroids: Oil shale (petroleum), native sulfur, niter, bauxite, native copper, azurite, sphalerite, lepidolite, spodumene, rutile, wolframite, galena and the other hydrothermal and uranium minerals.

**Better Stone's own rebalance (the companion mod `3511204395`, "Better Stone - Rebalanced Asteroids" by michi84o, which must load after Better Stone):** its files change only the asteroid weights of Better Stone's minerals and re-declare `Ice_02` (weight 5). The weights of its 27 minerals go from 40 to 82 in total (Iron_01 5 to 19, Taenite and Hapkeite 2 to 6, Sinoite and Quartz 1-2 to 4, Cohenite 1 to 3...) while the vanilla gold, silver, platinum and uranium keep their weight of 1, which makes them **about half as common in share**; its description says the idea is to push players to the planets (uranium on Alien and small moons). The Steam page also shows a notice that the item was removed or is flagged incompatible (read by a web fetch, not verified), so treat it as a reference for the numbers rather than a dependency.

**The weights below follow the same method:** every ordinary mineral is doubled while gold, silver, platinum, uranium and the platinum minerals stay at 2-3, so their share is about 1.3 % each (the rebalanced Better Stone gives 0.97 %, the original 1.6 %), while iron and silicon sources stay generous. Oil shale and sulfur are removed.

The generator has one list for all asteroids, so the mix is global; asteroid *families* (ice, stony, metallic) would need separate generator versions, which I have not checked.

| Mineral | Weight | Share % | Tier | Note |
|---|---|---|---|---|
| Ice | 24 | 15.2 | Abundant | Ice_02 (only ice asteroids allow it; Ice_01 and Ice_03 stay planet-only) |
| Hematite (Iron) | 20 | 12.7 | Abundant | Iron (Fe), the main asteroid iron |
| Kamacite | 16 | 10.1 | Abundant | Fe-Ni-Co metal |
| Olivine | 16 | 10.1 | Abundant | stony asteroids |
| Taenite | 14 | 8.9 | Abundant | Fe-Ni metal |
| Pyrite | 12 | 7.6 | Common | troilite (FeS) |
| Dolomite | 8 | 5.1 | Common | carbonates in carbonaceous asteroids |
| Cohenite | 6 | 3.8 | Common | iron carbide |
| Sinoite | 6 | 3.8 | Common | enstatite chondrite (Si, N) |
| Akimotoite | 4 | 2.5 | Uncommon | shock mineral |
| Wadsleyite | 4 | 2.5 | Uncommon | shock mineral |
| Hapkeite | 4 | 2.5 | Uncommon | lunar / stony Fe-Si |
| Heazlewoodite | 4 | 2.5 | Uncommon | Ni sulfide |
| Magnetite (Dense Iron) | 4 | 2.5 | Uncommon | dense iron fragments |
| Cooperite | 3 | 1.9 | Uncommon | PGE |
| Niggliite | 2 | 1.3 | Uncommon | PGE |
| Sperrylite | 2 | 1.3 | Uncommon | PGE |
| Platinum (native) | 2 | 1.3 | Uncommon | native platinum alloy grains |
| Gold (native) | 2 | 1.3 | Uncommon | trace, as in the rebalanced Better Stone |
| Silver (native) | 2 | 1.3 | Uncommon | trace |
| Uraninite | 2 | 1.3 | Uncommon | trace, uranium |
| Quartz | 1 | 0.6 | Rare | trace |

Total weight 158. Ice is the most common single material (about 15 %), then asteroid iron (Iron (Fe) with a little Ice) and the Fe-Ni metals; gold, silver, platinum and uranium are about 1.3 % each.

## 5. Which ingots can be mined where

Number of different minerals that give each ingot on each body (a dash = none).

| Ingot | Earth-like | Mars | Moon | Europa | Titan | Asteroids |
|---|---|---|---|---|---|---|
| Fe | 11 | 11 | 11 | 5 | 5 | 10 |
| Ni | 3 | 4 | 5 | 3 | 2 | 5 |
| Co | 3 | 3 | 3 | 2 | 2 | 2 |
| Mg | 4 | 4 | 3 | 2 | 2 | 4 |
| Si | 5 | 4 | 5 | 2 | 1 | 6 |
| Ag | 7 | 4 | 2 | 2 | 1 | 1 |
| Au | 4 | 2 | 2 | 1 | 1 | 1 |
| Pt | - | - | 4 | 2 | 1 | 4 |
| U | - | 2 | 1 | 1 | 1 | 1 |
| Cu | 3 | 1 | - | - | - | - |
| Al | 3 | - | - | - | - | - |
| Ti | 5 | 3 | 2 | 1 | 1 | 2 |
| Li | 2 | - | - | - | - | - |
| Zn | 2 | 2 | - | 2 | 1 | - |
| Pb | 1 | 2 | 1 | 2 | 1 | 1 |
| S | 7 | 5 | 4 | 6 | 6 | 3 |
| W | 1 | - | - | - | - | - |
| Sn | 1 | - | 1 | - | - | 1 |
| C | 3 | 2 | 1 | 1 | 2 | 2 |
| N | 1 | 1 | 1 | - | - | 1 |
| Ice | 1 | 1 | 1 | 1 | 1 | 1 |

**Gaps to look at:** aluminium, lithium and tungsten exist only on Earth-likes (bauxite, spodumene, lepidolite, wolframite), copper, zinc, lead and tin only as traces on Mars, Europa and Titan or not at all on the Moon and in asteroids, nitrogen only from Sinoite and Niter. Items made from Better Resources ingots (aluminium parts, lithium batteries, tungsten) would then be Earth-like-only. See decision 1.


### Progression check

Metals each body gives in the plan against vanilla (and Better Stone, which adds uranium to every planet except EarthLike). Alien is the Earth-like list plus uranium; Pertam and Triton as described in section 3.

| Body | Plan gives | Vanilla + Better Stone give | Match |
|---|---|---|---|
| Earth-like | Fe, Ni, Co, Mg, Si, Ag, Au | Fe, Ni, Co, Mg, Si, Ag, Au | yes |
| Mars | Fe, Ni, Co, Mg, Si, Ag, Au, U | Fe, Ni, Co, Mg, Si, Ag, Au, U | yes |
| Moon | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | yes |
| Europa | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | yes |
| Titan | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | yes |
| Asteroids | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | Fe, Ni, Co, Mg, Si, Ag, Au, Pt, U | yes |

Availability matches, and the **rate** now matches too: gold, silver and platinum follow Better Stone's ingots per ore, uranium half of it (section 8).

## 6. Host rock, vein size and regions (the data pack values)

Rock names: sed = sedimentary, met-low / met-high = metamorphic low / high grade, extr = igneous extrusive, intr = igneous intrusive. Size = vein pixels on the reference planet (60 km radius, 2048 px tiles; scaled automatically). Regions: strength Glob %, size km, coverage %.

| Mineral | Abundance ppm | Host rocks | Vein px | Region strength / size km / cover % |
|---|---|---|---|---|
| Magnetite (Dense Iron) | 16000 | intrusive igneous, high-grade metamorphic | 49 | 40 / 37.5 / 6 |
| Hematite (Iron) | 40000 | sedimentary, low-grade metamorphic | 49 | 40 / 37.5 / 6 |
| Hapkeite | 0.001 | intrusive igneous | 4 | 85 / 50 / 0.5 |
| Heazlewoodite | 1 | intrusive igneous, low-grade metamorphic | 15 | 70 / 30 / 0.5 |
| Porphyry | 0.01 | intrusive igneous | 26 | 65 / 32.5 / 1 |
| Dolomite | 20000 | sedimentary, low-grade metamorphic | 75 | 25 / 35 / 3.5 |
| Cattierite | 0.1 | sedimentary, intrusive igneous, low-grade metamorphic | 22 | 70 / 50 / 0.5 |
| Niggliite | 0.001 | intrusive igneous | 59 | 90 / 60 / 1 |
| Carnotite | 0.3 | sedimentary | 15 | 75 / 32.5 / 0.5 |
| Glaucodot | 0.5 | low-grade metamorphic, high-grade metamorphic, intrusive igneous | 7 | 60 / 32.5 / 0.5 |
| Sinoite | 0.001 | intrusive igneous | 4 | 85 / 50 / 0.5 |
| Pyrite | 1000 | extrusive igneous, intrusive igneous, low-grade metamorphic, sedimentary, high-grade metamorphic | 26 | 10 / 27.5 / 3 |
| Olivine | 30000 | intrusive igneous, extrusive igneous | 49 | 35 / 27.5 / 1 |
| Cooperite | 0.002 | intrusive igneous | 59 | 90 / 60 / 1 |
| Quartz | 120000 | sedimentary, intrusive igneous, low-grade metamorphic, high-grade metamorphic | 49 | 0 / 0 / 0 |
| Galena | 16 | sedimentary, intrusive igneous, extrusive igneous | 22 | 40 / 27.5 / 1.5 |
| Chlorargyrite | 0.01 | extrusive igneous, intrusive igneous | 9 | 60 / 30 / 0.5 |
| Electrum | 0.002 | extrusive igneous, intrusive igneous | 10 | 70 / 20 / 0.5 |
| Sperrylite | 0.002 | intrusive igneous | 59 | 90 / 50 / 1 |
| Autunite | 0.3 | sedimentary, intrusive igneous | 10 | 70 / 32.5 / 0.5 |
| Akimotoite | 0.01 | intrusive igneous, high-grade metamorphic | 4 | 85 / 50 / 0.5 |
| Wadsleyite | 0.01 | intrusive igneous, high-grade metamorphic | 4 | 85 / 50 / 0.5 |
| Taenite | 0.3 | intrusive igneous | 5 | 60 / 32.5 / 0.5 |
| Cohenite | 0.1 | intrusive igneous | 5 | 60 / 32.5 / 0.5 |
| Kamacite | 0.5 | intrusive igneous | 5 | 60 / 32.5 / 0.5 |
| Uraniaurite | 0.001 | low-grade metamorphic, intrusive igneous, sedimentary | 5 | 85 / 50 / 0.5 |
| Petzite | 0.001 | extrusive igneous, intrusive igneous | 10 | 70 / 20 / 0.5 |
| Gold (native) | 0.004 | low-grade metamorphic | 15 | 75 / 32.5 / 2 |
| Silver (native) | 0.075 | extrusive igneous, intrusive igneous | 10 | 55 / 27.5 / 1 |
| Platinum (native) | 0.005 | intrusive igneous | 75 | 90 / 50 / 1.5 |
| Uraninite | 1 | intrusive igneous, sedimentary, low-grade metamorphic | 19 | 65 / 32.5 / 1 |
| Azurite | 30 | intrusive igneous, extrusive igneous | 25 | 65 / 35 / 3 |
| Sphalerite | 70 | sedimentary, intrusive igneous, extrusive igneous | 22 | 40 / 27.5 / 1.5 |
| Ilmenite | 3000 | intrusive igneous, extrusive igneous, high-grade metamorphic | 26 | 45 / 32.5 / 1 |
| Spodumene | 150 | intrusive igneous | 12 | 80 / 30 / 0.5 |
| Bauxite | 30000 | sedimentary | 43 | 50 / 40 / 5 |
| Native Copper | 30 | intrusive igneous, extrusive igneous | 31 | 65 / 35 / 3 |
| Rutile | 400 | intrusive igneous, high-grade metamorphic | 26 | 45 / 32.5 / 1 |
| Lepidolite | 60 | intrusive igneous | 12 | 80 / 30 / 0.5 |
| Wolframite | 2 | intrusive igneous | 12 | 75 / 50 / 1 |
| Niter | 30 | sedimentary | 49 | 90 / 50 / 0.5 |
| Sulfur (native) | 350 | extrusive igneous, sedimentary | 26 | 70 / 30 / 0.5 |
| Oil shale (Petroleum) | 100 | sedimentary | 59 | 80 / 42.5 / 7 |

New entries (Azurite, Sphalerite, Ilmenite, Spodumene, and the renamed Bauxite, Native Copper, Rutile, Lepidolite, Niter, Oil shale) reuse the values of the Better Resources ore or mineral they were built from; Magnetite and Hematite split the Iron entry (abundance 16,000 and 40,000). Ice has no rock or region (it follows the polar regions you paint, or is left even).

## 7. Decisions to confirm

1. **Aluminium gap:** aluminium only exists on Earth-likes. Change Spodumene (`Silicon_01`) to **Anorthite (Al, Si)**, a real mineral of the lunar highlands, of basaltic crust (Mars, Moon) and of anorthosite on Earth-likes, with a trace of Bauxite-like clay on Mars? Lithium then comes only from Lepidolite (Earth-likes), tungsten stays Earth-like-only.
2. **Hematite / Magnetite names** for the two iron slots (Better Stone calls them Iron (Fe) and Dense Iron (Fe+)).
3. **Precious metals follow Better Stone's rate** (section 8): decided. Their realism comes from the host rocks and from a few rich regions, not from a thin even sprinkle.
4. **Planet-specific ore regions:** the pack values are Earth-scale; on a smaller moon (Europa, Triton) use bigger Cov % or fewer regions in the tool per planet.
5. **Asteroid weights** are one global list; asteroid families would need extra work.

## 8. Rare metal balance against vanilla and Better Stone

Measured on the real planet maps: the blue channel of the vanilla `*_mat.png` files (Better Stone ships no map files, it only re-assigns the ore values), counted per ore value and mapped to the ore types of the vanilla and the Better Stone definitions. The yardstick is **ingots per 100 ore mined**, averaged over the planet's ore (vanilla yields: Gold 0.01, Silver 0.1, Platinum 0.005, Uranium 0.01 per ore; Better Stone's mineral recipes for the rest; this plan uses Better Resources' 0.1 Ag for Galena and no gold in Pyrite).

**Finding:** Better Stone keeps vanilla's ingot rate. On EarthLike gold is 0.091 (vanilla) against 0.093 (Better Stone) ingots per 100 ore; it does this by making the gold-bearing ores (Gold, Electrum, Petzite, Porphyry, Pyrite, Uraniaurite) about **19 % of the ore pixels** (12 % silver-bearing, 7.5 % platinum-bearing on the Moon, Europa and Titan, 9-14 % uranium-bearing) with small yields each. Rare metals are not rare in Better Stone's ore maps; they are low-yield. The plan above, which follows real abundance, is **far below that**:

| Body | Metal | Vanilla | Better Stone | Plan now | Share of ore, Better Stone / plan now |
|---|---|---|---|---|---|
| Earth-like | Gold | 0.091 | 0.093 | 0.0048 (x0.05) | 19.5 % / 0.77 % |
| Earth-like | Silver | 0.684 | 0.699 | 0.2339 (x0.33) | 12.1 % / 2.60 % |
| Mars | Gold | 0.078 | 0.050 | 0.0007 (x0.01) | 12.0 % / 0.09 % |
| Mars | Silver | 0.586 | 0.477 | 0.0586 (x0.12) | 7.7 % / 0.60 % |
| Mars | Uranium | 0.000 | 0.040 | 0.0024 (x0.06) | 12.0 % / 0.31 % |
| Moon | Gold | 0.073 | 0.048 | 0.0012 (x0.02) | 11.2 % / 0.16 % |
| Moon | Silver | 0.555 | 0.494 | 0.0213 (x0.04) | 8.1 % / 0.25 % |
| Moon | Platinum | 0.037 | 0.023 | 0.0022 (x0.10) | 7.5 % / 0.72 % |
| Moon | Uranium | 0.000 | 0.038 | 0.0057 (x0.15) | 8.9 % / 0.57 % |
| Europa | Gold | 0.073 | 0.048 | 0.0005 (x0.01) | 11.2 % / 0.05 % |
| Europa | Silver | 0.555 | 0.494 | 0.0656 (x0.13) | 8.1 % / 0.66 % |
| Europa | Platinum | 0.037 | 0.023 | 0.0005 (x0.02) | 7.5 % / 0.13 % |
| Europa | Uranium | 0.000 | 0.038 | 0.0009 (x0.02) | 8.9 % / 0.09 % |
| Titan | Gold | 0.073 | 0.048 | 0.0008 (x0.02) | 11.2 % / 0.08 % |
| Titan | Silver | 0.555 | 0.494 | 0.0159 (x0.03) | 8.1 % / 0.16 % |
| Titan | Platinum | 0.037 | 0.023 | 0.0006 (x0.03) | 7.5 % / 0.12 % |
| Titan | Uranium | 0.000 | 0.038 | 0.0015 (x0.04) | 8.9 % / 0.15 % |

So in the plan gold would be 20-150 times scarcer than in vanilla and Better Stone, platinum and uranium 10-50 times. Progression is kept only on paper.

**Decision: applied in the tables above.** Gold, silver and platinum are brought gold, silver and platinum to the Better Stone rate (the progression), uranium to half of it (Better Stone adds it, you wanted less), and keep realism through the *regions* and host rocks: these ores are in a few rich regions (Glob 65-90 %, 8-17 km regions, section 6), so a miner who finds one gets a lot, instead of a thin even sprinkle. Multipliers that were applied to the body factors:

| Body | Metal groups: factor and resulting share of the ore |
|---|---|
| Earth-like | Gold x29 -> 10.8 %; Silver x9 -> 7.7 % |
| Mars | Gold x91 -> 4.6 %; Silver x38 -> 4.9 %; Uranium x10 -> 2.6 % |
| Moon | Gold x58 -> 4.1 %; Silver x29 -> 5.9 %; Platinum x12 -> 7.3 %; Uranium x4 -> 1.9 % |
| Europa | Gold x139 -> 4.8 %; Silver x63 -> 4.5 %; Platinum x62 -> 6.1 %; Uranium x28 -> 1.9 % |
| Titan | Gold x81 -> 4.8 %; Silver x40 -> 4.9 %; Platinum x48 -> 4.5 %; Uranium x16 -> 1.9 % |

The cost: the gold- and silver-bearing minerals then take about 18 % of the ore pixels on Earth-like (10.8 % and 7.7 %) and 10-20 % on the other bodies (gold, silver, platinum and uranium groups together), and the other minerals get proportionally less; Better Stone's own shares are in the table above. 

**Asteroids:** Better Stone's rebalanced list (companion mod) gives gold, silver, platinum and uranium about 1 / 103 = 0.97 % each, with the other ores summing to 82. In the plan they are 1 / 151 = 0.66 %. Applied: weight 2 for each (platinum minerals Cooperite 3, Niggliite 2, Sperrylite 2) for about 1.3 % each, still under the original Better Stone share of about 1.6 %.

