using UnityEngine;
using UnityEngine.UI;

namespace Keybound.UI
{
    [RequireComponent(typeof(UIView))]
    public sealed class SettingsPopupDisplay : MonoBehaviour
    {
        [SerializeField] private Text bgmPercentage;
        [SerializeField] private Text sfxPercentage;
        [SerializeField] private Text screenMode;
        [SerializeField] private Text skipTutorialMode;
        [SerializeField] private Text remainingTimeMode;

        private UIView view;

        private void Awake()
        {
            view = GetComponent<UIView>();
        }

        private void OnEnable()
        {
            view.bgmSlider.onValueChanged.AddListener(DisplayBgm);
            view.sfxSlider.onValueChanged.AddListener(DisplaySfx);
            view.fullscreenToggle.onValueChanged.AddListener(DisplayScreenMode);
            view.skipTutorialToggle.onValueChanged.AddListener(DisplaySkipTutorialMode);
            view.showRemainingTimeToggle.onValueChanged.AddListener(DisplayRemainingTimeMode);

            DisplayBgm(view.bgmSlider.value);
            DisplaySfx(view.sfxSlider.value);
            DisplayScreenMode(view.fullscreenToggle.isOn);
            DisplaySkipTutorialMode(view.skipTutorialToggle.isOn);
            DisplayRemainingTimeMode(view.showRemainingTimeToggle.isOn);
        }

        private void OnDisable()
        {
            view.bgmSlider.onValueChanged.RemoveListener(DisplayBgm);
            view.sfxSlider.onValueChanged.RemoveListener(DisplaySfx);
            view.fullscreenToggle.onValueChanged.RemoveListener(DisplayScreenMode);
            view.skipTutorialToggle.onValueChanged.RemoveListener(DisplaySkipTutorialMode);
            view.showRemainingTimeToggle.onValueChanged.RemoveListener(DisplayRemainingTimeMode);
        }

        private void DisplayBgm(float value)
        {
            bgmPercentage.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void DisplaySfx(float value)
        {
            sfxPercentage.text = $"{Mathf.RoundToInt(value * 100f)}%";
        }

        private void DisplayScreenMode(bool fullscreen)
        {
            screenMode.text = fullscreen ? "‹   Fullscreen   ›" : "‹   Windowed   ›";
        }

        private void DisplaySkipTutorialMode(bool skip)
        {
            skipTutorialMode.text = FormatToggleMode(skip);
        }

        private void DisplayRemainingTimeMode(bool show)
        {
            remainingTimeMode.text = FormatToggleMode(show);
        }

        private static string FormatToggleMode(bool enabled)
        {
            return enabled
                ? "[  <color=#A0A0A0>OFF</color>  |  ON  ]"
                : "[  OFF  |  <color=#A0A0A0>ON</color>  ]";
        }
    }
}
