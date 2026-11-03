using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    public void StartGame() => Load("GameScene");
    public void GoToStartScene() => Load("StartScene");
    // Retained for old serialized callers; the new UI does not expose restart.
    public void RestartGame() => Load(SceneManager.GetActiveScene().name);
    private void Load(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }
    public void QuitGame() { Time.timeScale = 1f; Application.Quit(); }
}
