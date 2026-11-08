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

Everything forms here (wet crust, hydrothermal and sedimentary systems, pegmatites, laterites, evaporites), except the **platinum and uranium minerals: vanilla EarthLike has neither, and Better Stone keeps it that way** (section 2). Meteoritic and impact minerals only in crater regions (tiny regions). Ice only at the poles. **Alien** uses the same list plus the uranium minerals (as in Better Stone, but at 0.5, 0.3, 0.3 and 0.2 of the Earth-like weight; still no platinum), and Oil shale at 0.5 (life, then drier); make Niter and Sulfur ×2 (volcanic / arid) and Petroleum ×0.5 if you want it to feel different. **Pertam** (arid, no ice in vanilla; plausibly had life before it dried out): the Earth-like list without Ice, Spodumene and Lepidolite, Oil shale at 0.3, Niter ×3 and Sulfur ×2, no uranium or platinum.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Olivine | `Olivine_01` | 16.55 | Abundant | Si, Mg, Fe |
| Quartz | `Quartz_01` | 10.59 | Abundant | Si |
| Heazlewoodite | `Heazlewoodite_01` | 9.35 | Abundant | Ni, S |
| Gold (native) | `Gold_01` | 8.09 | Abundant | Au |
| Glaucodot | `Glaucodot_01` | 6.46 | Common | Fe, Co, S |
| Spodumene | `Silicon_01` | 6.13 | Common | Li, Al, Si |
| Bauxite | `Aluminium_01` | 5.27 | Common | Al, Ti, Ga |
| Galena | `Galena_01` | 5.00 | Common | Pb, Ag, Zn |
| Magnetite (Dense Iron) | `Iron_02` | 4.97 | Common | Fe, Ti |
| Dolomite | `Dolomite_01` | 4.76 | Common | Mg, C |
| Pyrite | `Pyrite_01` | 2.53 | Uncommon | Fe, S |
| Ilmenite | `Magnesium_01` | 2.51 | Uncommon | Fe, Ti |
| Oil shale (Petroleum) | `Petroleum_01` | 2.21 | Uncommon | C, S, Oil |
| Hematite (Iron) | `Iron_01` | 1.80 | Uncommon | Fe, Ti |
| Rutile | `Titanium_01` | 1.79 | Uncommon | Ti, Nb |
| Sulfur (native) | `Sulfur_01` | 1.40 | Uncommon | S |
| Sphalerite | `Nickel_01` | 1.37 | Uncommon | Zn, S, Ga |
| Native Copper | `Copper_01` | 1.28 | Uncommon | Cu, Ag |
| Azurite | `Cobalt_01` | 1.21 | Uncommon | Cu, C |
| Lepidolite | `Lithium_01` | 1.09 | Uncommon | Li, Al, Nitre, Nb |
| Silver (native) | `Silver_01` | 0.92 | Rare | Ag |
| Wolframite | `Wolframite_01` | 0.81 | Rare | W, Sn, Fe, Nb |
| Niter | `Nitre_01` | 0.78 | Rare | Nitre, N, Perchlorate |
| Ice | `Ice_01 / Ice_02` | 0.77 | Rare | Ice |
| Cattierite | `Cattierite_01` | 0.62 | Rare | Co, S |
| Porphyry | `Porphyry_01` | 0.49 | Rare | Au, Cu, Ag |
| Chlorargyrite | `Chlorargyrite_01` | 0.35 | Rare | Ag, Perchlorate |
| Electrum | `Electrum_01` | 0.27 | Very rare | Ag, Au |
| Petzite | `Petzite_01` | 0.21 | Very rare | Ag, Au |
| Kamacite | `Kamacite_01` | 0.17 | Very rare (trace entry) | Fe, Ni, Co |
| Taenite | `Taenite_01` | 0.15 | Very rare (trace entry) | Fe, Ni |
| Akimotoite | `Akimotoite_01` | 0.06 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.06 | Very rare (trace entry) | Si, Mg, Fe |

33 ores.

### Mars

