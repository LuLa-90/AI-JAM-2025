using UnityEngine;

namespace AIJam.Validation {
    /// <summary>Layer conventions (see ARENAS.md) and the agent-camera fix used by every spawner.</summary>
    public static class VisionLayers {
        public const string RobotViewName = "RobotView";       // 6: agent view (palette colours only)
        public const string ConstructionViewName = "ConstructionView"; // 8: colliders + default view
        public const string TournamentViewName = "TournamentView";     // fancy visuals, tournament cameras only

        public static int RobotView => LayerMask.NameToLayer(RobotViewName);
        public static int AgentCameraMask => 1 << RobotView;

        /// <summary>
        /// Forces the robot's agent camera to see only RobotView on a black background.
        /// Fixes old robot prefabs whose camera also saw Default (a bug).
        /// </summary>
        public static void ForceAgentCamera(GameObject robot) {
            if (robot == null || RobotView < 0) return;
            foreach (var cam in robot.GetComponentsInChildren<Camera>(true)) {
                if (!cam.CompareTag("RobotCamera")) continue;
                cam.cullingMask = AgentCameraMask;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
            }
        }
    }
}
