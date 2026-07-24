using AudioSystem;
using SnowballSmash.Events;
using UnityEngine;

namespace SnowballSmash
{
    public class PlayMusic : MonoBehaviour
    {
        
        [SerializeField] private AudioEvent music;
        [SerializeField] private GameLifeCycleEvents cycleEvents;

        private AudioHandle _handle;

        public static PlayMusic Instance { get; private set; }

        private void Awake()
        {
            // Check if an instance already exists and it's not this one
            if (Instance != null && Instance != this)
            {
                // Destroy the duplicate GameObject
                Destroy(gameObject);
                return;
            }

            // Set the instance to this script
            Instance = this;

            // Keep this GameObject alive across scene changes
            DontDestroyOnLoad(gameObject);
        }
        private void Start()
        {
            _handle = music.Post(gameObject);   
        }

        private void OnEnable()
        {
            cycleEvents.onGameStart += StartMusic;
            cycleEvents.onGameEnd += StopMusic;
        }

        private void OnDisable()
        {
            cycleEvents.onGameStart -= StartMusic;
            cycleEvents.onGameEnd -= StopMusic;
        }

        private void StartMusic()
        {
            if (_handle == null) _handle = music.Post(gameObject);
            
        }

        private void StopMusic()
        {
            if(_handle != null)
            _handle.Stop();
            _handle = null;
        }
    }
}
