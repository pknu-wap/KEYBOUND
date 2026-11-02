using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneManager : MonoBehaviour
{
    // 게임 씬으로 이동
    public void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }

    // 시작 씬으로 이동
    public void GoToStartScene()
    {
        SceneManager.LoadScene("StartScene");
    }

    // 현재 게임 씬 다시 시작
    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    // 게임 종료
    public void QuitGame()
    {
        Application.Quit();

        Debug.Log("Game Quit");
    }
}