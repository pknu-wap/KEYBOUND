# KEYBOUND UI & Game Flow 구현 기록

## 기존 구조
- Unity 6000.6.1f1, Input System 1.20.0 (새 Input System).
- StartScene: 기존 UIManager → GameSceneManager 버튼 호출 구조.
- GameScene: 구현 전 Main Camera만 존재.
- SampleScene: KeyboardMapManager, PlayerKeyboardPosition, PlayerCollision, MineObstacle, Canvas, EventSystem.
- Test/TimeScene: TimeManager와 TimeUI(Canvas, TMP 텍스트, Slider).
- TimeManager는 문서와 달리 OnTimeOver 이벤트를 이미 제공함. 이 이벤트를 직접 구독.
- 오디오 에셋/AudioMixer는 없음. 실제 청취 검증 대상 없음.

## 구현 구조
- UIManager: Prefab별 1회 생성/기존 UIView 재사용, 배치 적용, 표시/숨김, 버튼 연결, 표시 순서 관리.
- GameFlowManager: Playing/Paused/GameOver 상태, 상태 변경 이벤트, ESC 입력 신호, 중복 게임오버 방지.
- 기존 GameSceneManager: SceneManager 호출의 공통 진입점. 이동 전에 timeScale을 1로 복원.
- UIView: Prefab의 버튼/설정/결과 텍스트 참조.
- UILayout: Anchor/Pivot/Position/Size/표시 순서의 ScriptableObject 데이터. Prefab 내부 동작과 배치를 분리함.
- GameSettings: BGM/SFX와 전체화면 설정의 PlayerPrefs 저장, Screen API 적용.
- AudioVolumeBinding: 향후 AudioSource 기본 음량에 채널 음량을 곱하거나 AudioMixer 노출 파라미터에 dB로 적용. 둘 중 한 방식만 사용.

## 기존 시스템 보존과 Scene 변경
- 키보드/플레이어/시간 스크립트 및 KeyboardKey Prefab 내부는 변경하지 않음.
- SampleScene과 TimeScene 원본 파일을 유지하고 필요한 오브젝트를 GameScene으로 가져옴.
- 기존 키보드 Canvas의 스케일/배치를 유지. 새 PopupCanvas만 1280×720 기준 반응형으로 구성하고 sortingOrder=100 적용.
- 기존 시간 UI 자식들을 TimeDisplay 컨테이너로 묶어 화면 상단으로 배치. 원래 텍스트/슬라이더 참조를 재사용.
- StartScene의 기존 화면은 기본 UI Prefab으로 교체. 기존 이미지 에셋 자체는 삭제하지 않음.
- ResultScene 파일 유지, 빌드 설정에서만 비활성화.
- 기존 미커밋 변경은 작업 시작 시 존재했으며 취소하지 않음.

## 입력 및 시간 처리
- 일시정지/게임오버 시 KeyboardMapManager와 PlayerKeyboardPosition의 enabled 상태를 저장한 후 비활성화.
- 재개 시 개별 컴포넌트의 이전 enabled 값과 시간 배율 복원.
- TimeManager는 기존 Time.deltaTime 기반 로직을 그대로 사용. timeScale=0에서 카운트다운 정지.
- 설정에서 ESC/BACK은 일시정지 메뉴로 복귀. 그 다음 ESC/RESUME만 게임 재개.
- GameOver 상태에서는 Resume 및 ESC 재개 불가. 재시작 UI 없음.

## 시간 담당자 연결 지점
- GameFlowManager의 timeManager 필드에 기존 TimeManager를 지정하면 OnEnable에서 OnTimeOver += HandleTimeExpired, OnDisable에서 해제.
- 외부에서 직접 GameFlowManager.HandleTimeExpired() 호출도 가능.
- 생존 시간 계산은 추가하지 않음. 실제 데이터를 소유한 시스템에서 SetSurvivalTime(float seconds) 호출. 게임오버 전후 모두 표시 갱신 가능.
- 생존 데이터 미제공 시 결과 UI는 Survival time: -- 표시. MaxTime-CurrentTime을 생존 시간으로 오인해 사용하지 않음.

## Editor 사용
- StartScene을 열고 Play로 전체 흐름 시작.
- GameScene 단독 Play도 가능.
- 게임오버 수동 시험: Play 중 GameFlowManager 컴포넌트 컨텍스트 메뉴 → Test/Game Over (Editor only).
- Assets/Prefabs/UI 아래 Prefab 및 StartLayout/GameLayout에서 배치 수정.
- UIFlowSetup은 최초 에셋 생성용. 재실행은 기존 생성 자산이 있으면 중단하여 후속 수동 편집을 보호함.
- UIFlowValidation은 Editor 전용 실제 Scene/입력/타이머 통합 검증. 게임 빌드에 포함되지 않음.

