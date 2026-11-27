using System;
using System.Text;
using Keybound.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LeaderboardHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private LeaderboardManager leaderboardManager;
    [SerializeField] private LeaderboardRecorder recorder;

    [Header("Result UI (GameScene)")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TMP_Text finalScoreText;
    [SerializeField] private GameObject registrationPanel;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_Text statusText;

    [Header("Leaderboard UI (both scenes)")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private TMP_Text leaderboardText;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;

    [SerializeField] private SceneUIManager startMenu;
    private GameObject previousSelection;

    private void Awake()
    {
        if (leaderboardManager == null) leaderboardManager = FindAnyObjectByType<LeaderboardManager>();
        if (recorder == null) recorder = FindAnyObjectByType<LeaderboardRecorder>();
        if (playerNameInput != null) playerNameInput.characterLimit = LeaderboardManager.MaxNameLength;
        if (leaderboardPanel != null) leaderboardPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (leaderboardManager != null) leaderboardManager.OnLeaderboardChanged += RefreshLeaderboard;
        if (recorder != null) recorder.OnResultChanged += RefreshResult;
        if (registerButton != null) registerButton.onClick.AddListener(RegisterScore);
        if (openButton != null) openButton.onClick.AddListener(OpenLeaderboard);
        if (closeButton != null) closeButton.onClick.AddListener(CloseLeaderboard);
    }

    private void Start()
    {
        var menuView = startMenu != null ? startMenu.GetView(UIViewKind.StartMenu) : null;
        if (menuView != null && openButton != null)
            openButton.transform.SetParent(menuView.transform, false);
        RefreshResult();
        RefreshLeaderboard();
    }

    private void OnDisable()
    {
        CloseLeaderboard();
        if (leaderboardManager != null) leaderboardManager.OnLeaderboardChanged -= RefreshLeaderboard;
        if (recorder != null) recorder.OnResultChanged -= RefreshResult;
        if (registerButton != null) registerButton.onClick.RemoveListener(RegisterScore);
        if (openButton != null) openButton.onClick.RemoveListener(OpenLeaderboard);
        if (closeButton != null) closeButton.onClick.RemoveListener(CloseLeaderboard);
    }

    public void RefreshResult()
    {
        bool gameOver = recorder != null && recorder.IsGameOver;
        if (resultPanel != null) resultPanel.SetActive(gameOver);
        var result = recorder != null ? recorder.CurrentResult : null;
        if (finalScoreText != null)
            finalScoreText.text = result == null ? "SCORE --" : $"SCORE {result.Score:N0}";
        bool canRegister = gameOver && recorder.CanRegister;
        if (registrationPanel != null) registrationPanel.SetActive(canRegister);
        if (registerButton != null) registerButton.interactable = canRegister;
        if (statusText == null) return;
        if (!gameOver) statusText.text = "";
        else if (recorder.HasSubmitted) statusText.text = "RECORD SAVED";
        else if (recorder.IsAborted) statusText.text = "INVALID RUN";
        else if (leaderboardManager == null || !leaderboardManager.IsStorageReady) statusText.text = "STORAGE UNAVAILABLE";
        else if (result == null) statusText.text = "WAITING FOR RESULT";
        else statusText.text = canRegister ? "TOP 10 QUALIFIED" : "NOT IN TOP 10";
    }

    public void RegisterScore()
    {
        if (recorder == null || playerNameInput == null) return;
        var result = recorder.SubmitRecord(playerNameInput.text);
        if (statusText == null) return;
        switch (result)
        {
            case LeaderboardSubmitResult.Saved: statusText.text = "RECORD SAVED"; break;
            case LeaderboardSubmitResult.InvalidName: statusText.text = "USE 1-8 LETTERS OR NUMBERS"; break;
            case LeaderboardSubmitResult.PersonalBestNotBeaten: statusText.text = "PERSONAL BEST NOT BEATEN"; break;
            case LeaderboardSubmitResult.NotQualified: statusText.text = "NOT IN TOP 10"; break;
            case LeaderboardSubmitResult.AlreadySubmitted: statusText.text = "ALREADY SAVED"; break;
            case LeaderboardSubmitResult.StorageError: statusText.text = "SAVE FAILED"; break;
            default: statusText.text = "INVALID RUN"; break;
        }
    }

    public void OpenLeaderboard()
    {
        if (leaderboardPanel == null || leaderboardPanel.activeSelf || (startMenu != null && startMenu.SettingsOpen))
            return;

        RefreshLeaderboard();
        var events = EventSystem.current;
        if (startMenu != null)
        {
            previousSelection = events?.currentSelectedGameObject;
            startMenu.SetLeaderboardOpen(true);
            if (openButton != null)
                openButton.gameObject.SetActive(false);
        }
        leaderboardPanel.SetActive(true);
        if (startMenu != null && closeButton != null)
            events?.SetSelectedGameObject(closeButton.gameObject);
    }

    public void CloseLeaderboard()
    {
        if (leaderboardPanel == null || !leaderboardPanel.activeSelf)
            return;

        leaderboardPanel.SetActive(false);
        if (startMenu == null)
            return;

        startMenu.SetLeaderboardOpen(false);
        if (openButton != null)
            openButton.gameObject.SetActive(true);
        if (isActiveAndEnabled)
        {
            var focus = previousSelection != null && previousSelection.activeInHierarchy
                ? previousSelection : startMenu.GetView(UIViewKind.StartMenu)?.primaryButton?.gameObject;
            EventSystem.current?.SetSelectedGameObject(focus);
        }
        previousSelection = null;
    }

    public void RefreshLeaderboard()
    {
        if (leaderboardText == null) return;
        if (leaderboardManager == null || !leaderboardManager.IsStorageReady)
        {
            leaderboardText.text = "STORAGE UNAVAILABLE";
            return;
        }
        var records = leaderboardManager.GetRecords();
        var builder = new StringBuilder("<mspace=0.63em>RANK   NAME          SCORE   STAGE   TIME\n");
        for (int i = 0; i < LeaderboardManager.MaxRecords; i++)
        {
            if (i >= records.Count)
            {
                builder.AppendLine($"{i + 1:00}     ---");
                continue;
            }
            var entry = records[i];
            double hundredths = entry.SurvivalHundredths;
            double minutes = Math.Floor(hundredths / 6000);
            int seconds = (int)(hundredths % 6000) / 100;
            int fraction = (int)(hundredths % 100);
            builder.AppendLine($"{i + 1:00}     {entry.PlayerName,-8}   {entry.Score,8:N0}   {entry.Stage,5:00}   {minutes:00}:{seconds:00}.{fraction:00}");
        }
        leaderboardText.text = builder.Append("</mspace>").ToString();
    }
}
