using System.Collections.Generic;
using UnityEngine;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.ContentLab
{
    public sealed partial class MissionLab
    {
        private enum MenuCommand
        {
            Mission, MiniGame, MissionProfile, GamePattern, RunQueue, StopQueue, Start, Practice,
            PassMission, FailMission, PassGame, FailGame, Library, PreviousPage, NextPage, CheckAll, Clear, Back,
            Entry = 100
        }

        private readonly struct MenuButton
        {
            internal readonly MenuCommand Command;
            internal readonly Rect Bounds;
            internal readonly string Label;
            internal readonly bool Enabled;

            internal MenuButton(MenuCommand command, Rect bounds, string label, bool enabled)
            {
                Command = command;
                Bounds = bounds;
                Label = label;
                Enabled = enabled;
            }
        }

        private readonly List<MenuButton> menuButtons = new(PickerPageSize + 6);
        private MenuCommand focus = MenuCommand.Mission;
        private bool pickerReferences;

        private bool CanStart => missionChoice > 0 || miniGameChoice > 0
            && GetStageMode(miniGameChoice) == MiniGameStageMode.OneStageAlone;
        private int PickerTotal => (picker == Picker.Mission ? MissionCount : MiniGameCount) - 1;
        private int PickerReferenceCount => picker == Picker.Mission ? LabCatalog.ReferenceMissionCount : LabCatalog.ReferenceMiniGameCount;
        private int PickerCoreCount => PickerTotal - PickerReferenceCount;
        private int PickerOffset => pickerReferences ? PickerCoreCount : 0;
        private int PickerCount => pickerReferences ? PickerReferenceCount : PickerCoreCount;
        private int PickerPages => (PickerCount + PickerPageSize - 1) / PickerPageSize;

        private void Update()
        {
            InputKeyboard keyboard = InputKeyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (picker != Picker.None)
                    ClosePicker();
                else
                    StopQueue();
                return;
            }

            if (picker != Picker.None)
            {
                if (keyboard.f2Key.wasPressedThisFrame)
                {
                    ToggleLibrary();
                    return;
                }
                if (keyboard.f3Key.wasPressedThisFrame)
                {
                    CheckAll(keyboard.shiftKey.isPressed);
                    return;
                }
                if (keyboard.f4Key.wasPressedThisFrame)
                {
                    ClearChecked(keyboard.shiftKey.isPressed);
                    return;
                }
                if (keyboard.tabKey.wasPressedThisFrame)
                {
                    ChangePage(keyboard.shiftKey.isPressed ? -1 : 1);
                    return;
                }
                if (keyboard.pageUpKey.wasPressedThisFrame || keyboard.pageDownKey.wasPressedThisFrame)
                {
                    ChangePage(keyboard.pageUpKey.wasPressedThisFrame ? -1 : 1);
                    return;
                }
            }

            Vector2 direction = Vector2.zero;
            if (keyboard.leftArrowKey.wasPressedThisFrame)
                direction = Vector2.left;
            else if (keyboard.rightArrowKey.wasPressedThisFrame)
                direction = Vector2.right;
            else if (keyboard.upArrowKey.wasPressedThisFrame)
                direction = Vector2.down;
            else if (keyboard.downArrowKey.wasPressedThisFrame)
                direction = Vector2.up;

            bool enter = keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;
            if (!enter && direction == Vector2.zero)
                return;

            int focusedIndex = BuildMenu();
            if (focusedIndex < 0)
                return;
            if (direction != Vector2.zero)
                MoveDirectional(focusedIndex, direction);
            else
                InvokeMenuCommand(focus);
        }

        private int BuildMenu()
        {
            menuButtons.Clear();
            if (picker == Picker.None)
                AddPanelButtons();
            else
                AddPickerButtons();

            int firstEnabled = -1;
            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (!menuButtons[i].Enabled)
                    continue;
                if (menuButtons[i].Command == focus)
                    return i;
                if (firstEnabled < 0)
                    firstEnabled = i;
            }

            if (firstEnabled >= 0)
                focus = menuButtons[firstEnabled].Command;
            return firstEnabled;
        }

        private void AddPanelButtons()
        {
            string miniLabel = MiniGameName(miniGameChoice);
            if (miniGameChoice > 0)
                miniLabel += $"  |  {ModeLabel(GetStageMode(miniGameChoice))}";
            AddButton(MenuCommand.Mission, new Rect(90, 67, 468, 34),
                $"Mission ({CheckedCount(Picker.Mission)} checked)   {MissionName(missionChoice)}");
            AddButton(MenuCommand.MiniGame, new Rect(572, 67, 618, 34),
                $"Mini-game ({CheckedCount(Picker.MiniGame)} checked)   {miniLabel}");

            if (missionChoice > 0 && missionChoice <= missionProfiles.Length)
            {
                var profiles = missionProfiles[missionChoice - 1];
                if (profiles.Length > 1)
                    AddButton(MenuCommand.MissionProfile, new Rect(433, 26, 126, 29), profiles[missionProfile].AimType.ToString());
            }
            if (miniGameChoice > 0 && miniGameChoice <= gameProfiles.Length)
            {
                var profiles = gameProfiles[miniGameChoice - 1];
                if (profiles.Length > 1)
                    AddButton(MenuCommand.GamePattern, new Rect(566, 26, 126, 29), $"Pattern {gameProfile + 1}/{profiles.Length}");
            }

            bool queueActive = trialQueue != null;
            AddButton(MenuCommand.RunQueue, new Rect(90, 112, 160, 35), "Run checked", checkedItems.Count > 0 && !queueActive);
            AddButton(MenuCommand.StopQueue, new Rect(256, 112, 118, 35), "Stop queue", queueActive);
            AddButton(MenuCommand.Start, new Rect(380, 112, 117, 35), "Run pair", CanStart && !queueActive);
            AddButton(MenuCommand.Practice, new Rect(503, 112, 159, 35), "Practice mini-game", miniGameChoice > 0 && !queueActive);
            AddButton(MenuCommand.PassMission, new Rect(668, 112, 125, 35), "Pass mission", IsMissionRunning);
            AddButton(MenuCommand.FailMission, new Rect(799, 112, 125, 35), "Fail mission", IsMissionRunning);
            AddButton(MenuCommand.PassGame, new Rect(930, 112, 124, 35), "Pass game", IsMiniGameRunning);
            AddButton(MenuCommand.FailGame, new Rect(1060, 112, 130, 35), "Fail game", IsMiniGameRunning);
        }

        private void AddPickerButtons()
        {
            AddButton(MenuCommand.Library, new Rect(321, 27, 173, 29),
                pickerReferences ? "Library: References" : "Library: Core");
            AddButton(MenuCommand.PreviousPage, new Rect(505, 27, 95, 29), "Previous", pickerPage > 0);
            AddButton(MenuCommand.NextPage, new Rect(672, 27, 95, 29), "Next", pickerPage + 1 < PickerPages);
            AddButton(MenuCommand.CheckAll, new Rect(835, 26, 111, 29), "Check all");
            AddButton(MenuCommand.Clear, new Rect(952, 26, 111, 29), "Uncheck all");
            AddButton(MenuCommand.Back, new Rect(1069, 26, 121, 29), "Back");

            const int columns = 4;
            const float width = 1100f / columns;
            int start = pickerPage * PickerPageSize + 1;
            int end = Mathf.Min(start + PickerPageSize - 1, PickerCount);
            for (int i = start; i <= end; i++)
            {
                int offset = i - start;
                var bounds = new Rect(90 + offset % columns * width, 65 + offset / columns * 35, width - 8, 31);
                int index = PickerOffset + i;
                string label = picker == Picker.Mission ? MissionName(index) : MiniGameName(index);
                string number = pickerReferences ? $"R{i:00}" : $"{i:000}";
                int order = checkedItems.IndexOf((picker, index)) + 1;
                string check = order > 0 ? $"[x{order}]" : "[ ]";
                AddButton((MenuCommand)((int)MenuCommand.Entry + index), bounds, $"{check} {number}  {label}");
            }
        }

        private void AddButton(MenuCommand command, Rect bounds, string label, bool enabled = true) =>
            menuButtons.Add(new MenuButton(command, bounds, label, enabled));

        private void DrawMenuButtons()
        {
            BuildMenu();
            bool guiEnabled = GUI.enabled;
            foreach (MenuButton button in menuButtons)
            {
                GUI.enabled = guiEnabled && button.Enabled;
                if (GUI.Button(button.Bounds, button.Label))
                {
                    focus = button.Command;
                    InvokeMenuCommand(button.Command);
                }

                if (Event.current.type == EventType.Repaint && button.Enabled && focus == button.Command)
                    DrawFocus(button.Bounds);
            }
            GUI.enabled = guiEnabled;
        }

        private static void DrawFocus(Rect bounds)
        {
            const float thickness = 2f;
            Color previous = GUI.color;
            GUI.color = new Color(0.45f, 0.96f, 0.86f);
            Texture2D pixel = Texture2D.whiteTexture;
            GUI.DrawTexture(new Rect(bounds.xMin, bounds.yMin, bounds.width, thickness), pixel);
            GUI.DrawTexture(new Rect(bounds.xMin, bounds.yMax - thickness, bounds.width, thickness), pixel);
            GUI.DrawTexture(new Rect(bounds.xMin, bounds.yMin, thickness, bounds.height), pixel);
            GUI.DrawTexture(new Rect(bounds.xMax - thickness, bounds.yMin, thickness, bounds.height), pixel);
            GUI.color = previous;
        }

        private void MoveDirectional(int root, Vector2 direction)
        {
            Vector2 origin = menuButtons[root].Bounds.center;
            bool vertical = direction.y != 0f;
            float best = float.PositiveInfinity;
            foreach (MenuButton button in menuButtons)
            {
                if (!button.Enabled)
                    continue;
                Vector2 delta = button.Bounds.center - origin;
                float distance = Vector2.Dot(delta, direction);
                if (distance <= 8f || !vertical && Mathf.Abs(delta.y) > 8f)
                    continue;

                // Prefer the next visual row, then the closest column in that row.
                float horizontal = picker == Picker.None
                    ? Mathf.Abs(button.Bounds.xMin - menuButtons[root].Bounds.xMin)
                    : Mathf.Abs(delta.x);
                float score = vertical
                    ? Mathf.Round(distance / 8f) * 1280f + horizontal
                    : distance;
                if (score < best)
                {
                    focus = button.Command;
                    best = score;
                }
            }
        }

        private void ChangePage(int change)
        {
            int next = Mathf.Clamp(pickerPage + change, 0, PickerPages - 1);
            if (next == pickerPage)
                return;
            int entry = (int)focus - (int)MenuCommand.Entry;
            if (entry > 0)
            {
                int newIndex = Mathf.Min(entry + (next - pickerPage) * PickerPageSize, PickerOffset + PickerCount);
                focus = (MenuCommand)((int)MenuCommand.Entry + newIndex);
            }
            pickerPage = next;
        }

        private void ClosePicker()
        {
            focus = picker == Picker.Mission ? MenuCommand.Mission : MenuCommand.MiniGame;
            picker = Picker.None;
        }

        private void ToggleLibrary()
        {
            pickerReferences = !pickerReferences;
            pickerPage = 0;
            // Put focus on the first item so Enter can pick it immediately.
            focus = (MenuCommand)((int)MenuCommand.Entry + PickerOffset + 1);
        }

        private void InvokeMenuCommand(MenuCommand command)
        {
            if (command > MenuCommand.Entry)
            {
                ToggleChecked((int)command - (int)MenuCommand.Entry);
                return;
            }

            switch (command)
            {
                case MenuCommand.Mission: OpenPicker(Picker.Mission, missionChoice); break;
                case MenuCommand.MiniGame: OpenPicker(Picker.MiniGame, miniGameChoice); break;
                case MenuCommand.MissionProfile: missionProfile = (missionProfile + 1) % missionProfiles[missionChoice - 1].Length; break;
                case MenuCommand.GamePattern: gameProfile = (gameProfile + 1) % gameProfiles[miniGameChoice - 1].Length; break;
                case MenuCommand.RunQueue: StartQueue(); break;
                case MenuCommand.StopQueue: StopQueue(); break;
                case MenuCommand.Start: StartTrial(missionChoice, miniGameChoice, false, missionProfile, gameProfile); break;
                case MenuCommand.Practice: StartTrial(missionChoice, miniGameChoice, true, missionProfile, gameProfile); break;
                case MenuCommand.PassMission: EndMission(true); break;
                case MenuCommand.FailMission: EndMission(false); break;
                case MenuCommand.PassGame: miniGames.CompleteMiniGame(); break;
                case MenuCommand.FailGame: miniGames.FailMiniGame(); break;
                case MenuCommand.Library: ToggleLibrary(); break;
                case MenuCommand.PreviousPage: ChangePage(-1); break;
                case MenuCommand.NextPage: ChangePage(1); break;
                case MenuCommand.CheckAll: CheckAll(false); break;
                case MenuCommand.Clear: ClearChecked(false); break;
                case MenuCommand.Back: ClosePicker(); break;
            }
        }
    }
}
