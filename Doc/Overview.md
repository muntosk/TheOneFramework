# Overzicht — wat bestaat er allemaal?

Naslag van alle eigen scripts en assets (`Assets/Core` en `Assets/Demo`), met waar ze gebruikt worden.
Bijgewerkt: 2026-10-19. Waar ze horen staat in `Assets/STRUCTURE.md`; hoe het verhaal loopt in `Doc/Narrative.md`.
Wat uit het framework van de docenten komt en wat zelf gemaakt is: `Doc/Framework.md`.

**Legenda gebruik:** ✅ gebruikt in de game (levels/menu) · 🧩 alleen via andere scripts of prefabs ·
🧪 alleen in oude/test-scènes · ❓ nergens gevonden → kandidaat om weg te halen (eerst even checken in de editor).

---

## Scènes

| Scène | Wat | In build? |
|---|---|---|
| `Demo/Scenes/MainMenu` | Startmenu (Start, Settings met volumesliders) | ✅ |
| `Levels/Level_0` | Oppervlak (terrein 200×200), lift naar beneden | ✅ |
| `Levels/Level_1` | Chamber 1 "Welcome" — portal gun oppakken | ✅ |
| `Levels/Level_2` | Chamber 2 "The neighbour" — kubus, plaat, dispenser, Wes | ✅ |
| `Levels/Level_3` | Chamber 3 — fizzler | ✅ |
| `Levels/Level_4` | Chamber 4 — scheuren, kruipruimte, uitklapbrug | ✅ |
| `Levels/Level_5` | Chamber 5 "Momentum": flingen, kubus flingen, rijzend platform met knop | ✅ |
| `Levels/Level_6` | Het raam (twist), klauw, lopende band, ontsnapping, Wes | ✅ |
| `Levels/Level_7` | Oppervlak bij zonsondergang, einde, robots marcheren | ✅ |
| `001_portal_test_level1` | Oude testscène | ⚠️ staat nog in de build list — **weghalen** |
| `000_portal_setup` | Oude portal-testopstelling | ❓ weg te halen |
| `Levels/Old/Old_Level_1`, `Old_Level_2` | Oude versies van chambers | ❓ weg te halen |
| `Demonstration`, `Playground` (+ mappen met NavMesh/Lighting) | Voorbeeldscènes van de docent | ❓ weg te halen als je ze niet meer nodig hebt |

---

## Scripts — Core (herbruikbaar framework)

### Audio (`Core/Audio/Scripts`)
| Script | Wat het doet | Gebruik |
|---|---|---|
| `GameAudio` | Speelt **elk** geluid af via de juiste mixergroep, beperkt dubbele geluiden | 🧩 door ~20 scripts |
| `GameAudioSettings` | Welke mixergroep per soort geluid (asset in `Resources`) | 🧩 |
| `AudioChannelRouter` | Zet een AudioSource die je zelf in een scène plaatst op het juiste kanaal | ✅ Level_0, Level_7 |
| `LevelMusic` | Achtergrondmuziek per level, `CrossFadeTo` | ✅ Level_6 (Chambers 1–4 nog niet) |
| `VolumeSlider` | UI-slider ↔ mixervolume, wordt opgeslagen | ✅ MainMenu |

