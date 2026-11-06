using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Keybound.UI
{
    public enum SoundChannel { BGM, SFX }
    public static class GameSettings
    {
        private const string Prefix = "KEYBOUND.";
        public static float Bgm => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "BGM", 0.5f));
        public static float Sfx => Mathf.Clamp01(PlayerPrefs.GetFloat(Prefix + "SFX", 0.5f));
        public static bool Fullscreen => PlayerPrefs.GetInt(Prefix + "Fullscreen", Screen.fullScreen ? 1 : 0) != 0;
        public static event Action VolumeChanged;
        public static void SetVolume(SoundChannel channel, float value)
        {
            PlayerPrefs.SetFloat(Prefix + channel, Mathf.Clamp01(value));
            PlayerPrefs.Save(); VolumeChanged?.Invoke();
        }
        public static void SetFullscreen(bool value)
        {
            PlayerPrefs.SetInt(Prefix + "Fullscreen", value ? 1 : 0); PlayerPrefs.Save();
            Screen.fullScreenMode = value ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
        public static void ApplyDisplay()
        {
            if (PlayerPrefs.HasKey(Prefix + "Fullscreen"))
                Screen.fullScreenMode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        }
    }
    // Add to a future AudioSource, or connect exposed mixer parameters.
    // Choose one routing method per source to avoid applying volume twice.
    public sealed class AudioVolumeBinding : MonoBehaviour
    {
        public SoundChannel channel;
        public AudioSource source;
        public AudioMixer mixer;
        public string exposedParameter;
        private float baseVolume;
        private void Awake() { if (source != null) baseVolume = source.volume; }
        private void OnEnable() { GameSettings.VolumeChanged += Apply; Apply(); }
        private void OnDisable() { GameSettings.VolumeChanged -= Apply; }
        private void Apply()
        {
            float value = channel == SoundChannel.BGM ? GameSettings.Bgm : GameSettings.Sfx;
            if (mixer != null && !string.IsNullOrEmpty(exposedParameter))
                mixer.SetFloat(exposedParameter, value <= .0001f ? -80f : 20f * Mathf.Log10(value));
            else if (source != null) source.volume = baseVolume * value;
        }
    }
}
