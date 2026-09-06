# Big Monster Game

A Unity arena game: a single arena with collectible cubes, a chasing enemy, a dash mechanic, and a complete menu/HUD flow.

Opens with **Unity 6000.4.9f1**. Scene: `Assets/RollablePlus/Scenes/Rollable Enhanced.unity`

![Scene preview](Scene%20preview.png)

This project is a standalone copy of the original Rollable project; the original was left untouched. All 1,323 original asset files (scenes, scripts, materials, prefabs, models, audio, and metadata) were verified identical by SHA-256 comparison.

Start with **PROJECT_GUIDE.md**: it walks through the scene objects, the audio slots, and the code flow with short examples.

## Getting started

1. Unity Hub → **Add / Add project from disk**, then select this `Rollable_Enhanced` folder.
2. Open it with **Unity 6000.4.9f1**. Wait for the initial asset import to finish.
3. The enhanced scene is selected automatically on first open. Even if you inspect a different scene in the editor, **Play** starts the enhanced scene by default. If you need to restore that, use **Rollable → Open Enhanced Arena**.
4. Press **Play**, then choose **Play**.

The new scene is `Assets/RollablePlus/Scenes/Rollable Enhanced.unity`.
If you update the project while Unity is open, stop Play mode and start it again. **Quit** only exits Play mode in the editor; in a built game it closes the application.

Visual additions and the UI are now **saved into the scene**. Scene View, and Game View before entering Play, both show the enhanced look. The hierarchy consists of ordinary game objects: `Arena`, `Player`, `Enemy`, `Pickups`, `Exit`, `Main Camera`, `Directional Light`, `UI`, and `Game Manager`. The `ENHANCEMENTS` object, the old Canvas, and the Bootstrap object that used to build the scene were removed. Generated materials, ring meshes, and the font live in `Assets/RollablePlus/Generated`. Play uses these saved objects. The setup code that generated the scene and UI at runtime is gone.

- `Arena`: floor, walls, obstacles, pushable boxes, and floor/light details as plain meshes.
- `Pickups`: four collectible cubes.
- `Exit`: the exit ring, its pedestal, and the exit label.
- `UI`: `Main Menu`, `HUD`, `Pause Menu`, `Results`, and `EventSystem`. The Collected and Time labels are inside `HUD`.
- `Game Manager`: rounds, collection, win/loss, and audio settings; music and particle effects are parented under it.

The previous enhanced scene and its setup code are preserved in the first commit of the Git history under `Backups/Before Hierarchy Cleanup`; the original project was not modified.

## Preserved choices

- The original arena layout, walls, and moving boxes.
- The turquoise player model, purple floor, grey walls, and pale yellow spinning collectible cubes.
- The positions of the four collectibles and the original NavMesh data.
- The selected `Music_Background06` track, the `SFX_Win03` win sound, and the collect and loss effects.
- The project's entire asset archive and license files.

To go back to the original scene, choose **Rollable → Open Original Mini Game**. That option also switches the Play start scene back. To play the enhanced version again, choose **Rollable → Open Enhanced Arena**. `Assets/Scenes/Mini Game.unity` and `SampleScene.unity` are unchanged.

## What was added

- Surfaces derived from the existing colors, lit wall edges, floor lines, corner lights, and energy rings beneath the cubes.
- Ring geometry, a portal, a motion trail, and a dash sound generated specifically for this project. No external assets were downloaded.
- A clean start screen, fully in English, with the original **Big Monster Game** title and the selected Orbitron font.
- Plain **Collected** and **Time** labels on the game screen, plus small health/round and dash indicators. The score is shown only on the results screen.
- A single **Menu** button covering **Resume**, **Music: On/Off**, **Sound Effects: On/Off**, a **Volume** slider, **Visual Effects: Full/Reduced**, controls, **Restart**, **Main Menu**, and **Quit**.
- The volume level affects both music and sound effects, while music and effects can be muted independently. Both audio preferences, the shared volume level, and the visual effects preference are saved.
- Physics-based movement with a speed cap, and a dash that recharges in 2.2 seconds.
- Three shields, 2 seconds of protection after taking damage, and contact immunity during a dash.
- Three rounds in the same original arena with the chaser speeding up each round; each round asks you to collect four cubes and reach the exit ring to the north.
- A chain bonus for fast pickups, a round bonus, and a locally stored high score.
- Pause, restart, return to main menu, audio toggles, and a reduced-effects option.

## Controls

| Input | Action |
|---|---|
| WASD / arrow keys | Move |
| Space | Dash |
| Esc / Menu | Open / close the menu; pauses during play |
| R (in the pause menu) | Restart the game from round one |
| Menu → Music / Sound Effects / Volume | Toggle audio / adjust the level |
| Enter | Activate the selected menu button |
| Gamepad left stick | Move |
| Gamepad south button | Dash / confirm menu selection |
| Gamepad Start | Open / close the menu |

## Verification status

The new game code and editor helpers **compile without errors** against the installed Unity 6000.4.9f1 Roslyn compiler and the project references. A pre-existing name-shadowing warning in the older `VFX_OnCollect.cs` file was left as it was.

**Play Mode playtesting was not completed. The editor view was built and checked inside Unity afterwards.** Unity's IL Post Processor service reported a `GetDomainName: -1` error on a system query the environment does not permit. A successful compile is not a guarantee that gameplay behaves correctly at runtime. `Scene preview.png` is a real scene preview captured from the Unity camera without entering Play mode; it does not substitute for a playtest.

The `RollableVerification` development check covers movement, dash, pause, damage protection, physical collection, three rounds, win/loss, restart, and settings. That check could not be run in this session. It does not activate during normal play and is not included in player builds.

## Project layout

All new code lives under `Assets/RollablePlus`. Caches, temporary compiler files, and the 2 GB `Library` folder are not part of this copy; Unity regenerates them on open.

## Inspector layout

The audio sources are saved into the scene: `Game Manager/Music` and `Game Manager/Sound Effects`. On the `Arena Game` component, the characters, exit, audio sources, audio clips, and visual effects each have their own heading. Change event sounds through the Sound clips fields. It is normal for the clip field on the Sound Effects AudioSource to be empty; the relevant clip is played from code via PlayOneShot. Music and sound effects toggle independently.

A single EventSystem is kept under UI. `Player (1)` and `Ground (1)`, along with its seven duplicate obstacles, were removed. The main player and arena were kept. A backup from before this change is in the first commit of the Git history under `Backups/BeforeInspectorCleanup`.

Collect and damage particles are visible in both Full and Reduced modes. Reduced dials back camera bloom/shake and shortens the player trail; it does not disable core gameplay effects. The win sound plays once, only when a round is completed. Opening the exit does not play an additional win sound.
