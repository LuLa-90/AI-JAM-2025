# Arenas and robot vision – knowledge base

Decisions with the user on 2026-11-22. Robot parts are in `ROBOT_PARTS.md`.

## Schedule
- **Hackathon:** next weekend (28.–29. 11. 2026). Teams build and train their robots and arenas, and the robot-vision colours are fixed at the start.
- **Tournament:** Monday 30. 11. 2026.

## Team asset (`Turnaj.soTeam`)
`TeamName`, `RobotPrefab`, `ArenaPrefab`. **The arena is a separate prefab from the robot.** Validation checks both, through the team asset (`TeamValidation`).

## Arena rules
1. **Centred at the origin.** The arena prefab root is at (0, 0, 0), and the floor is at height 0.
2. **Same solid colliders as the neutral arena** (`Turnaj/Prefabs/NeutralArena`). Every non-trigger collider must match the neutral arena's set: the same meshes, at the same position, rotation and scale, within a small tolerance. Today that set is `arena_collidersS/Cylinder` and `arena_collidersS/Cylinder.001`, both MeshColliders on layer 0. Floor size and shape are therefore identical. The wall is assumed to be included (my call, not yet confirmed).
3. **No hard obstacles.** Every other collider must be a **trigger**.
4. **Spawn points.** There is exactly one `SpawnPoint` Home and one Guest, and they must be **symmetric across the centre**: Guest position = −Home position (x and z), and Guest yaw = Home yaw + 180°, within tolerance.
5. **Allowed freely:** custom music, sounds, VFX and particle effects, and NPCs that spawn but don't interact with robots. NPCs have no solid colliders (rule 3).
6. **Traps and buffs** are team-written scripts on trigger colliders. The validator can't judge what custom code does, so it **lists every custom script** in the arena for a human reviewer, together with what each one is attached to.
7. **Runtime spawns stay under the arena** (decided 2026-11-23). Everything an arena creates while it runs (NPCs, VFX, projectiles, sounds) must be a **child of the arena object**, so it is destroyed together with the arena between rounds. The test match fails "Arena spawns" for any new scene root object, and the tournament destroys such objects at the round change and logs a warning.
8. **Optional:** `ArenaViewAngle` sets the camera angle from which the floor text reads correctly.

## Robot vision (agent camera)
- `RobotCameraSensor`: 32 × 32 pixels, in colour. Its culling mask is **Default (0), TransparentFX, Ignore Raycast, Water, UI, RobotView (6)**. It does **not** see ConstructionView (8).
- The robots learn what to do from colours. **Colours to validate (decided 2026-11-22):**

| What | Colour | Behaviour the agent should learn |
|---|---|---|
| Default: everything not listed below (floor, walls, NPCs…) | **black**: the camera background; arenas have **no** black RobotView objects (decided 2026-11-23) | – |
| Robot parts (the enemy) | **white** | ram it |
| Trap | **red** | avoid |
| Buff | **green** | approach |
| Battery | **blue** | attack it |

- **The colours stay selectable** (one palette asset). They may still change at the start of the hackathon.

### Layers (decided 2026-11-22)
- **TournamentView (new):** fancy visuals. Only the tournament cameras render it.
- **RobotView (6)** is the agent's layer; the name was kept. Only the robot camera sensor renders it, and only palette materials are allowed on it.
- **ConstructionView (8):** colliders plus the default editor view.

### State after the fixes (2026-11-22)
- `VisionPalette.asset` (Assets/Validation) holds one material per role. A new `BrightBlack` was added; the others are the existing Bright* materials.
  - The materials glow (HDR emission), and the glow decides most of what the camera sees (the battery looks azure because its emission has some green). Changing a colour in the palette sets that material's base colour **and** its emission colour, keeping the glow strength. Materials whose colour isn't changed are never touched, so last year's robots keep the look they were trained on (decided 2026-11-23).
- The template's agent camera sees RobotView only, on a black background. Spawners (training `AIJamManager`, `TournamentManager`, the validation scene) force this on every robot through `VisionLayers.ForceAgentCamera`.
- Body_Shape2's battery copy is now BrightBlue.
- **Arenas have no RobotView objects except red traps and green buffs on triggers** (decided 2026-11-23). The camera already clears to black, so floor and wall copies are unnecessary; the black copies that were added to NeutralArena and `Environment (0, 0)` were removed. A black RobotView object in an arena is an error.
- TournamentView is layer 9.
- Floor and wall are both black (default), so the agent camera can't see the walls. **Decided 2026-11-22: walls are detected with the proximity sensor**, and teams that need wall awareness add `ProximityRays` to their robot. No separate wall colour.

### Before the fixes (found 2026-11-22)
- Four vision materials exist, in `Assets/Lights (Colors)`: BrightRed, BrightGreen, BrightBlue and BrightWhite. **There is no black.**
- The robot parts' RobotView copies (layer 6) use **BrightWhite** for body, arm, wheels and bumper, which matches. The battery uses **BrightBlue** on Body_Shape1 and Body_Shape3, which matches, but **BrightWhite on Body_Shape2**, which is a prefab bug.
- The neutral arena has **no objects on RobotView (6)**. The agent camera sees its floor and wall through layer 0, with their normal (Lit) materials.
- Anything a team puts on **Default (0)** is seen by the agent camera. That includes fancy visuals, which could be painted "green" to fool the opponent. Which layers fancy visuals may use is still open.
