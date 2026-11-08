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
- **Per ore** (*Water* column in the redistribution window):
  - *Above water* (default): the whole ore layer (`Start + Depth`) must end above water + margin. Where the wanted layer is too deep
    for a pixel the next shallower layer is used (clamped); if no layer fits, the pixel is not used. Veins that creep towards the sea
    step back and follow the shore, so they still reach their size.
  - *Anywhere*: ignore the water.
  - *Under water only*: only where the terrain is below the water line.
- **Not enough dry ground?** A vein whose first site is too wet looks for another site on the same cube face (up to 400 tries), so
  ore keeps its share. If nothing fits at all the vein is dropped and listed in the message and in `placement_report.txt`
  (written next to the maps), together with how many veins moved or were made shallower. On a planet that is mostly ocean the ore
  ends up on the islands, deep layers only on high ground.
- The water window shows how much of the planet is underwater and on how much of the surface a layer ending at 10 / 25 / 50 / 100 / 200 / 400 m
  stays dry. Not covered: the Perlin distortion some planets add to the terrain (use the safety margin) and the planet's own lake layer.

Also changed: height maps that are not 16 bit (8 bit and palette PNGs, used by some mods) are now converted instead of being replaced
by a blank map, and ore map pixels whose value has no ore mapping are no longer painted green (Typhoon uses 0 for "no ore").

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
