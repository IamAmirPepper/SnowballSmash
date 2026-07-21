using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Draws a visual ruler in the Game and Scene views to measure world distances live.
    /// </summary>
    public class ScreenRuler : MonoBehaviour
    {
        [Header("Ruler Placement")]
        [Tooltip("Start position of the ruler in World Space.")]
        [SerializeField] private Vector3 startPoint = new Vector3(-10f, 0f, 0f);
        [Tooltip("End position of the ruler in World Space.")]
        [SerializeField] private Vector3 endPoint = new Vector3(10f, 0f, 0f);

        [Header("Visual Options")]
        [Tooltip("Color of the ruler line and tick marks.")]
        [SerializeField] private Color rulerColor = Color.cyan;
        [Tooltip("Distance in world units between each tick mark.")]
        [SerializeField] private float stepSize = 1.0f;
        [Tooltip("Size of the tick marks branching off the main line.")]
        [SerializeField] private float tickSize = 0.3f;

        private void OnDrawGizmos()
        {
            Gizmos.color = rulerColor;

            // 1. Draw main baseline
            Gizmos.DrawLine(startPoint, endPoint);

            // 2. Calculate distance and direction
            float totalDistance = Vector3.Distance(startPoint, endPoint);
            Vector3 direction = (endPoint - startPoint).normalized;

            // Calculate a perpendicular vector for tick marks (assuming X/Y 2D plane)
            Vector3 perpDirection = new Vector3(-direction.y, direction.x, 0f) * tickSize;

            if (stepSize <= 0.05f) return; // Prevent infinite loops if step is too small

            // 3. Draw tick marks & distance measurements along the line
            for (float d = 0; d <= totalDistance; d += stepSize)
            {
                Vector3 currentPos = startPoint + (direction * d);

                // Draw tick line
                Gizmos.DrawLine(currentPos - perpDirection, currentPos + perpDirection);

#if UNITY_EDITOR
                // Draw text label in Scene view
                UnityEditor.Handles.color = rulerColor;
                UnityEditor.Handles.Label(currentPos + (perpDirection * 1.5f), $"{d:F1}m");
#endif
            }
        }
    }
}