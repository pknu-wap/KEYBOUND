using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    public enum UIViewKind { StartMenu, PauseMenu, Settings, GameOver, Gameplay }
    public sealed class UIView : MonoBehaviour
    {
        public UIViewKind kind;
        public Button primaryButton;
        public Button settingsButton;
        public Button menuButton;
        public RectTransform escapeHoldGauge;
        public Button quitButton;
        public Slider bgmSlider;
        public Slider sfxSlider;
        public Toggle fullscreenToggle;
        public Text resultText;
        public Text resultStageText;
    }
}
