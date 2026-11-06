
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;

    // ADDED: Dedicated source for movement sounds.
    [SerializeField] private AudioSource moveSource;

    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private AudioClip clearClip;

    [Header("Volume")]
    [Range(0f, 1f)]
    [SerializeField] private float bgmVolume = 0.5f;

    [Range(0f, 1f)]
    [SerializeField] private float sfxVolume = 1f;

    private void Awake()
    {
        // Prevent duplicate AudioManager instances.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Configure BGM source.
        if (bgmSource != null)
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.volume = bgmVolume;
        }

        // ADDED: Configure dedicated movement source.
        if (moveSource != null)
        {
            moveSource.loop = false;
            moveSource.playOnAwake = false;
            moveSource.volume = sfxVolume;
            moveSource.clip = moveClip;
        }

        // Configure general SFX source.
        if (sfxSource != null)
        {
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
            sfxSource.volume = sfxVolume;
        }
    }

    // Start background music.
    public void PlayBGM()
    {
        if (bgmSource == null || bgmClip == null)
            return;

        if (bgmSource.isPlaying)
            return;

        bgmSource.clip = bgmClip;
        bgmSource.Play();
    }

    // Stop background music.
    public void StopBGM()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }

    // Play movement sound.
    public void PlayMoveSFX()
    {
        // ADDED: Use dedicated movement AudioSource.
        if (moveSource == null || moveClip == null)
            return;

        moveSource.PlayOneShot(moveClip);

    }

    // Play stage clear sound.
    public void PlayClearSFX()
    {
        PlaySFX(clearClip);
    }

    // Play a sound effect without stopping BGM.
    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.PlayOneShot(clip);
    }
}
