using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Shared runtime progress data for a single run. ScoreManager writes distance into this
    /// asset as the run progresses; any number of LaneObjectSpawner instances (background,
    /// obstacles, etc.) can read the resulting speed multiplier to ramp difficulty together.
    /// </summary>
    [CreateAssetMenu(menuName = "Snowball Smash/Game Progress Data")]
    public sealed class GameProgressData : ScriptableObject
    {
        [Header("Speed Ramp")]
        [Tooltip("Distance in meters between each speed increase step.")]
        [SerializeField, Min(1f)] private float metersPerSpeedStep = 100f;
        [Tooltip("Additional speed multiplier granted per step (0.05 = +5% per step).")]
        [SerializeField, Min(0f)] private float speedIncreasePerStep = 0.05f;
        [Tooltip("Upper bound for the speed multiplier so difficulty doesn't scale forever.")]
        [SerializeField] private float maxSpeedMultiplier = 2f;

        private float currentMeters;
        private float currentSpeedMultiplier = 1f;

        /// <summary>
        /// Current global speed multiplier derived from distance travelled this run.
        /// Multiply this into a travel speed calculation to apply the ramp.
        /// </summary>
        public float SpeedMultiplier => currentSpeedMultiplier;

        /// <summary>
        /// Resets runtime progress back to the start of a run. This ScriptableObject's state
        /// persists across scene reloads within a play session, so call this whenever a new
        /// run begins (ScoreManager does this in Awake).
        /// </summary>
        public void ResetProgress()
        {
            currentMeters = 0f;
            currentSpeedMultiplier = 1f;
        }

        /// <summary>
        /// Updates tracked distance and recalculates the stepped speed multiplier.
        /// </summary>
        public void SetMeters(float meters)
        {
            currentMeters = meters;

            int steps = Mathf.FloorToInt(currentMeters / metersPerSpeedStep);
            float multiplier = 1f + steps * speedIncreasePerStep;
            currentSpeedMultiplier = Mathf.Min(multiplier, maxSpeedMultiplier);
        }
    }
}
