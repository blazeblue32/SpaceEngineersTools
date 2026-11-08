# Composite minerals for the Better Stone + Better Resources pack

Proposal, 2026-11-09. Nothing here is built yet. Amounts are starting values per 1 ore (vanilla ores give 0.007-0.7 per ore, Better Stone 0.001-0.9), to be tuned.

## Ingots that exist (so what an ore can be made of)

* Vanilla: Iron, Nickel, Cobalt, Magnesium, Silicon, Silver, Gold, Platinum, Uranium (and Stone, Scrap).
* Better Resources adds: Aluminium, Copper, Titanium, Lithium, Niobium, Tungsten, Tin, Lead, Zinc, Gallium, Sulfur, Carbon, Nitrogen, **Nitre** (potassium nitrate powder), **Perchlorate**, Plutonium (and Steel, Brass, Plastic etc. that are made, not mined).
* Not ingots: K, Cl, As, P, Ca, O. In the old list below, K is replaced by Nitre (KNO3) and Cl by Perchlorate. Calcium, arsenic and phosphorus minerals stay as they are.

## Slots

* **Freed by removing pure ores that do not exist in nature:** vanilla `Cobalt_01`, `Nickel_01`, `Magnesium_01`, `Silicon_01` (4 slots). Kept because they occur natively or are a real mineral: Gold_01, Silver_01, Platinum_01, Uraninite_01.
* **Better Resources slots** (8 materials after Galena is merged into Better Stone's): Aluminium_01, Copper_01, Titanium_01, Lithium_01, Wolframite_01, Nitre_01, Sulfur_01, Petroleum_01. They stay materials, get new names and extra ingots.
* Voxel materials: vanilla 62 + Better Stone 25 + Better Resources 8 = 95, as before. The four freed vanilla slots become new composite minerals, no extra materials.

## The table

Type: **Adjust** = same mineral, ingots added (name stays); **Rename** = same recipe, better name; **Replace** = the slot becomes a different mineral; **Keep** = unchanged.

### Better Stone minerals (kept, missing ingots added)

| Mineral (slot) | Current ingots per ore | Proposed | Type |
|---|---|---|---|
| Iron (Fe) (`Iron_01`) | 0.4 Fe, 0.5 Ice | Hematite (Fe, Ti): + 0.02 Ti | Rename + adjust |
| Dense Iron (Fe+) (`Iron_02`) | 0.9 Fe | Magnetite (Fe, Ti): + 0.03 Ti | Rename + adjust |
| Hapkeite (Fe,Si) | 0.35 Fe, 0.45 Si | as is (Fe2Si) | Keep |
| Heazlewoodite (Ni) | 0.4 Ni, 0.15 Ice | + 0.15 S (Ni3S2) -> (Ni,S) | Adjust |
| Porphyry (Au) | 0.006 Au | + 0.10 Cu, + 0.005 Ag (copper-gold porphyry) -> (Au,Cu,Ag) | Adjust |
| Dolomite (Mg) | 0.007 Mg | + 0.10 C (carbonate) -> (Mg,C) | Adjust |
| Cattierite (Co) | 0.27 Co | + 0.15 S (CoS2) -> (Co,S) | Adjust |
| Niggliite (Pt) | 0.003 Pt | + 0.10 Sn (PtSn) -> (Pt,Sn) | Adjust |
| Carnotite (U) | 0.001 U, 0.572 Ice | + 0.05 Nitre (the potassium) -> (U,K) | Adjust |
| Glaucodot (Fe,Co) | 0.234 Fe, 0.4 Co | + 0.15 S -> (Fe,Co,S) | Adjust |
| Sinoite (Si) | 0.62 Si, 0.44 Ice | + 0.15 N (Si2N2O) -> (Si,N) | Adjust |
| Pyrite (Fe,Au) | 0.55 Fe, 0.001 Au | swap Au for S: 0.55 Fe + 0.35 S (FeS2; gold stays in Porphyry, Electrum) -> (Fe,S) | Adjust (swap) |
| Olivine (Si,Mg) | 0.18 Si, 0.008 Mg, 0.35 Ice | + 0.10 Fe ((Mg,Fe)2SiO4) -> (Si,Mg,Fe) | Adjust |
| Cooperite (Ni,Pt) | 0.36 Ni, 0.002 Pt | + 0.15 S (PtS) -> (Ni,Pt,S) | Adjust |
| Quartz (Si) | 0.49 Si, 1.12 Ice | as is (SiO2) | Keep |
| Galena (Ag) (`Galena_01`, merged with Better Resources') | 0.035 Ag | Better Resources' recipe: 0.3 Pb, 0.1 Ag, 0.1 Zn -> (Pb,Ag,Zn) | Rewrite of the recipe, name kept |
| Chlorargyrite (Ag) | 0.064 Ag, 0.002 Ice | + 0.03 Perchlorate (the chlorine) -> (Ag,Cl) | Adjust |
| Electrum (Au,Ag) | 0.045 Ag, 0.004 Au | as is (natural alloy) | Keep |
| Sperrylite (Pt) | 0.003 Pt | as is (the arsenic has no ingot) | Keep |
| Autunite (U) | 0.004 U | as is (calcium and phosphate have no ingot) | Keep |
| Akimotoite (Si,Mg) | 0.22 Si, 0.005 Mg | + 0.10 Fe ((Mg,Fe)SiO3) -> (Si,Mg,Fe) | Adjust |
| Wadsleyite (Si,Mg) | 0.30 Si, 0.003 Mg | + 0.10 Fe ((Mg,Fe)2SiO4) -> (Si,Mg,Fe) | Adjust |
| Taenite (Fe,Ni) | 0.35 Fe, 0.2 Ni | as is (natural alloy) | Keep |
| Cohenite (Ni,Co) | 0.21 Ni, 0.18 Co | + 0.15 Fe, + 0.08 C ((Fe,Ni,Co)3C) -> (Fe,Ni,Co,C) | Adjust |
| Kamacite (Fe,Ni,Co) | 0.284 Fe, 0.174 Ni, 0.1 Co | as is (natural alloy) | Keep |
| Uraniaurite (U,Au) | 0.003 Au, 0.003 U | as is | Keep |
| Petzite (Ag,Au) | 0.045 Ag, 0.004 Au | as is (the tellurium has no ingot) | Keep |

### Vanilla ores (slots)

| Ore (slot) | Current | Proposed | Type |
|---|---|---|---|
| Cobalt (`Cobalt_01`, cyan-blue texture) | 0.3 Co (no pure cobalt in nature) | **Azurite (Cu, C)**: 0.35 Cu + 0.10 C | Replace |
| Nickel (`Nickel_01`, tan-olive texture) | 0.4 Ni (no pure nickel in nature) | **Sphalerite (Zn, S, Ga)**: 0.30 Zn + 0.15 S + 0.02 Ga | Replace |
| Magnesium (`Magnesium_01`, dark blue-grey) | 0.007 Mg (no pure magnesium in nature) | **Ilmenite (Fe, Ti)**: 0.30 Fe + 0.25 Ti | Replace |
| Silicon (`Silicon_01`, light grey) | 0.7 Si (no pure silicon in nature) | **Spodumene (Li, Al, Si)**: 0.12 Li + 0.20 Al + 0.30 Si | Replace |
| Gold (`Gold_01`) | 0.01 Au | native gold: as is | Keep |
| Silver (`Silver_01`) | 0.1 Ag | native silver: as is | Keep |
| Platinum (`Platinum_01`) | 0.005 Pt | native platinum: as is | Keep |
| Uraninite (`Uraninite_01`) | 0.01 U (Better Resources: 0.1 U + 0.01 Pu) | UO2 is a real mineral; + 0.02 Pb (radiogenic lead) -> (U,Pb); decide on the plutonium | Adjust |
| Ice | 1 Ice | as is | Keep |

### Better Resources ores (kept as materials, new names, ingots added)

| Ore (slot) | Current ingots per ore | Proposed | Type |
|---|---|---|---|
| Aluminium (Bauxite) (`Aluminium_01`) | 0.4 Al, 0.05 Ga | **Bauxite (Al, Ti, Ga)**: + 0.03 Ti (bauxite carries TiO2) | Adjust |
| Copper (`Copper_01`, teal and orange patina) | 0.5 Cu | **Native Copper (Cu, Ag)**: + 0.005 Ag | Rename + adjust |
| Titanium (`Titanium_01`, a pure titanium ore does not exist) | 0.2 Ti | **Rutile (Ti, Nb)**: 0.35 Ti + 0.01 Nb | Replace |
| Lithium (`Lithium_01`, mauve texture, no pure lithium ore) | 0.2 Li, 0.05 Nb | **Lepidolite (Li, Al, K)**: 0.15 Li + 0.10 Al + 0.05 Nitre + 0.05 Nb | Replace |
| Wolframite (`Wolframite_01`) | 0.3 W, 0.2 Sn | + 0.10 Fe ((Fe,Mn)WO4) + 0.01 Nb -> (W,Sn,Fe,Nb) | Adjust |
| Nitre (`Nitre_01`) | 0.5 Nitre | **Niter (K, N)**: + 0.10 N, + 0.02 Perchlorate (Atacama type) | Adjust |
| Sulfur (`Sulfur_01`) | 0.5 S | native sulfur: as is | Keep |
| Petroleum (Shale) (`Petroleum_01`) | oil / petroleum | oil shale: + 0.10 C, + 0.05 S | Adjust |
| Galena (`Galena_01`, Better Resources' definition) | 0.3 Pb, 0.1 Ag, 0.1 Zn | removed: merged into Better Stone's Galena above | Remove (merge) |

## Ingot coverage after the changes

| Ingot | Sources (ores that give it) |
|---|---|
| Fe | Hematite, Magnetite, Pyrite, Ilmenite, Taenite, Kamacite, Hapkeite, Glaucodot, Cohenite, Olivine, Akimotoite, Wadsleyite, Wolframite |
| Ni | Heazlewoodite, Cooperite, Taenite, Kamacite, Cohenite |
| Co | Cattierite, Glaucodot, Cohenite, Kamacite |
| Mg | Dolomite, Olivine, Akimotoite, Wadsleyite |
| Si | Quartz, Olivine, Sinoite, Hapkeite, Akimotoite, Wadsleyite, Spodumene |
| Ag / Au / Pt | Galena, Chlorargyrite, Electrum, Petzite, silver, native Cu (Ag) / Porphyry, Electrum, Petzite, Uraniaurite, gold / Cooperite, Niggliite, Sperrylite, platinum |
| U | Uraninite, Autunite, Carnotite, Uraniaurite |
| Cu | Native Copper, Azurite, Porphyry |
| Zn | Sphalerite, Galena |
| Pb | Galena, Uraninite (trace) |
| Ga | Bauxite, Sphalerite |
| Sn | Wolframite, Niggliite |
| W | Wolframite |
| Al | Bauxite, Spodumene, Lepidolite |
| Ti | Rutile, Ilmenite, Bauxite, Hematite, Magnetite |
| Li | Spodumene, Lepidolite |
| Nb | Lepidolite, Rutile, Wolframite |
| S | Sulfur, Pyrite, Sphalerite, Heazlewoodite, Cattierite, Glaucodot, Cooperite, Petroleum |
| C | Azurite, Dolomite, Cohenite, Petroleum |
| N | Sinoite, Niter |
| Nitre (K) | Niter, Carnotite, Lepidolite |
| Perchlorate (Cl) | Chlorargyrite, Niter |

Every ingot has at least two sources except Tungsten (Wolframite only, one ore type; fine for a rare ingot) and Plutonium (not mined).

## Notes and open points

* The tool treats the **material id**, not the new name. For the data packs the four replaced vanilla slots and the renamed Better Resources slots need their own entries under the old id (`Cobalt_01` = Azurite and so on); a fourth data pack with a higher Order, written after you confirm the list, does that: rocks, abundance, vein size and regions per new mineral.
* Natural abundance and host rock of the new minerals (for the geology mode): Azurite and Native Copper: Earth-like oxidised copper zones; Sphalerite: with Galena (hydrothermal, Mississippi Valley); Ilmenite and Rutile: layered intrusions, beach sands, Moon mare basalt; Spodumene and Lepidolite: pegmatites; Bauxite: tropical laterite; Niter: arid evaporites.
* Changing a vanilla slot affects every planet in the save that uses it (see section 11 of MODPACK_REPORT.md).
* Refining recipes for all changed ores have to be written or edited in the mod (the tool does not touch them).
