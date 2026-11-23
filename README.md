# Operation Tidy-Up: Mission Poop-astrophe (Godot 4.7.2 Mono / C#)

Levels 1 (living room) and 2 (garden party) of the game. Level 2 adds the scraper (key 3) for dried mess, dirty bucket water, bigger puddles and an earlier, harder Buba panic with several exploding special drops. Pick the level on the title screen; finishing Level 1 offers 'Next level'. Everything is built in code (no art assets needed).

## Run
1. Open Godot 4.7.2 **.NET/Mono** editor -> Import -> select `project.godot`.
2. If the build complains about the SDK or target framework, edit `OperationTidyUp.csproj`
   (`Godot.NET.Sdk/<your version>`, `TargetFramework` net8.0 or what the editor suggests).
3. Press F5. Build errors on first compile are possible: this code was written without a compiler.

## Controls
W/S walk, A/D turn, Q/E sidestep, mouse look, Space action (hold to scrub), 1/2/F tool, M mute,
N minimap, L language (EN / PT-PT), Esc pause, Enter/R restart.
Touch: left thumb joystick (up/down walk, left/right turn), drag right side to look up/down, action button, tool chips.
T toggles the touch UI on desktop for testing.

## Optional
Put a photo at `art/custodio.png` to replace the 3D bust of Sr. Custódio.

## Export
Android / iOS / desktop work. Godot 4 C# projects cannot export to Web.

## Android export (Godot 4.7.2 .NET)
1. Install JDK 17 and the Android SDK (command-line tools, platform-tools, build-tools 34, platform 34, NDK r26+).
   In Godot: Editor > Editor Settings > Export > Android, set the Java SDK path and Android SDK path.
2. Project > Install Android Build Template (needed for C# / Gradle builds).
3. Project > Export > Add > Android. Set a unique package (e.g. `com.yourname.tidyup`), enable architectures
   `arm64-v8a` (and `armeabi-v7a` only if you need old phones), and tick "Use Gradle Build".
4. Debug keystore: Godot can use the default debug key. For Play Store releases create your own keystore with `keytool`.
5. Export APK for testing (adb install), or AAB for the Play Store.
Tip: the game already pauses when the app loses focus and uses the touch UI automatically on phones.

## Linux export
Project > Export > Add > Linux. Export templates for 4.7.2 .NET must be installed
(Editor > Manage Export Templates). Output is a self-contained binary plus a .pck.

## Free 3D assets (Kenney furniture kit + animal-dog)
Models load automatically from `res://art/`; anything missing falls back to the built-in shapes.
- `art/furniture/`: loungeSofa, tableGlass, books, bookcaseClosedWide, chairCushion, rugRound, pottedPlant,
  plantSmall1/2/3, trashcan (the 4 bins, tinted in their colour), televisionModern (all `.glb`)
- `art/dog/animal-dog.glb`: Buba. Idle/walk/run animations are picked by name; the names found are printed
  in the Output panel ("Buba animations: ...").
Models are auto-fitted to the room's footprints (collisions don't change). If one faces the wrong way, change its
yaw constant at the top of `Scripts/Assets.cs` (add 180). Kenney's licence is CC0; keep the licence files if you like.


## Menu
The game opens on a main menu (Play, Levels, Settings, Quit) over a slow camera pan of the level. Progress (unlocked levels) and the minimap setting are saved in user://save.cfg; language is saved in user://settings.cfg. The pause screen has a 'Main menu' button.
