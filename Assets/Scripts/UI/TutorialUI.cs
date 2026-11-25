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
        [SerializeField] private MissionUI missionUI;
        [SerializeField] private Font tutorialTypeface;
        [SerializeField] private Shader targetOutlineShader;

        private TMP_FontAsset tutorialFont;
        private TMP_Text terminalText;
        private TMP_Text instructionText;
        private TMP_Text skipHint;
        private GameObject terminal;
        private Image targetOutline;
        private MirrorKeyFace targetOutlineMirror;
        private Material targetOutlineMaterial;
        private float targetShownAt;
        private string previousMissionHeading;
        private TMP_FontAsset previousMissionFont;
        private bool missionDescriptionWasEnabled;
        private bool lessonShown;
        private bool missionGuideShown;

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
            BeginLesson();
            ClearMissionGuide();

            if (instructionText == null)
            {
                instructionText = Instantiate(missionDescription, missionDescription.transform.parent);
                instructionText.name = "Tutorial Instruction";
                instructionText.font = GetTutorialFont();
                instructionText.fontStyle = FontStyles.Normal;
                instructionText.gameObject.SetActive(false);
            }

            missionDescription.enabled = false;
            instructionText.text = $"<size=60%><color=#B3B3B3>{step:00} / 04</color></size>\n{instruction}";
            instructionText.enabled = true;
            instructionText.gameObject.SetActive(true);
        }

        internal void ShowMissionLesson(string instruction)
        {
            BeginLesson();
            if (instructionText != null)
                instructionText.gameObject.SetActive(false);

            if (!missionGuideShown)
            {
                missionGuideShown = true;
                previousMissionFont = missionDescription.font;
                missionDescription.font = GetTutorialFont();
            }

            missionUI.SetTutorialGuide($"<size=60%><color=#B3B3B3>03 / 04   {instruction}</color></size>");
            missionDescription.enabled = true;
        }

        internal void HideLesson()
        {
            if (lessonShown)
            {
                lessonShown = false;
                ClearMissionGuide();
                if (instructionText != null)
                    instructionText.gameObject.SetActive(false);
                if (missionHeading != null)
                    missionHeading.text = previousMissionHeading;
                if (missionDescription != null)
                    missionDescription.enabled = missionDescriptionWasEnabled;
            }

            ShowTarget(null);
        }

        private void BeginLesson()
        {
            if (lessonShown)
                return;

            lessonShown = true;
            previousMissionHeading = missionHeading.text;
            missionDescriptionWasEnabled = missionDescription.enabled;
            missionHeading.text = "TUTORIAL";
        }

        private void ClearMissionGuide()
        {
            if (!missionGuideShown)
                return;

            missionGuideShown = false;
            if (missionDescription != null)
                missionDescription.font = previousMissionFont;
            if (missionUI != null)
                missionUI.SetTutorialGuide(null);
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
            ShowSkipHint(false);
        }

        internal void ShowSkipHint(bool visible)
        {
            if (skipHint == null && visible)
            {
                skipHint = new GameObject("Skip Tutorial Hint", typeof(RectTransform),
                    typeof(TextMeshProUGUI)).GetComponent<TMP_Text>();
                var rect = skipHint.rectTransform;
                rect.SetParent(popupRoot, false);
                rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.right;
                rect.anchoredPosition = new Vector2(-22f, 8f);
                rect.sizeDelta = new Vector2(300f, 26f);
                PlaceBelowPause(rect);

                skipHint.font = missionHeading.font;
                skipHint.fontSize = 20f;
                skipHint.alignment = TextAlignmentOptions.Right;
                skipHint.color = Color.white;
                skipHint.raycastTarget = false;
                skipHint.text = "SKIP TUTORIAL [ENTER]";
            }

            if (skipHint != null && skipHint.gameObject.activeSelf != visible)
                skipHint.gameObject.SetActive(visible);
        }

        private void CreateTerminal()
        {
            terminal = new GameObject("Tutorial Terminal", typeof(RectTransform), typeof(Image));
            var panel = (RectTransform)terminal.transform;
            FitToParent(panel, popupRoot, Vector2.zero, Vector2.one);
            terminal.GetComponent<Image>().color = Color.black;

            PlaceBelowPause(panel);

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

        private void PlaceBelowPause(RectTransform overlay)
        {
            foreach (var view in popupRoot.GetComponentsInChildren<UIView>(true))
            {
                if (view.kind != UIViewKind.PauseMenu)
                    continue;

                overlay.SetSiblingIndex(view.transform.GetSiblingIndex());
                return;
            }
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
            if (skipHint != null)
                Destroy(skipHint.gameObject);
            if (targetOutline != null)
                Destroy(targetOutline.gameObject);
            if (targetOutlineMaterial != null)
                Destroy(targetOutlineMaterial);
            if (tutorialFont != null)
                Destroy(tutorialFont);
        }
    }
}
