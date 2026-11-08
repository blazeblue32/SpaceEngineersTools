# Where each mineral appears, and how rare it is

Proposal, 2026-11-09, for the Better Stone + Better Resources pack with the composite minerals of `MODPACK_COMPOSITES.md`. Planet shares are **estimates** of the share of each mineral in the ore pixels of a planet, computed with the tool's own rule (share ~ abundance^0.25 within the planet's ore list, times a per-body factor); Ice is a fixed share. Nothing is built yet.

## 1. Principles

* **Real geology decides first.** A body gets a mineral only if it forms there (native metals and sulfides on wet Earth-likes, Fe-Ni metal and impact minerals on airless bodies, hydrocarbons on Titan...). Where a planet would otherwise lack an ingot completely, a very rare trace entry keeps it obtainable (marked as trace).
* **Rarity tiers (share of the ore on that body):** Abundant 8 % and up, Common 3-8 %, Uncommon 1-3 %, Rare 0.3-1 %, Very rare under 0.3 %.
* **Host rock, vein size and regions come from the data packs** (`Rocks`, `Size`, `Glob`, `GlobSize`, `GlobCoverage`); the table in section 5 lists them per mineral. Regions are in km on the planet, so they scale to each body's size; change Glob km per planet if a moon is much smaller.
* **Per body ore lists**, each with its own 254-number budget; the lists here have 15-40 ores, so 6-12 layers per ore is possible on the small ones.

## 2. Planets and moons

Vanilla planets: EarthLike, Alien, Mars, Moon, Europa, Titan, Triton, Pertam. Vanilla places only 8 ore types on each (Iron_02, Nickel, Cobalt, Magnesium, Silicon, Silver, Gold, Ice; Platinum only on Moon, Europa, Titan; Uranium nowhere, only in asteroids).

### Earth-like (EarthLike, Alien)

Everything forms here (wet crust, hydrothermal and sedimentary systems, pegmatites, laterites, evaporites). Meteoritic and impact minerals only in crater regions (tiny regions). Ice only at the poles (3 %). **Alien** uses the same list; make Niter and Sulfur ×2 (volcanic / arid) and Petroleum ×0.5 if you want it to feel different. **Pertam** (arid, no ice in vanilla): the Earth-like list without Ice, Petroleum, Spodumene and Lepidolite, Niter ×3 and Sulfur ×2.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Quartz | `Quartz_01` | 14.58 | Abundant | Si |
| Hematite (Iron) | `Iron_01` | 11.08 | Abundant | Fe, Ti |
| Bauxite | `Aluminium_01` | 10.31 | Abundant | Al, Ti, Ga |
| Dolomite | `Dolomite_01` | 9.32 | Abundant | Mg, C |
| Magnetite (Dense Iron) | `Iron_02` | 8.81 | Abundant | Fe, Ti |
| Ilmenite | `Magnesium_01` | 5.80 | Common | Fe, Ti |
| Olivine | `Olivine_01` | 5.16 | Common | Si, Mg, Fe |
| Pyrite | `Pyrite_01` | 4.41 | Common | Fe, S |
| Rutile | `Titanium_01` | 3.50 | Common | Ti, Nb |
| Ice | `Ice_01 / Ice_02` | 3.00 | Common | Ice |
| Oil shale (Petroleum) | `Petroleum_01` | 2.48 | Uncommon | C, S, Oil |
| Sulfur (native) | `Sulfur_01` | 2.37 | Uncommon | S |
| Sphalerite | `Nickel_01` | 2.27 | Uncommon | Zn, S, Ga |
| Spodumene | `Silicon_01` | 1.92 | Uncommon | Li, Al, Si |
| Native Copper | `Copper_01` | 1.83 | Uncommon | Cu, Ag |
| Azurite | `Cobalt_01` | 1.83 | Uncommon | Cu, C |
| Galena | `Galena_01` | 1.57 | Uncommon | Pb, Ag, Zn |
| Lepidolite | `Lithium_01` | 1.53 | Uncommon | Li, Al, Nitre, Nb |
| Wolframite | `Wolframite_01` | 0.93 | Rare | W, Sn, Fe, Nb |
| Niter | `Nitre_01` | 0.92 | Rare | Nitre, N, Perchlorate |
| Heazlewoodite | `Heazlewoodite_01` | 0.78 | Rare | Ni, S |
| Uraninite | `Uraninite_01` | 0.78 | Rare | U, Pb |
| Glaucodot | `Glaucodot_01` | 0.66 | Rare | Fe, Co, S |
| Autunite | `Autunite_01` | 0.58 | Rare | U |
| Carnotite | `Carnotite_01` | 0.58 | Rare | U, Nitre |
| Cattierite | `Cattierite_01` | 0.44 | Rare | Co, S |
| Silver (native) | `Silver_01` | 0.41 | Rare | Ag |
| Porphyry | `Porphyry_01` | 0.25 | Very rare | Au, Cu, Ag |
| Chlorargyrite | `Chlorargyrite_01` | 0.25 | Very rare | Ag, Perchlorate |
| Platinum (native) | `Platinum_01` | 0.21 | Very rare | Pt |
| Gold (native) | `Gold_01` | 0.20 | Very rare | Au |
| Cooperite | `Cooperite_01` | 0.17 | Very rare | Ni, Pt, S |
| Electrum | `Electrum_01` | 0.17 | Very rare | Ag, Au |
| Sperrylite | `Sperrylite_01` | 0.17 | Very rare | Pt |
| Petzite | `Petzite_01` | 0.14 | Very rare | Ag, Au |
| Niggliite | `Niggliite_01` | 0.14 | Very rare | Pt, Sn |
| Uraniaurite | `Uraniaurite_01` | 0.14 | Very rare | Au, U |
| Kamacite | `Kamacite_01` | 0.13 | Very rare (trace entry) | Fe, Ni, Co |
| Taenite | `Taenite_01` | 0.12 | Very rare (trace entry) | Fe, Ni |
| Akimotoite | `Akimotoite_01` | 0.05 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.05 | Very rare (trace entry) | Si, Mg, Fe |

