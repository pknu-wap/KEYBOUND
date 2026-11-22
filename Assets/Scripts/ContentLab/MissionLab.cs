using System.Linq;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Keybound.ContentLab
{
    [DefaultExecutionOrder(50)]
    public sealed partial class MissionLab : MonoBehaviour
    {
        private enum Outcome { Inactive, Running, Success, Failure, Expired }
        private enum Picker { None, Mission, MiniGame }

        [SerializeField] private KeyboardMapManager map;
        [SerializeField] private PlayerKeyboardPosition player;
        [SerializeField] private AimManager aimManager;
        [SerializeField] private AimConditionProcessor processor;
        [SerializeField] private MiniGameManager miniGames;
        [SerializeField] private AimGenerationSettings[] missions;
        [SerializeField] private MiniGameData[] games;

        private AimGenerationSettings[][] missionProfiles;
        private MiniGameData[][] gameProfiles;
        private MiniGameData activeGame;
        private Picker picker;
        private int pickerPage;
        private Outcome missionResult;
        private Outcome miniResult;
        private bool running;
        private bool miniGamePractice;
        private int missionChoice = 1;
        private int miniGameChoice;
        private int missionProfile;
        private int gameProfile;
        private int activeMissionChoice;
        private int activeMissionProfile;
        private int stage;
        private int stageCount;
        private float rapidDeadline;
        private GUIStyle heading;
        private GUIStyle detail;
        private GUIStyle note;

        private const int PickerPageSize = 20;
        public int MissionCount => 1 + missionProfiles.Length;
        public int MiniGameCount => 1 + gameProfiles.Length;
        public int CurrentStage => stage;
        public bool IsRunning => running;
        public bool IsMissionRunning => missionResult == Outcome.Running;
        public bool IsMiniGameRunning => miniResult == Outcome.Running;

        private void Awake()
        {
            missionProfiles = missions.GroupBy(mission => mission.AimType switch
            {
                AimType.RapidPress => AimType.PressCount,
                AimType.Typing => AimType.Sequence,
                _ => mission.AimType
            }).Select(group => group.ToArray()).ToArray();

            gameProfiles = games.GroupBy(game => game.DisplayName).Select(group => group.ToArray()).ToArray();
        }

        private void OnEnable()
        {
            processor.OnAimCompleted += OnAimCompleted;
            miniGames.OnMiniGameCompleted += OnMiniGameCompleted;
            miniGames.OnMiniGameFailed += OnMiniGameFailed;
        }

        private void OnDisable()
        {
            if (processor != null)
                processor.OnAimCompleted -= OnAimCompleted;
            if (miniGames != null)
            {
                miniGames.OnMiniGameCompleted -= OnMiniGameCompleted;
                miniGames.OnMiniGameFailed -= OnMiniGameFailed;
            }
            StopTrial();
            trialQueue = null;
        }

        public bool StartTrial(int missionIndex, int gameIndex, bool practice = false) =>
            StartTrial(missionIndex, gameIndex, practice, 0, 0);

        private bool StartTrial(int missionIndex, int gameIndex, bool practice, int missionVariant, int gameVariant)
        {
            if (!BeginTrial(missionIndex, gameIndex, practice, missionVariant, gameVariant))
                return false;

            trialQueue = null;
            queueSummary = null;
            missionChoice = missionIndex;
            miniGameChoice = gameIndex;
            missionProfile = missionVariant;
            gameProfile = gameVariant;
            return true;
        }

        private bool BeginTrial(int missionIndex, int gameIndex, bool practice, int missionVariant, int gameVariant)
        {
            if (missionIndex < 0 || missionIndex >= MissionCount ||
                gameIndex < 0 || gameIndex >= MiniGameCount || player.CurrentKey == null)
                return false;

            if (gameIndex == 0 && (missionIndex == 0 || practice) ||
                gameIndex > 0 && GetStageMode(gameIndex) != MiniGameStageMode.OneStageAlone && missionIndex == 0 && !practice)
                return false;

            StopTrial();
            MiniGameData selectedGame = SelectGame(gameIndex, gameVariant);
            activeGame = selectedGame;
            miniGamePractice = practice;
            activeMissionChoice = practice || selectedGame != null && !selectedGame.RunsWithMission ? 0 : missionIndex;
            activeMissionProfile = missionVariant;
            stage = 1;
            stageCount = practice || selectedGame == null ? 1 : selectedGame.StageCount;
            missionResult = Outcome.Inactive;
            miniResult = Outcome.Inactive;
            if (map.TryGetKey(Key.S, out var start))
                player.SetCurrentKey(start);
            running = true;

            BeginMission();
            if (selectedGame != null)
            {
                miniResult = Outcome.Running;
                if (!miniGames.StartMiniGame(stage, selectedGame))
                    miniResult = Outcome.Failure;
            }
            return true;
        }

        private MiniGameData SelectGame(int index, int variant)
        {
            if (index == 0)
                return null;
            return gameProfiles[index - 1][variant];
        }

        public void EndMission(bool success)
        {
            if (!running || missionResult != Outcome.Running)
                return;
            missionResult = success ? Outcome.Success : Outcome.Failure;
            processor.StopAim();
        }

        private void BeginMission()
        {
            if (activeMissionChoice == 0)
                return;

            missionResult = Outcome.Running;
            var selected = missionProfiles[activeMissionChoice - 1][activeMissionProfile];
            if (!aimManager.StartAim(selected))
            {
                EndMission(false);
                return;
            }

            rapidDeadline = selected.AimType == AimType.RapidPress
                ? Time.time + processor.RequiredProgress * 0.5f : 0f;
        }

        private void StopTrial()
        {
            running = false;
            if (processor != null)
                processor.StopAim();
            if (miniGames != null)
                miniGames.CancelMiniGame();
            activeGame = null;
        }

        private void OnAimCompleted(AimData _) => EndMission(true);
        private void OnMiniGameCompleted(int _) => SetMiniGameResult(Outcome.Success);
        private void OnMiniGameFailed(int _) => SetMiniGameResult(Outcome.Failure);

        private void SetMiniGameResult(Outcome result)
        {
            if (running && miniResult == Outcome.Running)
                miniResult = result;
        }

        private void LateUpdate()
        {
            if (!running)
            {
                AdvanceQueue();
                return;
            }

            if (missionResult == Outcome.Running && rapidDeadline > 0f && Time.time >= rapidDeadline)
            {
                EndMission(false);
            }

            if (missionResult == Outcome.Running)
                return;
            if (stageCount == 1 && miniResult == Outcome.Running)
                return;

            if (stage < stageCount)
            {
                stage++;
                BeginMission();
                return;
            }

            if (miniResult == Outcome.Running)
            {
                miniResult = Outcome.Expired;
                miniGames.CancelMiniGame();
            }
            running = false;
            FinishQueuedTrial();
        }

        private string MissionName(int index)
        {
            if (index == 0) return "None";
            return string.Join(" / ", missionProfiles[index - 1].Select(profile => profile.AimType));
        }

        private string MiniGameName(int index)
        {
            if (index == 0) return "None";
            return gameProfiles[index - 1][0].DisplayName;
        }

        private MiniGameStageMode GetStageMode(int index) => gameProfiles[index - 1][0].StageMode;

        private static string ModeLabel(MiniGameStageMode mode) => mode switch
        {
            MiniGameStageMode.OneStageAlone => "Solo stage",
            MiniGameStageMode.OneStageWithMission => "One stage with mission",
            MiniGameStageMode.TwoStagesWithMission => "Two stages with missions",
            _ => "Three stages with missions"
        };

        private string MissionStatus()
        {
            if (missionResult != Outcome.Running || processor.CurrentAim == null)
                return $"Mission  {missionResult}";

            var aim = processor.CurrentAim;
            string prompt = string.IsNullOrEmpty(aim.QuestionText) ? aim.Description : aim.QuestionText;
            if (aim.Type == AimType.Typing || aim.Type == AimType.Calculation)
                return $"{prompt}    {processor.SubmittedText}{new string('_', Mathf.Max(0, processor.RequiredProgress - processor.CurrentProgress))}";

            return $"{prompt}    {processor.CurrentProgress}/{processor.RequiredProgress}" +
                (rapidDeadline > 0f ? $"    {Mathf.Max(0f, rapidDeadline - Time.time):0.0} s" : "");
        }

        private string MiniGameStatus()
        {
            if (activeGame == null)
                return "Mini-game  Inactive";
            if (miniResult != Outcome.Running)
                return $"{activeGame.DisplayName}  {miniResult}";

            return miniGames.ActiveController switch
            {
                NumberHuntController hunt => $"Score  {hunt.CurrentScore}/{hunt.TargetScore}",
                ObstacleDodgeController dodge => $"Wave  {dodge.CurrentWave}/{dodge.WaveCount}    Hits  {dodge.CollisionCount}/{dodge.MaxCollisions}",
                _ => activeGame.DisplayName
            };
        }

        private void OnGUI()
        {
            if (heading == null)
            {
                heading = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold };
                detail = new GUIStyle(GUI.skin.label) { fontSize = 17, wordWrap = true };
                note = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            }

            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            Matrix4x4 previous = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) / 2f, 0), Quaternion.identity,
                new Vector3(scale, scale, 1f));

            if (picker == Picker.None)
                DrawPanel();
            else
                DrawPicker();
            DrawMenuButtons();

            GUI.matrix = previous;
        }

        private void DrawPanel()
        {
            GUI.Box(new Rect(70, 12, 1140, 275), GUIContent.none);
            GUI.Label(new Rect(90, 19, 355, 38), "KEYBOUND  |  Content Lab", heading);
            GUI.Label(new Rect(700, 26, 490, 26), QueueStatus() ?? (stage > 0
                ? $"Stage {stage}/{stageCount}    {(miniGamePractice ? "Mini-game practice" : running ? "Running" : "Finished")}"
                : "Check missions and mini-games to build a queue."), note);

            GUI.Label(new Rect(90, 158, 1100, 47), MissionStatus(), detail);
            GUI.Label(new Rect(90, 207, 1100, 45), MiniGameStatus(), detail);
            GUI.Label(new Rect(90, 256, 1100, 22),
                "Arrows  Move focus     Enter  Select     Esc  Stop active queue", note);
        }

        private void OpenPicker(Picker mode, int index)
        {
            picker = mode;
            int highlighted = Mathf.Max(1, index);
            pickerPage = (highlighted - 1) / PickerPageSize;
            focus = (MenuCommand)((int)MenuCommand.Entry + highlighted);
        }

        private void DrawPicker()
        {
            bool choosingMission = picker == Picker.Mission;
            GUI.Box(new Rect(70, 12, 1140, 275), GUIContent.none);
            GUI.Label(new Rect(90, 19, 450, 38), choosingMission ? "Choose a mission" : "Choose a mini-game", heading);
            GUI.Label(new Rect(609, 28, 58, 28), $"{pickerPage + 1} / {PickerPages}", note);
            GUI.Label(new Rect(90, 240, 985, 22),
                "Arrows  Move focus     Enter  Check / uncheck     Tab / Shift+Tab  Page     Esc  Back", note);
            GUI.Label(new Rect(90, 260, 985, 22),
                "F3 / F4  Check / clear this list     Shift+F3 / Shift+F4  All lists", note);
            GUI.Label(new Rect(1052, 248, 138, 24), $"{checkedItems.Count} checked", note);
        }
    }
}
