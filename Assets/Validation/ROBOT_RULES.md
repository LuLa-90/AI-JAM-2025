# AI JAM 2026 – Robot rules

These rules are for the teams. Your robot must follow them to fight in the tournament (30. 11. 2026).
- **Check your robot any time:** open `Assets/Validation/Validation.unity`, pick your team, press Play and click **Run validation**.
- **Final validation:** the organisers run it after the presentations on 29. 11. 2026 at 12:00. A robot with errors cannot fight. Warnings don't block a robot, but a person reviews them.

Values marked *(not final)* are announced at the start of the hackathon.

## 1. Your team
1. Your team is one `soTeam` asset with:
   - `TeamName`;
   - `RobotPrefab`, your robot;
   - `ArenaPrefab`, your home arena (see `ARENAS.md`).
2. The robot and the arena must be **separate prefabs**.

## 2. The three looks of a robot
| Look | Who sees it | Rules |
|---|---|---|
| **Colliders + default view** (layer ConstructionView) | the physics, you in the Editor | **Strict.** Most of the rules below are about this look. |
| **Fancy model** (layer TournamentView) | the audience | **Free.** Use any models, materials and effects. They must have no colliders. |
| **Agent view** (layer RobotView) | your robot's camera | Fixed colours only (rule 7) |

The rules check the colliders, not what the robot looks like. Your fancy model can be anything, but the physical robot is the same size and strength as everyone else's.

## 3. Root of the robot
1. Start from the **Robot Agent Template** prefab.
2. The root keeps `RobotAgent`, `RobotMovement`, `BehaviorParameters`, `Rigidbody`, `CollisionForwarder` and the camera sensor. Its scale stays at 1.
3. **Behavior Name** = `RobotAgent`. A trained model (`.onnx`) must be assigned in `BehaviorParameters`.

## 4. Parts
1. Build only from the provided **Base Shapes** prefabs. Combine them freely within these limits:

| Group | Prefabs | How many |
|---|---|---|
| Body | Body_Shape1, 2, 3 | exactly 1 |
| Arm (weapon) | Arm_Shape1, 2 | exactly 1 |
| Wheel sets | Wheels_Shape1, 2, 3 | exactly 2: one under `Wheels Left`, one under `Wheels Right`. The same shape twice is allowed. |
| Bumpers | Bumper_Shape1, 2 | 0–2 |

2. **Structure:**
   - The body is a **direct child of the robot root**.
   - **Every other part sits under the body**, at any depth. That includes the arm, the bumpers, and the `Wheels Left` / `Wheels Right` containers with their wheel colliders.
   - `RobotCameraSensor`, `ButtMarker` and `Bounding Box` may stay under the root.
3. **Parts must touch, but not overlap:**
   - wheels, battery, bumpers and arm touch the body;
   - the weapon touches the arm;
   - **no hiding parts inside each other.** A wheel may sink at most **2 cm** into the body and at most **0.5 cm** into a bumper. A wheel buried in the body is an error.
4. **Angles are free.** Rotate whole parts to any angle you like. Only the colliders inside a part stay as they are (rule 5.4).
5. Don't rename the part objects that carry a damage zone. Their names decide the damage and resistance: body, battery, wheels, armbase, weapon, bumper.

## 5. Colliders
1. **Every enabled collider on the robot must be one of these:**
   - a collider of a provided part;
   - a collider of the template (wheel colliders, `ButtMarker`);
   - a sensor's fixed collider.

   Any other collider is an error. You can't add extra shields, spikes or hidden blocks.
2. **Shape:** a collider keeps its original mesh. You can't swap one mesh for another.
3. **Size:** each axis of each collider may differ from the part prefab by at most **±10 %** *(not final)*. Each axis is checked separately.
   - Mirroring (−1 on an axis) is allowed.
   - The size is measured relative to the robot root. Scaling a parent object counts too.
4. **Position:** a collider stays where it is inside its part. Move and rotate **whole parts**, not the colliders inside them.
5. Colliders stay enabled, stay solid (not triggers) and keep their physics material.

## 6. Physics is locked
These must stay exactly as in the template or the part prefab:
- Rigidbody mass, drag, angular drag and centre of mass (robot root and arm);
- the 4 wheel colliders: radius, suspension, friction and position;
- the arm's HingeJoint: spring, motor and limits.

Don't add extra Rigidbodies or Joints.

## 7. Agent view (what your robot's camera sees)
1. The robot camera sees **only the RobotView layer**, on a black background. The tournament and training scenes force this setting.
2. Colours *(the exact colours can change at the hackathon start)*:

| Colour | Meaning |
|---|---|
| black | everything else (floor, walls) |
| white | robot parts |
| red | trap (avoid it) |
| green | buff (go for it) |
| blue | battery (attack it) |

3. **No fake shapes.** Every RobotView mesh on your robot must be the part's own copy:
   - the same mesh, position, rotation and size as that part's collider;
   - the battery copy is blue, every other part copy is white;
   - no extra RobotView meshes.
4. The camera's own view isn't validated. To see what your robot sees, use the preview in the validation scene.

## 8. Sensors
1. Sensors (`ProximityRays`, `ButtSensorReciever`) are **not limited**. The agent has a fixed number of inputs, so extra sensors give no advantage.
2. Sensor colliders are fixed:
   - `ProximityRays/Sphere` stays exactly as it is in the prefab;
   - the `ButtSensorReciever` colliders stay disabled.
3. **Walls:** the camera sees walls only as black, so use a `ProximityRays` sensor to detect them.

## 9. Scripts
Your own scripts on the robot are allowed, but they are listed as warnings and the organisers check them. A script that changes physics, HP or the rules during a match disqualifies the robot.

## 10. Test your robot
Use **Run test match** in the validation scene. It puts your robot in your arena against a scripted enemy and measures six events:
1. the enemy rams you;
2. you fall out of the arena;
3. you drive through the traps and buffs;
4. you flip over;
5. your body is hit;
6. your battery is hit.

The results show how your robot takes damage. They don't decide whether you pass.

---
Details of every part (colliders, damage table): `ROBOT_PARTS.md`. Arena rules: `ARENAS.md`. Exact limits: `ValidationRules.asset`.
