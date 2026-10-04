using UnityEngine;

public class UIManager : MonoBehaviour
{
    // 씬 전환을 담당하는 GameSceneManager
    [SerializeField]
    private GameSceneManager gameSceneManager;


    // 게임 시작 버튼
    public void OnClickStart()
    {
        gameSceneManager.StartGame();
    }


    // 게임 재시작 버튼
    public void OnClickRestart()
    {
        gameSceneManager.RestartGame();
    }


    // 시작 화면으로 돌아가기 버튼
    public void OnClickGoToStart()
    {
        gameSceneManager.GoToStartScene();
    }


    // 게임 종료 버튼
    public void OnClickQuit()
    {
        gameSceneManager.QuitGame();
    }
}