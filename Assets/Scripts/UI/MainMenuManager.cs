using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SnowballSmash
{
    public class MainMenuManager : MonoBehaviour
    {
        private const string GAME_SCENE_NAME = "Game";
        [SerializeField] private Button playBt, quitBt;
        
        void Awake()
        {
            playBt.onClick.AddListener(LoadGameScene);
            quitBt.onClick.AddListener(QuitGame);
        }

        private void LoadGameScene()
        {
            SceneManager.LoadScene(GAME_SCENE_NAME);
        }

        private void QuitGame()
        {
            Application.Quit();
        }

        void OnDestroy()
        {
            playBt.onClick.RemoveListener(LoadGameScene);
            quitBt.onClick.RemoveListener(QuitGame);
        }
    }
}
