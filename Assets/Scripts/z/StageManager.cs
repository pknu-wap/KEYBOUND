using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 현재 진행 중인 스테이지 번호
    private int currentStage;

    // AimManager 참조
    [SerializeField]
    private AimManager aimManager;


    // 게임 시작 시 자동으로 호출
    private void Start()
    {
        StartGame();
    }


    // 게임 시작
    private void StartGame()
    {
        // 첫 번째 스테이지부터 시작
        currentStage = 1;

        Debug.Log("Game Start");

        // 첫 번째 스테이지 시작
        StartStage();
    }


    // 현재 스테이지 시작
    private void StartStage()
    {
        Debug.Log("Stage " + currentStage + " Start");

        // 새로운 목표 시작
        aimManager.StartAim();
    }


    // 현재 스테이지 클리어
    // 목표를 달성하면 AimManager에서 호출
    public void ClearStage()
    {
        Debug.Log("Stage " + currentStage + " Clear");

        // 다음 스테이지로 증가
        currentStage++;

        // 다음 스테이지 시작
        StartStage();
    }
}