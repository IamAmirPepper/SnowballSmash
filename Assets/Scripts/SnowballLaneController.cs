using UnityEngine;
using UnityEngine.Serialization;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Moves the snowball along a continuous lane path using mouse input and fake-depth scaling.
    /// </summary>
    public class SnowballLaneController : MonoBehaviour
    {
        [Header("Lanes")]
        [Tooltip("Ordered player path anchors from left to right.")]
        [SerializeField] private Transform[] lanePoints = new Transform[3];
        [Tooltip("Lane index where the snowball starts when play begins.")]
        [SerializeField] private int startLaneIndex = 1;

        [Header("Movement")]
        [Tooltip("Camera used to convert mouse screen position into world position.")]
        [SerializeField] private Camera inputCamera;
        [FormerlySerializedAs("laneMoveDuration")]
        [Tooltip("Smoothing time for following the mouse-controlled path position.")]
        [SerializeField] private float followSmoothTime = 0.12f;
        [Tooltip("Maximum movement speed while following the mouse.")]
        [SerializeField] private float maxFollowSpeed = 30f;
        [Tooltip("How far side positions are pulled toward camera to fake depth.")]
        [SerializeField] private float forwardArcAmount = 0.7f;

        [Header("Fake 3D Scale")]
        [Tooltip("Scale multiplier when the snowball is centered in the lane path.")]
        [SerializeField] private float middleLaneScale = 0.7f;
        [Tooltip("Scale multiplier at the left and right edges of the lane path.")]
        [SerializeField] private float sideLaneScale = 1.45f;
        [Tooltip("Smoothing time for scale changes across the fake-depth path.")]
        [SerializeField] private float scaleSmoothTime = 0.08f;

        private Vector3 baseScale;
        private Vector3 moveVelocity;
        private float scaleVelocity;
        private float currentScaleMultiplier;
        private float targetPathPosition;

        /// <summary>
        /// Captures the authored player scale and resolves the starting path position.
        /// </summary>
        private void Awake()
        {
            baseScale = transform.localScale;
            targetPathPosition = GetNormalizedLanePosition(startLaneIndex);
            currentScaleMultiplier = GetPathScale(targetPathPosition);
        }

        /// <summary>
        /// Resolves the input camera and snaps the snowball to its starting lane position.
        /// </summary>
        private void Start()
        {
            inputCamera ??= Camera.main;
            SnapToPathPosition(targetPathPosition);
        }

        /// <summary>
        /// Reads mouse input and follows the requested lane path position.
        /// </summary>
        private void Update()
        {
            UpdateMouseTarget();
            FollowTarget();
        }

        /// <summary>
        /// Converts the mouse screen position into a normalized position across the lane path.
        /// </summary>
        private void UpdateMouseTarget()
        {
            if (inputCamera == null || !HasUsableLanePoints())
            {
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            mousePosition.z = Mathf.Abs(inputCamera.transform.position.z - transform.position.z);

            Vector3 mouseWorldPosition = inputCamera.ScreenToWorldPoint(mousePosition);
            targetPathPosition = GetNormalizedMousePosition(mouseWorldPosition.x);
        }

        /// <summary>
        /// Smoothly moves and scales the snowball toward the current mouse target.
        /// </summary>
        private void FollowTarget()
        {
            if (!HasUsableLanePoints())
            {
                return;
            }

            Vector3 targetPosition = GetPathPosition(targetPathPosition);
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref moveVelocity,
                Mathf.Max(0.01f, followSmoothTime),
                maxFollowSpeed);

            float targetScale = GetPathScale(targetPathPosition);
            currentScaleMultiplier = Mathf.SmoothDamp(
                currentScaleMultiplier,
                targetScale,
                ref scaleVelocity,
                Mathf.Max(0.01f, scaleSmoothTime));

            transform.localScale = baseScale * currentScaleMultiplier;
        }

        /// <summary>
        /// Returns whether the lane path has enough anchors to evaluate movement.
        /// </summary>
        private bool HasUsableLanePoints()
        {
            return lanePoints.Length >= 2 && lanePoints[0] != null && lanePoints[^1] != null;
        }

        /// <summary>
        /// Converts a world X coordinate into a normalized left-to-right lane path position.
        /// </summary>
        private float GetNormalizedMousePosition(float worldX)
        {
            float leftX = lanePoints[0].position.x;
            float rightX = lanePoints[^1].position.x;

            return Mathf.Approximately(leftX, rightX) ? 0.5f : Mathf.InverseLerp(leftX, rightX, worldX);
        }

        /// <summary>
        /// Converts a discrete lane index into a normalized path position.
        /// </summary>
        private float GetNormalizedLanePosition(int laneIndex)
        {
            if (lanePoints.Length <= 1)
            {
                return 0.5f;
            }

            return Mathf.Clamp01(Mathf.Clamp(laneIndex, 0, lanePoints.Length - 1) / (float)(lanePoints.Length - 1));
        }

        /// <summary>
        /// Immediately places the snowball at a normalized path position.
        /// </summary>
        private void SnapToPathPosition(float pathPosition)
        {
            if (!HasUsableLanePoints())
            {
                return;
            }

            transform.position = GetPathPosition(pathPosition);
            currentScaleMultiplier = GetPathScale(pathPosition);
            transform.localScale = baseScale * currentScaleMultiplier;
        }

        /// <summary>
        /// Evaluates the curved fake-depth path position for a normalized path coordinate.
        /// </summary>
        private Vector3 GetPathPosition(float pathPosition)
        {
            pathPosition = Mathf.Clamp01(pathPosition);

            if (lanePoints.Length >= 3 && lanePoints[1] != null)
            {
                Vector3 left = lanePoints[0].position;
                Vector3 middle = lanePoints[1].position;
                Vector3 right = lanePoints[^1].position;

                Vector3 lanePosition = pathPosition <= 0.5f
                    ? Vector3.Lerp(left, middle, pathPosition / 0.5f)
                    : Vector3.Lerp(middle, right, (pathPosition - 0.5f) / 0.5f);

                float sideAmount = GetSideAmount(pathPosition);
                lanePosition.y -= sideAmount * forwardArcAmount;
                return lanePosition;
            }

            return Vector3.Lerp(lanePoints[0].position, lanePoints[^1].position, pathPosition);
        }

        /// <summary>
        /// Evaluates scale for a normalized path coordinate.
        /// </summary>
        private float GetPathScale(float pathPosition)
        {
            float sideAmount = GetSideAmount(pathPosition);
            return Mathf.Lerp(middleLaneScale, sideLaneScale, sideAmount);
        }

        /// <summary>
        /// Returns how far a normalized path position is from the center lane.
        /// </summary>
        private float GetSideAmount(float pathPosition)
        {
            return Mathf.Abs(Mathf.Clamp01(pathPosition) - 0.5f) * 2f;
        }
    }
}
