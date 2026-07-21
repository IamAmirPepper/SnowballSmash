using System;
using UnityEngine;

namespace SnowballSmash.Events
{
    [CreateAssetMenu(menuName = "GameEventsSO/SnowballCollisionEvents")]
    public class SnowballCollisionEvents : ScriptableObject
    {
        public event Action onObstacleHit;
        public event Action onCollectHit;
        public event Action onNearMiss;

        public void RaiseOnObstacleHit() => onObstacleHit?.Invoke();
        public void RaiseOnCollectHit() => onCollectHit?.Invoke();
        public void RaiseOnNearMiss() => onNearMiss?.Invoke();



    }
}
