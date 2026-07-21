using System;

namespace SnowballSmash.Events
{
    public class LifeCycleEvents 
    {
        public static event Action onGameStart;
        public static event Action onGamePause;
        public static event Action onGameEnd;
        
        public static event Action onObstacleHit;
        public static event Action onCollectHit;
        public static event Action onPowerUpActivation;

        public static event Action onNearMiss;
        
    }
}
