using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using InputKeyboard = UnityEngine.InputSystem.Keyboard;

namespace Keybound.UI
{
    public sealed class StartMenuShortcuts : MonoBehaviour
    {
        [SerializeField] private SceneUIManager menu;
        [SerializeField] private InputSystemUIInputModule inputModule;

        private InputActionAsset navigation;
        private InputActionReference submit;
        private InputActionReference cancel;

        private void Awake()
        {
            // The menu handles keyboard shortcuts directly. Keep controller UI navigation.
            navigation = ScriptableObject.CreateInstance<InputActionAsset>();
            var actions = new InputActionMap("Start Menu Navigation");
            var submitAction = AddControllerAction(actions, "Submit");
            var cancelAction = AddControllerAction(actions, "Cancel");
            navigation.AddActionMap(actions);

            submit = InputActionReference.Create(submitAction);
            cancel = InputActionReference.Create(cancelAction);
            inputModule.submit = submit;
            inputModule.cancel = cancel;
        }

        private void Update()
        {
            var keyboard = InputKeyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                if (menu.SettingsOpen)
                    menu.CloseSettings();
                else
                    menu.OnClickQuit();
                return;
            }

            if (menu.SettingsOpen)
                return;

            if (keyboard.tabKey.wasPressedThisFrame)
                menu.OpenSettings();
            else if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
                menu.OnClickStart();
        }

        private void OnDestroy()
        {
            Destroy(submit);
            Destroy(cancel);
            Destroy(navigation);
        }

        private static InputAction AddControllerAction(InputActionMap map, string usage)
        {
            var action = map.AddAction(usage, InputActionType.Button);
            action.AddBinding($"<Gamepad>/{{{usage}}}");
            action.AddBinding($"<Joystick>/{{{usage}}}");
            action.AddBinding($"<XRController>/{{{usage}}}");
            return action;
        }
    }
}
