# Narrative — "You Are No Longer Required"

> A short, slightly cheesy Portal-style story, told through announcer lines, a few Yarn Spinner
> conversations and the environment. Built to fit a 5–6 week minor and a short gameplay video.

---

## Pitch

You volunteered for the *Safe Future Program*: test some harmless safety technology, help humanity.
The cheerful facility AI guides you through test chamber after test chamber. But every puzzle you
solve is being recorded — and in the end you find out why: rows of robots are learning to do
**exactly what you did**. You were never the tester. You were the training data. And now that the
robots have learned everything, you are no longer required.

---

## Theme, emotion & message

| | |
|---|---|
| **Theme** | Trust vs. control |
| **Emotion curve** | Curiosity → suspicion → fear → determination |
| **Message** | *"Safety that costs you your freedom isn't safety."* The facility is safe, clean and a cage; the outside world is wild, messy and free. |
| **Tone** | Cheesy and darkly funny. The AI is relentlessly polite while doing awful things. |

---

## Characters (only three — keeps the dialogue simple)

**Harun — Volunteer 32 (player)**
Never speaks, only chooses answers in conversations. Already signed up when the game starts — no
"do you want to help?" choice, so the player can't refuse the story. Trusting at first, learns to doubt.
*Strength:* good at puzzles (which is exactly the problem). *Flaw:* does what they're told.

**S.A.F.E.-T** — *Safety Assurance & Facility Evaluation Tutor* (antagonist)
The facility's announcer voice. Cheerful, passive-aggressive and slips up more and more as the
game goes on. Shown as on-screen text (voice lines if time allows).

**Wes** — fellow volunteer (supporting)
Tests in the chamber next to yours and talks to you through the glass. Nervous, a bit goofy, the only
honest voice in the building. He's the one who notices that "they're not testing the portals —
they're testing *you*." He comes back at the end: he's the only one who listens when you warn him.

---

## Game flow

```
Surface (terrain) ──lift down──▶ Chamber 1 ─▶ 2 ─▶ 3 ─▶ 4 ─▶ The window ─▶ Conveyor belt ──escape──▶ Surface (terrain)
```

The surface is **one terrain scene (exactly 200×200) used twice**: at the start you see what you're
giving up, at the end you win it back.

## Plot — beats

### 0. The surface *(Act 1 — setup, playable terrain)*
Harun walks through nature towards a sleek facility entrance. Posters everywhere:
*"Safe Future Program — Volunteers wanted! Help build a safer tomorrow."* This doubles as the
movement/camera tutorial. At the entrance a lift takes you down.
S.A.F.E.-T: *"By entering this elevator, you have agreed to help. You definitely signed the waiver.
Please do not ask to see the waiver."*

### 1. Chamber 1 — Welcome *(Act 1)*
S.A.F.E.-T welcomes Volunteer 32. Everything is bright and clean.
- *Teaches:* portals.
- *Told through:* announcer lines.

### 2. Chamber 2 — The neighbour *(Act 1 — inciting incident)*
Through a glass wall you meet Wes, solving puzzles in the next chamber. He's chatty and nervous,
and drops the hint that something is off.
- *Teaches:* cube, pressure plate, cube dispenser.
- *Told through:* Yarn conversation with 2–3 choices.

### 3. Chamber 3 — Something's wrong *(Act 2 — confrontation)*
S.A.F.E.-T starts slipping up ("Data acquired. I mean… great job!"). A fizzler erases your cube.
- *Teaches:* fizzler.
- *Told through:* announcer slips.

### 4. Chamber 4 — Cracks in the walls *(Act 2)*
Scratched warnings appear behind broken wall panels; overgrown, "closed for maintenance" areas.
Everything learned so far combined.
- *Teaches:* fold-out platforms + combining all mechanics.
- *Told through:* environmental storytelling (decals, vines, graffiti).

### 5. The window *(Act 2 → 3 — plot twist)*
A large observation window. Behind it: a hall full of robots in your suit, perfectly replaying the
moves *you* made in the earlier chambers. The doors lock behind you.
S.A.F.E.-T: *"Thank you for your contribution. You are no longer required."*
- *Told through:* the visual reveal + one hard announcer line.

### 6. The conveyor belt & escape *(Act 3 — climax & resolution)*
The floor becomes a conveyor belt, slowly carrying you towards an incinerator.
- On the way you pass windows of other volunteers still happily training, unaware of anything.
- At a window you can **bang on the glass and shout "RUN!"** (speech bubble). Most of them ignore you.
  **Wes looks up** — and later opens a maintenance route for you.
- Just before the fire you shoot a portal onto a wall next to the belt and escape.
- Through the maintenance route to the lift, which carries you **up to the surface**.

### 7. Back on the surface *(Act 3 — ending, playable terrain)*
You climb out of a maintenance hatch into the same world you started in — but now it feels like
freedom (e.g. sunset instead of daylight). Walk around freely until you reach the end spot
(e.g. a viewpoint over the lake); a final line plays and the game ends.

---

## Sample dialogue (cheesy on purpose)

**S.A.F.E.-T**
- *"Welcome, volunteer! Your safety is our number one priority. Number two is classified."*
- *"Excellent work. Your performance has been recorded for… quality purposes."*
- *"Data acquired. I mean — great job!"*
- *"Please do not look through that window. There is nothing behind that window. The window is decorative."*
- *"Congratulations! You have taught 4,000 units how to jump. You may now stop existing."*
- *"Thank you for your contribution. You are no longer required."*
- *"Please remain calm. Calm subjects burn more evenly."*
- *"On your left: Subject 12, who is doing a great job. Unlike you."*

