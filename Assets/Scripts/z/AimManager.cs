using UnityEngine;

public class AimManager : MonoBehaviour
{
    // StageManager 참조
    [SerializeField]
    private StageManager stageManager;

    // 현재 진행 중인 목표 번호
    private int currentAim;

    // 테스트용 전체 목표 개수
    [SerializeField]
    private int aimCount = 3;


    // 새로운 목표 시작
    public void StartAim()
    {
        // 0 ~ aimCount - 1 중 하나를 랜덤으로 선택
        currentAim = Random.Range(0, aimCount);

        Debug.Log("Aim " + currentAim + " Start");

        // 추후 실제 목표를 실행하는 로직 추가
    }


    // 현재 목표 달성
    public void CompleteAim()
    {
        Debug.Log("Aim " + currentAim + " Complete");

        // 목표를 달성했으므로 현재 스테이지 클리어
        stageManager.ClearStage();
    }
}