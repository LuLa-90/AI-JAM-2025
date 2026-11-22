using UnityEngine;

namespace Turnaj {
    /// <summary>
    /// Optional, on the arena prefab root: the yaw from which the arena floor texts read correctly.
    /// Cameras 8 / 5 / 2 are turned to this angle whenever they are selected.
    /// Without it, TournamentCameras.DefaultViewYaw is used (120° fits the default arena logo).
    /// </summary>
    public class ArenaViewAngle : MonoBehaviour {
        [Tooltip("Camera yaw in degrees (rotation around Y) from which the floor texts are readable.")]
        public float Yaw = 120f;
    }
}
