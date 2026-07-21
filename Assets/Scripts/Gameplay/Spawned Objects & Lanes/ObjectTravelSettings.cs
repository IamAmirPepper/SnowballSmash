using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Shared travel tuning for lane objects, reusable by obstacles, collectables, and scenery.
    /// </summary>
    [CreateAssetMenu(menuName = "Snowball Smash/Object Travel Settings")]
    public sealed class ObjectTravelSettings : ScriptableObject
    {
        [Header("Speed")]
        [Tooltip("Movement speed at the beginning of the travel path.")]
        [SerializeField] private float startSpeed = 3.5f;
        [Tooltip("Movement speed near the end of the travel path.")]
        [SerializeField] private float endSpeed = 9f;
        [Tooltip("Curve used to blend from start speed to end speed over normalized travel progress.")]
        [SerializeField] private AnimationCurve acceleration = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Scale")]
        [Tooltip("Scale multiplier at the beginning of the travel path.")]
        [SerializeField] private float startScale = 0.5f;
        [Tooltip("Scale multiplier near the end of the travel path.")]
        [SerializeField] private float endScale = 2f;

        [Header("Cleanup")]
        [Tooltip("Y position at or below which lane objects are returned to their pool.")]
        [SerializeField] private float despawnY = -6f;

        /// <summary>
        /// Y position at which a travelling lane object is returned to its pool.
        /// </summary>
        public float DespawnY => despawnY;

        /// <summary>
        /// Returns movement speed for the supplied normalized travel progress.
        /// </summary>
        public float GetSpeed(float progress)
        {
            float speedProgress = Mathf.Clamp01(acceleration.Evaluate(progress));
            return Mathf.Lerp(startSpeed, endSpeed, speedProgress);
        }

        /// <summary>
        /// Returns scale multiplier for the supplied normalized travel progress.
        /// </summary>
        public float GetScale(float progress)
        {
            return Mathf.Lerp(startScale, endScale, progress);
        }
    }
}
