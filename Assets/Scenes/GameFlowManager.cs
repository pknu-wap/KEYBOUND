using UnityEngine;

public class GameFlowTestManager : MonoBehaviour
{
    // 테스트할 스테이지
    [SerializeField]
    private int selectedStage;

    // 테스트할 목표
    [SerializeField]
    private int selectedAim;


    // 테스트 시작
    public void StartTest()
    {
        // TODO: 스테이지 및 목표 선택 후 테스트 시작
    }


    // 클리어 판정 테스트
    public void TestClear()
    {
        // TODO: 클리어 처리 구현
    }


    // 실패 판정 테스트
    public void TestFail()
    {
        // TODO: 실패 처리 구현
    }
}