41 ores.

### Mars

Basaltic crust, iron oxides (hematite) and sulfate / carbonate salts; hydrothermal ore only near volcanoes and craters (trace). Polar Ice 6 %. Aluminium and lithium minerals are absent (no laterites or pegmatites).

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Hematite (Iron) | `Iron_01` | 32.05 | Abundant | Fe, Ti |
| Olivine | `Olivine_01` | 29.83 | Abundant | Si, Mg, Fe |
| Sulfur (native) | `Sulfur_01` | 6.53 | Common | S |
| Ice | `Ice_01 / Ice_02` | 6.00 | Common | Ice |
| Dolomite | `Dolomite_01` | 4.49 | Common | Mg, C |
| Magnetite (Dense Iron) | `Iron_02` | 4.25 | Common | Fe, Ti |
| Pyrite | `Pyrite_01` | 4.25 | Common | Fe, S |
| Quartz | `Quartz_01` | 4.22 | Common (trace entry) | Si |
| Ilmenite | `Magnesium_01` | 2.80 | Uncommon | Fe, Ti |
| Niter | `Nitre_01` | 1.77 | Uncommon | Nitre, N, Perchlorate |
| Sphalerite | `Nickel_01` | 0.66 | Rare (trace entry) | Zn, S, Ga |
| Kamacite | `Kamacite_01` | 0.51 | Rare | Fe, Ni, Co |
| Galena | `Galena_01` | 0.45 | Rare (trace entry) | Pb, Ag, Zn |
| Taenite | `Taenite_01` | 0.45 | Rare | Fe, Ni |
| Heazlewoodite | `Heazlewoodite_01` | 0.38 | Rare | Ni, S |
| Native Copper | `Copper_01` | 0.35 | Rare (trace entry) | Cu, Ag |
| Uraninite | `Uraninite_01` | 0.23 | Very rare (trace entry) | U, Pb |
| Glaucodot | `Glaucodot_01` | 0.19 | Very rare (trace entry) | Fe, Co, S |
| Cohenite | `Cohenite_01` | 0.17 | Very rare | Fe, Ni, Co, C |
| Silver (native) | `Silver_01` | 0.12 | Very rare (trace entry) | Ag |
| Akimotoite | `Akimotoite_01` | 0.07 | Very rare (trace entry) | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.07 | Very rare (trace entry) | Si, Mg, Fe |
| Gold (native) | `Gold_01` | 0.06 | Very rare (trace entry) | Au |
| Cooperite | `Cooperite_01` | 0.05 | Very rare (trace entry) | Ni, Pt, S |
| Platinum (native) | `Platinum_01` | 0.04 | Very rare (trace entry) | Pt |
| Electrum | `Electrum_01` | 0.03 | Very rare (trace entry) | Ag, Au |