### Portal (`Core/Portal/Scripts`) — de puzzelmechanica
| Script | Wat het doet | Gebruik |
|---|---|---|
| `Portal`, `PortalPair`, `PortalCamera` | De portals zelf, het paar, rendering door de portal heen | ✅ alle levels |
| `PortalGun`, `PortalGunPickup`, `Crosshair` | Schieten, het pistool oppakken (Chamber 1), richtkruis | ✅ |
| `PortalSurface` | Markeert een muur als portal-baar | 🧩 level kit |
| `PlayerPortalTraveller`, `PortalableObject`, `IPortalTraveller` | Speler / objecten door een portal sturen | 🧩 PlayerRig, kubus |
| `PlayerCarry`, `Carryable` | Kubus oppakken en dragen | 🧩 PlayerRig, kubus |
| `CubeDispenser` | Laat een kubus vallen (+ opnieuw als hij weg is) | ✅ |
| `PressurePlate`, `PressurePlateWeight`, `PoleButton` | Vloerknop, gewicht, drukknop op paal | ✅ via prefabs |
| `RequireAllTriggers` | EN-poort: meerdere knoppen/platen samen | ✅ Level_1–4 |
| `Door`, `Lift`, `ExtendingPlatform` | Schuifdeur, lift tussen levels, uitklap-/opkomend paneel | ✅ |
| `PortalFizzleField` | Fizzler: wist portals en wat je draagt | ✅ Level_3/4 |
| `ChamberShell`, `ChamberSign` | De chamber-doos in panelen + het chamberbord | ✅ via `Chamber`-prefab |
| `DoorSurround` | Vult de hoeken tussen de ronde deur en het vierkante gat in de muur (ChamberShell plaatst hem zelf) | ✅ elke chamber |
| `IndicatorLine` | Portal-"antlines": lijnen van een knop naar wat hij aanstuurt, blauw → oranje bij `TurnOn()` | ✅ Level_5, `PoleButton`-prefab |
| `ConveyorBelt` | Lopende band die speler/objecten meeneemt | ✅ Level_6 |
| `LevelManager` | Laadt `Level_` + (nummer + 1) | ✅ |

### Speler (`Core/PlayerOverrides/Scripts`)
| Script | Wat het doet | Gebruik |
|---|---|---|
| `ThirdPersonControllerFixed` | Aangepaste StarterAssets-controller (schaal, portal-momentum, camera) | ✅ PlayerRig |
| `CameraModeToggle` | Wisselt tussen third-person en een andere cameramodus | ✅ |
| `StarterAssetsInputsAdditionalInputs`, `RightMouseButtonLook` | Extra input / kijken met de muis | 🧩 docent-spelerprefab |
| `FootstepSoundSurface` | Voetstapgeluid per ondergrond (docent) | ❓ |

### Spawn / dood (`Core/SpawnPoint/Scripts`)
| Script | Wat | Gebruik |
|---|---|---|
| `RespawnBehaviour` | Onthoudt waar je stond, respawnt je | 🧩 spelerprefab |
| `KillZone` | Gifwater / vuur: fade uit en respawn | ✅ ToxicWater, verbrandingsoven |
| `SpawnPoint` | Spawnpunt (docent) | ❓ (alleen genoemd in `RespawnBehaviour`) |

### Dialoog en interactie
| Script | Wat | Gebruik |
|---|---|---|
| `Interaction/DialogueTrigger` | Start een Yarn-gesprek bij een trigger | ✅ alle levels |
| `Interaction/UnityEventTrigger` | Trigger → UnityEvent | ✅ Level_6 |
| `Interaction/PlayerActivatable` | Basisklasse van bijna alle triggers | 🧩 |
| `Interaction/LoadScene` | Scène laden | 🧩 via `YarnSpinnerFunctions`, `GameEnding` |
| `Interaction/AnimationToggle`, `Collectable`, `ShowMenu`, `SimpleAnimationTrigger`, `SimpleAudioTrigger` | Docent-voorbeelden | 🧪 alleen Demonstration |
| `Interaction/FMODEventTrigger`, `OnItemUnityEventTrigger`, `VariableSetter` | Docent-voorbeelden | ❓ |
| `Interaction/TriggerPlayerActivatables` | Activeert triggers rond de speler | 🧩 docent-spelerprefab |
| `YarnSpinnerFunctions/YarnSpinnerFunctions` | Yarn-commando's (`set_enabled_in_unity`, …) | ✅ via `.yarn` |
| `YarnSpinnerFunctions/DialogueKeyControls` | E / W / S voor dialoogkeuzes; maakt zichzelf aan | ✅ |
| `YarnSpinnerFunctions/DialogueCharacterColors` | Kleur per personage voor de naam in de dialoog (S.A.F.E.-T blauw-wit, Wes oranje) | ✅ via `Dialogue System Subtitles` |
| `YarnSpinnerFunctions/DialogKeyAdvance` | Oude variant van hierboven | 🧪 Demonstration |
| `SpeechBubbles/*` | Spraakballonnen boven personages (`<<say>>`) | ✅ |
| `ProximityTrigger/*` | "Druk op E"-prompt in de buurt van iets | 🧩 knoppen |

