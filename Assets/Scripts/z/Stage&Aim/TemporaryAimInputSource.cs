
using System;
using Keybound.Keyboard;
using UnityEngine;
using UnityEngine.InputSystem;

public class TemporaryAimInputSource : MonoBehaviour, IAimInputSource
{
    [SerializeField] private bool logInput = true;

    public event Action<Key> OnKeyPressed;

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        foreach (KeyboardKeyData keyData in KeyboardMapData.Keys)
        {
            Key key = keyData.InputKey;

            if (!keyboard[key].wasPressedThisFrame)
                continue;

            if (logInput)
            {
                Debug.Log(
                    $"[Input] Key pressed: {keyData.Name}",
                    this
                );
            }

            OnKeyPressed?.Invoke(key);
        }
    }
}