26 ores.

### Moon

Anorthosite highlands and mare basalt: ilmenite is the titanium ore, olivine and the high-pressure impact minerals, meteoritic Fe-Ni metal and PGE minerals from impactors, troilite (Pyrite) and KREEP uranium (weak). Polar Ice 5 %. **Gap:** no aluminium mineral (anorthite is the real one; see section 7). Gold and silver are trace only, kept so the ingots stay obtainable.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ilmenite | `Magnesium_01` | 34.02 | Abundant | Fe, Ti |
| Olivine | `Olivine_01` | 30.25 | Abundant | Si, Mg, Fe |
| Hematite (Iron) | `Iron_01` | 16.25 | Abundant | Fe, Ti |
| Pyrite | `Pyrite_01` | 6.46 | Common | Fe, S |
| Ice | `Ice_01 / Ice_02` | 5.00 | Common | Ice |
| Kamacite | `Kamacite_01` | 1.93 | Uncommon | Fe, Ni, Co |
| Taenite | `Taenite_01` | 1.70 | Uncommon | Fe, Ni |
| Cohenite | `Cohenite_01` | 0.65 | Rare | Fe, Ni, Co, C |
| Uraninite | `Uraninite_01` | 0.57 | Rare | U, Pb |
| Heazlewoodite | `Heazlewoodite_01` | 0.57 | Rare | Ni, S |
| Hapkeite | `Hapkeite_01` | 0.41 | Rare | Fe, Si |
| Akimotoite | `Akimotoite_01` | 0.36 | Rare | Si, Mg, Fe |
| Wadsleyite | `Wadsleyite_01` | 0.36 | Rare | Si, Mg, Fe |
| Glaucodot | `Glaucodot_01` | 0.29 | Very rare (trace entry) | Fe, Co, S |
| Cooperite | `Cooperite_01` | 0.24 | Very rare | Ni, Pt, S |
| Niggliite | `Niggliite_01` | 0.20 | Very rare | Pt, Sn |
| Silver (native) | `Silver_01` | 0.18 | Very rare (trace entry) | Ag |
| Platinum (native) | `Platinum_01` | 0.15 | Very rare | Pt |
| Sperrylite | `Sperrylite_01` | 0.12 | Very rare | Pt |
| Sinoite | `Sinoite_01` | 0.10 | Very rare | Si, N |
| Gold (native) | `Gold_01` | 0.09 | Very rare (trace entry) | Au |
| Electrum | `Electrum_01` | 0.07 | Very rare (trace entry) | Ag, Au |

22 ores.

### Europa

Ice shell over a silicate floor: Ice dominates (35 %), salts and sulfur compounds, sulfide and iron minerals from hydrothermal vents on the sea floor; the rest are traces.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ice | `Ice_01 / Ice_02` | 35.00 | Abundant | Ice |
| Dolomite | `Dolomite_01` | 21.76 | Abundant | Mg, C |
| Olivine | `Olivine_01` | 12.04 | Abundant | Si, Mg, Fe |
| Sulfur (native) | `Sulfur_01` | 11.87 | Abundant | S |
| Pyrite | `Pyrite_01` | 5.14 | Common | Fe, S |
| Magnetite (Dense Iron) | `Iron_02` | 5.14 | Common | Fe, Ti |
| Quartz | `Quartz_01` | 5.11 | Common (trace entry) | Si |
| Sphalerite | `Nickel_01` | 0.79 | Rare (trace entry) | Zn, S, Ga |
| Niter | `Nitre_01` | 0.64 | Rare (trace entry) | Nitre, N, Perchlorate |
| Galena | `Galena_01` | 0.55 | Rare (trace entry) | Pb, Ag, Zn |
| Heazlewoodite | `Heazlewoodite_01` | 0.46 | Rare | Ni, S |
| Native Copper | `Copper_01` | 0.43 | Rare (trace entry) | Cu, Ag |
| Kamacite | `Kamacite_01` | 0.38 | Rare | Fe, Ni, Co |
| Glaucodot | `Glaucodot_01` | 0.23 | Very rare (trace entry) | Fe, Co, S |
| Uraninite | `Uraninite_01` | 0.18 | Very rare (trace entry) | U, Pb |
| Silver (native) | `Silver_01` | 0.10 | Very rare (trace entry) | Ag |
| Platinum (native) | `Platinum_01` | 0.07 | Very rare (trace entry) | Pt |
| Cooperite | `Cooperite_01` | 0.06 | Very rare (trace entry) | Ni, Pt, S |
| Gold (native) | `Gold_01` | 0.05 | Very rare (trace entry) | Au |

