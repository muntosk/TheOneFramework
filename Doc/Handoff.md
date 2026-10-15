# Handoff — where we left off (2026-10-15, desktop)

Read this together with `CLAUDE.md` and `Doc/Narrative.md` at the start of the next session.
What was discussed and decided along the way: `Doc/Sessions/2026-10-15_laptop.md` (first session that day)
and `Doc/Sessions/2026-10-15_desktop.md` (second session, this handoff).

## Level order (scene names matter)
`LevelManager` loads the next level by name: `Level_` + (current number + 1). So:
- `MainMenu` → `Levels/Level_0` (surface) → lift down → `Levels/Level_1` (Chamber 1 "Welcome") → `Levels/Level_2`
  (Chamber 2 "The neighbour") → `Level_3` (doesn't exist yet).
- The old test levels are in `Levels/Old/` (`Old_Level_1`, `Old_Level_2`) and out of the build list.

## Current state
**Chamber 1 (`Level_1`)** — portals intro. Starts **without** the portal gun: it lies spinning on a pedestal next to
the spawn (`Puzzle/PortalGunPedestal`, `PortalGunPickup`). Walking into it equips the gun and starts Yarn node
`Chamber01_Gun` (mouse buttons, white/black surfaces, goo). Pit with ToxicWater, ledge, plate → exit door.

**Chamber 2 (`Level_2`)** — built with the `Chamber` prefab (6×7×4 panels, sign 02):
- Player side: dispenser drops a cube on a 4.9 m balcony at start; two white panels (floor level + above the balcony)
  so you need portals to get up; cube on the `ExitPlate` (box plate) opens the exit door + line `Chamber02_Plate`.
- Wes' room behind a glass partition: `WesRoom/Wes_Placeholder` = the player's Exo Gray model with the StarterAssets
  idle (**replace with the Mixamo Wes**), his own plate + cube, a security camera on him. Trigger `Talk_Wes` by the
  window starts `Chamber02_Wes` (two choice moments). Intro line `Chamber02_Intro` explains F to pick up the cube.

**Lift rooms** — both looks are on in every chamber: the clean Portal dressing (`Dressing_Clean`) plus the lift
prefab's own `Decoration` (vines/debris/grime). Controlled by `ChamberShell.liftDecoration` (default on).

## Systems added this session
- **Portal gun pick-up**: `PortalGun.equippedAtStart` (off in `Level_0` and `Level_1`, on by default elsewhere);
  once picked up it stays for the next levels (static flag, reset by Start Game in the main menu). Not equipped =
  no firing, no crosshair, no gun model.
- **Holding the gun**: prefab `Core/Portal/Prefabs/Props/PortalGun.prefab` (pivot in the middle, barrel +Z, white side
  up). In the hand: `PlayerRig/Player/HeldPortalGun`, parented to the right hand bone at runtime
  (`PortalGun.heldPosition/heldRotation`). Upper-body pose from Mixamo `Rifle Idle` on animator layer `PortalGun`
  (own controller `Core/PlayerOverrides/Animations/PlayerRig.controller` + `UpperBody.mask`), faded in when equipped.
- **First person (V)**: a copy of the gun in front of the camera, Portal-style, on layer `ViewModel` (portal cameras
  skip it); the hand gun hides with the body. Tune with `PortalGun.viewModelPosition/Rotation`.
- **Controls**: right mouse button now also fires the orange portal (middle still works). E = continue dialogue and
  **choose the highlighted option** (W/S to move); key hints under the dialogue boxes (`DialogueKeyControls`,
  attaches itself in every scene). F = pick up/drop, LMB while carrying = throw.
- **Portals**: no more overhang past the edge of a white panel (edges/corners must be on a portal surface, otherwise
  the portal slides inward). Fixed walking through **black walls** after using a portal (stale wall list after a
  warp + "in the portal" check without a depth limit). Cube exits a portal without being shoved by the wall behind.
- **Cube**: now 0.75 m (was 0.95, almost as wide as the 1 m portal). Carrying it into a portal shows it out of the
  other portal (like Portal) and it comes along when you walk through; you can also pick a cube up **through** a
  portal (F while looking through it).

## Still to do (next steps)
- **Play Chamber 1 → 2 once yourself** and check: walking/running with the gun (animation layer), first-person view
  near walls (the view model can poke through walls), carrying through portals, the Wes conversation with E.
- Mixamo **Wes** (+ volunteers) to replace `Wes_Placeholder`.
- Sculpt/paint the surface terrain a bit **by hand** (rubric: built-in sculpting tools — Raise/Lower, Smooth, Set
  Height, Paint Texture; take screenshots as proof).
- Chamber 3 (fizzler, S.A.F.E.-T slips) and 4 (broken panels, decals) with the `Chamber` prefab.
- Ending on the surface: sunset variant + maintenance hatch + end trigger.
- In-game HUD (progress), pause menu, playtests, credits for the music (source unknown, `Demo/Audio/Music`).

## Machine setup notes
- **Laptop**: Unity MCP already set up there (uv in `~/.local/bin`, `unityMCP` registered). Just `git pull`, open the
  project, Window > MCP for Unity > Start Server, start Claude Code in this folder.
- **Desktop** (done 2026-10-15): uv via `nix profile add nixpkgs#uv` (not in the NixOS config yet — later add `pkgs.uv`
  to `extraPkgs` of unityhub in `/etc/nixos/modules/home/apps.nix`); `claude mcp add --transport http --scope local
  unityMCP http://127.0.0.1:8080/mcp` (per machine, restart Claude Code after adding).
- The new layer `ViewModel` is in `ProjectSettings/TagManager.asset` (committed with the project).
