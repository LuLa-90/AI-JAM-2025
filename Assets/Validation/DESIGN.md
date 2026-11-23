# Validation scene – design

Date: 2026-11-22. The hackathon ends **29. 11. 2026 at 12:00** (presentations), then the organisers validate each team. The tournament is on 30. 11. 2026.
Reference documents in this folder: `ROBOT_PARTS.md` (all base parts, robot rules) and `ARENAS.md` (arena rules, vision colours, layers).

## Intent
Teams build a robot (from the provided parts) and a home arena during the hackathon. They need a scene where they can **continuously check** that both follow the rules, and where they can **see how their robot behaves** in typical match situations. After the presentations the organisers use the same scene for the **final validation, one team at a time**. The goal is a fair tournament: same physical limits for everyone, personalised looks allowed.

## Goals
- One scene, `Assets/Validation/Validation.unity`. Pick a team (`soTeam`) in the Inspector, press Play, then use one of two buttons:
  1. **Run validation**: automated check of the robot prefab and the arena prefab against the rules. The result is a list of findings (error or warning, message, clickable object), plus a copyable text report.
  2. **Run test match**: the team's robot in its own arena against a scripted enemy, with six test events, run as a sequence or one at a time. Each event writes a measured result.
- A live preview of **what the team robot's camera sees** (RobotView only), so teams can check their colours.
- The same rule engine is used by the tournament scene's pre-Play check (`TeamValidation`).

## Non-goals
- Validating all teams in one click.
- Judging what custom trap/buff code does. It's only listed for a human reviewer and measured in the test match.
- Limiting sensors. The agent has a fixed number of inputs.
- Checking fancy visuals (TournamentView). They're free.

## Anti-goals
- Rules matched by object **names**. Parts are identified by collider **mesh**, so renaming must neither break nor bypass a check.
- A result that passes a robot which has extra solid colliders, renamed damage zones, or scaled-up fixed colliders.
- Changing how training works without saying so. Changes to the robot template or the spawners are listed explicitly below.

## Rules to check (from `ROBOT_PARTS.md` and `ARENAS.md`)

### Robot (team `RobotPrefab`)
1. **Root:** has `RobotAgent`, `BehaviorParameters` with a model assigned, `Rigidbody`, and `CollisionForwarder`.
2. **Structure:** exactly one body part, a direct child of the root. Every other part sits anywhere below the body. The template objects (`Wheels Left/Right` with their WheelColliders, `RobotCameraSensor`, `ButtMarker`, `Bounding Box`) are not parts; where they sit is explained under "Left to the builder".
3. **Parts:** only Base Shapes prefabs, identified by collider mesh. Group min/max: Body 1/1, Arm 1/1, Wheel sets 2/2 (one under `Wheels Left`, one under `Wheels Right`), Bumpers 0/2. There's also a maximum count per prefab. All numbers live in the rules asset.
4. **Colliders:** every enabled collider on the robot must be one of:
   - a part's collider mesh;
   - a template collider (the WheelColliders and `ButtMarker`);
   - a sensor's fixed collider.

   Anything else is an error.
5. **Size:** each part collider's scale on each axis may differ from the prefab's own scale by at most **±X %** (rules asset, placeholder 10 %). Mirroring (−1) is allowed. The mesh must be unchanged. `ProximityRays/Sphere` must match exactly (0 %), and the `ButtSensorReciever` colliders must stay disabled.
6. **Damage zones:** every part collider keeps its `CollisionZoneBehaviour`, and its object name keeps the same damage keyword as in the prefab (`body`, `battery`, `wheels`, `armbase`, `weapon`, `bumper`). The check compares the zone stats `CollisionForwarder` would assign against the prefab's.
7. **Attachment** (taken from `PhysicalOverlapValidator`): wheels, battery, bumpers and arm touch the body, and the weapon touches the arm, each with a penetration limit. This uses `Physics.ComputePenetration` on the part colliders.
8. **Robot vision:**
   - The robot camera's culling mask is **RobotView (6) only**.
   - Every renderer on RobotView uses a palette material: the battery uses the battery colour, every other part uses the robot-part colour.
   - **No fake shapes** (decided 2026-11-22): every RobotView renderer must be its part's own copy, with the same mesh and the same position, rotation and size as that part's collider. Any extra RobotView renderer is an error.
9. **Physical settings are locked** (decided 2026-11-22). They must match the template or part prefab **exactly**:
   - Rigidbody mass, drag, angular drag and centre of mass (root and arm);
   - WheelCollider count (4), radius, suspension, friction and local position;
   - the arm's HingeJoint spring, motor and limits;
   - PhysicMaterials on colliders;
   - no extra Rigidbodies or Joints.
10. **How size and position are measured** (my call): each collider's size is compared relative to the robot root, including the scale of its parents, so scaling a parent is caught. Mirroring is allowed. A collider must keep its offset inside its part; teams position whole parts.

