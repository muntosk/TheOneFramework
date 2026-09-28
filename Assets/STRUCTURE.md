# Where things go

Three top-level buckets. Before adding a new file, ask "which bucket does this belong to" and it should be obvious.

## Assets/Core/
The reusable framework itself — anything that could be dropped into a *different* game unchanged.
One folder per system, each holding all of that system's own scripts/prefabs/materials/shaders together
(no separate "Scripts" / "Prefabs" folders scattered at the top level — keep a system's files next to each other).

- `Interaction/` — generic trigger/response scripts (Collectable, DialogueTrigger, AnimationToggle, UnityEventTrigger, ...)
- `Inventory/` — the inventory system and everything about using/adding items
- `CodeLock/`, `PathVisualiser/`, `ProximityTrigger/`, `SpawnPoint/`, `SpeechBubbles/`, `Portal/`, `YarnSpinnerFunctions/` — self-contained systems
- `UI/` — generic UI glue (HUD, menu toggling) that isn't specific to one system
- `PlayerOverrides/` — scripts that extend/patch the third-party StarterAssets character controller
- `Utilities/`, `Editor/` — small helpers and editor-only tooling

**Rule of thumb:** if you'd want it in a framework export, it goes in `Core/`, in its own subfolder if it's a new system.

## Assets/ThirdParty/
Imported/vendored packages you didn't author and shouldn't restructure internally
(StarterAssets, TextMeshPro). If you need to change their behavior, add an override script in
`Core/PlayerOverrides/` instead of editing the vendored files directly.

## Assets/Demo/
Content specific to *this* demonstration project — not reusable, one-off. Scenes, dialogue (Yarn scripts),
inventory item data, art/animation, audio, and demo-only objects (e.g. `NPC/`, `Prefabs/`). If you're building
a scene, writing dialogue, or adding a one-off prop, it goes here.

`Demo/Materials/` is the one deliberate exception to "no type-based folders": it's a *shared* bucket for
materials/textures that genuinely have no single owning prefab or system (skybox, a plain placeholder
material, scene-level set dressing). Anything that *does* belong to one system or one prefab lives with
that system/prefab instead — see the import rule below.

## Assets/Settings/
Unity/URP project configuration (render pipeline assets, volume profiles). Left at the root since it's
project infrastructure, not framework code or demo content.

---

## When you import something new

Ask "what is this *for*", not "what type of file is it":

1. **Belongs to an existing Core system** (e.g. a new material for the portal shader) → put it in that
   system's own folder, e.g. `Core/Portal/`.
2. **A new self-contained thing** (a prop, a character, an interactable — anything with its own
   model/texture/material/prefab) → make it its own folder named after the thing, e.g. `Demo/Prefabs/WoodenCrate/`,
   holding everything that thing needs.
3. **Genuinely generic/shared, no single owner** → the shared `Demo/Materials/` bucket. This should stay small —
   if you're unsure, it's probably actually case 1 or 2.

This is exactly how `Demo/Materials/` was cleaned up: every file that actually belonged to one prefab
(`Arrow.mat`/`arrow.png` → `PathVisualiser`, `speech.png`/`tail.png` → `SpeechBubbles`, `FaceMappingURP.*`/`spawn.png`
→ `SpawnPoint`, `letter.png` → `Demo/InventoryItems`, `moms letter.png` → `Demo/Prefabs`) got moved next to its
owner, `NeonPostFXVolume.asset` moved to `Settings/` next to the other volume profiles, and three files that
turned out to be completely unreferenced (`Materials/spawn.mat`, `Materials/Materials/spawn.mat`,
`Materials/Resources/letter.png`) were deleted outright. What's left in `Demo/Materials/` (`Neon.mat`,
`Smoke.mat`+`smokepuff.png`, `Waypoint.mat`, `White.mat`, `Skybox/`) is genuinely shared scene dressing with
no single owner.

**Still unresolved:** `SGGridBox_Default.mat`, `SGGridBox_Default.png`, and `SGGridBox_TerrainLayer.terrainlayer`
showed no references anywhere (no prefab, scene, or terrain data uses them) — left in place since that's less
certain than the others, but worth double-checking in the Editor and deleting if truly unused.