19 ores.

### Titan

Ice bedrock with hydrocarbon lakes and dunes: **Petroleum is the main mineral** (6 times its normal weight), Ice 30 %, a few silicate and metal traces. **Triton**: like Europa with Sinoite (nitrogen) added at 1.

| Mineral | Slot | Share % | Tier | Ingots |
|---|---|---|---|---|
| Ice | `Ice_01 / Ice_02` | 30.00 | Abundant | Ice |
| Oil shale (Petroleum) | `Petroleum_01` | 28.72 | Abundant | C, S, Oil |
| Olivine | `Olivine_01` | 9.96 | Abundant | Si, Mg, Fe |
| Dolomite | `Dolomite_01` | 9.00 | Abundant | Mg, C |
| Magnetite (Dense Iron) | `Iron_02` | 8.51 | Abundant | Fe, Ti |
| Sulfur (native) | `Sulfur_01` | 6.55 | Common | S |
| Pyrite | `Pyrite_01` | 4.26 | Common | Fe, S |
| Sphalerite | `Nickel_01` | 0.88 | Rare (trace entry) | Zn, S, Ga |
| Kamacite | `Kamacite_01` | 0.64 | Rare | Fe, Ni, Co |
| Heazlewoodite | `Heazlewoodite_01` | 0.45 | Rare (trace entry) | Ni, S |
| Glaucodot | `Glaucodot_01` | 0.38 | Rare (trace entry) | Fe, Co, S |
| Uraninite | `Uraninite_01` | 0.30 | Rare (trace entry) | U, Pb |
| Silver (native) | `Silver_01` | 0.16 | Very rare (trace entry) | Ag |
| Platinum (native) | `Platinum_01` | 0.12 | Very rare (trace entry) | Pt |
| Gold (native) | `Gold_01` | 0.08 | Very rare (trace entry) | Au |

15 ores.

## 3. Asteroids

Asteroids pick their ore from **every material with `SpawnsInAsteroids` true**, repeated `AsteroidGeneratorSpawnProbabilityMultiplier` times (checked in the game code, see MODPACK_REPORT section 12). A mod can replace these weights. Proposed weights below (the weights are integers; the share is weight / total); **every other mineral is set to `SpawnsInAsteroids` false** (planet-only: bauxite, native copper, azurite, sphalerite, lepidolite, spodumene, rutile, wolframite, niter, galena, the gold / silver / uranium minerals other than the trace ones).

The generator has one list for all asteroids, so the mix is global; asteroid *families* (ice, stony, metallic) would need separate generator versions, which I have not checked.

| Mineral | Weight | Share % | Tier | Note |
|---|---|---|---|---|
| Ice | 12 | 14.0 | Abundant | Ice_02 (only ice asteroids allow it; Ice_01 and Ice_03 stay planet-only) |
| Hematite (Iron) | 10 | 11.6 | Abundant | Iron (Fe), the main asteroid iron |
| Kamacite | 8 | 9.3 | Abundant | Fe-Ni-Co metal |
| Olivine | 8 | 9.3 | Abundant | stony asteroids |
| Taenite | 7 | 8.1 | Abundant | Fe-Ni metal |
| Pyrite | 6 | 7.0 | Common | troilite (FeS) |
| Dolomite | 4 | 4.7 | Common | carbonates in carbonaceous asteroids |
| Cohenite | 3 | 3.5 | Common | iron carbide |
| Sinoite | 3 | 3.5 | Common | enstatite chondrite |
| Sulfur (native) | 3 | 3.5 | Common | carbonaceous |
| Oil shale (Petroleum) | 3 | 3.5 | Common | organics in carbonaceous asteroids |
| Akimotoite | 2 | 2.3 | Uncommon | shock mineral |
| Wadsleyite | 2 | 2.3 | Uncommon | shock mineral |
| Hapkeite | 2 | 2.3 | Uncommon | lunar / stony Fe-Si |
| Heazlewoodite | 2 | 2.3 | Uncommon | Ni sulfide |
| Cooperite | 2 | 2.3 | Uncommon | PGE |
| Magnetite (Dense Iron) | 2 | 2.3 | Uncommon | dense iron fragments |
| Niggliite | 1 | 1.2 | Uncommon | PGE |
| Sperrylite | 1 | 1.2 | Uncommon | PGE |
| Platinum (native) | 1 | 1.2 | Uncommon | native platinum alloy grains |
| Gold (native) | 1 | 1.2 | Uncommon | trace |
| Silver (native) | 1 | 1.2 | Uncommon | trace, as in vanilla |
| Uraninite | 1 | 1.2 | Uncommon | trace, as in vanilla |
| Quartz | 1 | 1.2 | Uncommon | trace |

