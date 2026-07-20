using System;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Describes one travel lane: where objects enter, where they move toward, and how often they spawn.
    /// </summary>
    [Serializable]
    public sealed class SpawnLane
    {
        /// <summary>
        /// Transform where objects enter this lane.
        /// </summary>
        [Tooltip("World-space transform where objects enter this lane.")]
        public Transform spawnPoint;

        /// <summary>
        /// Transform that objects move toward. Falls back to the first spawn-point child when empty.
        /// </summary>
        [Tooltip("World-space transform that objects move toward. If empty, the first child of the spawn point is used.")]
        public Transform targetPoint;

        /// <summary>
        /// Minimum seconds before this lane spawns another object.
        /// </summary>
        [Tooltip("Minimum seconds before this lane spawns another object.")]
        [Min(0.05f)] public float spawnIntervalMin = 0.8f;

        /// <summary>
        /// Maximum seconds before this lane spawns another object.
        /// </summary>
        [Tooltip("Maximum seconds before this lane spawns another object.")]
        [Min(0.05f)] public float spawnIntervalMax = 1.8f;

        /// <summary>
        /// Runtime timestamp for this lane's next spawn.
        /// </summary>
        [NonSerialized] public float nextSpawnTime;

        /// <summary>
        /// Whether this lane has enough information to spawn an object.
        /// </summary>
        public bool HasSpawnPoint => spawnPoint != null;

        /// <summary>
        /// Resolves the target point, falling back to the first child or a vertical despawn position.
        /// </summary>
        public Vector3 GetTargetPosition(float fallbackY)
        {
            if (targetPoint != null)
            {
                return targetPoint.position;
            }

            if (spawnPoint != null && spawnPoint.childCount > 0)
            {
                return spawnPoint.GetChild(0).position;
            }

            Vector3 fallback = spawnPoint != null ? spawnPoint.position : Vector3.zero;
            fallback.y = fallbackY;
            return fallback;
        }

        /// <summary>
        /// Returns the next randomized spawn delay for this lane.
        /// </summary>
        public float GetRandomInterval()
        {
            float minInterval = Mathf.Max(0.05f, spawnIntervalMin);
            float maxInterval = Mathf.Max(minInterval, spawnIntervalMax);
            return UnityEngine.Random.Range(minInterval, maxInterval);
        }
    }
}