Basaltic crust, iron oxides (hematite) and sulfate / carbonate salts; hydrothermal ore only near volcanoes and craters (trace). **No platinum** (vanilla Mars has none); uranium as in Better Stone but weak (Uraninite 0.3, Carnotite 0.15). Polar Ice. Aluminium and lithium minerals are absent (no laterites or pegmatites).

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Quartz | `Quartz_01` | 17.24 | Abundant (trace entry) | Si |
| Olivine | `Olivine_01` | 12.19 | Abundant | Si, Mg, Fe |
| Sulfur (native) | `Sulfur_01` | 10.73 | Abundant | S |
| Heazlewoodite | `Heazlewoodite_01` | 6.91 | Common | Ni, S |
| Dolomite | `Dolomite_01` | 6.41 | Common | Mg, C |
| Hematite (Iron) | `Iron_01` | 5.12 | Common | Fe, Ti |
| Gold (native) | `Gold_01` | 4.75 | Common (trace entry) | Au |
| Niter | `Nitre_01` | 4.37 | Common | Nitre, N, Perchlorate |
| Glaucodot | `Glaucodot_01` | 3.94 | Common (trace entry) | Fe, Co, S |
| Uraninite | `Uraninite_01` | 3.87 | Common (trace entry) | U, Pb |
| Galena | `Galena_01` | 3.76 | Common (trace entry) | Pb, Ag, Zn |
| Ice | `Ice_01 / Ice_02` | 3.66 | Common | Ice |
| Ilmenite | `Magnesium_01` | 3.38 | Common | Fe, Ti |
| Pyrite | `Pyrite_01` | 3.04 | Common | Fe, S |
| Magnetite (Dense Iron) | `Iron_02` | 2.30 | Uncommon | Fe, Ti |
| Sphalerite | `Nickel_01` | 1.91 | Uncommon (trace entry) | Zn, S, Ga |
| Kamacite | `Kamacite_01` | 1.68 | Uncommon | Fe, Ni, Co |
| Taenite | `Taenite_01` | 1.48 | Uncommon | Fe, Ni |
| Native Copper | `Copper_01` | 1.15 | Uncommon (trace entry) | Cu, Ag |
| Silver (native) | `Silver_01` | 0.68 | Rare (trace entry) | Ag |
| Cohenite | `Cohenite_01` | 0.56 | Rare | Fe, Ni, Co, C |
| Carnotite | `Carnotite_01` | 0.27 | Very rare (trace entry) | U, Nitre |
| Akimotoite | `Akimotoite_01` | 0.24 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.24 | Very rare (trace entry) | Si, Mg, Fe |
| Electrum | `Electrum_01` | 0.12 | Very rare (trace entry) | Ag, Au |

25 ores.

### Moon

Anorthosite highlands and mare basalt: ilmenite is the titanium ore, olivine and the high-pressure impact minerals, meteoritic Fe-Ni metal and PGE minerals from impactors, troilite (Pyrite) and KREEP uranium (weak). Polar Ice. Uranium is a weak KREEP spot (0.5). **Gap:** no aluminium mineral (anorthite is the real one; see section 7). Gold and silver are trace only, kept so the ingots stay obtainable.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Hapkeite | `Hapkeite_01` | 11.76 | Abundant | Fe, Si |
| Ilmenite | `Magnesium_01` | 9.60 | Abundant | Fe, Ti |
| Olivine | `Olivine_01` | 9.33 | Abundant | Si, Mg, Fe |
| Heazlewoodite | `Heazlewoodite_01` | 8.98 | Abundant | Ni, S |
| Cohenite | `Cohenite_01` | 8.88 | Abundant | Fe, Ni, Co, C |
| Wadsleyite | `Wadsleyite_01` | 7.43 | Common | Si, Mg, Fe |
| Kamacite | `Kamacite_01` | 5.69 | Common | Fe, Ni, Co |
| Cooperite | `Cooperite_01` | 4.90 | Common | Ni, Pt, S |
| Silver (native) | `Silver_01` | 4.68 | Common (trace entry) | Ag |
| Gold (native) | `Gold_01` | 4.52 | Common (trace entry) | Au |
| Uraninite | `Uraninite_01` | 3.82 | Common | U, Pb |
| Taenite | `Taenite_01` | 3.37 | Common | Fe, Ni |
| Pyrite | `Pyrite_01` | 3.18 | Common | Fe, S |
| Hematite (Iron) | `Iron_01` | 2.92 | Uncommon | Fe, Ti |
| Akimotoite | `Akimotoite_01` | 2.90 | Uncommon | Si, Mg, Fe |
| Ice | `Ice_01 / Ice_02` | 2.12 | Uncommon | Ice |
| Glaucodot | `Glaucodot_01` | 1.49 | Uncommon (trace entry) | Fe, Co, S |
| Niggliite | `Niggliite_01` | 1.48 | Uncommon | Pt, Sn |
| Platinum (native) | `Platinum_01` | 1.22 | Uncommon | Pt |
| Sperrylite | `Sperrylite_01` | 0.69 | Rare | Pt |
| Sinoite | `Sinoite_01` | 0.59 | Rare | Si, N |
| Electrum | `Electrum_01` | 0.46 | Rare (trace entry) | Ag, Au |

