using TMPro;
using UnityEngine;

namespace Keybound.UI
{
    public sealed class MissionUI : MonoBehaviour
    {
        [SerializeField] private AimConditionProcessor processor;
        [SerializeField] private TMP_Text description;

        private string tutorialGuide;

        private void OnEnable()
        {
            if (processor == null)
                return;

            processor.OnAimProgressChanged += Display;
            Display(processor.CurrentProgress, processor.RequiredProgress);
        }

        private void OnDisable()
        {
            if (processor != null)
                processor.OnAimProgressChanged -= Display;
        }

        internal void SetTutorialGuide(string guide)
        {
            tutorialGuide = guide;
            if (processor != null)
                Display(processor.CurrentProgress, processor.RequiredProgress);
        }

        private void Display(int current, int required)
        {
            AimData aim = processor.CurrentAim;
            if (aim == null)
            {
                DisplayText("");
                return;
            }

            if (aim.Type == AimType.Typing || aim.Type == AimType.Calculation)
            {
                string prompt = string.IsNullOrEmpty(aim.QuestionText) ? aim.Description : aim.QuestionText;
                string submitted = processor.SubmittedText;
                int remaining = Mathf.Max(0, required - submitted.Length);
                DisplayText($"{prompt}\n<color=#B9C7A9>{submitted}{new string('_', remaining)}</color>");
                return;
            }

            string instruction = aim.Description;
            if (aim.TargetKeys.Count == 1)
            {
                string key = AimConditionGenerator.GetKeyName(aim.TargetKeys[0]);
                instruction = instruction.Replace(
                    $" {key} ",
                    $" <mark=#FFFFFF42><color=#FFFFFF> {key} </color></mark> ");
            }

            DisplayText(aim.Type != AimType.Hold && required > 0
                ? $"{instruction}  <color=#B9C7A9>{current}/{required}</color>"
                : instruction);
        }

        private void DisplayText(string mission)
        {
            if (string.IsNullOrEmpty(tutorialGuide))
                description.text = mission;
            else if (string.IsNullOrEmpty(mission))
                description.text = tutorialGuide;
            else
                description.text = $"{tutorialGuide}\n{mission}";
        }
    }
}
