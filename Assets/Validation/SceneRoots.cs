using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AIJam.Validation {
    /// <summary>
    /// Arena rule: everything an arena spawns at runtime (NPCs, VFX, projectiles...) must be a child of the
    /// arena object, so it is destroyed with the arena. Take a snapshot of the scene's root objects before the
    /// arena is instantiated; any later root object that isn't allowed was spawned outside the arena.
    /// </summary>
    public sealed class SceneRoots {
        private readonly HashSet<GameObject> known;

        private SceneRoots(HashSet<GameObject> known) { this.known = known; }

        public static SceneRoots Take() => new SceneRoots(new HashSet<GameObject>(Roots()));

        /// <summary>Root objects created since the snapshot, except <paramref name="allowed"/> (the arena, the match root...).</summary>
        public List<GameObject> NewSince(params GameObject[] allowed) =>
            Roots().Where(g => g != null && !known.Contains(g) && !allowed.Contains(g)).ToList();

        public static string Names(List<GameObject> objects) => string.Join(", ", objects.Select(g => $"'{g.name}'"));

        private static IEnumerable<GameObject> Roots() => SceneManager.GetActiveScene().GetRootGameObjects();
    }
}