22 ores.

### Europa

Ice shell over a silicate floor: salts and sulfur compounds, sulfide and iron minerals from hydrothermal vents on the sea floor; the rest are traces (uranium at 0.1).

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Heazlewoodite | `Heazlewoodite_01` | 17.65 | Abundant | Ni, S |
| Dolomite | `Dolomite_01` | 16.03 | Abundant | Mg, C |
| Quartz | `Quartz_01` | 11.07 | Abundant (trace entry) | Si |
| Sulfur (native) | `Sulfur_01` | 10.28 | Abundant | S |
| Magnetite (Dense Iron) | `Iron_02` | 7.60 | Common | Fe, Ti |
| Glaucodot | `Glaucodot_01` | 6.05 | Common (trace entry) | Fe, Co, S |
| Pyrite | `Pyrite_01` | 5.87 | Common | Fe, S |
| Gold (native) | `Gold_01` | 4.59 | Common (trace entry) | Au |
| Platinum (native) | `Platinum_01` | 4.27 | Common (trace entry) | Pt |
| Uraninite | `Uraninite_01` | 3.68 | Common (trace entry) | U, Pb |
| Kamacite | `Kamacite_01` | 3.04 | Common | Fe, Ni, Co |
| Galena | `Galena_01` | 2.53 | Uncommon (trace entry) | Pb, Ag, Zn |
| Olivine | `Olivine_01` | 2.28 | Uncommon | Si, Mg, Fe |
| Silver (native) | `Silver_01` | 2.12 | Uncommon (trace entry) | Ag |
| Sphalerite | `Nickel_01` | 1.18 | Uncommon (trace entry) | Zn, S, Ga |
| Ice | `Ice_01 / Ice_02` | 1.16 | Uncommon | Ice |
| Wadsleyite | `Wadsleyite_01` | 0.25 | Very rare (trace entry) | Si, Mg, Fe |
| Akimotoite | `Akimotoite_01` | 0.19 | Very rare (trace entry) | Si, Mg, Fe |
| Cooperite | `Cooperite_01` | 0.15 | Very rare (trace entry) | Ni, Pt, S |

19 ores.

### Titan

