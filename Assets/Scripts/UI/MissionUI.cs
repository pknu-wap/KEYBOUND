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
                description.text = "Waiting for mission";
                return;
            }

            string instruction = aim.Description;
            if (aim.TargetKeys.Count == 1)
            {
                string key = AimConditionGenerator.GetKeyName(aim.TargetKeys[0]);
                instruction = instruction.Replace(
                    $" {key} ",
                    $" <mark=#F4F4F4><color=#141414> {key} </color></mark> ");
            }

            description.text = aim.Type != AimType.Hold && required > 0
                ? $"{instruction}  <color=#B3B3B3>{current}/{required}</color>"
                : instruction;
        }
    }
}
