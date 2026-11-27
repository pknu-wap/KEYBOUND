using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Keybound.UI
{
    public sealed class LeaderboardDeveloperPanel : MonoBehaviour
    {
        private const int FirstRowY = 90;
        private const int ViewportHeight = 360;
        private const int RowSpacing = 36;
        private const string EmptyMessage = "No records saved.";
        private static readonly CultureInfo NumberCulture = CultureInfo.InvariantCulture;
        private readonly List<Row> rows = new List<Row>();
        private LeaderboardManager leaderboard;
        private ScrollRect recordList;
        private Button addButton;
        private Button revertButton;
        private Button saveButton;
        private Text status;
        private bool dirty;

        internal void Initialize(LeaderboardManager manager)
        {
            leaderboard = manager;
            DeveloperControls.Label(transform, "LEADERBOARD", 0, 0, 310, 30, 22, FontStyle.Bold);
            DeveloperControls.Label(transform, "Save applies changes. Closing discards unsaved edits.",
                0, 33, 920, 25, 16);

            DeveloperControls.Label(transform, "NAME", 8, 61, 183, 25, 16, FontStyle.Bold);
            DeveloperControls.Label(transform, "SCORE", 201, 61, 162, 25, 16, FontStyle.Bold);
            DeveloperControls.Label(transform, "STAGE", 373, 61, 119, 25, 16, FontStyle.Bold);
            DeveloperControls.Label(transform, "SURVIVAL (SEC)", 502, 61, 160, 25, 16, FontStyle.Bold);
            DeveloperControls.Label(transform, "MISSIONS", 672, 61, 128, 25, 16, FontStyle.Bold);

            CreateRecordList();
            status = DeveloperControls.Label(transform, "", 0, 454, 932, 28, 17);
            addButton = DeveloperControls.Button(transform, "ADD RECORD", 0, 494, 160, 36);
            revertButton = DeveloperControls.Button(transform, "REVERT", 658, 494, 126, 36);
            saveButton = DeveloperControls.Button(transform, "SAVE", 796, 494, 132, 36);
            var colors = saveButton.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(218, 218, 218, 255);
            saveButton.colors = colors;

            addButton.onClick.AddListener(AddRecord);
            revertButton.onClick.AddListener(Reload);
            saveButton.onClick.AddListener(Save);
        }

        private void CreateRecordList()
        {
            var viewport = DeveloperControls.Rect(transform, "Record Viewport", 0, FirstRowY, 910, ViewportHeight);
            viewport.gameObject.AddComponent<RectMask2D>();
            viewport.gameObject.AddComponent<Image>().color = Color.clear;
            var content = DeveloperControls.Rect(viewport, "Records", 0, 0, 910, 0);

            var track = DeveloperControls.Rect(transform, "Record Scrollbar", 916, FirstRowY, 12, ViewportHeight);
            track.gameObject.AddComponent<Image>().color = new Color32(36, 36, 36, 255);
            var handle = DeveloperControls.Rect(track, "Handle", 0, 0, 0, 0);
            handle.anchorMin = Vector2.zero;
            handle.anchorMax = Vector2.one;
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.handleRect = handle;
            scrollbar.targetGraphic = handle.gameObject.AddComponent<Image>();
            scrollbar.targetGraphic.color = new Color32(140, 140, 140, 255);
            scrollbar.direction = Scrollbar.Direction.BottomToTop;

            recordList = viewport.gameObject.AddComponent<ScrollRect>();
            recordList.viewport = viewport;
            recordList.content = content;
            recordList.horizontal = false;
            recordList.movementType = ScrollRect.MovementType.Clamped;
            recordList.scrollSensitivity = RowSpacing;
            recordList.verticalScrollbar = scrollbar;
            recordList.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        }

        internal void Reload()
        {
            foreach (var row in rows)
                DestroyRow(row);
            rows.Clear();

            foreach (var entry in leaderboard.GetRecords())
                AddRow(entry);
            ResizeRecordList();
            recordList.StopMovement();
            recordList.verticalNormalizedPosition = 1f;
            dirty = false;
            UpdateButtons();
            status.text = leaderboard.IsStorageReady
                ? (rows.Count == 0 ? EmptyMessage : $"{rows.Count} records")
                : "STORAGE UNAVAILABLE";
        }

        private void AddRecord()
        {
            if (!addButton.interactable) return;
            var row = AddRow(new LeaderboardEntry { PlayerName = "", Stage = 1 });
            ResizeRecordList();
            MarkDirty();
            FocusField(rows.Count - 1, row.Name);
        }

        private Row AddRow(LeaderboardEntry entry)
        {
            var root = DeveloperControls.Rect(recordList.content, "Record Row", 0, RowSpacing * rows.Count, 910, 34);
            var row = new Row(root, entry);
            rows.Add(row);
            foreach (var field in row.Fields)
                field.onValueChanged.AddListener(_ => MarkDirty());
            DeveloperControls.Button(root, "REMOVE", 810, 2, 98, 30).onClick.AddListener(() => RemoveRow(row));
            return row;
        }

        private void RemoveRow(Row row)
        {
            if (!rows.Remove(row)) return;
            DestroyRow(row);
            for (int index = 0; index < rows.Count; index++)
                rows[index].Root.anchoredPosition = new Vector2(0, -RowSpacing * index);
            ResizeRecordList();
            MarkDirty();
        }

        private void ResizeRecordList()
        {
            recordList.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, RowSpacing * rows.Count);
        }

        private void FocusField(int index, InputField field)
        {
            float top = RowSpacing * index;
            float bottom = top + RowSpacing;
            float maxOffset = Mathf.Max(0, recordList.content.rect.height - ViewportHeight);
            if (maxOffset > 0)
            {
                float offset = recordList.content.anchoredPosition.y;
                offset = Mathf.Clamp(offset, bottom - ViewportHeight, top);
                recordList.StopMovement();
                recordList.verticalNormalizedPosition = 1f - Mathf.Clamp01(offset / maxOffset);
            }
            EventSystem.current?.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();
        }

        private void DestroyRow(Row row)
        {
            var events = EventSystem.current;
            if (events != null && events.currentSelectedGameObject != null &&
                events.currentSelectedGameObject.transform.IsChildOf(row.Root))
                events.SetSelectedGameObject(null);

            row.Root.gameObject.SetActive(false);
            Destroy(row.Root.gameObject);
        }

        private void MarkDirty()
        {
            dirty = true;
            status.text = "Unsaved changes.";
            UpdateButtons();
        }

        private void UpdateButtons()
        {
            addButton.interactable = leaderboard.IsStorageReady;
            saveButton.interactable = leaderboard.IsStorageReady && dirty;
            revertButton.interactable = dirty;
        }

        private void Save()
        {
            if (!saveButton.interactable) return;
            var draft = new List<LeaderboardEntry>(rows.Count);
            string now = DateTime.UtcNow.ToString("O", NumberCulture);
            for (int index = 0; index < rows.Count; index++)
            {
                if (!rows[index].TryRead(now, out var entry, out var invalidField, out var message))
                {
                    status.text = $"Row {index + 1}: {message}";
                    FocusField(index, invalidField);
                    return;
                }
                draft.Add(entry);
            }

            if (!leaderboard.TrySaveEdits(draft, out string error))
            {
                status.text = error;
                return;
            }

            Reload();
            status.text = $"Leaderboard saved. {rows.Count} records";
        }

        private sealed class Row
        {
            private readonly string recordedAt;
            private readonly InputField score;
            private readonly InputField stage;
            private readonly InputField survival;
            private readonly InputField missions;

            internal RectTransform Root { get; }
            internal InputField Name { get; }
            internal InputField[] Fields { get; }

            internal Row(RectTransform root, LeaderboardEntry entry)
            {
                Root = root;
                recordedAt = entry.RecordedAt;
                Name = DeveloperControls.Field(root, "Name", entry.PlayerName, 8, 183);
                score = DeveloperControls.Field(root, "Score", entry.Score.ToString(NumberCulture), 201, 162);
                stage = DeveloperControls.Field(root, "Stage", entry.Stage.ToString(NumberCulture), 373, 119);
                survival = DeveloperControls.Field(root, "Survival", entry.SurvivalTime.ToString("R", NumberCulture), 502, 160);
                missions = DeveloperControls.Field(root, "Missions", entry.CompletedAimCount.ToString(NumberCulture), 672, 128);
                Fields = new[] { Name, score, stage, survival, missions };
            }

            internal bool TryRead(string now, out LeaderboardEntry entry, out InputField invalidField, out string message)
            {
                entry = null;
                if (!int.TryParse(score.text, NumberStyles.Integer, NumberCulture, out int scoreValue) || scoreValue < 0)
                {
                    invalidField = score;
                    message = "Score must be a whole number of 0 or higher.";
                    return false;
                }
                if (!int.TryParse(stage.text, NumberStyles.Integer, NumberCulture, out int stageValue) || stageValue < 1)
                {
                    invalidField = stage;
                    message = "Stage must be a whole number of 1 or higher.";
                    return false;
                }
                if (!float.TryParse(survival.text, NumberStyles.Float, NumberCulture, out float survivalValue) ||
                    !(survivalValue >= 0f) || float.IsInfinity(survivalValue))
                {
                    invalidField = survival;
                    message = "Survival must be a non-negative number in seconds. Use a dot for decimals.";
                    return false;
                }
                if (!int.TryParse(missions.text, NumberStyles.Integer, NumberCulture, out int missionsValue) || missionsValue < 0)
                {
                    invalidField = missions;
                    message = "Missions must be a whole number of 0 or higher.";
                    return false;
                }

                entry = new LeaderboardEntry
                {
                    PlayerName = Name.text,
                    Score = scoreValue,
                    Stage = stageValue,
                    SurvivalTime = survivalValue,
                    CompletedAimCount = missionsValue,
                    RecordedAt = recordedAt ?? now
                };
                invalidField = null;
                message = null;
                return true;
            }
        }
    }
}