Ice bedrock with hydrocarbon lakes and dunes: **Petroleum is one of the main minerals** (kept at about 20 % after the ingot fit), a few silicate and metal traces, uranium only as a trace. Oil shale stays on Titan because it has real hydrocarbons, even though it is not a rock-forming mineral there; drop it if you want oil on Earth-likes only. **Triton** (vanilla has no platinum, uranium or ice ore there): like Europa without the platinum and uranium minerals, with Sinoite (nitrogen) added at 1.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Olivine | `Olivine_01` | 20.89 | Abundant | Si, Mg, Fe |
| Heazlewoodite | `Heazlewoodite_01` | 15.23 | Abundant (trace entry) | Ni, S |
| Oil shale (Petroleum) | `Petroleum_01` | 15.10 | Abundant | C, S, Oil |
| Kamacite | `Kamacite_01` | 6.69 | Common | Fe, Ni, Co |
| Magnetite (Dense Iron) | `Iron_02` | 5.70 | Common | Fe, Ti |
| Glaucodot | `Glaucodot_01` | 4.95 | Common (trace entry) | Fe, Co, S |
| Dolomite | `Dolomite_01` | 4.73 | Common | Mg, C |
| Silver (native) | `Silver_01` | 4.66 | Common (trace entry) | Ag |
| Gold (native) | `Gold_01` | 4.49 | Common (trace entry) | Au |
| Platinum (native) | `Platinum_01` | 4.25 | Common (trace entry) | Pt |
| Uraninite | `Uraninite_01` | 3.62 | Common (trace entry) | U, Pb |
| Sulfur (native) | `Sulfur_01` | 3.44 | Common | S |
| Pyrite | `Pyrite_01` | 2.62 | Uncommon | Fe, S |
| Wadsleyite | `Wadsleyite_01` | 1.51 | Uncommon (trace entry) | Si, Mg, Fe |
| Ice | `Ice_01 / Ice_02` | 0.99 | Rare | Ice |
| Sphalerite | `Nickel_01` | 0.76 | Rare (trace entry) | Zn, S, Ga |
| Akimotoite | `Akimotoite_01` | 0.35 | Rare (trace entry) | Si, Mg, Fe |

17 ores.

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
| Fe | 11 | 11 | 11 | 7 | 7 | 10 |
| Ni | 3 | 4 | 5 | 3 | 2 | 5 |
| Co | 3 | 3 | 3 | 2 | 2 | 2 |
| Mg | 4 | 4 | 3 | 4 | 4 | 4 |
| Si | 5 | 4 | 5 | 4 | 3 | 6 |
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

## 8. Ingot balance against vanilla and Better Stone (applied)

Measured on the real planet maps: the blue channel of the vanilla `*_mat.png` files (Better Stone ships no map files, it only re-assigns the ore values), counted per ore value and mapped to the ore types of the vanilla and Better Stone definitions. The yardstick is **ingots per 100 ore mined**, averaged over the planet's ore, using vanilla yields for the vanilla ores and Better Stone's refining recipes (and the added ingots of `MODPACK_COMPOSITES.md` for this plan).

**Finding 1: Better Stone keeps vanilla's ingot rate** for every ingot a planet gave in vanilla (iron, nickel, cobalt, magnesium, silicon, gold, silver, platinum; ice through the Ice by-product of its minerals, uranium is its own addition). It does this with many ore types at a few percent each and small yields, not with rarity. Rare metals are not rare in its ore maps, they are low-yield.

**Finding 2: the abundance-based plan was far off:** nickel and cobalt 5-50 times too scarce, gold 20-150 times, silicon about half, iron up to twice too much, ice 1.5-5 times too much. The share of each mineral was therefore fitted per body (a weight for each mineral, kept as close to the realistic value as possible; impact and meteorite minerals only up to 8 times; oil shale kept on Earth-like and Titan) so that every ingot reaches the **Better Stone rate** (uranium too, at the full Better Stone rate). Realism now comes from host rocks and regions (section 6), not from thin shares.

