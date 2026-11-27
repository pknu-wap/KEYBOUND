using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Keybound.UI
{
    public sealed class LeaderboardEditorPopup : MonoBehaviour
    {
        private GameObject overlay;
        private LeaderboardDeveloperPanel panel;
        private EventSystem eventSystem;
        private GameObject previousSelection;
        private bool previousNavigation;

        internal bool IsOpen => overlay != null && overlay.activeSelf;

        internal void Open(LeaderboardManager leaderboard)
        {
            if (!DeveloperMode.IsEnabled || IsOpen || leaderboard == null) return;
            if (overlay == null) BuildOverlay(leaderboard);

            eventSystem = EventSystem.current;
            if (eventSystem != null)
            {
                previousSelection = eventSystem.currentSelectedGameObject;
                previousNavigation = eventSystem.sendNavigationEvents;
                eventSystem.SetSelectedGameObject(null);
                eventSystem.sendNavigationEvents = false;
            }

            overlay.SetActive(true);
            panel.Reload();
        }

        internal void Close()
        {
            if (!IsOpen) return;

            if (eventSystem != null)
            {
                eventSystem.SetSelectedGameObject(null);
                eventSystem.sendNavigationEvents = previousNavigation;
            }
            overlay.SetActive(false);
            if (eventSystem != null && previousSelection != null && previousSelection.activeInHierarchy)
                eventSystem.SetSelectedGameObject(previousSelection);

            previousSelection = null;
            eventSystem = null;
        }

        private void OnDisable() => Close();

        private void OnDestroy()
        {
            if (overlay != null) Destroy(overlay);
        }

        private void BuildOverlay(LeaderboardManager leaderboard)
        {
            overlay = new GameObject("Leaderboard Edit Popup", typeof(RectTransform), typeof(Canvas),
                typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(Image));
            overlay.SetActive(false);
            var canvas = overlay.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            var scaler = overlay.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            overlay.GetComponent<Image>().color = new Color32(0, 0, 0, 218);

            var frame = DeveloperControls.Rect(overlay.transform, "Developer Panel", 0, 0, 984, 636);
            frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(.5f, .5f);
            frame.anchoredPosition = Vector2.zero;
            frame.gameObject.AddComponent<Image>().color = new Color32(26, 30, 32, 255);
            DeveloperControls.Label(frame, "DEVELOPER MODE", 26, 17, 480, 38, 26, FontStyle.Bold);
            DeveloperControls.Label(frame, "Ctrl + Shift + F10", 654, 20, 155, 35, 16);
            DeveloperControls.Button(frame, "CLOSE", 818, 20, 140, 35).onClick.AddListener(Close);

            var tool = DeveloperControls.Rect(frame, "Leaderboard Editor", 26, 74, 932, 540);
            panel = tool.gameObject.AddComponent<LeaderboardDeveloperPanel>();
            panel.Initialize(leaderboard);
        }
    }

    internal static class DeveloperControls
    {
        private static readonly Color Foreground = new Color32(237, 243, 238, 255);
        private static readonly Color ControlBackground = new Color32(44, 52, 53, 255);

        internal static RectTransform Rect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(width, height);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        internal static Text Label(Transform parent, string value, float x, float y, float width, float height,
            int size, FontStyle style = FontStyle.Normal)
        {
            var text = Rect(parent, "Text", x, y, width, height).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Foreground;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = TextAnchor.MiddleLeft;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        internal static Button Button(Transform parent, string caption, float x, float y, float width, float height)
        {
            var rect = Rect(parent, caption, x, y, width, height);
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = Background(rect);
            Label(rect, caption, 0, 0, width, height, 16, FontStyle.Bold).alignment = TextAnchor.MiddleCenter;
            return button;
        }

        internal static InputField Field(Transform parent, string name, string value, float x, float width)
        {
            const int height = 30;
            var rect = Rect(parent, name, x, 2, width, height);
            var input = rect.gameObject.AddComponent<InputField>();
            input.targetGraphic = Background(rect);
            input.textComponent = Label(rect, "", 8, 0, width - 16, height, 17);
            input.textComponent.supportRichText = false;
            input.text = value;
            return input;
        }

        private static Image Background(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = ControlBackground;
            return image;
        }
    }
}
