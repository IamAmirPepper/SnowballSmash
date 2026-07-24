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

        public bool hasGameStarted { get; private set; }

        public void RaiseGameStarted()
        {
            onGameStart?.Invoke();
            hasGameStarted = true;
        }
        public void RaiseGamePaused() => onGamePause?.Invoke();
        public void RaiseGameEnd()
        { 
            onGameEnd?.Invoke();
            hasGameStarted = false;
        } 
    }
}
