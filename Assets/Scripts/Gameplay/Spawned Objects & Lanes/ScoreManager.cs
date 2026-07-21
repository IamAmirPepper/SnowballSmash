using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Tracks elapsed play time and displays it as a meter score.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private const float ScoreUpdateInterval = 0.1f;

        [Header("Score")]
        [Tooltip("Text that displays the distance score in meters.")]
        [SerializeField] private TMP_Text scoreText;
        
        [Tooltip("How many meters are added to the score per second of play time.")]
        [SerializeField] private float metersPerSecond = 7f;

        private float elapsedTime;
        private Coroutine scoreRoutine;
        private CancellationTokenSource scoreCancellation;

        /// <summary>
        /// Starts score tracking when this component becomes active.
        /// </summary>
        private void OnEnable()
        {
            scoreCancellation = new CancellationTokenSource();
            scoreRoutine = StartCoroutine(ScoreRoutine(scoreCancellation.Token));
        }

        /// <summary>
        /// Cancels score tracking and releases the cancellation source.
        /// </summary>
        private void OnDisable()
        {
            scoreCancellation?.Cancel();

            if (scoreRoutine != null)
            {
                StopCoroutine(scoreRoutine);
            }

            scoreCancellation?.Dispose();
            scoreCancellation = null;
        }

        /// <summary>
        /// Tracks elapsed play time and refreshes the visible meter score until cancelled.
        /// </summary>
        private IEnumerator ScoreRoutine(CancellationToken cancellationToken)
        {
            float nextTextRefresh = 0f;

            while (!cancellationToken.IsCancellationRequested)
            {
                elapsedTime += Time.deltaTime;

                if (Time.time >= nextTextRefresh)
                {
                    UpdateScoreText();
                    nextTextRefresh = Time.time + ScoreUpdateInterval;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Converts elapsed play time into meters and writes it to the score label.
        /// </summary>
        private void UpdateScoreText()
        {
            int meters = Mathf.FloorToInt(elapsedTime * metersPerSecond);
            scoreText.text = meters + "m";
        }
    }
}