### UI (`Core/UI`)
| Script | Wat | Gebruik |
|---|---|---|
| `MainMenu` | Startscherm | ✅ |
| `ScreenFade` | Zwarte fade tussen scènes; maakt zichzelf aan | ✅ |
| `TogglePlayerControls` | Besturing uit tijdens dialoog | 🧩 |
| `HUDValueUpdate` | HUD-waarde (docent) | 🧪 Demonstration |

### Overig Core
| Script | Wat | Gebruik |
|---|---|---|
| `Inventory/*` | Inventarissysteem van de docent | 🧪 zit nog in elke scène, maar de UI staat uit (niet gebruikt in de game) |
| `CodeLock/CodeLockScript` | Cijferslot (docent) | ❓ |
| `PathVisualiser/PathVisualizer` | Pijlen die een pad tonen | 🧩 Yarn-commando, niet gebruikt in levels |
| `Utilities/TiledBox` | Paneel dat je kunt schalen zonder dat de tegels uitrekken | ✅ level kit |
| `Utilities/TextureScroller` | Bewegende textuur (fizzler, lopende band) | ✅ |
| `Utilities/DressingScatter` | Strooit puin/decals (editor) | 🧩 scatter-prefabs |
| `Utilities/HangingCable` | Hangende kabel tussen twee punten | ❓ |
| `Utilities/ShowObjectNamesEditor` | Toont namen in de scene view (docent) | 🧪 |

### Editor-tools (`Core/Editor`, menu **Tools**)
| Script | Menu |
|---|---|
| `PortalLevelKitBuilder` | Tools > Portal > Build Level Kit (+ Replace Water With ToxicWater) |
| `Portal2DressingBuilder` | Tools > Portal 2 > Build Set Dressing |
| `AssignPortalSounds` | Tools > Portal > Assign Portal 2 Sounds |
| `MainMenuBuilder` | Tools > UI > Build Main Menu Scene / Capture Menu Background |
| `FixZUpToYUp` | Zet geïmporteerde modellen rechtop |
| `DependencyChecker`, `FMODChecker` | Docent: checkt of packages aanwezig zijn |

---

## Scripts — Demo (alleen voor deze game)

| Script | Wat | Gebruik |
|---|---|---|
| `Escape/ClawGrab` | Klauw pakt je op na de twist (`<<grab_player>>`) | ✅ Level_6 |
| `Escape/Incinerator` | Vuur bij de oven, particles aan/uit op afstand | ✅ Level_6 |
| `Escape/InterruptingDialogueTrigger` | Gesprek dat een lopend gesprek mag onderbreken | ✅ Level_6/7 |
| `Escape/EscapeYarnCommands` | `<<turn_to>>`, `<<look_at>>` | ✅ via Yarn |
| `Escape/GameEnding` | Eindkaart + credits → menu (`<<end_game>>`) | ✅ Level_7 |
| `RobotCopies/DeploymentMarch` | **Nieuw**: robots lopen vanaf de lift het pad af naar je toe en stoppen vóór je | ✅ Level_7 |
| `RobotCopies/FollowTerrainHeight` | Houdt voeten op het terrein | 🧩 Deploy-prefab (sinds `DeploymentMarch` eigenlijk overbodig) |
| `RobotCopies/RobotCopyAnimationEvents` | Slikt voetstap-events van de robots in | 🧩 robotprefabs |
| `NPC/WalkToBehaviour` | NPC loopt naar een punt (docent) | 🧩 Yarn-commando, niet gebruikt in levels |

---

## Assets per map

