using Keybound.Keyboard;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    public sealed class TutorialUI : MonoBehaviour
    {
        private const float TargetBlinkPeriod = 0.9f;
        private const float TargetBlinkOnTime = 0.55f;

        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private TMP_Text missionHeading;
        [SerializeField] private TMP_Text missionDescription;
        [SerializeField] private Font tutorialTypeface;
        [SerializeField] private Shader targetOutlineShader;

        private TMP_FontAsset tutorialFont;
        private TMP_Text terminalText;
        private TMP_Text instructionText;
        private GameObject terminal;
        private Image targetOutline;
        private MirrorKeyFace targetOutlineMirror;
        private Material targetOutlineMaterial;
        private float targetShownAt;
        private string previousMissionHeading;
        private bool missionDescriptionWasEnabled;

        private void Update()
        {
            if (targetOutline != null && targetOutline.gameObject.activeInHierarchy)
                targetOutline.enabled = Mathf.Repeat(Time.time - targetShownAt, TargetBlinkPeriod) < TargetBlinkOnTime;
        }

        internal void ShowTerminal(string message)
        {
            if (terminal == null)
                CreateTerminal();

            terminalText.text = message;
            terminalText.maxVisibleCharacters = 0;
            terminal.SetActive(true);
        }

        internal void RevealTerminalCharacters(int count) => terminalText.maxVisibleCharacters = count;

        internal void HideTerminal()
        {
            if (terminal != null)
                terminal.SetActive(false);
        }

        internal void ShowLesson(int step, string instruction)
        {
            if (instructionText == null)
            {
                instructionText = Instantiate(missionDescription, missionDescription.transform.parent);
                instructionText.name = "Tutorial Instruction";
                instructionText.font = GetTutorialFont();
                instructionText.fontStyle = FontStyles.Normal;
                instructionText.gameObject.SetActive(false);
            }

            if (!instructionText.gameObject.activeSelf)
            {
                previousMissionHeading = missionHeading.text;
                missionDescriptionWasEnabled = missionDescription.enabled;
            }

            missionHeading.text = "TUTORIAL";
            missionDescription.enabled = false;
            instructionText.text = $"<size=60%><color=#B3B3B3>{step:00} / 04</color></size>\n{instruction}";
            instructionText.enabled = true;
            instructionText.gameObject.SetActive(true);
        }

        internal void HideLesson()
        {
            if (instructionText != null && instructionText.gameObject.activeSelf)
            {
                instructionText.gameObject.SetActive(false);
                if (missionHeading != null)
                    missionHeading.text = previousMissionHeading;
                if (missionDescription != null)
                    missionDescription.enabled = missionDescriptionWasEnabled;
            }

            ShowTarget(null);
        }

        internal void ShowTarget(KeyboardKey key)
        {
            if (key == null || !key.TryGetComponent(out Image keyFace) || keyFace.overrideSprite == null)
            {
                if (targetOutline != null)
                    targetOutline.gameObject.SetActive(false);
                return;
            }

            if (targetOutline == null)
                CreateTargetOutline();

            FitToParent(targetOutline.rectTransform, key.transform, Vector2.zero, Vector2.one);
            targetOutline.transform.SetAsFirstSibling();
            targetOutline.sprite = keyFace.overrideSprite;
            targetOutline.preserveAspect = keyFace.preserveAspect;
            targetOutline.useSpriteMesh = keyFace.useSpriteMesh;
            targetOutlineMirror.enabled = key.TryGetComponent<MirrorKeyFace>(out var mirror) && mirror.enabled;
            targetShownAt = Time.time;
            targetOutline.enabled = true;
            targetOutline.gameObject.SetActive(true);
        }

        internal void Clear()
        {
            HideTerminal();
            HideLesson();
        }

        private void CreateTerminal()
        {
            terminal = new GameObject("Tutorial Terminal", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)terminal.transform;
            FitToParent(panel, popupRoot, Vector2.zero, Vector2.one);
            terminal.GetComponent<Image>().color = Color.black;

            // Keep the pause and settings views above the terminal in the existing popup canvas.
            foreach (var view in popupRoot.GetComponentsInChildren<UIView>(true))
            {
                if (view.kind != UIViewKind.PauseMenu)
                    continue;

                panel.SetSiblingIndex(view.transform.GetSiblingIndex());
                break;
            }

            terminalText = new GameObject("Tutorial Terminal Text", typeof(RectTransform),
                typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
            FitToParent(terminalText.rectTransform, panel, new Vector2(0.18f, 0.30f), new Vector2(0.82f, 0.70f));
            terminalText.font = GetTutorialFont();
            terminalText.fontSize = 28f;
            terminalText.enableAutoSizing = true;
            terminalText.fontSizeMin = 16f;
            terminalText.fontSizeMax = 28f;
            terminalText.alignment = TextAlignmentOptions.Left;
            terminalText.lineSpacing = 12f;
            terminalText.color = Color.white;
            terminalText.raycastTarget = false;
        }

        private TMP_FontAsset GetTutorialFont()
        {
            if (tutorialFont == null)
                tutorialFont = TMP_FontAsset.CreateFontAsset(tutorialTypeface);
            return tutorialFont;
        }

        private void CreateTargetOutline()
        {
            targetOutline = new GameObject("Tutorial Target", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            targetOutlineMirror = targetOutline.gameObject.AddComponent<MirrorKeyFace>();
            targetOutlineMaterial = new Material(targetOutlineShader);
            targetOutline.material = targetOutlineMaterial;
            targetOutline.color = Color.white;
            targetOutline.raycastTarget = false;
        }

        private static void FitToParent(RectTransform rect, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private void OnDestroy()
        {
            Clear();
            if (terminal != null)
                Destroy(terminal);
            if (instructionText != null)
                Destroy(instructionText.gameObject);
            if (targetOutline != null)
                Destroy(targetOutline.gameObject);
            if (targetOutlineMaterial != null)
                Destroy(targetOutlineMaterial);
            if (tutorialFont != null)
                Destroy(tutorialFont);
        }
    }
}
