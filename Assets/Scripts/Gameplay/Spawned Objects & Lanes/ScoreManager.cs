using System.Collections;
using System.Threading;
using SnowballSmash.Events;
using TMPro;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Tracks elapsed play time as a meter score, driven by the shared game lifecycle events
    /// rather than its own enable/disable state, and persists the high score on game end.
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        private const float ScoreUpdateInterval = 0.1f;
        private const string HighScoreKey = "SnowballSmash.HighScoreMeters";

        [Header("Score")]
        [Tooltip("Text that displays the distance score in meters.")]
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("How many meters are added to the score per second of play time.")]
        [SerializeField] private float metersPerSecond = 7f;

        [Header("High Score")]
        [Tooltip("Optional text that displays the stored high score in meters. Leave empty if unused.")]
        [SerializeField] private TMP_Text highScoreText;

        [Header("Progress")]
        [Tooltip("Shared progress data updated as distance increases. Assign the same asset referenced by lane spawners so their travel speed ramps with distance.")]
        [SerializeField] private GameProgressData gameProgress;

        [Header("Events")]
        [Tooltip("Raised by RoundManager on round start/end. Drives when scoring runs and when the high score is saved.")]
        [SerializeField] private GameLifeCycleEvents lifeCycleEvents;

        private float _elapsedTime;
        private int _currentMeters;
        private int _highScoreMeters;
        private Coroutine _scoreRoutine;
        private CancellationTokenSource _scoreCancellation;

        /// <summary>
        /// Distance covered in the run that just ended (or is currently running).
        /// Safe to read right after onGameEnd fires, since it's set before that event completes.
        /// </summary>
        public int CurrentMeters => _currentMeters;

        /// <summary>
        /// Best distance ever recorded this session, persisted via PlayerPrefs.
        /// </summary>
        public int HighScoreMeters => _highScoreMeters;

        /// <summary>
        /// Loads the stored high score once when this object is created.
        /// </summary>
        private void Awake()
        {
            _highScoreMeters = PlayerPrefs.GetInt(HighScoreKey, 0);
            UpdateHighScoreText();
        }

        /// <summary>
        /// Subscribes to round lifecycle events.
        /// </summary>
        private void OnEnable()
        {
            lifeCycleEvents.onGameStart += HandleGameStart;
            lifeCycleEvents.onGameEnd += HandleGameEnd;
        }

        /// <summary>
        /// Unsubscribes from round lifecycle events and stops scoring as a safety net.
        /// </summary>
        private void OnDisable()
        {
            lifeCycleEvents.onGameStart -= HandleGameStart;
            lifeCycleEvents.onGameEnd -= HandleGameEnd;

            StopScoreRoutine();
        }

        /// <summary>
        /// Resets run state for a new round and starts tracking elapsed time.
        /// </summary>
        private void HandleGameStart()
        {
            _elapsedTime = 0f;
            _currentMeters = 0;
            UpdateScoreText();

            if (gameProgress != null)
            {
                gameProgress.ResetProgress();
            }

            StopScoreRoutine();

            _scoreCancellation = new CancellationTokenSource();
            _scoreRoutine = StartCoroutine(ScoreRoutine(_scoreCancellation.Token));
        }

        /// <summary>
        /// Stops tracking, finalizes the displayed score, and saves a new high score if one was reached.
        /// This is the only place PlayerPrefs is written to, instead of on every score tick.
        /// </summary>
        private void HandleGameEnd()
        {
            StopScoreRoutine();
            UpdateScoreText();
            SaveHighScoreIfNeeded();
        }

        /// <summary>
        /// Cancels and clears the running score coroutine, if any.
        /// </summary>
        private void StopScoreRoutine()
        {
            _scoreCancellation?.Cancel();

            if (_scoreRoutine != null)
            {
                StopCoroutine(_scoreRoutine);
                _scoreRoutine = null;
            }

            _scoreCancellation?.Dispose();
            _scoreCancellation = null;
        }

        /// <summary>
        /// Tracks elapsed play time and refreshes the visible meter score until cancelled.
        /// </summary>
        private IEnumerator ScoreRoutine(CancellationToken cancellationToken)
        {
            float nextTextRefresh = 0f;

            while (!cancellationToken.IsCancellationRequested)
            {
                _elapsedTime += Time.deltaTime;

                if (Time.time >= nextTextRefresh)
                {
                    UpdateScoreText();
                    nextTextRefresh = Time.time + ScoreUpdateInterval;
                }

                yield return null;
            }
        }

        /// <summary>
        /// Converts elapsed play time into meters, writes it to the score label, and feeds the
        /// distance into shared progress data. Does not touch PlayerPrefs.
        /// </summary>
        private void UpdateScoreText()
        {
            _currentMeters = Mathf.FloorToInt(_elapsedTime * metersPerSecond);
            scoreText.text = _currentMeters + "m";

            if (gameProgress != null)
            {
                gameProgress.SetMeters(_currentMeters);
            }
        }

        /// <summary>
        /// Writes the final meters to PlayerPrefs only if it beats the stored high score.
        /// </summary>
        private void SaveHighScoreIfNeeded()
        {
            if (_currentMeters <= _highScoreMeters)
            {
                return;
            }

            _highScoreMeters = _currentMeters;
            PlayerPrefs.SetInt(HighScoreKey, _highScoreMeters);
            PlayerPrefs.Save();
            UpdateHighScoreText();
        }

        /// <summary>
        /// Writes the current high score to its label, if one is assigned.
        /// </summary>
        private void UpdateHighScoreText()
        {
            if (highScoreText != null)
            {
                highScoreText.text = _highScoreMeters + "m";
            }
        }
    }
}