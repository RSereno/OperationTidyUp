# Operation Tidy-Up: Mission Poop-astrophe (Godot 4.7.2 Mono / C#)

Levels 1 (living room), 2 (garden party) and 3 (kitchen, with the detergent spray on key 4: 8 charges, works at a distance, shrinks puddles and softens dried mess; Dona Ermelinda arrives early at half way) of the game. Level 2 adds the scraper (key 3) for dried mess, dirty bucket water, bigger puddles and an earlier, harder Buba panic with several exploding special drops. Pick the level on the title screen; finishing Level 1 offers 'Next level'. Everything is built in code (no art assets needed).

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

## Level 4: The Inspection (boss)
Dona Ermelinda enters after 20 s and walks a fixed route of 6 marked spots (gold ring on the floor and minimap), inspecting each for 3.5 s with her white glove. Any mess near her is a strike (3 strikes restart the level). Buba keeps dropping new poops and puddles. Clear all 6 spots to win.

## Shop and coins
Earn 2 coins for every poop dumped or puddle cleaned (4 for Buba's special drops). The shop is in the main menu, the pause screen and the victory panel; the list scrolls. Spend them in the main menu Shop: wide shovel (25, carry 2 poops of the same colour), big bucket (20, mop water lasts longer), non-slip slippers (30), spray refill pack (40, +4 charges), giant shovel (60, carry 3, needs the wide shovel). With a bigger shovel, press the action near another poop of the same colour (away from a bin) to stack it; a bump or slip spills the whole stack. Coins and purchases are saved in user://save.cfg.

## Dropping what you carry
Press the same action button while carrying, away from any bin and with nothing stackable in reach, to put the poop down in front of you, e.g. to grab a different colour first. A stack from the wide/giant shovel is dropped as separate poops.
