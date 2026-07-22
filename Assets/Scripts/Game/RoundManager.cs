using SnowballSmash.Events;
using SnowballSmash.Gameplay;
using System.Collections;
using UnityEngine;

namespace SnowballSmash
{
    public class RoundManager : MonoBehaviour
    {
        [SerializeField] private GameLifeCycleEvents lifeCycleEvents;
        [SerializeField] private SnowballCollisionEvents collisionEvents;
        [SerializeField] private ScoreManager scoreManager;

        private Coroutine _raiseGameStartRoutine;

        /// <summary>
        /// Best distance ever recorded
        /// </summary>
        public int HighScore => scoreManager.HighScoreMeters;

        /// <summary>
        /// Distance covered in the run that just ended.
        /// </summary>
        public int LastRunMeters => scoreManager.CurrentMeters;


        private void OnEnable()
        {
            collisionEvents.onObstacleHit += OnCollidedWithObstacle;
            _raiseGameStartRoutine = StartCoroutine(RaiseGameStartedNextFrame());
        }

        private void OnDisable()
        {
            collisionEvents.onObstacleHit -= OnCollidedWithObstacle;

            if (_raiseGameStartRoutine != null)
            {
                StopCoroutine(_raiseGameStartRoutine);
                _raiseGameStartRoutine = null;
            }
        }



        private IEnumerator RaiseGameStartedNextFrame()
        {
            yield return null;
            lifeCycleEvents.RaiseGameStarted();
        }

        private void OnCollidedWithObstacle()
        {
            lifeCycleEvents.RaiseGameEnd();


            // do stuff like raise menu, e.g. ShowGameOverMenu(finalMeters, highScore);
        }
    }
}