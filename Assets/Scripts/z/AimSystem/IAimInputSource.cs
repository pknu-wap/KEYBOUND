
using System;
using UnityEngine.InputSystem;

public interface IAimInputSource
{
    event Action<Key> OnKeyPressed;
}
