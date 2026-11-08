# Ore Redistribution – Linux native (Avalonia)

Linux port of [`SpaceEngineersOreRedistribution`](../SpaceEngineersOreRedistribution) (originally by michi84o, WPF, Windows only).
The original folder is untouched; this folder is a separate, self-contained project.

## What is the same
All logic is unchanged: planet definition parsing, map viewer, ore inspector, ore redistribution,
biome rewrite, occlusion maps, ore overview, presets. Output files are identical in format.

## What is different
| Original (WPF) | Linux port |
|---|---|
| WPF windows / XAML | [Avalonia 11](https://avaloniaui.net) (`.axaml`), runs on X11 and Wayland (XWayland) |
| `Viewport3D` for the ore inspector | Small software 3D renderer (`Ore3dViewport.cs`): perspective, translucent boxes, depth sorted |
| Blocking `MessageBox` / `OpenFileDialog` | Same call shape, implemented on Avalonia dialogs (`Dialogs.cs`) |
| "Redistribute Ore" asked for a folder via a fake *Save as `specifyFolder.png`* dialog | Real folder picker |
| `explorer.exe` opened after redistribution | `xdg-open` on the output folder |
| Presets saved next to the exe | `~/.config/OreRedistLinux/OreTierPresets.xml` (seeded from the shipped file) |
| Windows case-insensitive paths | `PlanetDataFiles/<Planet>` and map file names are matched case-insensitively |
| ImageSharp 3.1.12 | Same. (3.2+ needs a paid Six Labors license for Release builds. NuGet warns about advisories for 3.1.12; the tool only reads PNGs you chose yourself.) |

Menu bar differences: Avalonia menus cannot hold check boxes/combo boxes, so *Ore Inspector*, *Size*, *Tile Width*
moved into a toolbar next to the menu. *Map* options are checkable menu items.

## Added in the Linux version: ore colors
- With no ore type selected, every ore type is drawn in its own color (48 default colors, chosen to stay apart from
  each other and readable on the gray height/slope map and the green biome view; first colors are the most distinct,
  after 48 ore types the colors repeat).
- Each entry in the *Ore Types* list ends with a color tag. Click it to pick your own color, *Reset to default* undoes it.
  Custom colors are saved by ore name in `~/.config/OreRedistLinux/OreColors.xml`.
- Selecting an ore type works like the original: its depth tiers get the tier colors and all other ore is dimmed.

Also fixed: random depth for ore without a preferred depth used `VeryDeepOre ? 9 : 10` as the (exclusive) upper bound, which is
backwards: with *Very Deep Ore* ticked depth 9 never spawned, and with it unticked depth 9 (disabled) could still be picked, so a share of
that ore's random picks started on a disabled layer (measured: ~0.7% fewer ore pixels overall; with the box ticked, depth 9 got 12k pixels
instead of 27k). Both fixes can be switched back to the original behavior in the **Debug** menu
(*Original depth weighting*, *Original Very Deep Ore range*), in case a setup was tuned around them. The choice is remembered in
`~/.config/OreRedistLinux/settings.xml`. The depth weighting bug: with a *Preferred Depth* the weights were summed twice per step
(`weightSum += weightSum += w`), which skewed veins toward shallow layers (preferred depth 5 peaked at 3).
Unhandled errors are written to `~/.config/OreRedistLinux/crash.log` and shown in a dialog.

Also fixed: the six cube faces were loaded in parallel into a non-thread-safe dictionary, which could randomly drop a face
(ore statistics too low, face without ore overlay).

