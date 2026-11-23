using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AIJam.Validation {
    /// <summary>
    /// Put on the root of a robot part (body, arm, wheels, bumper). Gives the part's own agent-view copies
    /// (renderers on the RobotView layer) the palette colour:
    /// - main body: the battery copy gets the battery colour, every other copy the robot-part colour;
    /// - every other part: one colour, the robot-part colour.
    /// Only this part's renderers are touched; parts placed under it (wheels, arm... under the body) colour themselves.
    /// Runs in the Editor and at runtime, so a palette change reaches every robot.
    /// </summary>
    [ExecuteAlways, DisallowMultipleComponent]
    public class PartVisionColor : MonoBehaviour {
        public VisionPalette Palette;

        private void OnEnable() => Apply();
        private void OnValidate() => Apply();

        [ContextMenu("Apply palette")]
        public void Apply() {
            if (Palette == null) return;
            var robotView = LayerMask.NameToLayer(VisionLayers.RobotViewName);
            if (robotView < 0) return;
            var partMat = Palette.MaterialFor(VisionRole.RobotPart);
            var batteryMat = Palette.MaterialFor(VisionRole.Battery);
            var batteryMeshes = BatteryMeshes();

            foreach (var r in GetComponentsInChildren<Renderer>(true)) {
                if (r.gameObject.layer != robotView || PartCatalog.PartRootOf(r.transform) != transform) continue;
                var filter = r.GetComponent<MeshFilter>();
                bool battery = filter != null && batteryMeshes.Contains(filter.sharedMesh);
                Set(r, battery ? batteryMat : partMat);
            }
        }

        /// <summary>Meshes of this part's battery colliders (identified by the damage zone, not by renderer names). Only the body has one.</summary>
        private HashSet<Mesh> BatteryMeshes() {
            var set = new HashSet<Mesh>();
            if (GetComponent<BodyComponent>() == null) return set;
            foreach (var col in GetComponentsInChildren<MeshCollider>(true)) {
                if (PartCatalog.PartRootOf(col.transform) != transform || col.sharedMesh == null) continue;
                if (col.GetComponent<CollisionZoneBehaviour>() != null && PartCatalog.ZoneKeyword(col.name) == "battery") set.Add(col.sharedMesh);
            }
            return set;
        }

        private static void Set(Renderer r, Material m) {
            if (m == null) return;
            var mats = r.sharedMaterials;
            if (mats.Length > 0 && mats.All(x => x == m)) return;
            r.sharedMaterials = Enumerable.Repeat(m, Mathf.Max(1, mats.Length)).ToArray();
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(r);
#endif
        }
    }
}
