using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

public class AimManager : MonoBehaviour
{
    // StageManager 참조
    [SerializeField]
    private StageManager stageManager;

    // 키보드 입력을 관리하는 KeyboardMapManager
    [SerializeField]
    private KeyboardMapManager keyboardMapManager;

    // 사용할 목표 목록
    [SerializeField]
    private AimData[] aims;

    // 현재 진행 중인 목표
    private AimData currentAim;

    // 현재 목표 키 입력 횟수
    private int currentCount;


    private void Start()
    {
        // KeyboardMapManager 확인
        if (keyboardMapManager == null)
        {
            Debug.LogError("[AimManager] KeyboardMapManager가 연결되지 않았습니다.");
            return;
        }

        // 키 입력 이벤트 구독
        keyboardMapManager.KeyPressed += HandleKeyPressed;

        Debug.Log("[AimManager] 키 입력 이벤트 연결 완료");
    }


    private void OnDestroy()
    {
        // 이벤트 구독 해제
        if (keyboardMapManager != null)
        {
            keyboardMapManager.KeyPressed -= HandleKeyPressed;
        }
    }


    // 새로운 목표 시작
    public void StartAim()
    {
        if (aims == null || aims.Length == 0)
        {
            Debug.LogError("[AimManager] 등록된 Aim이 없습니다.");
            return;
        }

        // 목표 중 하나를 랜덤 선택
        currentAim = aims[Random.Range(0, aims.Length)];

        // 진행 횟수 초기화
        currentCount = 0;

        Debug.Log(
            "Aim Start: " +
            currentAim.aimName
        );
    }


    // 키 입력 이벤트
    private void HandleKeyPressed(KeyboardKey pressedKey)
    {
        Debug.Log(
            "[Aim 입력 감지] " +
            pressedKey.Data.Name
        );

        // 현재 목표가 없으면 검사하지 않음
        if (currentAim == null)
        {
            return;
        }

        // AimData의 키를 Input System Key로 변환
        Key targetKey = (Key)currentAim.targetKey;

        // 목표 키가 아니면 무시
        if (pressedKey.Data.InputKey != targetKey)
        {
            return;
        }

        // 목표 키 입력 횟수 증가
        currentCount++;

        Debug.Log(
            "[Aim 진행] " +
            currentAim.aimName +
            " : " +
            currentCount +
            " / " +
            currentAim.requiredCount
        );

        // 목표 달성
        if (currentCount >= currentAim.requiredCount)
        {
            CompleteAim();
        }
    }


    // 목표 완료
    private void CompleteAim()
    {
        Debug.Log(
            "Aim Complete: " +
            currentAim.aimName
        );

        // StageManager에게 스테이지 클리어 전달
        stageManager.ClearStage();
    }
}