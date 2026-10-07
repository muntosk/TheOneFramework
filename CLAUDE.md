# TheOneFramework — Portal-style school project

Unity 6 (6000.6) + URP project for a Saxion minor (Unity Fundamentals + Design Fundamentals).
Started from a teacher's framework (Yarn Spinner dialogue, triggers, inventory, ...) and grew into
the user's own Portal-style puzzle game. The user is Dutch — reply in Dutch.

## Read first
- `Doc/Narrative.md` — the story ("You Are No Longer Required"), game flow, build list and planning.
- `Assets/STRUCTURE.md` — where things go (per system, then per type, PascalCase folders).
- `Doc/UF_self_assessment_*.xlsx` and `Doc/Design Fundamentals Assessment Template*.docx` — the rubrics.
  Every criterion must be at least Sufficient. Notable: terrain exactly 200×200 (UF min, DF max),
  ≥ 2 self-keyframed Animation clips, particles toggled by script, AudioMixer + settings UI,
  playtests (2× ≥ 4 people), plot twist + engaging dialogue for excellent.

## Working conventions
- **Unity is usually open.** Don't edit scenes/prefabs as YAML from outside; write an editor menu
  tool or tell the user what to do in the editor. Moving assets from outside is fine as long as the
  `.meta` file moves along (keeps GUIDs/references).
- **Commits:** one simple sentence, no Co-Authored-By / attribution trailer. Leave `.idea/` out.
  Only commit/push when asked.
- Play every sound through `GameAudio` (`Assets/Core/Audio`), never `AudioSource.PlayClipAtPoint`
  — it routes to the `GameMixer` channels (Sfx/Music/Voice/Ambience) and limits identical sounds.
- `ThirdPersonControllerFixed` scales movement/jump/camera with the player's scale: resize the
  character by scaling `Player` inside `Core/PlayerOverrides/Prefabs/PlayerRig.prefab`
  (~0.78 ≈ Chell's 1.37 m, matching Portal's world scale).
- Several scripts use CRLF line endings (and some a BOM); keep them that way when editing.

## Where the content is
- Puzzle pieces: `Core/Portal/Prefabs/Gameplay/` (door, lift, buttons, plates, dispenser, portals).
- Level kit: `Core/Portal/Prefabs/LevelKit/` (walls/floors/ceilings portalable or not, glass, fizzler, platforms).
- Set dressing: `Demo/Dressing/` — `Models/` (Vines, LiftShaft, Electrical, Ending = conveyor belt +
  incinerator hatch, Facility = security camera + monitors), `Prefabs/Decals/`, `Prefabs/Scatter/`.
- Audio: Portal SFX in `Core/Portal/Audio/`; game audio in `Demo/Audio/` (`Music/`, `Ambience/`,
  `Ending/`, `Announcer/` chimes). Mixer: `Core/Audio/Mixer/GameMixer`.
- Terrain assets (trees, grass, rocks, water, skyboxes): `ThirdParty/TerrainPainting/`.
- UI font: `Demo/UI/Fonts/TitilliumWeb/` (a TextMeshPro font asset still has to be generated).
- Player: `Core/PlayerOverrides/Prefabs/PlayerRig.prefab`. Robot NPCs: the StarterAssets robot model.
- Still missing: human characters (Wes, volunteers) — user downloads them from Mixamo.

## Portal 2 asset pipeline
World scale: 1 Source unit = 0.01905 m (all Portal assets, kit panels and converted models use it).

- **Materials:** Portal 2 `.vmt`/`.vtf` go in `Assets/ThirdParty/Portal2HarunExport/Export/<game path>`
  (git-ignored, too big). In Unity: select `Export` → Tools > Portal 2 > Bake VMTs To Editable Materials
  → `.png` + URP `.mat` in `Baked/` (that part is in git).
- **Models:** `.mdl` → FBX with Blender + the SourceIO addon (only installed on the desktop PC).
- The extracted game files (`~/Downloads/portal2_extracted/pak01_dir/`) and Blender/SourceIO only
  exist on the desktop PC, so new Portal assets are converted there; the laptop works with what's baked.

Editor tools (menu), all re-runnable, updating assets in place:
- Tools > Portal 2 > Build Set Dressing — decals, scatter prefabs, remaps converted model materials.
- Tools > Portal > Build Level Kit — portalable/non-portalable panels, glass, fizzler, platforms.
- Tools > Portal > Assign Portal 2 Sounds — fills the audio slots on prefabs and scenes.
