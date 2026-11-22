using System.Collections.Generic;
using UnityEngine;

namespace Keybound.ContentLab
{
    public sealed partial class MissionLab
    {
        private readonly struct QueuedTrial
        {
            internal readonly Picker Kind;
            internal readonly int Index;
            internal readonly int Variant;

            internal QueuedTrial(Picker kind, int index, int variant)
            {
                Kind = kind;
                Index = index;
                Variant = variant;
            }
        }

        private const float ResultDisplaySeconds = 1.5f;
        private readonly List<(Picker Kind, int Index)> checkedItems = new();
        private QueuedTrial[] trialQueue;
        private int queuePosition;
        private int queuePassed;
        private float nextTrialAt;
        private string queueSummary;

        private int CheckedCount(Picker kind)
        {
            int count = 0;
            foreach (var item in checkedItems)
            {
                if (item.Kind == kind)
                    count++;
            }
            return count;
        }

        private void ToggleChecked(int index)
        {
            var item = (picker, index);
            if (checkedItems.Remove(item))
            {
                if (PairChoice(picker) == index)
                    SetPairChoice(picker, LastChecked(picker));
            }
            else
            {
                checkedItems.Add(item);
                SetPairChoice(picker, index);
            }
        }

        private int PairChoice(Picker kind) => kind == Picker.Mission ? missionChoice : miniGameChoice;

        private int LastChecked(Picker kind) => checkedItems.FindLast(item => item.Kind == kind).Index;

        private void SetPairChoice(Picker kind, int index)
        {
            if (kind == Picker.Mission)
            {
                missionChoice = index;
                missionProfile = 0;
            }
            else
            {
                miniGameChoice = index;
                gameProfile = 0;
            }
        }

        private void CheckAll(bool allLists)
        {
            if (allLists)
            {
                CheckRange(Picker.Mission, 0, MissionCount - 1);
                CheckRange(Picker.MiniGame, 0, MiniGameCount - 1);
            }
            else
                CheckRange(picker, PickerOffset, PickerCount);
        }

        private void CheckRange(Picker kind, int offset, int count)
        {
            for (int index = offset + 1; index <= offset + count; index++)
            {
                var item = (kind, index);
                if (!checkedItems.Contains(item))
                    checkedItems.Add(item);
            }

            if (PairChoice(kind) == 0 && count > 0)
                SetPairChoice(kind, offset + 1);
        }

        private void ClearChecked(bool allLists)
        {
            bool IsInScope((Picker Kind, int Index) item) =>
                allLists || item.Kind == picker && item.Index > PickerOffset && item.Index <= PickerOffset + PickerCount;

            bool clearMissionChoice = checkedItems.Contains((Picker.Mission, missionChoice))
                                      && IsInScope((Picker.Mission, missionChoice));
            bool clearGameChoice = checkedItems.Contains((Picker.MiniGame, miniGameChoice))
                                   && IsInScope((Picker.MiniGame, miniGameChoice));

            checkedItems.RemoveAll(IsInScope);
            if (clearMissionChoice)
                SetPairChoice(Picker.Mission, LastChecked(Picker.Mission));
            if (clearGameChoice)
                SetPairChoice(Picker.MiniGame, LastChecked(Picker.MiniGame));
        }

        private void StartQueue()
        {
            if (checkedItems.Count == 0 || trialQueue != null || player.CurrentKey == null)
                return;

            trialQueue = new QueuedTrial[checkedItems.Count];
            for (int i = 0; i < trialQueue.Length; i++)
            {
                var item = checkedItems[i];
                int variant = item.Index != PairChoice(item.Kind) ? 0
                    : item.Kind == Picker.Mission ? missionProfile : gameProfile;
                trialQueue[i] = new QueuedTrial(item.Kind, item.Index, variant);
            }

            queuePosition = 0;
            queuePassed = 0;
            queueSummary = null;
            BeginQueuedTrial();
        }

        private void BeginQueuedTrial()
        {
            QueuedTrial item = trialQueue[queuePosition];
            bool started = item.Kind == Picker.Mission
                ? BeginTrial(item.Index, 0, false, item.Variant, 0)
                : BeginTrial(0, item.Index, true, 0, item.Variant);
            if (!started)
                StopQueue();
        }

        private void FinishQueuedTrial()
        {
            if (trialQueue == null)
                return;
            Outcome result = trialQueue[queuePosition].Kind == Picker.Mission ? missionResult : miniResult;
            if (result == Outcome.Success)
                queuePassed++;

            int total = trialQueue.Length;
            if (queuePosition + 1 == total)
            {
                queueSummary = $"Queue complete   {queuePassed}/{total} passed";
                trialQueue = null;
            }
            else
                nextTrialAt = Time.unscaledTime + ResultDisplaySeconds;
        }

        private void AdvanceQueue()
        {
            if (trialQueue == null || picker != Picker.None || Time.unscaledTime < nextTrialAt)
                return;
            queuePosition++;
            BeginQueuedTrial();
        }

        private void StopQueue()
        {
            if (trialQueue == null)
                return;
            int total = trialQueue.Length;
            trialQueue = null;
            queueSummary = $"Queue stopped   {queuePassed}/{total} passed";
            StopTrial();
            missionResult = Outcome.Inactive;
            miniResult = Outcome.Inactive;
            stage = 0;
            focus = MenuCommand.RunQueue;
        }

        private string QueueStatus()
        {
            if (trialQueue == null)
                return queueSummary;
            QueuedTrial item = trialQueue[queuePosition];
            string label = item.Kind == Picker.Mission ? MissionName(item.Index) : MiniGameName(item.Index);
            return $"Queue {queuePosition + 1}/{trialQueue.Length}   {label}";
        }
    }
}
