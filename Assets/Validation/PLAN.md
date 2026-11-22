# Validation – implementation plan

> Executed natively in this session. The spec is `DESIGN.md`; the reference docs are `ROBOT_PARTS.md` and `ARENAS.md`.

**Goal:** `Assets/Validation/Validation.unity`: pick a team, press Play, then **Run validation** (rule findings) or **Run test match** (six measured events).
**Architecture:**
- A pure engine (`RobotValidator`, `ArenaValidator`) checks prefab assets against a `ValidationRules` asset and returns `Finding`s.
- Part fingerprints (collider meshes, local offsets, physical settings) are read from the Base Shapes prefabs at check time, so the rules asset holds only limits.
- `TeamValidation` (used by the tournament) wraps the engine.
- The scene's runner draws the report in a custom Inspector and drives the test match.

## Tasks
- [x] **1. Vision assets and fixes outside the folder:**
  - `VisionPalette` asset plus a new `BrightBlack` material; the TournamentView layer.
  - Template camera mask → RobotView only; Body_Shape2 battery → BrightBlue.
  - Black RobotView floor and wall in NeutralArena and Environment (0, 0).
  - Spawners (`AIJamManager`, `TournamentManager`) force the camera mask.
- [x] **2. Rules and engine:**
  - `ValidationRules` asset (part groups, per-prefab maximums, X %, tolerances, palette, reference arena).
  - `Finding`, `RobotValidator.Validate(GameObject, ValidationRules)` and `ArenaValidator.Validate(GameObject, ValidationRules)`.
- [x] **3. Reference robot:** `Validation/Robots/ReferenceBot.prefab` with the new layout (every part under the body, wheel containers under the body), built from Firehawk. The test teams are rebuilt the same way.
- [x] **4. Tests (EditMode):**
  - The reference robot passes, and so does the neutral arena checked as a team arena.
  - Mutated copies each fail with the expected finding: scaled collider, extra solid collider, renamed zone, mass changed, fake RobotView shape, red without trigger, asymmetric spawn.
- [x] **5. Tournament hookup:** `TeamValidation` uses the engine; errors block, warnings don't; an "Ignore validation" switch.
- [x] **6. Validation scene:** runner, Inspector (team dropdown, the two buttons, findings with click-to-select, copyable report), and an agent-camera preview.
- [x] **7. Test match:** a scripted enemy (ReferenceBot with a scripted driver) and events 1–6 with measurements, run as a sequence or one at a time.
- [x] **8. Verify:** tests, plus a Play run of both buttons for a test team.

## Done (2026-11-22) – notes from building
- 19 validator tests and 24 tournament rule tests pass. The validation scene runs both buttons for test team Alpha; all six events behave as expected (a trap with no effect measures 100 % speed and unchanged HP).
- **A disabled `DecisionRequester` still requests decisions.** ML-Agents ignores its enabled flag, so the validation scene removes the component; robots there are driven only by the test.
- **The robot template's Behavior settings are stale** ("My Behavior", 7 observations). The rules require `RobotAgent` (konfig.yaml); the observation size is not checked (0) until decided.
- **Firehawk fails the new rules:** old layout, disabled sensor colliders, wheels sunk 2.9 cm into the bumper. `Validation/Robots/ReferenceBot` is Firehawk rebuilt to the new layout (bumper moved 7 cm forward) and passes. The test teams in `Turnaj/Teams` are now ReferenceBot variants.
