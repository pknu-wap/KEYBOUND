
using UnityEngine;
using UnityEngine.InputSystem; // ADDED: New Input System

public class AudioTest : MonoBehaviour
{
    private void Update()
    {
        // Check if keyboard is available.
        if (Keyboard.current == null)
            return;

        // Press 1 to play BGM.
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayBGM();
        }

        // Press 2 to play movement SFX.
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayMoveSFX();
        }

        // Press 3 to play stage clear SFX.
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            AudioManager.Instance?.PlayClearSFX();
        }

        // Press 4 to stop BGM.
        if (Keyboard.current.digit4Key.wasPressedThisFrame)
        {
            AudioManager.Instance?.StopBGM();
        }
    }
}
