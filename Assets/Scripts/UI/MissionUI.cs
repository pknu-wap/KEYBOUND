using TMPro;
using UnityEngine;

namespace Keybound.UI
{
    public sealed class MissionUI : MonoBehaviour
    {
        [SerializeField] private AimConditionProcessor processor;
        [SerializeField] private TMP_Text description;

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

        private void Display(int current, int required)
        {
            AimData aim = processor.CurrentAim;
            if (aim == null)
            {
                description.text = "";
                return;
            }

            if (aim.Type == AimType.Typing || aim.Type == AimType.Calculation)
            {
                string prompt = string.IsNullOrEmpty(aim.QuestionText) ? aim.Description : aim.QuestionText;
                string submitted = processor.SubmittedText;
                int remaining = Mathf.Max(0, required - submitted.Length);
                description.text = $"{prompt}\n<color=#B3B3B3>{submitted}{new string('_', remaining)}</color>";
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

            description.text = aim.Type != AimType.Hold && required > 0
                ? $"{instruction}  <color=#B3B3B3>{current}/{required}</color>"
                : instruction;
        }
    }
}
