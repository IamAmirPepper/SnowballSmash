using System;
using UnityEngine;

namespace SnowballSmash
{
    [CreateAssetMenu(menuName = "GameEventsSO/SpecialOccasionsEvents")]
    public class SpecialOccasionsEvents : ScriptableObject
    {
        /// <summary>
        /// temporary string - needs to be upgraded to Type of the powerup
        /// </summary>
        public event Action<string> onPowerUpActivation;

        public void RaiseOnPowerupActiovation(string powerUpActivationName) => onPowerUpActivation?.Invoke(powerUpActivationName);
    }
}
