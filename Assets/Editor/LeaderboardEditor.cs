using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public sealed class LeaderboardEditor : EditorWindow
{
    private List<LeaderboardEntry> records = new List<LeaderboardEntry>();
    private Vector2 scroll;
    private bool loaded;
    private bool dirty;
    private string message = "";
    private MessageType messageType = MessageType.Info;

    [MenuItem("KEYBOUND/Leaderboard Editor")]
    public static void Open()
    {
        var window = GetWindow<LeaderboardEditor>("Leaderboard Editor");
        window.minSize = new Vector2(460, 360);
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
        if (!EditorApplication.isPlayingOrWillChangePlaymode) Reload();
    }

    private void OnDisable()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 게임 실행 중 Manager의 메모리 기록과 충돌하지 않도록 한다.
        if (state == PlayModeStateChange.EnteredEditMode) Reload();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("KEYBOUND Leaderboard", EditorStyles.boldLabel);
        EditorGUILayout.SelectableLabel(LeaderboardStorage.FilePath,
            EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
        if (GUILayout.Button("Open Save Folder")) OpenSaveFolder();
        EditorGUILayout.HelpBox("Save Changes before entering Play Mode or closing this window. Records reload after Play Mode.", MessageType.Info);

        bool playing = EditorApplication.isPlayingOrWillChangePlaymode;
        if (playing)
            EditorGUILayout.HelpBox("Stop Play Mode to edit records.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(playing))
        {
            if (GUILayout.Button("Reload") && ConfirmReload()) Reload();
            using (new EditorGUI.DisabledScope(!loaded))
            {
                EditorGUILayout.BeginHorizontal();
                using (new EditorGUI.DisabledScope(records.Count >= LeaderboardManager.MaxRecords))
                    if (GUILayout.Button("Add Record")) AddRecord();
                if (GUILayout.Button("Sort"))
                {
                    records = LeaderboardManager.OrderByRank(records).ToList();
                    dirty = true;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.LabelField($"Records: {records.Count}/{LeaderboardManager.MaxRecords}" + (dirty ? " (Unsaved)" : ""));
                scroll = EditorGUILayout.BeginScrollView(scroll);
                int removeIndex = -1;
                for (int i = 0; i < records.Count; i++)
                {
                    var entry = records[i];
                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Record {i + 1}", EditorStyles.boldLabel);
                    if (GUILayout.Button("Delete", GUILayout.Width(70))) removeIndex = i;
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.BeginChangeCheck();
                    entry.PlayerName = EditorGUILayout.TextField("Player Name", entry.PlayerName);
                    entry.Score = EditorGUILayout.IntField("Score", entry.Score);
                    entry.Stage = EditorGUILayout.IntField("Stage", entry.Stage);
                    entry.SurvivalTime = EditorGUILayout.FloatField("Survival Time (sec)", entry.SurvivalTime);
                    entry.CompletedAimCount = EditorGUILayout.IntField("Completed Aims", entry.CompletedAimCount);
                    entry.RecordedAt = EditorGUILayout.TextField("Recorded At (UTC)", entry.RecordedAt);
                    if (EditorGUI.EndChangeCheck()) dirty = true;
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndScrollView();
                if (removeIndex >= 0)
                {
                    records.RemoveAt(removeIndex);
                    dirty = true;
                }

                if (GUILayout.Button("Save Changes")) Save();
                if (GUILayout.Button("Clear All Records") &&
                    EditorUtility.DisplayDialog("Clear All Records", "Remove all records? Click Save Changes to apply.", "Clear", "Cancel"))
                {
                    records.Clear();
                    dirty = true;
                    SetMessage("All records cleared in the editor. Click Save Changes to apply.");
                }
            }
        }
        if (!string.IsNullOrEmpty(message)) EditorGUILayout.HelpBox(message, messageType);
    }

    private bool ConfirmReload()
    {
        return !dirty || EditorUtility.DisplayDialog("Reload", "Discard unsaved changes?", "Reload", "Cancel");
    }

    private void Reload()
    {
        loaded = LeaderboardStorage.TryLoad(out var data);
        if (!loaded)
        {
            SetMessage("Load failed. Saving is disabled to protect the existing file.", MessageType.Error);
            return;
        }
        records = data;
        // 손상된 null 항목은 편집 목록에서 제외한다.
        records.RemoveAll(entry => entry == null);
        records = LeaderboardManager.OrderByRank(records).ToList();
        dirty = false;
        SetMessage("Records loaded.");
    }

    private void AddRecord()
    {
        int number = 1;
        while (records.Exists(entry => string.Equals(entry.PlayerName, "PLAYER" + number, StringComparison.OrdinalIgnoreCase))) number++;
        records.Add(new LeaderboardEntry
        {
            PlayerName = "PLAYER" + number,
            Score = 0,
            Stage = 1,
            SurvivalTime = 0f,
            CompletedAimCount = 0,
            RecordedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
        });
        dirty = true;
    }

    private void Save()
    {
        // 기존 Manager의 닉네임, 데이터 유효성, 순위 기준을 유지한다.
        if (records.Count > LeaderboardManager.MaxRecords)
        {
            SetMessage("Keep at most 10 records before saving.", MessageType.Error);
            return;
        }
        var names = new HashSet<string>(StringComparer.Ordinal);
        var updated = new List<LeaderboardEntry>();
        for (int i = 0; i < records.Count; i++)
        {
            var entry = records[i].Copy();
            if (!LeaderboardManager.TryNormalizeName(entry.PlayerName, out var name) || !names.Add(name))
            {
                SetMessage($"Record {i + 1}: invalid or duplicate player name.", MessageType.Error);
                return;
            }
            if (entry.Score < 0 || entry.Stage < 1 || entry.CompletedAimCount < 0 ||
                entry.SurvivalTime < 0f || float.IsNaN(entry.SurvivalTime) || float.IsInfinity(entry.SurvivalTime) ||
                !DateTime.TryParseExact(entry.RecordedAt, "O", CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var timestamp))
            {
                SetMessage($"Record {i + 1}: invalid data or timestamp. Use the original UTC round-trip format.", MessageType.Error);
                return;
            }
            entry.PlayerName = name;
            entry.RecordedAt = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            updated.Add(entry);
        }
        updated = LeaderboardManager.OrderByRank(updated).ToList();
        if (!LeaderboardStorage.TrySave(updated))
        {
            SetMessage("Save failed. Check the Console and folder permissions.", MessageType.Error);
            return;
        }
        records = updated;
        dirty = false;
        SetMessage("Records saved successfully.");
    }

    private void OpenSaveFolder()
    {
        try
        {
            Directory.CreateDirectory(LeaderboardStorage.SaveDirectory);
#if UNITY_EDITOR_WIN
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = LeaderboardStorage.SaveDirectory,
                UseShellExecute = true
            });
#else
            EditorUtility.RevealInFinder(LeaderboardStorage.SaveDirectory);
#endif
        }
        catch (Exception exception)
        {
            SetMessage("Cannot open folder: " + exception.Message, MessageType.Error);
        }
    }

    private void SetMessage(string value, MessageType type = MessageType.Info)
    {
        message = value;
        messageType = type;
    }
}
