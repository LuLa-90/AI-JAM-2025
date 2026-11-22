using UnityEngine;
using UnityEngine.InputSystem;

namespace Turnaj {
    /// <summary>
    /// Director cameras (port of last year's Rezia) on the new Input System.
    /// Numpad 8 top, 5 front, 2 close, 1 / 3 rotating, 4 / 6 Team A / Team B chase.
    /// Keys only arrive while the Game view is focused (default Input System behaviour).
    /// </summary>
    public class TournamentCameras : MonoBehaviour {
        public Camera TopCamera;
        public Camera FrontCamera;
        public Camera CloseCamera;
        public Camera LeftRotCamera;
        public Camera RightRotCamera;
        [Tooltip("Slow orbit shown on the waiting screen (between rounds, after Play).")]
        public Camera WaitingCamera;
        [Tooltip("Camera mask: everything except the RobotView layer, which only robot sensors render.")]
        public LayerMask CullingMask = ~(1 << 6);
        [Tooltip("Parent of the Top / Front / Close cameras; turned to the arena's reading angle on 8 / 5 / 2.")]
        public Transform FixedPivot;
        [Tooltip("Reading angle for arenas without an ArenaViewAngle component.")]
        public float DefaultViewYaw = 120f;

        private float viewYaw;

        [HideInInspector] public Camera ChaseA;
        [HideInInspector] public Camera ChaseB;

        public Camera Active { get; private set; }

        private void Start() {
            foreach (var c in All()) {
                if (c == null) continue;
                c.cullingMask = CullingMask;
                c.enabled = false;
            }
            ShowWaiting();
        }

        public void SetChaseCameras(Camera a, Camera b) {
            ChaseA = a;
            ChaseB = b;
            foreach (var c in new[] { a, b }) {
                if (c == null) continue;
                c.cullingMask = CullingMask;
                c.enabled = false;
            }
        }

        public void ShowWaiting() => On(WaitingCamera != null ? WaitingCamera : FrontCamera);
        public void ShowFront() => OnFixed(FrontCamera);

        /// <summary>Called when an arena is loaded: remembers the angle its floor texts read from.</summary>
        public void SetArena(GameObject arena) {
            var angle = arena != null ? arena.GetComponent<ArenaViewAngle>() : null;
            viewYaw = (angle != null ? angle.Yaw : DefaultViewYaw) + (arena != null ? arena.transform.eulerAngles.y : 0f);
            ResetFixedAngle();
        }

        private void ResetFixedAngle() {
            if (FixedPivot != null) FixedPivot.rotation = Quaternion.Euler(0f, viewYaw, 0f);
        }

        private void OnFixed(Camera cam) {
            ResetFixedAngle();
            On(cam);
        }

        private void Update() {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.numpad8Key.wasReleasedThisFrame) OnFixed(TopCamera);
            if (kb.numpad5Key.wasReleasedThisFrame) OnFixed(FrontCamera);
            if (kb.numpad2Key.wasReleasedThisFrame) OnFixed(CloseCamera);
            if (kb.numpad1Key.wasReleasedThisFrame) On(LeftRotCamera);
            if (kb.numpad3Key.wasReleasedThisFrame) On(RightRotCamera);
            if (kb.numpad4Key.wasReleasedThisFrame) On(ChaseA);
            if (kb.numpad6Key.wasReleasedThisFrame) On(ChaseB);
        }

        public void On(Camera cam) {
            if (cam == null) return;
            foreach (var c in All()) if (c != null) c.enabled = false;
            cam.enabled = true;
            Active = cam;
        }

        private Camera[] All() => new[] { TopCamera, FrontCamera, CloseCamera, LeftRotCamera, RightRotCamera, WaitingCamera, ChaseA, ChaseB };
    }
}
