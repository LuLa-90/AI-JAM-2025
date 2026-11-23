# Tournament scene – implementation plan

> Executed natively in this session (the user said "go ahead"). Steps use checkboxes.

**Goal:** `Assets/Turnaj/Turnaj.unity`: one match per Play session, run by a director and a referee, captured by OBS.
**Architecture:**
- `TournamentManager` is a phase machine: Waiting → Countdown → Fight → SuddenDeath → RoundOver → MatchOver.
- Every round end goes through the robot's own death path: the loser's zones are set to 0, `OnRobotDie` fires, then `EndEpisode` resets HP.
- Pure rules live in `MatchRules` and `DoubleTap`, and they're unit-tested. The HUD builds itself in code, and the HP boards reuse `StatisticsGUI`.

**Tech:** Unity 6000.6.6f1, ML-Agents 4.1.0, Input System 1.20, uGUI + TMP, Unity Test Framework 1.8 (EditMode).
**Spec:** `Assets/Turnaj/DESIGN.md`

## Global constraints
- Nothing changes outside `Assets/Turnaj/`. Input settings are not changed.
- Keys work only while the Game view has focus. F1–F4 are not used.
- Points: 2:0 = 3, 2:1 = 2, 1:2 = 1, 0:2 = 0. Flawless: bar % > 51, where bar = (hp − 0.2) / 0.8.
- Double tap: two presses within 0.4 s, for Q, W, E and R. Separator cooldown: 1 s. Round length: 90 s by default.

## Review focus
1. A robot falls during sudden death. It must lose, and not respawn into a loop.
2. Pressing R or T during the countdown must not leave the robots unfrozen or the time scale stuck at 0.
3. Respawn events fired by `EndEpisode` at a round end must not trigger fall logic or knock out the wrong robot.
4. An arena with no spawn markers, or a robot prefab with no model, must be caught before Play and shown in the Inspector.
5. Hits from walls and the floor during sudden death must not end the round. Only zone hits tagged `Robot` or `RobotArm` count.

## Tasks
- [ ] **1. Rules + tests:** `Scripts/MatchRules.cs`:
  - `LeaguePoints(int won, int lost)`, `BarPercent(float hp)`, `IsFlawless(float hp)`
  - `IsMatchOver(int a, int b)`, `ArenaOwner(int round)` (0 = A, 1 = B, 2 = neutral)
  - `MirrorThroughCentre(Vector3 p, Vector3 centre)`

  `Scripts/DoubleTap.cs`: `bool Tap(float now)`. Tests in `Scripts/Editor/Tests/MatchRulesTests.cs`, run with `unity command test run --filter MatchRules`.
- [ ] **2. Data:** `soTeam` (`TeamName`, `RobotPrefab`, `ArenaPrefab`) and `SpawnPoint` (`SpawnSide Home/Guest`, gizmo). Also `TeamValidation.Check(soTeam) -> List<string>`, which checks for a robot with a `RobotAgent`, a model, an arena, and both markers near y = 0.
- [ ] **3. Match logic:** `TournamentManager`, covering spawning, freezing (kinematic rigidbodies), respawn on markers, sudden death, the separator, knockouts, replay, pause, sounds and the result summary.
- [ ] **4. Show:** `TournamentHUD` (built in code: title, big centre text, timer, score, flash), `TournamentCameras` (Rezia port on the Input System, plus chase and orbit cameras), and a stats board prefab.
- [ ] **5. Editor:** `TournamentManagerEditor` (team dropdowns, validation, a focus banner, the result with a Copy button) and the Scene view overlay.
- [ ] **6. Assets + scene:** copy the sounds, build the neutral arena prefab with markers, create 6 test teams (Firehawk variants with models 1–4), and assemble `Turnaj.unity` with a RewardEditor.
- [ ] **7. Verify:** run the tests, then a Play-mode run through the countdown, a fight, a separator push, a knockout and the match end, checking Game view captures.
