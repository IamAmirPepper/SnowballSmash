using System;
using UnityEngine;

namespace SnowballSmash.Events
{
    [CreateAssetMenu(menuName = "GameEventsSO/GameLifeCycleEvents")]
    public class GameLifeCycleEvents : ScriptableObject
    {
        public event Action onGameStart;
        public event Action onGamePause;
        public event Action onGameEnd;

        public void RaiseGameStarted() => onGameStart?.Invoke();
        public void RaiseGamePaused() => onGamePause?.Invoke();
        public void RaiseGameEnd() => onGameEnd?.Invoke();
    }
}