## 남은 실제 환경 검증
- 오디오가 없으므로 음량 조절의 실제 청취/믹서 그룹 검증은 할 수 없음.
- Screen API 설정의 실제 창 모드 변화, 재실행 복원 및 여러 해상도에서 육안 확인은 그래픽 실행 환경에서 추가 검증 필요.
- 향후 실제 BGM/SFX가 생기면 AudioVolumeBinding을 지정하거나 Mixer를 연결할 것.
- 생존 시간 원본 데이터 제공 필요.

## 실제 검증 결과 (2026-11-02)
- Unity 6000.6.1f1에서 스크립트 컴파일 및 Prefab/Scene 생성 완료.
- 열린 Unity Editor에서 KEYBOUND > Validate UI Flow 실행, KEYBOUND_INTEGRATION_OK 확인.
- 실제 Play 프레임과 InputSystem 키 이벤트를 사용한 통합 검사 25개 통과. 상세 항목은 UI-Flow-Validation.txt 참조.
- 초기 검증에서 발견한 GameScene UILayout 참조 누락을 수정한 뒤 전체 검사 통과.
- 마우스로 START → PAUSE → SETTINGS를 직접 클릭하고 화면 배치 확인. 키보드, 플레이어, 시간 UI 및 팝업 표시 순서 정상.
- 16:9 Game View에서 확인했으며 여러 해상도와 빌드 실행의 육안 검증은 미실시.
- 음량 설정은 채널별 값 저장 및 Scene 이동 후 유지까지 확인. 프로세스 완전 종료 후 재실행 검증은 미실시.
- 오디오 실제 청취, Mixer 그룹 연결, 빌드에서의 전체화면/창 전환 및 재실행 복원은 미검증.
- 보호 대상 Keyboard/Player/Time 스크립트, KeyboardKey Prefab, SampleScene, TimeScene에 Git diff 없음.
- 작업 브랜치: feature/ui-game-flow. 커밋/푸시 미실시.

## 최신 main 병합 후 연결 변경
- UI 관리 파일은 main의 이동을 유지하여 Assets/Scripts/z/SceneUIManager.cs 사용. 이번 UI 구현을 유지하고 Scene 및 Editor 도구 참조를 SceneUIManager에 맞춤.
- Scene 이동 관리도 Assets/Scripts/z/GameSceneManager.cs 위치 유지. 이번 구현의 시간 배율 복원 기능 유지.
- GameFlowManager와 GameScene은 이번 UI 구현을 유지하고 GameFlowManager 메타데이터 및 GameScene GUID도 복원하여 참조 단절 방지.
- main의 최신 TimeManager(RemainingTime/ElapsedTime/OnRemainingTimeUpdated)를 그대로 사용. 시간 계산은 추가하거나 수정하지 않음.
- 삭제된 기존 TimeUI 대신 CountdownUI 표시 어댑터를 추가. 기존 GameScene의 TMP 텍스트와 Slider를 그대로 재사용.
- GameFlowManager는 StageManager가 없는 현재 GameScene에서만 공개 StartTimer API로 타이머를 시작. StageManager가 있는 Scene에서는 기존 시작 흐름을 유지.
- GameOver 시 기존 TimeManager.ElapsedTime을 결과에 표시. SetSurvivalTime 외부 입력 진입점도 유지.
- main의 최신 이동·충돌·키보드·시간·Stage/Aim·GameHUD 파일은 수정 없이 보존. main의 TestScene도 유지.
- UIFlowSetup은 이전 소스 Scene을 사용하던 최초 생성 도구이므로 소스가 없는 최신 구조에서는 명시적으로 중단. 현재 Prefab과 Scene은 이미 생성되어 있어 실행에 필요하지 않음.
- 기존 25개 검사 기록은 최초 구현 검증 기록이며, 병합본 검증 결과는 아래 별도로 기록.

### 병합본 검증 결과
- Unity 6000.6.1f1 배치 실행, 컴파일 오류 없이 종료 코드 0.
- 최초 25개 검사와 최신 TimeManager.ElapsedTime 연결 검사를 포함한 26개 통합 검사 통과. KEYBOUND_INTEGRATION_OK 확인.
- 상세 기록: UI-Flow-Merge-Validation.txt.
- 배치 검증은 EnteredPlayMode 이후 실행하며 검증용 임시 InputSettings로 키 이벤트를 전달. 배치용 Play 옵션과 임시 입력 설정 및 음량 값은 검증 종료 시 복원.
- 타이머 만료는 실제 OnTimeOver 이벤트를 기다려 검사. 시간 시스템 내부 변경이나 수동 게임오버 대체 호출 없이 확인.
- 초기화 과정에서 UnityEditor.Search 검색 인덱스 내부 예외가 관찰되었으나 프로젝트 스크립트 컴파일과 런타임 통합 검사는 통과함.
- 병합본의 화면 배치 재촬영/실제 청취/빌드 화면 전환 검증은 추가하지 않음. 기존 제한사항 유지.
