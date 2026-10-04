using UnityEngine;

public class StageManager : MonoBehaviour
{
    // 현재 진행 중인 스테이지 번호
    private int currentStage;

    // AimManager 참조
    [SerializeField]
    private AimManager aimManager;

    // TimeManager 참조
    [SerializeField]
    private TimeManager timeManager;

    // 스테이지 클리어 시 추가할 시간
    [SerializeField]
    private float clearTimeBonus = 5f;


    // 게임 시작
    private void Start()
    {
        Debug.Log("[StageManager] 게임 시작");

        StartGame();
    }


    // 게임 초기화
    private void StartGame()
    {
        currentStage = 1;

        Debug.Log(
            "[StageManager] 첫 스테이지 설정: Stage " +
            currentStage
        );

        StartStage();
    }


    // 현재 스테이지 시작
    private void StartStage()
    {
        Debug.Log(
            "[스테이지 시작] Stage " +
            currentStage
        );

        // 새로운 목표 시작
        aimManager.StartAim();
    }


    // 현재 스테이지 클리어
    public void ClearStage()
    {
        Debug.Log(
            "[스테이지 클리어] Stage " +
            currentStage
        );

        // 클리어 시간 보상
        if (timeManager != null)
        {
            float beforeTime = timeManager.CurrentTime;

            timeManager.AddTime(clearTimeBonus);

            float afterTime = timeManager.CurrentTime;

            Debug.Log(
                "[시간 보상] +" +
                clearTimeBonus +
                "초 / " +
                beforeTime.ToString("F1") +
                "초 → " +
                afterTime.ToString("F1") +
                "초"
            );
        }
        else
        {
            Debug.LogWarning(
                "[StageManager] TimeManager가 연결되지 않았습니다."
            );
        }

        // 다음 스테이지로 증가
        currentStage++;

        Debug.Log(
            "[다음 스테이지] Stage " +
            currentStage
        );

        StartStage();
    }
}