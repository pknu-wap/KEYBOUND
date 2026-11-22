using System;
using System.Collections.Generic;
using System.Linq;
using Keybound.Keyboard;
using Keybound.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

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

        private readonly List<Key> submissions = new();
        private AimGenerationSettings[][] missionProfiles;
        private MiniGameData[][] gameProfiles;
        private LabMission currentMission;
        private CatalogRun catalogMission;
        private LabBoard missionBoard;
        private MiniGameData activeGame;
        private KeyboardKey origin;
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
        private string missionFeedback;
        private int stage;
        private int stageCount;
        private float rapidDeadline;
        private GUIStyle heading;
        private GUIStyle detail;
        private GUIStyle note;

        private const int PickerPageSize = 20;
        private int OriginalMissionCount => missionProfiles.Length + LabMission.Names.Length;
        public int MissionCount => 1 + OriginalMissionCount + LabCatalog.Missions.Length;
        public int MiniGameCount => 1 + gameProfiles.Length + LabCatalog.MiniGames.Length;
        public int CurrentStage => stage;
        public bool IsRunning => running;
        public bool IsMissionRunning => missionResult == Outcome.Running;
        public bool IsMiniGameRunning => miniResult == Outcome.Running;

        private void OnEnable()
        {
            missionProfiles = missions.GroupBy(mission => mission.AimType switch
            {
                AimType.RapidPress => AimType.PressCount,
                AimType.Typing => AimType.Sequence,
                _ => mission.AimType
            }).Select(group => group.ToArray()).ToArray();

            gameProfiles = games.GroupBy(GameProfileName).Select(group => group.ToArray()).ToArray();
            missionBoard = new LabBoard(map, 0.49f, new Vector2(-14f, 9f));
            map.KeyPressed += OnKeyPressed;
            processor.OnAimCompleted += OnAimCompleted;
            miniGames.OnMiniGameCompleted += OnMiniGameCompleted;
            miniGames.OnMiniGameFailed += OnMiniGameFailed;
        }

        private void OnDisable()
        {
            if (map != null)
                map.KeyPressed -= OnKeyPressed;
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
            if (index <= gameProfiles.Length)
                return gameProfiles[index - 1][variant];

            var template = Array.Find(games, game => game != null && game.ControllerPrefab is LabMiniGameController);
            return CatalogMiniGameData.Create(LabCatalog.MiniGames[index - gameProfiles.Length - 1], template.ControllerPrefab);
        }

        public void EndMission(bool success)
        {
            if (!running || missionResult != Outcome.Running)
                return;
            missionResult = success ? Outcome.Success : Outcome.Failure;
            missionFeedback = catalogMission?.Feedback;
            currentMission = null;
            catalogMission = null;
            processor.StopAim();
            missionBoard.Clear();
            submissions.Clear();
        }

        private void BeginMission()
        {
            if (activeMissionChoice == 0)
                return;

            missionResult = Outcome.Running;
            missionFeedback = null;
            origin = player.CurrentKey;
            if (activeMissionChoice <= missionProfiles.Length)
            {
                var selected = missionProfiles[activeMissionChoice - 1][activeMissionProfile];
                if (!aimManager.StartAim(selected))
                {
                    EndMission(false);
                    return;
                }

                rapidDeadline = selected.AimType == AimType.RapidPress
                    ? Time.time + processor.RequiredProgress * 0.5f : 0f;
                return;
            }

            if (activeMissionChoice > OriginalMissionCount)
            {
                catalogMission = new CatalogRun(LabCatalog.Missions[activeMissionChoice - OriginalMissionCount - 1],
                    missionBoard, origin.Data.InputKey, startAtS: true);
                rapidDeadline = 0f;
                return;
            }

            var kind = (LabMissionKind)(activeMissionChoice - missionProfiles.Length - 1);
            currentMission = new LabMission(kind, missionBoard, () => player.SetCurrentKey(origin), stage);
            rapidDeadline = 0f;
        }

        private void StopTrial()
        {
            running = false;
            missionFeedback = null;
            currentMission = null;
            catalogMission = null;
            submissions.Clear();
            missionBoard?.Clear();
            if (processor != null)
                processor.StopAim();
            if (miniGames != null)
                miniGames.CancelMiniGame();
            if (activeGame is CatalogMiniGameData transient)
                Destroy(transient);
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

        private void OnKeyPressed(KeyboardKey key)
        {
            if ((currentMission != null || catalogMission != null) && player.CurrentKey == key)
                submissions.Add(key.Data.InputKey);
        }

        private void LateUpdate()
        {
            if (!running)
            {
                AdvanceQueue();
                return;
            }

            if (catalogMission != null && player.CurrentKey != null)
            {
                Key position = player.CurrentKey.Data.InputKey;
                catalogMission.Tick(position, InputKeyboard.current, Time.deltaTime);
                foreach (Key key in submissions)
                {
                    if (key == position)
                        catalogMission.Submit(key);
                }
                submissions.Clear();
                if (catalogMission.Result.HasValue)
                    EndMission(catalogMission.Result.Value);
            }
            else if (currentMission != null && player.CurrentKey != null)
            {
                Key position = player.CurrentKey.Data.InputKey;
                currentMission.Tick(position, InputKeyboard.current, Time.deltaTime);
                foreach (Key key in submissions)
                {
                    if (key == player.CurrentKey?.Data.InputKey)
                        currentMission.Submit(key);
                }
                submissions.Clear();

                if (currentMission.Completed)
                    EndMission(true);
            }
            else if (missionResult == Outcome.Running && rapidDeadline > 0f && Time.time >= rapidDeadline)
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
            if (index > OriginalMissionCount)
                return LabCatalog.Missions[index - OriginalMissionCount - 1].Name;
            return index <= missionProfiles.Length ? string.Join(" / ", missionProfiles[index - 1].Select(profile => profile.AimType))
                : LabMission.Names[index - missionProfiles.Length - 1];
        }

        private string MiniGameName(int index)
        {
            if (index == 0) return "None";
            return index > gameProfiles.Length ? LabCatalog.MiniGames[index - gameProfiles.Length - 1].Name
                : GameProfileName(gameProfiles[index - 1][0]);
        }

        private static string GameProfileName(MiniGameData game) =>
            game.ControllerPrefab is ObstacleDodgeController ? "Obstacle Dodge" : game.DisplayName;

        private MiniGameStageMode GetStageMode(int index) => index <= gameProfiles.Length
            ? gameProfiles[index - 1][0].StageMode
            : LabCatalog.MiniGames[index - gameProfiles.Length - 1].StageMode;

        private static string ModeLabel(MiniGameStageMode mode) => mode switch
        {
            MiniGameStageMode.OneStageAlone => "Solo stage",
            MiniGameStageMode.OneStageWithMission => "One stage with mission",
            MiniGameStageMode.TwoStagesWithMission => "Two stages with missions",
            _ => "Three stages with missions"
        };

        private string MissionStatus()
        {
            if (catalogMission != null)
                return catalogMission.Status;
            if (currentMission != null)
                return currentMission.Status;
            if (missionResult != Outcome.Running || processor.CurrentAim == null)
                return $"Mission  {missionResult}   {missionFeedback}";

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
                return activeGame is CatalogMiniGameData catalogData && !string.IsNullOrEmpty(catalogData.Feedback)
                    ? $"{activeGame.DisplayName}  {miniResult}   {catalogData.Feedback}"
                    : $"{activeGame.DisplayName}  {miniResult}";

            return miniGames.ActiveController switch
            {
                LabMiniGameController lab => lab.Progress,
                NumberHuntController hunt => $"Score  {hunt.CurrentScore}/{hunt.TargetScore}",
                ObstacleDodgeController dodge => $"Wave  {dodge.CurrentWave}/{dodge.WaveCount}    Hits  {dodge.CollisionCount}",
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
            if (index > 0)
                pickerReferences = index > PickerCoreCount;
            int highlighted = Mathf.Max(PickerOffset + 1, index);
            pickerPage = (highlighted - PickerOffset - 1) / PickerPageSize;
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
                "F2  Library     F3 / F4  Check / clear this list     Shift+F3 / Shift+F4  All lists", note);
            GUI.Label(new Rect(1052, 248, 138, 24), $"{checkedItems.Count} checked", note);
        }
    }
}