| Body | Ingot | Better Stone | Plan before | Plan after |
|---|---|---|---|---|
| Earth-like | Iron | 11.223 | 17.884 | 10.696 |
| Earth-like | Nickel | 4.193 | 0.370 | 3.799 |
| Earth-like | Cobalt | 2.923 | 0.407 | 2.770 |
| Earth-like | Magnesium | 0.177 | 0.110 | 0.166 |
| Earth-like | Silicon | 15.904 | 8.928 | 10.037 |
| Earth-like | Ice | 15.042 | 27.489 | 20.724 |
| Earth-like | Gold | 0.093 | 0.005 | 0.086 |
| Earth-like | Silver | 0.699 | 0.245 | 0.644 |
| Mars | Iron | 9.291 | 23.187 | 10.071 |
| Mars | Nickel | 3.586 | 0.365 | 3.471 |
| Mars | Cobalt | 1.907 | 0.158 | 1.844 |
| Mars | Magnesium | 0.142 | 0.271 | 0.144 |
| Mars | Silicon | 12.100 | 7.473 | 10.767 |
| Mars | Ice | 27.320 | 37.294 | 30.988 |
| Mars | Gold | 0.050 | 0.001 | 0.048 |
| Mars | Silver | 0.477 | 0.060 | 0.455 |
| Mars | Uranium | 0.040 | 0.002 | 0.039 |
| Moon | Iron | 12.686 | 24.810 | 16.353 |
| Moon | Nickel | 9.161 | 1.130 | 8.883 |
| Moon | Cobalt | 2.818 | 0.426 | 2.765 |
| Moon | Magnesium | 0.110 | 0.245 | 0.111 |
| Moon | Silicon | 11.427 | 5.881 | 10.201 |
| Moon | Ice | 7.620 | 23.844 | 8.449 |
| Moon | Gold | 0.048 | 0.001 | 0.047 |
| Moon | Silver | 0.494 | 0.021 | 0.489 |
| Moon | Platinum | 0.023 | 0.002 | 0.022 |
| Moon | Uranium | 0.038 | 0.006 | 0.038 |
| Europa | Iron | 12.675 | 8.980 | 12.618 |
| Europa | Nickel | 9.158 | 0.275 | 7.643 |
| Europa | Cobalt | 2.820 | 0.133 | 2.724 |
| Europa | Magnesium | 0.110 | 0.253 | 0.132 |
| Europa | Silicon | 11.419 | 4.787 | 5.952 |
| Europa | Ice | 7.625 | 45.157 | 17.007 |
| Europa | Gold | 0.048 | 0.000 | 0.046 |
| Europa | Silver | 0.494 | 0.065 | 0.465 |
| Europa | Platinum | 0.023 | 0.000 | 0.022 |
| Europa | Uranium | 0.038 | 0.001 | 0.037 |
| Titan | Iron | 12.675 | 11.274 | 11.908 |
| Titan | Nickel | 9.158 | 0.292 | 7.257 |
| Titan | Cobalt | 2.820 | 0.216 | 2.650 |
| Titan | Magnesium | 0.110 | 0.144 | 0.207 |
| Titan | Silicon | 11.419 | 1.864 | 4.291 |
| Titan | Ice | 7.625 | 33.547 | 10.592 |
| Titan | Gold | 0.048 | 0.001 | 0.045 |
| Titan | Silver | 0.494 | 0.016 | 0.466 |
| Titan | Platinum | 0.023 | 0.001 | 0.021 |
| Titan | Uranium | 0.038 | 0.002 | 0.036 |

Still off by more than 15-20 % after the fit (limited by the ores a body can have and by keeping specialist minerals within 0.4-2.5 times their realistic share): Earth-like Silicon 10.0 vs 15.9; Earth-like Ice 20.7 vs 15.0; Moon Iron 16.4 vs 12.7; Europa Nickel 7.6 vs 9.2; Europa Magnesium 0.1 vs 0.1; Europa Silicon 6.0 vs 11.4; Europa Ice 17.0 vs 7.6; Titan Nickel 7.3 vs 9.2; Titan Magnesium 0.2 vs 0.1; Titan Silicon 4.3 vs 11.4; Titan Ice 10.6 vs 7.6. Silicon and ice trade against each other (Quartz is the main silicon ore and gives 1.1 Ice per ore); Better Stone's own ice is by-product only on all planets except Mars.

**Asteroids:** Better Stone's rebalanced list (companion mod) gives gold, silver, platinum and uranium about 1 / 103 = 0.97 % each, with the other ores summing to 82. In the plan they are weight 2 of 158 = 1.3 % each (platinum minerals Cooperite 3, Niggliite 2, Sperrylite 2), still under the original Better Stone share of about 1.6 %.