## Water Mod support (keep ore out of the water)
Planets that use the [Water Mod](https://steamcommunity.com/sharedfiles/filedetails/?id=2200451495) have a global water sphere,
so ore below it can only be reached by digging into water. The new **Water** menu (and *Map > Show Water*) deal with that.

- **Settings**: read from `WaterConfig.xml` in the same folder as the planet definition (matched by `SubtypeId`), otherwise enter them
  yourself. Values are remembered per planet in `~/.config/OreRedistLinux/water.xml`.
- **Geometry** (checked in the Water Mod scripts and the game's own code): water radius = `MinimumRadius x Radius`,
  `MinimumRadius = R x (1 + HillParams.Min)`, terrain radius = `R x (1 + Min + (Max - Min) x height)`. Ore `Start`/`Depth` are plain
  meters below the surface and do not scale with the planet; the terrain height does, so the **planet radius** (not stored in any
  file, default 60 km = EarthLike) is needed.
- **Safety margin**: tide + wave height by default (editable), because the water level moves.
- **Per ore** (*Water* column in the redistribution window, *Water, all ores* + *Apply* sets every ore at once):
  - *Land only (dry)* (default): the whole ore layer (`Start + Depth`) must end above water + margin. Where the wanted layer is too deep
    for a pixel the next shallower layer is used (clamped); if no layer fits, the pixel is not used. Veins that creep towards the sea
    step back and follow the shore, so they still reach their size.
  - *Land + seabed*: both rules at once, decided per pixel. On land the dry rule above applies, under standing water (oceans, lakes)
    any layer is allowed. Veins cross the shore freely. This avoids the bunching on islands of water planets: on Typhoon the ore pixel
    total is the same as without water rules (368.9k vs 368.9k) while every land layer stays dry.
  - *Seabed only*: only where the terrain is below the water line.
  - *Anywhere (ignore water)*.
- **Land / seabed balance** (Water window, for *Land + seabed* ores): *By area* follows the size of land and sea, nudged by a
  **land preference** (default 1.5: land counts 1.5 times in the dice; 1 = purely by area; below 1 favors the seabed). On EarthLike with
  44% water the land share of the veins is 54% at 1, 63% at 1.5, 71% at 4. *Fixed* sends a chosen share of the veins to the seabed.
  Deep layers still end up on the seabed wherever the land is too low for them. If a cube face lacks one kind of ground the other is used.
- **Per ore and per layer overrides** (*Land %*, only for ores set to *Land + seabed*): the **Land %** column in the ore table overrides the
  baseline above for that ore, the **Land %** column in the layer list overrides it again for one layer (layer, else ore, else baseline).
  Empty = inherit, **100 = land only, 0 = seabed only**, anything in between = the share of veins that start on land, with the other kind of
  ground as fallback. 0 and 100 are also enforced pixel by pixel, so a "seabed only" layer does not creep onto the shore. Example: layers 0-2 at 0
  and layers 6-9 at 100 gives shallow ore under the sea and deep ore under the land (EarthLike with 44% water: those layers came out at exactly
  0% / 100% land); 95 on a whole ore gave 93% of its ore on land, 5 gave 6%. If a strict layer finds no suitable ground the vein is dropped
  and shows in the report. Saved with *Export* / *Import*.
- **Keeping the ore ratios** (Water window, on by default): the tool first works out what it would place *without* water rules
  (target pixels per ore and layer, and the normal ore density = ore pixels per surface pixel). Then:
  - a vein keeps the layer the dice chose and looks for a site where that layer is dry (land pixels sorted by height, so deep layers
    land in the highlands), instead of being pushed to a shallower layer;
  - land never gets more than *Most ore on land* x the normal density (default 1.25, measured per 128 x 128 block, a soft limit). When land is
    full, *Land + seabed* ores go to the seabed instead ("Diverted" in the report), *Land only* ores lose the vein;
  - veins prefer empty pixels, so they do not paint over other ore;
  - after the main pass, whatever is still missing per ore and layer (cut-off veins, overlap) is topped up with extra veins where
    the ore's mode allows it (Land + seabed ores can use the seabed).
  Typhoon, Land + seabed, by area: every ore and layer at 98-100% of the target (layers 7-9: 29.7% of the ore, target 29.8%),
  land and seabed at 0.97x / 1.00x of the normal density, no wet land layer. With a fixed 30% seabed share: 97-113% of target,
  land density 1.16x. *Land only* on that ocean world gets only ~5% of the target (land is 3.8% of the surface, capped at 1.25x density),
  with the layer proportions preserved and the shortfall listed; set those ores to *Land + seabed*, or raise the limit (0 = none:
  everything piles up on the islands at 27x normal density).
- *Allow shallower layers* (off by default) brings back the old way out: a layer that does not fit is replaced by the next shallower one
  that does. That fills the land but shifts the rarity (layer 9: 1.2% of the ore instead of 7.4%).
- If an ore still cannot reach its target, the message after the run and `placement_report.txt` (next to the maps) list it, with
  veins wanted / placed / dropped, where they started (land / seabed), diverted and top-up veins, and the achieved % of target per layer.
- The water window shows how much of the planet is underwater and on how much of the surface a layer ending at 10 / 25 / 50 / 100 / 200 / 400 m
  stays dry. Not covered: the Perlin distortion some planets add to the terrain (use the safety margin) and the planet's own lake layer.

Also changed: the same seed now always gives the same maps (the faces were visited in the order they happened to finish loading,
which changed the random number stream); height maps that are not 16 bit (8 bit and palette PNGs, used by some mods) are now converted instead of being replaced
by a blank map, and ore map pixels whose value has no ore mapping are no longer painted green (Typhoon uses 0 for "no ore").

The output folder is no longer opened after *Redistribute Ore* (the original opened Explorer). *File > Open output folder when finished*
turns it back on.

## Switching layers off (more ores under the 254 limit)
Every ore keeps its ten layers, because the vein generator is built around them. In the mapping list under the ore table each layer has an
**On** checkbox and shows the **Number** it gets in `oremappings.xml`: a layer that is off gets no number (`off`), one removed by
Very Deep Ore or Typ. Depth Min/Max shows `cut`. The *Layers* column shows how many layers each ore uses (e.g. 7/10), the bottom line the total
(*Values Count*, max 254) and the number of ores, so you can keep it under the limit by hand. Veins skip layers that are off: random depth
picks only use layers that are on, and a vein that wanders onto a switched-off layer uses the nearest one that is on. With nothing switched
off the maps are identical to before. The switches are saved with *Export* / loaded with *Import*.

## Convenience
- **Autosave** (`~/.config/OreRedistLinux/Setups/<planet>/`): while the redistribution window is open the setup is autosaved every 5 minutes (only when
  something changed), when the window is closed without confirming, and before a saved setup replaces it. Confirming saves the **last run**.
  The last run and the newest 5 autosaves are kept per planet. Nothing is ever loaded by itself: *Load saved* in the setup window lists them
  (and *From a file...*); Export / Import use the same format.
- **Open Recent** in the File menu (last 10 planet definitions), **drag and drop** a `.sbc` onto the window, **Ctrl+O**. File pickers start in the
  last used folder, the output folder picker in the last output folder.
- **Redistribute Ore** asks for the output folder after you confirm the setup, runs in the background with a progress bar and a Cancel button, and
  writes all files only at the end (cancelling leaves the folder untouched). The window stays usable-looking and never "freezes".
- **Map**: the whole map is fitted when a planet's maps load (**Fit** / **100%** buttons, Ctrl+0 / Ctrl+1), and the status bar shows what is under the
  mouse: face and pixel, height (and in meters / above or under water when the hill range is known), ore and layer, biome and material.
- **Setup window**: compact columns (long names as tooltips), all ten layers visible, the help text in a collapsible *Help*, and the reason why Confirm is
  not possible (too many values, no ore) in red under the buttons.

## Planet size and the distance between ore patches
*Randomly spawn ore every N pixels* used to mean the same number of pixels on every planet, but a pixel is about 42 m wide on a 60 km radius
planet and four times that on a 240 km one, so ore was far apart on big planets and close together on small ones. The number is now tied to the
**planet diameter** shown in the redistribution window (the same value as the planet radius in the Water window, remembered per planet):
**1000 is the default for the EarthLike size, 120 km diameter / 60 km radius**, and when you change the diameter (Enter or click elsewhere) N is
recalculated so the patches stay the same distance apart in meters (240 km: 250, 60 km: 4000). A line shows the resulting distance, about 1.3 km at the
default. A number you type yourself is kept as the wanted distance when you change the size and back, so nothing drifts; the tile width is
included the same way (4096 pixel tiles: 4 times as many pixels). The **Ore density** box next to it is linked to that number: density = the default number of pixels for this planet size / your number, so on EarthLike
2.0 = 500 pixels, 0.5 = 2000, and you can edit either one and watch the other (density applies on Enter or when you click elsewhere, the pixel box
immediately). The density stays the same when you change the planet size. Saved setups (autosave, Export) remember the size they were made for and are converted
to the planet they are loaded on. The engine is unchanged: it uses the number shown. At the default size the maps are identical to before.
(The 100 km in the original code was a *radius* and only used for the slope map; the spawn number never had a size.) The lowest allowed value is now 20
(was 100) because planets larger than about 190 km radius need it. 

**Ore patch size** follows the planet size too (*Scale ore patches with the planet size*, on by default; it replaces the manual *Small Planet Mode* of the
original tool). The Size column is the number of pixels of a vein on the reference planet (EarthLike: 60 km radius, 2048 pixel tiles). A pixel covers
(radius / tile width)^2 of ground, so the pixels per vein are multiplied by the same factor as the spawn number: on a 60 km diameter planet a vein of 15
becomes 60 pixels, on a 240 km one 3.8, and the vein covers the same ground (about 27,000 m2) everywhere. A line under the checkbox shows the factor.
At the reference size the maps are identical to before. Switch it off to draw exactly the pixels in the Size column. The size of globs and rock patches is in
km on the planet and needs no scaling.

## Globbing (ores cluster around ore of the same kind)
Real ore is not scattered evenly: it occurs in regions where it is common and is rare elsewhere. Every ore has a **Glob %** (ore table; 0 = evenly
spread, which is the default and what the tool always did, 100 = only inside its globs). Below the ore table, **globs** sets their size (default
15 km) and how much of the planet an ore fills at full strength (default 20 %). Each ore gets its own smooth random field over the planet (3-D noise on the
sphere, so globs continue across the edges of the cube map and different ores have different regions); a new vein is accepted at a place with a chance that
depends on that field and the strength (outside the globs: (1 - strength)^3, so 25 % is already visible, 50 % clear, 75 % strong). A vein that is not accepted
looks for another place, so **the number of veins and the ore ratios do not change, only where they are**; the same seed gives the same maps. Works with the water
rules (the glob test is part of the site search, tried first) and the top-up. On EarthLike, Iron_02: the share of the ore found in its fullest fifth of the map cells
goes 36 % (0) / 41 % (25) / 58 % (50) / 79 % (75) / 83 % (100), empty cells 2 % to 63 %, total ore pixels unchanged. Saved in setups. With every ore at 0 the maps
are identical to before.

## Geology (ore follows simulated rock types and regions)
An alternative to globbing: in the redistribution window set **Ore placement** to *Geology* (globbing stays the default, all ores at 0). The tool then
simulates where kinds of rock are found and places each ore where the rock suits it. It is not the exact rock (granite, quartz...), only the kind:
sedimentary, metamorphic (low / high grade), igneous extrusive (volcanic surface rock) and igneous intrusive (cooled underground).

* **Regions** (map): *Land*, *Seabed* (ground under the water line when the Water rules are on, otherwise under the lakes, red channel "Lake areas" value),
  *Uplift* (the highest part of the land by height, default 15 %) and *Volcanic* (only where you paint it). Seabed, uplift and land can be painted too.
* **Rock layers**: *Surface* = ore layers 1-3, *Layer 1* = 4-6, *Layer 2* = 7-10. Every region has a recipe (weights of the five rock types per rock layer).
  Defaults after the geology guide: land is sedimentary on top over metamorphic rock of rising grade, the seabed is igneous, uplift shows metamorphic and
  intrusive rock at the surface, volcanic areas are extrusive over intrusive.
* **Per ore** the *Rocks* button (setup window) gives weights per rock type (0 = never, 100 = normal). A vein is placed where the rock of its layer suits
  the ore, so an ore of high grade metamorphic rock is deep under plain land but near the surface in uplift regions.
* Menu **Geology**: *Show regions* (overlay on the map), *Paint regions...* (brush, eraser, brush size, undo / redo; left mouse paints, middle mouse moves the
  map) and *Geology settings...* (uplift share, size of the rock patches, the recipes). The hover text of the map shows the region.
* Stored per planet in `~/.config/OreRedistLinux/Geology/<planet>/` (`geology.xml`, `paint.png`). The placement report gets a geology section.
* **Ore regions** (like globbing, but rock aware): in geology mode the **Glob %** column of an ore means regional strength: 0 = the ore may occur anywhere its rock
  is, 100 = it only occurs in a few regions (gold in a greenstone belt, platinum in one big intrusion). Each ore can have its own region size (**Glob km**) and
  share of the planet (**Cov %**); empty = the values of the globbing row. The setting *ore regions follow the rock* (default 70 %) decides how much the place
  of a region depends on the rock that suits the ore: at 0 a region is a plain glob wherever the noise puts it, at 100 regions lie where the ore's rock is.
  The rock still decides what a vein can be made of, a region only makes a place more likely. Output with Glob 0 is unchanged.
* Water rules, ratios and density limits still apply; veins are counted by the rock layer they start in. Output with Globbing / geology off is unchanged.

## Ore data packs (rock types and rarity per ore)
The **Data packs...** button in the redistribution window applies ore data from pack files: the rock types an ore forms in (used by the geology mode)
and / or the Weight of each ore from its abundance in the Earth's crust, relative to the other ores of the list: weight = (abundance / highest abundance)
^ strength. Strength 1 = the real ratios (iron about 14 million times as common as gold), 0 = all equal, the default 0.25 compresses the range so rare ores
still appear. Tick the packs to use; where two packs have the same ore, the later one (lower *Order*) wins. Ores that no selected pack knows are left alone
(their Weight is scaled along so the share stays the same). Your choice is remembered.

Packs are plain XML files in `~/.config/OreRedistLinux/DataPacks/`. The three built-in packs (**Vanilla**, **Better Resources** workshop 2841960221,
**Better Stone** workshop 406244471) are exported there on first run and again whenever they are missing; an edited pack is never overwritten. To make your
own, copy one, change its `Name` and the ores, and save it in that folder (*Reload packs* in the window picks it up). Format (`vanilla.xml` explains it too):

```xml
<OreDataPack Format="1" Tool="OreRedistLinux" Name="My pack" Author="me" Order="30" Description="...">
  <Ore Name="Iron" Abundance="56300" Rocks="80;60;50;15;40" />   <!-- Sed;MetL;MetH;Extr;Intr, 0-1000 -->
</OreDataPack>
```
`Name` matches the ore name from the planet definition (`Iron` matches `Iron_02`, `Iron_02` only that one); `Abundance` (ppm), `Rocks` and the regional values are all optional. `Glob` (0-100) is the regional strength, `GlobSize` (km) and `GlobCoverage` (% of the planet) size the regions, e.g. `Glob="75" GlobSize="12" GlobCoverage="12"`. The Data packs window has a checkbox for applying them. Built-in packs that were exported by an older version keep their old content (delete the file to get the new one back).

Packs are data only. A file is loaded only if its root element is `OreDataPack` with `Format="1"` and `Tool="OreRedistLinux"`; files with DOCTYPE or
external entities, over 1 MB, symbolic links and anything malformed are skipped (and listed in the window). Ore names must match a strict pattern, numbers are
range checked, text is only displayed, and nothing in a pack is ever executed or used as a path.

## Build and run
Needs the .NET 8 SDK (or newer; `dotnet --list-sdks`). On CachyOS/Arch: `sudo pacman -S dotnet-sdk`.

```bash
./run.sh            # build + start
./publish.sh        # self-contained build in ./dist, no .NET install needed to run it
```

## Typical Linux paths
Space Engineers (Steam, native or Proton) keeps mods in the Wine prefix, e.g.
`~/.local/share/Steam/steamapps/compatdata/244850/pfx/drive_c/users/steamuser/AppData/Roaming/SpaceEngineers/Mods/<Mod>/Data/PlanetGeneratorDefinitions.sbc`
Use *File → Open Planet Definition* and pick that file; the maps are read from the `PlanetDataFiles/<Planet>` folder.
(the tool looks for `PlanetDataFiles` in the same folder as the opened `.sbc`).
Vanilla definitions: `<SteamLibrary>/steamapps/common/SpaceEngineers/Content/Data/PlanetGeneratorDefinitions.sbc`.

## Credits / license
Original tool and algorithms: michi84o, MIT License (see `../LICENSE`). Port to Avalonia: blazeblue32 with Claude.