### Arena (team `ArenaPrefab`)
1. The root is at (0, 0, 0) with identity rotation and scale 1.
2. **Solid colliders:** every enabled non-trigger collider matches the reference arena's set (the neutral arena's colliders: same mesh, same position, rotation and scale within a small tolerance). Nothing is missing and nothing is extra. All other colliders are triggers.
3. **Spawns:** exactly one `SpawnPoint` Home and one Guest, both near floor height, with Guest = −Home in x and z, and yaws 180° apart, within tolerance.
4. **Vision** (decided 2026-11-22):
   - Every renderer on RobotView uses a palette material.
   - **Red or green only on an object that has a trigger collider or sits inside one.** That rules out fake traps and buffs.
   - **White and blue are forbidden in arenas.** That rules out fake robots and batteries.
   - **Nothing else on RobotView** (decided 2026-11-23): no black floor or wall copies; the camera background is black. A black RobotView object is an error.
5. **Custom scripts:** every MonoBehaviour whose type is not from Unity, ML-Agents or this project's known scripts is listed with its object path, as a manual-review list (warning level).
6. Music, sounds, VFX, particles and NPCs are allowed. NPCs fall under rule 2: they may have no solid colliders.
7. **Runtime spawns** (decided 2026-11-23) must be children of the arena object. `SceneRoots` snapshots the scene roots before the arena is instantiated; the test match reports new roots as a failed "Arena spawns" row, and `TournamentManager` destroys them at the round change with a warning.

### Team
- `TeamName` is set, and `RobotPrefab` and `ArenaPrefab` are set and are separate prefabs.
- `TeamValidation` (used by the tournament scene) runs the full robot and arena checks through the new engine.

## Vision palette and layers (decided)
| Role | Colour |
|---|---|
| Default (everything not listed) | black |
| Robot parts | white |
| Trap | red |
| Buff | green |
| Battery | blue |

- **`VisionPalette` asset:** one material per role. It reuses BrightWhite, BrightRed, BrightGreen and BrightBlue from `Assets/Lights (Colors)` and adds a new **BrightBlack**. Changing a colour in the palette updates the shared material, so every prefab follows. Colours can change at the hackathon start.
- **Layers:**
  - **RobotView (6):** the agent's view. The robot camera sees only this layer. The name stays RobotView.
  - **TournamentView:** a new layer for fancy visuals. Only the tournament cameras see it.
  - **ConstructionView (8):** colliders plus the default view.

## Test match (Run test match)
- **Setup:**
  - The team's arena sits at the origin, with the team's robot on Home.
  - The **scripted enemy** stands on Guest: a valid robot built from base parts, driven by a simple script, not an ML model.
  - The team robot runs its own model (InferenceOnly), and its camera mask is forced to RobotView.
- **Events** (sequence or individually; each restores HP and positions before it starts):

| # | Event | How it's produced | Measured | ✔ / ⚠ rule |
|---|---|---|---|---|
| 1 | Enemy rams | the enemy drives straight at the robot | per-zone HP before/after, robot HP before/after | ✔ if any zone took damage |
| 2 | Robot falls | lifted over the wall and dropped outside | HP change, respawn position | ✔ if respawned at its own marker with HP lowered by the fall penalty |
| 3 | Traps and buffs | moved slowly through the centre of each trigger collider in the arena, one at a time | HP, speed and position change while inside | measured only (team design) |
| 4 | Robot flips | turned upside down on the floor | HP change, respawn | ✔ if respawned with HP lowered by the flip penalty |
| 5 | Body hit | a weapon probe (rigid body tagged `RobotArm` with weapon zone stats) launched at the body collider | body zone and robot HP before/after | ✔ if the body zone HP dropped |
| 6 | Battery hit | the same probe launched at the battery collider | battery zone and robot HP before/after | ✔ if the battery zone HP dropped |

- The report is a table in the Inspector, plus copyable text.

## Tournament pre-Play check (my call)
- Errors block the match; warnings don't.
- The neutral arena is the reference, so it's checked only for origin and spawns.
- The tournament Inspector gets an "Ignore validation" switch, for a manual decision on match day.

## Required changes outside `Assets/Validation` (approved by the user 2026-11-22)
- `Robot Agent Template.prefab`: set the camera culling mask to RobotView only. This fixes the bug where the agent saw Default.
- `Body_Shape2.prefab`: set the battery's RobotView copy to the battery material (it's white now).
- The neutral arena (`Turnaj/Prefabs/NeutralArena` and `Prefabs/Environment (0, 0)`): ~~add black RobotView copies of the floor and wall~~. Reverted 2026-11-23: arenas have no RobotView parts besides traps and buffs.
- Add the TournamentView layer. Tournament cameras render TournamentView plus the current layers.
- Spawners (training `AIJamManager` and `TournamentManager`): force the camera mask to RobotView when a robot spawns, so old robot prefabs are fixed too. Old models then see a black floor and wall; check Firehawk's behaviour once.
- The old `RobotHierarchyValidator` / `PhysicalOverlapValidator` stay in place for now, because `ConstructionScene` uses them. Replacing or removing them comes later.

## Left to the builder
- The engine's structure, as long as the robot and arena checks are pure functions over prefab assets that return findings.
- Template objects (decided by the builder): every part must have the body as an ancestor, so the `Wheels Left/Right` containers, with their WheelColliders, move **under the body**. `RobotCameraSensor`, `ButtMarker` and `Bounding Box` may stay under the root. The template and the test robots are rebuilt to this layout.
- The scripted enemy's driving logic and its exact part combination.
- Numeric tolerances other than X (spawn symmetry, arena collider match). Pick sensible values and expose them in the rules asset.
- The Inspector layout and where the camera preview is drawn (Inspector preview or a small Game view inset; it must not appear in the tournament scene).