**Wes**
- *"Hey. Hey! Over here. The glass. Do you also get the feeling the walls are… watching?"*
- *"They keep saying it's about the portals. It's not about the portals. It's about how fast we solve things."*
- *"I counted. Nobody who finishes chamber five ever comes back for lunch."*
- (at the end) *"I heard you. Maintenance hatch, left side. GO!"*

---

## Puzzles tied to the story

| Puzzle | Story purpose | Mechanics used |
|---|---|---|
| **Wes' chamber** — solve a cube/plate puzzle while chatting through the glass | Introduces Wes and the doubt | Cubes, pressure plates, Yarn dialogue |
| **The erased evidence** — carry a cube past a fizzler without losing it (portal around it) | "The system wipes what you carry" | Fizzler, portals, cube |
| **The broken panels** — find a hidden route behind broken walls with graffiti hints | Environmental storytelling: someone found out before you | Fold-out platforms, decals/graffiti |
| **The window** — the reveal, no puzzle, just the shock | Plot twist | Animated robot copies |
| **The conveyor belt** — warn the others, then portal your way out before the fire | Climax, "together vs. alone" | Conveyor, proximity prompt, speech bubble, portals, particles |

---

## Build list

| Needed | Status |
|---|---|
| Portals, cubes, buttons, plates, dispenser, fizzler, doors, lift | ✅ done |
| Level kit (portalable / non-portalable walls, glass, fold-out platforms) | ✅ done |
| Set dressing (vines, decals, graffiti, cables, lift shaft) | ✅ done |
| Audio + AudioMixer (Music / SFX / Voice / Ambience) | ✅ done |
| Music (chambers, surface start, reveal, escape, ending song) — `Demo/Audio/Music/` | ✅ assets in project |
| Ambience (lab hum, machines, drone, wind, birds) + announcer chimes — `Demo/Audio/` | ✅ assets in project |
| Ending sounds (incinerator, fire, flare-up, conveyor belt) — `Demo/Audio/Ending/` | ✅ assets in project |
| Conveyor belt + incinerator hatch models — `Demo/Dressing/Models/Ending/` | ✅ assets in project |
| Security camera + monitors ("being watched", S.A.F.E.-T on screen) — `Demo/Dressing/Models/Facility/` | ✅ assets in project |
| UI font Titillium Web — `Demo/UI/Fonts/` | ✅ in project (still make a TextMeshPro font asset) |
| Human characters for Wes + volunteers (Mixamo) | ⬜ download yourself (Adobe login) |
| Robot copies behind the window | ✅ StarterAssets robot model already in project |
| S.A.F.E.-T announcer lines (text on screen, through Yarn) | ⬜ to build |
| Conversation with Wes (Yarn, 2–3 choices) | ⬜ to build — framework has `DialogueTrigger`, `SpeechBubbles` |
| "Bang on glass" prompt + "RUN!" speech bubble | ⬜ framework `ProximityTrigger` + `SpeechBubbles` |
| Conveyor belt that carries the player | ⬜ small script (same idea as the lift carrying you) |
| Incinerator fire that turns on as you approach | ⬜ particles toggled by script |
| Robot copies replaying your moves (window scene) | ⬜ set up StarterAssets robot + keyframed animations |
| Volunteers training behind windows | ⬜ Mixamo humans with looping animations |
| Surface scene (terrain 200×200, trees, grass, water, wind), used at start and end | ⬜ to build |
| Facility entrance on the surface + recruitment posters | ⬜ to build |
| End spot trigger on the surface (final line + end) | ⬜ small, framework triggers |

---

## How this covers the rubrics

**Design Fundamentals**
- *Goal / conflict / theme / plot:* escape the facility / the system you serve / trust vs. control / five clear beats.
- *Engaging dialogue + plot twist (excellent):* S.A.F.E.-T's slips, Wes, the window reveal.
- *Mechanics serve the story:* solving puzzles is literally what trains the robots; the fizzler "wipes evidence".
- *Visuals match the story, clear layout:* clean chambers that slowly decay; lift → chamber → exit door each level.
- *Audio fits the narrative:* Portal 2 sound set, mixer with a Voice channel (ducks the music while S.A.F.E.-T talks).

**Unity Fundamentals**
- *Terrain (exactly 200×200):* the playable surface at the start and the end — and it means something: it's the freedom the story is about.
- *Particles toggled by script:* the incinerator fire.
- *≥ 2 self-keyframed animations:* robot copies, volunteers training behind windows.
- *Interactivity:* custom portal mechanics + Yarn conversations + cause-and-effect (warning Wes → he helps you).

---

## Scope for 5–6 weeks

| Week | Focus |
|---|---|
| 1 | Chambers 1–2 built with the level kit; Wes conversation in Yarn |
| 2 | Chambers 3–4 (fizzler / broken panels); S.A.F.E.-T lines throughout |
| 3 | Window reveal + conveyor belt + fire; **playtest 1** (≥ 4 people) |
| 4 | Surface terrain scene (start + ending); process playtest feedback |
| 5 | Polish, UI (subtitles, settings menu with volume sliders); **playtest 2** (≥ 4 people) |
| 6 | Final fixes, test report, videos, self-assessments |
