using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Runtime state for an object currently travelling through a lane.
    /// </summary>
    public sealed class PooledLaneObject
    {
        /// <summary>
        /// Runtime instance controlled by the pool.
        /// </summary>
        public GameObject gameObject;

        /// <summary>
        /// Cached transform for movement and scale updates.
        /// </summary>
        public Transform transform;

        /// <summary>
        /// Authored prefab scale used as the base for travel scaling.
        /// </summary>
        public Vector3 baseScale;

        /// <summary>
        /// World position where this travel instance started.
        /// </summary>
        public Vector3 startPosition;

        /// <summary>
        /// World position this travel instance moves toward.
        /// </summary>
        public Vector3 targetPosition;

        /// <summary>
        /// Cached distance from start to target for normalized progress calculation.
        /// </summary>
        public float travelDistance;

        /// <summary>
        /// SpriteRenderers affected by optional fade logic.
        /// </summary>
        public SpriteRenderer[] spriteRenderers;

        /// <summary>
        /// Authored SpriteRenderer colors restored as the basis for fade updates.
        /// </summary>
        public Color[] spriteBaseColors;

        /// <summary>
        /// Calculates normalized progress between this object's start and target positions.
        /// </summary>
        public float GetTravelProgress(Vector3 position)
        {
            float traveledDistance = Vector3.Distance(startPosition, position);
            return Mathf.Clamp01(traveledDistance / Mathf.Max(0.01f, travelDistance));
        }
    }
}
