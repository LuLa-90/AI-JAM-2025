using System.Collections.Generic;
using UnityEngine;

namespace AIJam.Validation {
    /// <summary>
    /// All limits for robot and arena validation (see DESIGN.md, ROBOT_PARTS.md, ARENAS.md).
    /// Part shapes are NOT stored here: they are read from the listed prefabs at check time.
    /// </summary>
    [CreateAssetMenu(fileName = "ValidationRules", menuName = "AI Jam/Validation Rules")]
    public class ValidationRules : ScriptableObject {
        [System.Serializable]
        public class PartEntry {
            public GameObject Prefab;
            [Tooltip("Maximum uses of this prefab on one robot.")]
            public int MaxUses = 1;
        }

        [System.Serializable]
        public class PartGroup {
            public string Name;
            public int Min;
            public int Max;
            public List<PartEntry> Parts = new List<PartEntry>();
        }

        [Header("Robot parts (provided Base Shapes)")]
        public List<PartGroup> Groups = new List<PartGroup>();
        [Tooltip("Sensor prefabs: unlimited count, colliders fixed (0 % change, same enabled state).")]
        public List<GameObject> SensorPrefabs = new List<GameObject>();
        [Tooltip("Robot template: reference for root physics, wheel colliders, ButtMarker and camera.")]
        public GameObject RobotTemplate;

        [Header("Model")]
        [Tooltip("Behavior Name every robot must use (trainer config konfig.yaml).")]
        public string BehaviorName = "RobotAgent";
        [Tooltip("Required vector observation size; 0 = not checked (not decided yet).")]
        public int VectorObservationSize = 0;

        [Header("Robot limits")]
        [Tooltip("Max change of each collider axis vs. the part prefab, in percent (not decided yet).")]
        public float ScaleTolerancePercent = 10f;
        [Tooltip("Relative tolerance for locked physics values (mass, drag, springs...).")]
        public float PhysicsTolerance = 0.0001f;

        [Header("Attachment (penetration in metres)")]
        public float MinAttachmentPenetration = 0.0001f;
        public float MaxWheelBodyPenetration = 0.02f;
        public float MaxWheelBumperPenetration = 0.005f;

        [Header("Vision")]
        public VisionPalette Palette;

        [Header("Arena")]
        [Tooltip("Arena whose solid colliders every team arena must match (the neutral arena).")]
        public GameObject ReferenceArena;
        public float ArenaPositionTolerance = 0.005f;
        public float ArenaScaleTolerance = 0.001f;
        public float SpawnSymmetryTolerance = 0.05f;
        public float SpawnYawToleranceDegrees = 2f;
        public float SpawnMaxHeight = 0.3f;
    }
}
