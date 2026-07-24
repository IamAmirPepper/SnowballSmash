using SnowballSmash.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SnowballSmash
{
    public class EndGameCanvas : MonoBehaviour
    {
        [SerializeField] private TMP_Text highscoreText;
        [SerializeField] private Button replayButton;
        [SerializeField] private Canvas canvas;
        [SerializeField] private GraphicRaycaster graphicRaycaster;

        private void Awake()
        {
            if (canvas == null)
            {
                canvas = GetComponent<Canvas>();
            }

            if (graphicRaycaster == null)
            {
                graphicRaycaster = GetComponent<GraphicRaycaster>();
            }

            SetVisible(false);
        }

        private void OnEnable()
        {
            if (replayButton != null)
            {
                replayButton.onClick.AddListener(Replay);
            }
        }

        private void OnDisable()
        {
            if (replayButton != null)
            {
                replayButton.onClick.RemoveListener(Replay);
            }
        }

        public void Show(ScoreManager scoreManager)
        {
            gameObject.SetActive(true);

            if (scoreManager != null)
            {
                if (highscoreText != null)
                {
                    highscoreText.text = scoreManager.HighScoreMeters + "m";
                }
            }

            Time.timeScale = 0f;
            SetVisible(true);
        }

        private void Replay()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SetVisible(bool visible)
        {
            if (canvas != null)
            {
                canvas.enabled = visible;
            }

            if (graphicRaycaster != null)
            {
                graphicRaycaster.enabled = visible;
            }
        }
    }
}
