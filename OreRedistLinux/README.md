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

Also fixed: the six cube faces were loaded in parallel into a non-thread-safe dictionary, which could randomly drop a face
(ore statistics too low, face without ore overlay).

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
