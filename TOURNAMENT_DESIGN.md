# Tournament scene – design

Date: 2026-11-22 · Tournament: 30. 11. 2026 · Runs **only in the Unity Editor** (no builds).

## Intent
Recreate last year's tournament scene (`D:\Projects\AI-JAM-2025-master\Assets\Scenes\novy_turnaj.unity`) in the new project, as a show for a live audience: a big screen fed by OBS, two e-sport commentators, a director, and a referee. Unity runs exactly one match (2–3 rounds) per Play session. The schedule, results and league table live on a website. The director reads the next pairing there and types the result back.

## Goals
- The director picks Team A and Team B from **Inspector dropdowns** that list the team names from a team list. Nothing is linked by hand per match.
- Match flow: round 1 in **A's home arena**, round 2 in **B's home arena**, round 3 (only at 1:1) in the **neutral arena**.
- Every round starts only when the referee presses Space. Before that the robots are frozen and a waiting screen is shown.
- Same fight rules as training (the robots' own HP, fall and flip logic), plus sudden death and a separator.
- A clean Game view that OBS can capture. Director-only information goes in the Inspector and the Scene view, never in the Game view.

## Non-goals
- Building a player, a runtime menu or a lobby.
- Storing results, a league table or a match list (that's the website's job).
- Checking robots and arenas against the rules. A separate validation scene will do that later. This scene only checks that the required fields and markers exist.
- Mouse-wheel zoom.

## Anti-goals (a failure even if it "works")
- Any change outside `Assets/Turnaj/`, or anything that could break training.
- Surprise actions: a single stray key press that knocks out a robot or pushes the robots apart.
- Director or debug UI visible on the stream.
- Hotkeys that work while the Game view is not focused. The director must be able to use other apps, such as the OBS playlist, safely.

## Constraints (found in the code)
- **Robot code (`RobotAgent`, no changes):**
  - Death: weighted HP ≤ 0.2 fires `OnRobotDie`, then calls `EndEpisode()` on both robots, which resets HP and fires `OnRobotRespawn`.
  - Falling (`localPosition.y < -1`): −0.3 HP on every zone, and `OnRobotRespawn` fires.
  - Flipping over: −0.2 HP on every zone, and `OnRobotRespawn` fires.
  - `enemyRobot` is found in `Start()` among the children of the robot's **parent**. Both robots must therefore share one parent, and their local Y is measured relative to it.
  - `RewardEditor` is looked up with `FindAnyObjectByType`. The builder must check whether a RewardEditor is needed in the scene so the code doesn't throw a null reference.
- **Public members the scene can use:**
  - `RobotAgent.weightedAverageHealth`, `OnRobotDie`, `OnRobotRespawn`;
  - `CollisionZoneBehaviour.CurrentHealth` and `ChangeHealth()`, reachable through `GetComponentsInChildren`.
  - `RobotMovement.stationaryRobot` is private, so freezing must go through physics (kinematic rigidbodies, or something equivalent).
- **Existing board scripts:** `StatisticsGUI` (HP boards, children `RobotBoardA/B`) and `ScoreGUI` live in `Assets/Scripts/GUIScripts`. They can be reused as they are, as long as they are driven through `SetRobots`. `StatisticsGUI` shows `robot.gameObject.name`, so the robot instances are named after the teams.
- **Input:**
  - The new Input System is in use (`activeInputHandler: 2`).
  - The Play-mode input behaviour is **left at its default**: keys reach the game only while the Game view has focus. This is intentional.
  - OBS uses the global keys **F1–F4** (game / promo / sponsors / webpage). Unity must not use F1–F4.
- Cameras use layers (robot parts are visible only to some cameras). The tournament cameras must render the right layers.

## Approaches
1. **A new, self-contained tournament script in `Assets/Turnaj/`** – **chosen.** It keeps training untouched and everything in one folder. It's the most new code, so it needs a test run.
2. Adding a tournament mode to the training `AIJamManager` – rejected. It touches training, which goes against the anti-goal.
3. Copying last year's scene and patching it – rejected. It points to scripts and assets that don't exist here and carries last year's logic bugs.

## Decisions made with the user
### Teams
- `soTeam : ScriptableObject` with `public string TeamName; public GameObject RobotPrefab; public GameObject ArenaPrefab;` (more fields allowed later).
- The `TournamentManager` holds a list of `soTeam` (6 at the tournament) and a neutral arena prefab.
- A custom Inspector gives two dropdowns, **Team A** and **Team B**, listing the team names.
- **Teams are picked before Play.** One match = one Play session.

### Robot models
- Each team delivers a **robot prefab with its ONNX model already assigned** (the `soTeam` fields stay unchanged).
- When a robot is spawned, the scene forces `BehaviorParameters.BehaviorType = InferenceOnly`, so nothing can train by accident.
- A prefab with no model gives a warning before Play.

### Arenas and spawning
- **Arena prefab rule:** the prefab's origin is the arena centre and the floor is at local y = 0, as in the default arena. The robots' shared parent sits at the arena origin. The scene warns if a spawn marker is far from floor height.
- Every arena (team home arenas and the neutral one) contains two `SpawnPoint` markers, **Home** and **Guest**. A robot spawns at its marker's position, facing the marker's forward direction.
- Round 1: A's arena, A at Home. Round 2: B's arena, B at Home. Round 3: the neutral arena, A at Home (the neutral arena is symmetrical).
- **Respawn after a fall or flip** goes to the robot's own marker. If the opponent is within about 0.5 m of that marker, the robot uses the point mirrored through the arena centre instead.

### Rounds
- Round length: an Inspector field, **90 s** by default.
- A win by knockout comes from the robots' own death rule.
- **Time up (changed after the first test):** the robot with higher HP wins the round. HP is compared as whole % on the bar. Equal HP in round 1 or 2 shows "DRAW" and the round is replayed (my call). Equal HP in the **final round** goes to sudden death.
- **Sudden death (final round with equal HP only):** the round goes on and "SUDDEN DEATH" is shown. The first robot that takes **damage from the opponent** (zone `OnCollision` with `colliderHitName == "Robot"`), **falls** or **flips** loses the round. Touching walls or the floor does not count. If both robots take damage in the same frame, the one that took more loses.
- **"Flawless victory"** plays when the round winner's HP **as shown on the average bar** (scaled 0.2–1) is above **51 %**, that is weighted HP > 0.2 + 0.51 × 0.8 ≈ 0.61. HP is read at the moment of the knockout, before `EndEpisode` resets it.
- Match: first to 2 round wins.

### Win screen (Game view, audience)
```
                 WINNER
                <Team>
     <TeamA>   2 : 1   <TeamB>
     +2 points        +1 point
```
League points: **2:0 = 3, 2:1 = 2, 1:2 = 1, 0:2 = 0**. The same summary line appears in the Inspector for copying to the website, plus one line per round.

### Operators and keys (all only while the Game view has focus)
- Director: watches the Game view, which is maximized on its own screen and captured by OBS. Uses the numpad for cameras.
- Referee: watches the Scene view on another screen. Uses the left side of the keyboard.

| Who | Key | Action |
|---|---|---|
| Director | Num 8 / 5 / 2 | top / front / close camera |
| | Num 1 / 3 | rotating cameras |
| | Num 4 / 6 | Team A / Team B chase camera |
| Referee | Space | start countdown (3-2-1, FIGHT!!, release robots) |
| | **W ×2** | separator |
| | **Q ×2** | knock out Team A |
| | **E ×2** | knock out Team B |
| | **R ×2** | replay current round (my call: also double tap) |
| | T | pause / resume |
| OBS | F1–F4 | reserved, ignored by Unity |

- **Cameras 8 / 5 / 2** turn to the arena's reading angle every time they're selected, so floor texts read correctly.
  - The angle comes from an optional `ArenaViewAngle` component (`Yaw`) on the arena root. The default is 120°, which fits the default arena's "AI JAM" logo.
  - Team arenas with different floor text set their own value.
- **Double tap** means two presses within about 0.4 s. A single tap only shows a hint in the Inspector and Scene view, never on the stream.
- **"Game view not focused – hotkeys off"** warning: a red banner in the `TournamentManager` Inspector and an overlay in the Scene view. It never appears in the Game view.

### Separator
- Each robot gets a one-off push away from the other, along the line between them, with a slight upward lift.
- It's a velocity change (`ForceMode.VelocityChange`), so it's mass-independent and fair. The strength is an Inspector field, and a cooldown of about 1 s applies.
- It does no damage by itself. A fall it causes is penalized as usual.
- A brief "BREAK!" text appears on screen.

### Screens (Game view)
- **Waiting screen** (after Play, and between rounds): "TeamA vs TeamB", the round title, and a slow orbiting camera. The robots are frozen.
- **During a fight:**
  - two HP boards, Team A on the left and Team B on the right, reusing `StatisticsGUI` (bumper, body, L/R wheels, average);
  - round score A : B;
  - a timer that turns red under 10 s;
  - the "FIGHT!!", "SUDDEN DEATH", "BREAK!" and "PAUSED" texts.
- Round over: a short knockout message, then back to the waiting screen with the next round's title.
- Match over: the win screen above.

### Sounds
Last year's voice lines are copied to `Assets/Turnaj/Sounds`: round-1, round-2, final-round, flawless-victory, hahaha. `mk_style_90sec` is copied but unused. There is no 3-2-1 sound, so the countdown is text only (a field for a clip is fine).

## Folder layout (all new work)
```
Assets/Turnaj/
  Turnaj.unity                  scene
  (design: TOURNAMENT_DESIGN.md in the project root)
  Scripts/                      soTeam, SpawnPoint, TournamentManager, cameras (Rezia port), HUD, helpers
  Scripts/Editor/               TournamentManager inspector (dropdowns, focus banner, result), Scene view overlay
  Prefabs/                      HUD canvas, camera rig, neutral arena with spawn markers
  Teams/                        6 test soTeam assets (Training Results robots + default arena with markers)
  Sounds/                       voice lines
```

## Left to the builder
- The internal state machine, and how robots are frozen (kinematic rigidbodies, or something equivalent that doesn't edit robot code).
- The order of operations when the robots' own `EndEpisode` respawn fires at the end of a round, before the arena is swapped. The robots must end up on the new arena's markers.
- The camera rig, built from last year's `Rezia` but on the new Input System.
- HUD layout details and fonts (TMP).
- The round score is tracked by the tournament script itself. `ScoreGUI` counts only `OnRobotDie`, so it can't see sudden-death or referee knockouts.
- Test teams: copies of `Firehawk.prefab` with the ONNX models from `Training Results/RobotAgent_1…4` assigned, stored in `Assets/Turnaj/Teams`.
- Test team assets and the neutral arena copy (derived from `Prefabs/Environment (0, 0).prefab`, with markers added, and with its world-space training GUIs removed if they get in the way).
