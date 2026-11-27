using System;
using UnityEngine;

namespace Keybound.UI
{
    internal static class DeveloperMode
    {
        internal static bool IsEnabled { get; private set; }
        internal static event Action Changed;

        internal static void Toggle()
        {
            IsEnabled = !IsEnabled;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSession()
        {
            IsEnabled = false;
            Changed = null;
        }
    }
}
