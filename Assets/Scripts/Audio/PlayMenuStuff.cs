using AudioSystem;
using SnowballSmash.Events;
using UnityEngine;

namespace SnowballSmash
{
    public class PlayMenuStuff : MonoBehaviour
    {
        [SerializeField] private AudioEvent wind;
        [SerializeField] private GameLifeCycleEvents cycleEvents;

        private AudioHandle _handle;

        private void Start()
        {
            _handle = wind.Post(gameObject);
        }
        private void OnDestroy()
        {
            _handle.Stop(); 
        }

    }
}