| Map | Inhoud | Opmerking |
|---|---|---|
| `Core/Portal/Prefabs/Gameplay` | Deur, lift (+ varianten), knoppen (incl. `PoleButton`: staande knop met E-prompt + gloeiring), platen, dispenser, portals | handgemaakt |
| `Core/Portal/Prefabs/LevelKit` | Panelen, glas, fizzler, platforms, ToxicWater, `Chamber` | **gegenereerd** — niet met de hand aanpassen |
| `Core/Portal/Prefabs/Props` | 14 statische Portal 2-props | |
| `Core/Portal/Audio` | Portal 2-geluiden | |
| `Core/PlayerOverrides/Prefabs/PlayerRig` | De speler in elk level | |
| `Demo/Dressing` | Vines, liftschacht, kabels, lopende band, oven, camera's/monitors; ~53 decals (vuil, puin, begroeiing, Rat Man) + 3 scatter-prefabs | |
| `Demo/RobotCopies` | Robotprefabs (Patrol, Hurdles, Waiting, Deploy), zelf-gekeyframede clips `Patrol` + `Hurdles` (rubric), body-controllers | `RobotCopy_Deploy.anim/.controller` zijn **niet meer in gebruik** (vervangen door `DeploymentMarch`) — mag weg |
| `Demo/Escape/Prefabs` | `Volunteer_Patrol`, `Volunteer_Standing` | |
| `Demo/Animations/ExoGray` (67 MB!) | Placeholder-mens + Mixamo-animaties | Grootste Demo-map; opruimen zodra Wes/vrijwilligers echte modellen hebben. De losse `.anim`/`.controller` erboven (Open/Close/AnimationToggle/UseInventoryItem) zijn docent-voorbeelden |
| `Demo/Audio` | `Music/` (6), `Ambience/` (7), `Ending/` (4), `Announcer/` (2) | `coin.wav`, `magical_1.ogg` = docent-voorbeelden ❓ |
| `Demo/Dialogue` | `MyStoryScript.yarn` (alle levels), `DemonstrationScript/` (docent) | |
| `Demo/Surface`, `Demo/TerrainData` | Terreindata oppervlak | check welke van de twee echt gebruikt wordt |
| `Demo/UI` | TitilliumWeb-font (+ SDF-assets, `Subtitle`-materialen met schaduw), `Prefabs/Dialogue System Subtitles` (Portal-ondertitel-stijl, variant van de Yarn-prefab, in alle levels) + `Option Item Subtitle`, `Textures/SubtitleGradient` | |
| `Demo/Materials` | Gedeelde materialen + skyboxes | |
| `Demo/InventoryItems`, `Demo/Prefabs/Letter`, `Demo/NPC` | Docent-voorbeelden | ❓ alleen in Demonstration |
| `ThirdParty/` | StarterAssets (88 MB), TerrainPainting (404 MB), Portal2AssetCollection (151 MB), Portal2HarunExport (333 MB), TextMeshPro | niet herstructureren |

---

## Opruimkandidaten (samengevat)

Zeker veilig:
- `001_portal_test_level1` uit de build list halen.
- `RobotCopies/Animations/RobotCopy_Deploy.anim` + `.controller` (niet meer gebruikt).

Waarschijnlijk veilig (alleen docent-/testscènes gebruiken ze) — eerst beslissen of je `Demonstration`/`Playground` wilt houden:
- Scènes `000_portal_setup`, `001_portal_test_level1`, `Old_Level_1/2`, `Demonstration`, `Playground`.
- Daarna: `Demo/InventoryItems`, `Demo/Prefabs/Letter`, `Demo/NPC`, `Demo/Dialogue/DemonstrationScript`, de docent-`.anim`s in `Demo/Animations`, `coin.wav`, `magical_1.ogg`.
- Scripts met ❓: `FMODEventTrigger`, `OnItemUnityEventTrigger`, `VariableSetter`, `CodeLockScript`, `HangingCable`, `FootstepSoundSurface`.

Let op: het Inventory-systeem zit in elke scène (via de spelerprefab van de docent) — dat niet zomaar weghalen.
