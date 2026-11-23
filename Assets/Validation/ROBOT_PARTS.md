# Robot parts – knowledge base

Source: a dump of every prefab in `Assets/Prefabs/Base Shapes` and of `Assets/Prefabs/Robot Agent Template.prefab` (2026-11-22). The damage values come from `Assets/Scripts/AgentScripts/CollisionForwarder.cs`.

## Layers and the three visuals
| Layer | Name | Used for |
|---|---|---|
| 0 | Default | Containers, sensors, the template's "Bounding Box" visual |
| 6 | RobotView | Visual 3: simple-colour meshes (`BrightWhite` / `BrightBlue`) seen by the agent's camera, under `[No Editing] RobotViewMaterials` |
| 8 | ConstructionView | **Colliders** (MeshCollider, convex) together with their default-view meshes (`Lit`) |

Every part root (Body, Arm, Bumper, Wheels) has `PartVisionColor` (Assets/Validation) with a reference to `VisionPalette`. It sets the part's own RobotView copies to the palette materials: on the body, the copy whose mesh matches the battery collider gets the battery colour and the rest the robot-part colour; on every other part, all copies get the robot-part colour. It runs in the Editor and at runtime, and it doesn't touch parts placed under it.

Visual 2, the team's fancy models, isn't part of the base prefabs. Validation applies to colliders, not to visuals.

## Robot rules agreed so far (2026-11-22)
- Only the provided Base Shapes prefabs may be used, combined freely.
- Each prefab has a maximum number of uses, and each group has a min/max (see below). Every value is configurable in the rules asset.
- All parts except the main body must be children (at any depth) of the main body. The body is a direct child of the robot root.
- Each collider must keep its original mesh. Its scale on each axis may differ from the prefab's own scale by at most ±X %. X is configurable and not decided yet. Mirroring (−1) is allowed, because the right wheel set uses it.
- Sensors are not limited. The agent has a fixed number of inputs, so extra sensors give no advantage. Sensor colliders are fixed: `ProximityRays/Sphere` must stay unchanged, and the `ButtSensorReciever` colliders must stay disabled.

| Group | Prefabs | Min | Max |
|---|---|:---:|:---:|
| Body | Body_Shape1–3 | 1 | 1 |
| Arm | Arm_Shape1–2 | 1 | 1 |
| Wheel sets | Wheels_Shape1–3 | 2 | 2 (one under "Wheels Left", one under "Wheels Right") |
| Bumpers | Bumper_Shape1–2 | 0 | 2 |

## Parts

### Body_Shape1 / 2 / 3: `BodyComponent`
| Child | Layer | Collider (mesh) | Zone | Damage / resistance |
|---|---|---|---|---|
| `body_shape` | 8 | MeshCollider `Body_shapeN_collider` | ✔ | 1 / 3 |
| `battery_shape` | 8 | MeshCollider `Battery` | ✔ | 1 / 1 |
| `ForwardSpeedSensor`, `SidewaysSpeedSensor`, `DamageSensor` | 0 | – | – | – |
| `RobotViewMaterials/body_shape (1)`, `battery_shape (1)` | 6 | none. Body_Shape3 used to have duplicate MeshColliders here; the user removed them on 2026-11-22 | – | – |

### Arm_Shape1 / 2: `Rigidbody` (mass 1) + `HingeJoint` + `ArmComponent`, tag `RobotArm`
| Child | Layer | Collider (mesh) | Zone | Damage / resistance |
|---|---|---|---|---|
| `arm_armbase_shape` | 8 | MeshCollider `Arm_weaponN_collider.001` | ✔ | 2 / 3 |
| `arm_weapon_shape` | 8 | MeshCollider `Arm_weaponN_collider`; the prefab's own scale is (1, 0.88, 1) for Arm 1 and (0.93, 0.90, 1) for Arm 2 | ✔ | 200 / 15 |
| `AngleSensor`, `DamageSensor` | 0 | – | – | – |
| RobotView copies | 6 | none | – | – |

The arm has its own Rigidbody. `RobotMovement` connects its HingeJoint to the robot root's Rigidbody at Start.

### Bumper_Shape1 / 2: `BumperComponent`
| Child | Layer | Collider (mesh) | Zone | Damage / resistance |
|---|---|---|---|---|
| `bumper_shape` | 8 | MeshCollider `Bumper_shapeN` | ✔ | 150 / 10 |
| `DamageSensor` | 0 | – | – | – |
| RobotView copy | 6 | none | – | – |

### Wheels_Shape1 / 2 / 3: `WheelComponent`
| Child | Layer | Collider (mesh) | Zone | Damage / resistance |
|---|---|---|---|---|
| `wheels_shape` | 8 | MeshCollider `Wheel_shape1_L` / `Z_colliderWheel2` / `Z_colliderWheel3`. For Wheels 2 and 3 the visual mesh differs from the collider mesh | ✔ | 5 / 4 |
| `DamageSensor` | 0 | – | – | – |
| RobotView copy | 6 | none | – | – |

The drive WheelColliders are **not** in the wheel prefabs. They are in the template (`Wheels Left/Right/Wheel Coll Front|Back`, 4 in total).

### Sensors (not limited)
| Prefab | Root component | Colliders |
|---|---|---|
| `ProximityRays` | `ProximitySensorRay` (+ `Proximity`, `Rayinfo` sensors) | `Sphere`: SphereCollider, enabled, layer 8, solid, about 1.7 cm radius, no damage zone. **Fixed: it must stay exactly as in the prefab (enabled, same radius, same scale). The ±X % rule doesn't apply to it; it gets 0 %.** |
| `ButtSensorReciever` | `ButtSensorComponent` (+ `ButtSensor`) | `Sphere` SphereCollider and `Cylinder` CapsuleCollider, both **disabled** |

### Robot Agent Template (robot root)
- Root: `Rigidbody` (mass 10), `RobotMovement`, `RobotAgent`, `BehaviorParameters`, `DecisionRequester`, `CameraSensorComponent`, `CollisionForwarder`, tag `Robot`.
- `Bounding Box`: two visual cubes on layer 0 (Cube scale 1.11 × 1.05 × 1.20; Cube.013 0.57 × 0.44 × 2.29). They have no colliders. This was the original size limit for colliders. It's **obsolete**: the ±X % per-collider rule replaces it, because it's easier to use and to check. The validator ignores it.
- `Wheels Left` / `Wheels Right` (scale X −1): 2 WheelColliders each.
- `RobotCameraSensor`: the agent's camera.
- `ButtMarker/MarkerHitPoint` (SphereCollider, enabled, tag `Robot`) and `ButtMarker/Cylinder` (CapsuleCollider, enabled): this is the target the enemy's butt sensor detects. It's part of the template, not a team part.

## Damage-zone naming (CollisionForwarder)
`CollisionForwarder` assigns damage and resistance by checking whether the zone **name contains** one of these words, in this order: `body` 1/3, `battery` 1/1, `wheels` 5/4, `armbase` 2/3, `weapon` 200/15, `bumper` 150/10, otherwise 1/5. Renaming a zone therefore changes its stats. The validator must identify parts by mesh and also check that zone names are unchanged.

## Decided (2026-11-22)
- `ProximityRays` keeps its solid sphere collider, which may not be modified (0 % tolerance).
- `Body_Shape3`'s duplicate RobotView colliders were removed (prefab fixed by the user).
- `Bounding Box` is obsolete; the ±X % rule is used instead.