Total weight 86. Ice is the most common single material (about 14 %), then asteroid iron (Iron (Fe) with a little Ice) and the Fe-Ni metals; precious metals are rare, as in vanilla (gold and platinum weight 1).

## 4. Which ingots can be mined where

Number of different minerals that give each ingot on each body (a dash = none).

| Ingot | Earth-like | Mars | Moon | Europa | Titan | Asteroids |
|---|---|---|---|---|---|---|
| Fe | 11 | 11 | 11 | 5 | 5 | 10 |
| Ni | 4 | 5 | 5 | 3 | 2 | 5 |
| Co | 3 | 3 | 3 | 2 | 2 | 2 |
| Mg | 4 | 4 | 3 | 2 | 2 | 4 |
| Si | 5 | 4 | 5 | 2 | 1 | 6 |
| Ag | 7 | 4 | 2 | 3 | 1 | 1 |
| Au | 5 | 2 | 2 | 1 | 1 | 1 |
| Pt | 4 | 2 | 4 | 2 | 1 | 4 |
| U | 4 | 1 | 1 | 1 | 1 | 1 |
| Cu | 3 | 1 | - | 1 | - | - |
| Al | 3 | - | - | - | - | - |
| Ti | 5 | 3 | 2 | 1 | 1 | 2 |
| Li | 2 | - | - | - | - | - |
| Zn | 2 | 2 | - | 2 | 1 | - |
| Pb | 2 | 2 | 1 | 2 | 1 | 1 |
| S | 8 | 6 | 4 | 6 | 6 | 5 |
| W | 1 | - | - | - | - | - |
| Sn | 2 | - | 1 | - | - | 1 |
| C | 3 | 2 | 1 | 1 | 2 | 3 |
| N | 1 | 1 | 1 | 1 | - | 1 |
| Ice | 1 | 1 | 1 | 1 | 1 | 1 |

**Gaps to look at:** aluminium, lithium and tungsten exist only on Earth-likes (bauxite, spodumene, lepidolite, wolframite), copper, zinc, lead and tin only as traces on Mars, Europa and Titan or not at all on the Moon and in asteroids, nitrogen only from Sinoite and Niter. Items made from Better Resources ingots (aluminium parts, lithium batteries, tungsten) would then be Earth-like-only. See decision 1.

## 5. Host rock, vein size and regions (the data pack values)

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

## 6. Decisions to confirm

1. **Aluminium gap:** aluminium only exists on Earth-likes. Change Spodumene (`Silicon_01`) to **Anorthite (Al, Si)**, a real mineral of the lunar highlands, of basaltic crust (Mars, Moon) and of anorthosite on Earth-likes, with a trace of Bauxite-like clay on Mars? Lithium then comes only from Lepidolite (Earth-likes), tungsten stays Earth-like-only.
2. **Hematite / Magnetite names** for the two iron slots (Better Stone calls them Iron (Fe) and Dense Iron (Fe+)).
3. **Precious-metal traces on airless bodies** (Moon, Europa, Titan, Mars): real only as ppb, kept at 0.2-0.3 % so every ingot stays obtainable; set them to zero if you want strict realism.
4. **Planet-specific ore regions:** the pack values are Earth-scale; on a smaller moon (Europa, Triton) use bigger Cov % or fewer regions in the tool per planet.
5. **Asteroid weights** are one global list; asteroid families would need extra work.
