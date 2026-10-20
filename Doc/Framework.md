# Wat is van het framework, wat is van mij?

Het project begon als **TheOneFramework** van de docenten (GitHub `YvensFaos/TheOneFramework`; commits van acdh,
Yvens, Mark Boerrigter, Bram). De laatste framework-commit is `a67ba75` (2026-08-27). Alles daarna is van mij
(Harun / muntos), vanaf `4c25f93` "Add ported portal system" (2026-09-21).

Vastgesteld door elk framework-bestand van `a67ba75` te vergelijken met de huidige versie (op inhoud, dus
ook als het verplaatst is). In oktober 2026 heb ik de mappen herschikt (`Core/` · `Demo/` · `ThirdParty/`).
Daardoor staan framework-bestanden nu op andere plekken dan in het origineel.
Zie `Doc/Overview.md` voor wat elk script doet en waar het gebruikt wordt.

---

## 1. Framework — ongewijzigd overgenomen

Alleen verplaatst, inhoud is precies zoals de docenten het maakten.

| Systeem | Scripts (nu in) |
|---|---|
| Interactie / triggers | `Core/Interaction/`: `PlayerActivatable`, `TriggerPlayerActivatables`, `DialogueTrigger`, `UnityEventTrigger`, `AnimationToggle`, `Collectable`, `LoadScene`, `ShowMenu`, `SimpleAnimationTrigger`, `OnItemUnityEventTrigger`, `VariableSetter`, `FMODEventTrigger` |
| Inventory | `Core/Inventory/`: `Inventory`, `InventoryView`, `InventoryItemDefinition`, `AddInventoryItem`, `UseInventoryItem`, `UseInventoryItemBase`, `UseInventoryItemOnTarget`, `InventoryItemDefinitions`, `InventoryItemDictionary` |
| Yarn Spinner | `Core/YarnSpinnerFunctions/`: `YarnSpinnerFunctions` (alle standaard Yarn-commando's), `DialogKeyAdvance` |
| Spraakballonnen | `Core/SpeechBubbles/`: `SpeechBubbleManager`, `SpeechBubbleContainer`, `SpeechBubbleKeypressAdvance` |
| Proximity trigger | `Core/ProximityTrigger/`: `ProximityTrigger`, `ProximityTriggerActivator`, `ProximityTriggerRoot` |
| Spawn | `Core/SpawnPoint/`: `SpawnPoint` |
| Overig | `Core/CodeLock/CodeLockScript`, `Core/PathVisualiser/PathVisualizer`, `Core/UI/HUDValueUpdate`, `Core/UI/TogglePlayerControls`, `Core/Utilities/ShowObjectNamesEditor`, `Demo/NPC/WalkToBehaviour` |
| Speler-overrides | `Core/PlayerOverrides/`: `StarterAssetsInputsAdditionalInputs`, `FootstepSoundSurface` |
| Editor | `Core/Editor/`: `DependencyChecker`, `FMODChecker` |

**Assets uit het framework:**
- De scènes `Demonstration` en `Playground` (met NavMesh/Lighting-mappen), nu in `Demo/Scenes/Archived/`.
- `Demo/Dialogue/DemonstrationScript/` en `Demo/InventoryItems/`.
- `Demo/Prefabs/Letter`, `Demo/NPC/` (NPC-prefab en controller).
- De losse `.anim`/`.controller` in `Demo/Animations/` (Open/Close, AnimationToggle, SimpleAnimationTrigger, UseInventoryItem).
- De prefabs in `Core/Inventory`, `CodeLock`, `ProximityTrigger`, `SpawnPoint`, `SpeechBubbles` en `PathVisualiser`.
- `Demo/Materials/` (Neon, Smoke, Waypoint, White, Skybox) en `Demo/Audio/coin.wav`, `magical_1.ogg`.
- `Demo/TerrainData/`.
- `Settings/` (URP-assets).
- `ThirdParty/StarterAssets` en `ThirdParty/TextMeshPro`: deze zaten al in het framework.

---

## 2. Framework — door mij aangepast

| Script | Wat ik veranderd heb | Omvang |
|---|---|---|
| `PlayerOverrides/ThirdPersonControllerFixed` | Schaalt beweging/sprong/camera mee met de spelergrootte, momentum door portals, `SetLook`/`LookYaw` voor camera-commando's, portal gun-richten | ~227 van 563 regels |
| `SpawnPoint/RespawnBehaviour` | Doodgaan met fade, KillZones, "laatste veilige grond", bescherming tijdens liftritten, teleport-fixes; standaard terug naar spawnpunt/checkpoint | ~128 van 212 regels |
| `SpeechBubbles/SpeechBubbleBehaviour` | Ballonnen tekenen bovenop muren/glas (praten door een raam) | ~27 van 63 regels |
| `Interaction/SimpleAudioTrigger` | Speelt af via `GameAudio` (mixer) in plaats van `PlayClipAtPoint` | herschreven |
| `PlayerOverrides/RightMouseButtonLook` | Kijken volgt altijd de muis, geen rechtermuisknop meer nodig | herschreven |
| `Inventory/.../InventoryItemScanner` | Alleen het pad aangepast na het verplaatsen (`Assets/Demo/InventoryItems`) | 1 regel |
| `Demo/Dialogue/MyStoryScript.yarn` | Het bestand komt uit het framework, maar alle inhoud (S.A.F.E.-T, Wes, alle levels) is van mij | volledig nieuw |

---

## 3. Helemaal van mij

### Scripts — Core (herbruikbaar)
| Systeem | Scripts |
|---|---|
| Portal-mechanica (`Core/Portal/Scripts`) | `Portal`, `PortalPair`, `PortalCamera`, `PortalSurface`, `PortalGun`, `PortalGunPickup`, `Crosshair`, `PlayerPortalTraveller`, `PortalableObject`, `IPortalTraveller`, `PlayerCarry`, `Carryable`, `CubeDispenser`, `PressurePlate`, `PressurePlateWeight`, `PoleButton`, `RequireAllTriggers`, `Door`, `Lift`, `ExtendingPlatform`, `PortalFizzleField`, `ConveyorBelt`, `ChamberShell`, `ChamberSign`, `DoorSurround`, `LevelManager` |
| Audio (`Core/Audio`) | `GameAudio`, `GameAudioSettings`, `AudioChannelRouter`, `LevelMusic`, `VolumeSlider` + de `GameMixer` |
| UI (`Core/UI`) | `MainMenu`, `ScreenFade` |
| Speler | `PlayerOverrides/CameraModeToggle`, de `PlayerRig`-prefab |
| Dood / gevaar | `SpawnPoint/KillZone` |
| Dialoog | `YarnSpinnerFunctions/DialogueKeyControls` |
| Utilities | `TiledBox`, `TextureScroller`, `DressingScatter`, `HangingCable` |
| Editor-tools | `PortalLevelKitBuilder`, `Portal2DressingBuilder`, `AssignPortalSounds`, `MainMenuBuilder`, `FixZUpToYUp`, plus de VMT/VTF-importer/baker in `ThirdParty/Portal2HarunExport` |

### Scripts — Demo (alleen deze game)
`Escape/`: `ClawGrab`, `Incinerator`, `GameEnding`, `InterruptingDialogueTrigger`, `EscapeYarnCommands` ·
`RobotCopies/`: `DeploymentMarch`, `FollowTerrainHeight`, `RobotCopyAnimationEvents`

### Assets
- **Scènes:** `MainMenu`, `Level_0` t/m `Level_8`.
- **Prefabs:** level kit (`Core/Portal/Prefabs/LevelKit`, gegenereerd), puzzelstukken (`Gameplay/`: deur, lift, knoppen, platen, dispenser, portals), Portal-props, `PlayerRig`.
- **Set dressing:** `Demo/Dressing/` (decals, scatter, omgezette Portal 2-modellen).
- **Animatie:** de zelf-gekeyframede clips `RobotCopy_Patrol` en `RobotCopy_Hurdles` (rubric) en de robot-controllers in `Demo/RobotCopies`; robot- en vrijwilligerprefabs (`Demo/Escape/Prefabs`).
- **Terrein:** `Demo/Surface/` (het 200×200-oppervlak).
- **UI:** `Demo/UI/` (font-setup, menu).
- **Docs:** `Doc/Narrative.md`, `Handoff.md`, `Overview.md`, `Sessions/`, `Assets/STRUCTURE.md`, `CLAUDE.md`.

---

## 4. Third-party (niet van het framework, niet zelf gemaakt)

Door mij toegevoegd, gemaakt door anderen:

| Wat | Waar | Bron |
|---|---|---|
| Portal 2-materialen, -modellen en -geluiden | `ThirdParty/Portal2HarunExport` (gebakken door mijn eigen tool), `ThirdParty/Portal2AssetCollection`, `Core/Portal/Audio` | Valve / Portal 2 |
| Terrein-assets (bomen, gras, rotsen, water, skyboxes) | `ThirdParty/TerrainPainting` | Unity terrain demo-assets |
| Exo Gray + animaties (placeholder-mens) | `Demo/Animations/ExoGray` | Mixamo |
| Muziek, ambience, eindgeluiden, announcer-chimes | `Demo/Audio/` (behalve `coin.wav`/`magical_1.ogg`) | extern (bron muziek nog invullen in de credits) |
| Lopende band, verbrandingsoven, camera, monitoren | `Demo/Dressing/Models/Ending`, `Facility` | extern |
| Font Titillium Web | `Demo/UI/Fonts/TitilliumWeb` | Google Fonts (OFL) |
| Packages | Yarn Spinner, Cinemachine, URP, Input System | Unity / Yarn Spinner |
