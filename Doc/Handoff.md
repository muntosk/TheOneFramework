# Handoff — where we left off (2026-10-16, laptop)

Read this together with `CLAUDE.md` and `Doc/Narrative.md` at the start of the next session.
What was discussed and decided along the way: `Doc/Sessions/2026-10-16_laptop.md` (this session); earlier ones in
`Doc/Sessions/`.

## Level order (scene names matter)
`LevelManager` loads the next level by name: `Level_` + (current number + 1). The whole game is playable end to end:
- `MainMenu` → `Level_0` (surface, lift down) → `Level_1` (Chamber 1 "Welcome", portal gun) → `Level_2` (Chamber 2
  "The neighbour", cube + Wes) → `Level_3` (Chamber 3, fizzler) → `Level_4` (Chamber 4, cracks + fold-out bridge) → `Level_5` (Chamber 5 "Momentum", flinging) →
  `Level_6` (the window, claw, conveyor, escape) → `Level_7` (surface at sunset, ending) → end card → `MainMenu`.
- `001_portal_test_level1` is still in the build list (old test scene, harmless — remove when convenient).

## Current state (what's new this session)
**Chamber 3 (`Level_3`)** — full-width fizzler; white panel front-left + back-right (shoot the far one through the
field), carry the cube round it. Lines `Chamber03_*` (intro, cube erased, plate = first slip).

**Chamber 4 (`Level_4`)** — exit on a ledge split by a toxic trench; cube on the ledge plate folds out a two-panel
bridge (`Platform_FromWall`, made black/non-portalable). Second white panel hidden in a crawlspace behind a broken
false wall (`Crawlspace/`, Rat Man graffiti, `BrokenEdges`, BTS concrete `Lining`, orange lamp). Exit fizzler.

**`Level_6` — the window and the escape**
- Observation room (`ChamberShell` with the new right-wall window opening) over a hall of robot copies
  (`Hall/RobotCopies`: patrols + hurdlers on self-keyframed route clips, 33 waiting units facing the window).
  `Sequence/Trigger_Reveal` locks the entrance, cross-fades `LevelMusic` to `Reveal_Tension`, starts
  `Chamber05_Reveal` (now explains *why*: humans hand over their data without asking what it's for).
- `<<grab_player Claw>>` → ceiling claw (`Claw`, `ClawGrab`) drops the player at `Disposal/DropOff` on the conveyor
  (`Disposal/Belt`, `ConveyorBelt`) towards the incinerator (`Disposal/Incinerator`: particles on/off by distance,
  KillZone → back to the belt start).
- Escape: white panel beside the belt + one in the maintenance room seen through the high opening. In the maintenance
  room the exit-lift door is locked; Wes calls out, you bang on the window down into his room
  (`Maintenance/Window_Wes`, `Maintenance_Wes`), he turns and unlocks `Trigger_WesOpensExit`.
- Training rooms seen from the belt (`Disposal/TrainingRooms`): Volunteer_A/B/C and **Wes = Exo Gray placeholders**.

**`Level_7` — ending at sunset**
- Copy of Level_0: `Skybox_Sunset`, warm fog, low sun, no music (nature ambience only). The entrance lift
  (`Lift_Up`) brings you *up*.
- Guidance: green lightsticks along the existing path (`Gameplay/Ending/Lightsticks`), Wes' campfire with a smoke
  column at the west shore of the lake, S.A.F.E.-T's hint, Wes calls you when near (`Surface_WesCalls`).
- `Ending` node: talk to Wes (2 choices) → S.A.F.E.-T: training complete, deployment begins → robot copies
  (`Gameplay/Ending/Deployment`, `RobotCopy_Deploy` + `FollowTerrainHeight`) walk down the path in your step, camera
  turns to them (`<<look_at Unit_5 1.2 14>>`) → `<<end_game Ending>>` (`GameEnding`: end card, then MainMenu).
  Credits text is neutral — fill in names/music credits in `GameEnding.credits`.

## New systems / changes worth knowing
- Core: `ConveyorBelt`, `LevelMusic` (Music channel, `CrossFadeTo`), `ChamberShell` window opening, fizzler only
  erases what you carry (+ `onObjectFizzled`), `CubeDispenser.respawnWhenLost`, speech bubbles draw on top of walls.
- Lifts carry the rider robustly (direct shift + `Physics.SyncTransforms`); fall protection is frame-based too.
  `LevelManager` follows every loaded scene's number (restarting from the menu works).
- Demo: `Demo/RobotCopies/` (3 self-keyframed clips: Patrol, Hurdles, Deploy), `Demo/Escape/` (ClawGrab,
  Incinerator, GameEnding, InterruptingDialogueTrigger, EscapeYarnCommands `turn_to`/`look_at`, volunteer prefabs).
- Yarn commands added: `grab_player`, `end_game`, `turn_to`, `look_at`. `turn_to`/`look_at` don't make Yarn wait on
  purpose (a waiting command of an interrupted conversation would skip a line of the next one).

## Still to do (next steps)
- **Play the whole game once yourself** (especially Level_6 → 7) and run **playtest 1** (≥ 4 people, document it).
- Mixamo **Wes** + volunteers: replace the `Body` child of the `Wes`/`Volunteer_*` objects (Level_2 `Wes_Placeholder`,
  Level_6 `Disposal/TrainingRooms`, Level_7 `Gameplay/Ending/Wes`). Keep the object names — Yarn finds them by name.
- Open the self-keyframed clips in the Animation window and tweak a few keys yourself (rubric: ≥ 2 self-keyframed).
- Sculpt/paint the surface terrain **by hand** (rubric; screenshots as proof) — e.g. flatten the campfire spot.
- Music in Chambers 1–4 (`LevelMusic` with `Chambers_Calm_01/02`) and maybe `Escape.ogg` on the belt.
- GLaDOS-style TTS for S.A.F.E.-T: the user has a file somewhere (not found on the laptop) — ask where it is.
- In-game HUD/pause menu, credits for the music (source unknown, `Demo/Audio/Music`).
- Robots at the very end keep their walk cycle after stopping (only seconds before the end card; fine for now).

## Machine setup notes
- **Laptop**: Unity MCP set up (uv in `~/.local/bin`, `unityMCP` registered). `git pull`, open the project,
  Window > MCP for Unity > Start Server, start Claude Code in this folder.
- **Desktop** (done 2026-10-15): uv via `nix profile add nixpkgs#uv` (not in the NixOS config yet — later add `pkgs.uv`
  to `extraPkgs` of unityhub in `/etc/nixos/modules/home/apps.nix`); `claude mcp add --transport http --scope local
  unityMCP http://127.0.0.1:8080/mcp` (per machine, restart Claude Code after adding).
