# Handoff — where we left off (2026-10-15, laptop)

Read this together with `CLAUDE.md` and `Doc/Narrative.md` at the start of the next session.
What was discussed and decided along the way: `Doc/Sessions/2026-10-15_laptop.md`.

## Built this session

**Scenes** (`Assets/Demo/Scenes/`)
- `MainMenu` — start screen (background screenshot, Start / Settings with volume sliders / Quit). Start loads `Level_0`.
  Rebuild with Tools > UI > Build Main Menu Scene; new background with Tools > UI > Capture Menu Background.
- `Levels/Level_0` — **Act 1 surface**: terrain exactly 200×200 (procedurally sculpted + splatted with the
  bootcamp-4 TerrainPainting assets: 8 layers, ~850 trees, grass textures, detail meshes, rocks, lake with a
  shoreline boundary, wind, fog, sun lens flare, TerrainDemoProfile post-processing, bird/wind ambience on the
  Ambience channel). Path from the start past 3 recruitment posters to the "SAFE FUTURE PROGRAM" building at
  the cliff; inside, S.A.F.E.-T's waiver line (Yarn node `Surface_Elevator`), then a lift goes 16 m **down**
  through a terrain hole into `Level_1`. Terrain data: `Assets/Demo/Surface/`.
- `Levels/Level_Test` — **Chamber 1 "Welcome"** (teaches portals): built from the `Chamber` prefab; pit with
  ToxicWater, ledge with two white portal panels, plate opens the exit door, S.A.F.E.-T lines
  (`Chamber01_Welcome`, `Chamber01_Ledge`), security camera. Plan: rename it to `Level_1` (and the old
  `Level_1` to something else) so the lift from `Level_0` arrives in it.

**Systems / tools**
- Level kit (`Tools > Portal > Build Level Kit`): now also `Hazards/ToxicWater` and `Chamber/` —
  `Chamber.prefab` (`ChamberShell`: size in panels, openings, ceiling light strips, chamber sign number),
  `LiftConnector_Arrival/_Exit` (lift + closed lift room + corridor + round door; entrance door opens on
  approach; clean Portal dressing in the lift room), `ChamberSign.prefab`.
- `KillZone` (water/goo): fall in from above → fade, respawn on last safe ground; cubes return to origin.
- `ScreenFade` (persistent fade), lift transitions preload the next level and fade; `LevelManager` fades in
  when no arrival lift does.
- Portals now ignore every solid collider right behind them (white panel on a black wall works).
- `RespawnBehaviour`: `Kill()`, safe-ground tracking, no deaths while carried by a lift.

## Still to do (next steps)
- Rename `Level_Test` → `Level_1`; build Chamber 2 (Wes + cube/plate/dispenser) with the `Chamber` prefab.
- Sculpt/paint the surface terrain a bit **by hand** too (rubric: built-in sculpting tools).
- Ending on the surface: sunset variant (Viking Village sky, warm low sun) + maintenance hatch + end trigger.
- Decide on the decayed look per chamber (lift `Decoration` on/off, RatMan decals in chamber 4).
- In-game HUD (progress), pause menu, playtests.
- Credits for the music (source unknown — see `Demo/Audio/Music`).

## Desktop setup (once)
1. `git pull` in this repo.
2. Unity MCP (lets Claude drive the editor): the package is in `Packages/manifest.json`. Unity runs in the NixOS
   unityhub sandbox, which needs Python: `/etc/nixos/modules/home/apps.nix` has
   `unityhub.override { extraPkgs = pkgs: [ pkgs.python3 ]; }` — commit/push that NixOS change on the laptop,
   pull it on the desktop, then `sudo nixos-rebuild switch --flake /etc/nixos#desktop`.
3. Open the project, Window > MCP for Unity > Start Server (HTTP on `127.0.0.1:8080`).
4. In this folder: `claude mcp add --transport http --scope local unityMCP http://127.0.0.1:8080/mcp`,
   then start Claude Code.
