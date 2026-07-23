using AudioSystem;
using SnowballSmash.Events;
using UnityEngine;

namespace SnowballSmash
{
    public class Play_AMB : MonoBehaviour
    {
        [SerializeField] private GameLifeCycleEvents lifeCycleEvents;

        [Tooltip("How far to duck the wind bus at round end, in dB.")]
        [SerializeField] private float volumeLowering = 12f;
        [SerializeField] private float transitionTime = 1f;

        [SerializeField] private AudioEvent snowEvent;
        [SerializeField] private AudioEvent windEvent;
        [Tooltip("Bus the wind routes to. Ducking this affects EVERYTHING on it.")]
        [SerializeField] private AudioBus windBus;

        private AudioHandle _snowHandle;
        private AudioHandle _windHandle;

        private void OnEnable()
        {
            lifeCycleEvents.onGameStart += OnStartRound;
            lifeCycleEvents.onGameEnd += OnEndRound;
        }

        private void OnDisable()
        {
            lifeCycleEvents.onGameStart -= OnStartRound;
            lifeCycleEvents.onGameEnd -= OnEndRound;
        }

        // Idempotent — safe to call on round 1, 2, 50. No _firstTime, no recursion.
        public void OnStartRound()
        {
            StopSnow();
            StopWind();

            RaiseWind();   // undo any duck left over from the previous round

            _snowHandle = snowEvent.Post(gameObject);
            _windHandle = windEvent.Post(gameObject);
        }

        public void OnEndRound()
        {
            StopSnow();
            LowerWind();   // snow stops, wind ducks and keeps blowing
        }

        public void StopSnow()
        {
            if (_snowHandle != null && _snowHandle.isPlaying) _snowHandle.Stop();
            _snowHandle = null;
        }

        public void StopWind()
        {
            if (_windHandle != null && _windHandle.isPlaying) _windHandle.Stop();
            _windHandle = null;
        }

        public void LowerWind()
        {
            if (windBus == null || AudioManager.Instance == null) return;
            AudioManager.Instance.TransitionBusVolume(windBus, -volumeLowering, transitionTime);
        }

        public void RaiseWind()
        {
            if (windBus == null || AudioManager.Instance == null) return;
            AudioManager.Instance.TransitionBusVolume(windBus, 0f, transitionTime);
        }
    }
}