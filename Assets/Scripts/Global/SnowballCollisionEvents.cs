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

        public void RaiseOnObstacleHit()
        {
            onObstacleHit?.Invoke();
            //Debug.Log("ObstacleHit Invoked :)");
        }
        public void RaiseOnCollectHit()
        {
            onCollectHit?.Invoke();
            //Debug.Log("OnCollect Invoked :)");
        }
        public void RaiseOnNearMiss()
        {

            onNearMiss?.Invoke();
            //Debug.Log("OnNearMiss Invoked :)");
        }



    }
}
