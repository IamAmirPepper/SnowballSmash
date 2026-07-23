using AudioSystem;
using UnityEngine;

namespace SnowballSmash
{
    public class MenuAudio : MonoBehaviour
    {
        [SerializeField] private AudioEvent hoverEvent;
        [SerializeField] private AudioEvent pressedEvent;

        public void onHover()
        {
            hoverEvent.Post(gameObject);
        }
        public void onPressed()
        {
            pressedEvent.Post(gameObject);
        }
    }
}
