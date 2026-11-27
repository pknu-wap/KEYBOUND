using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Keybound.UI
{
    public sealed class GameOverLeaderboard : MonoBehaviour
    {
        [SerializeField] private Text finalScoreText;
        [SerializeField] private GameObject registrationPanel;
        [SerializeField] private InputField playerNameInput;
        [SerializeField] private Button registerButton;
        [SerializeField] private Text statusText;

        private LeaderboardRecorder recorder;

        internal void Bind(LeaderboardRecorder source) => recorder = source;

        private void OnEnable()
        {
            playerNameInput.SetTextWithoutNotify("");
            registerButton.onClick.AddListener(RegisterScore);
            if (recorder != null)
                recorder.OnResultChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            registerButton.onClick.RemoveListener(RegisterScore);
            if (recorder != null)
                recorder.OnResultChanged -= Refresh;
        }

        private void Refresh()
        {
            var result = recorder?.CurrentResult;
            finalScoreText.text = result == null ? "SCORE --" : $"SCORE {result.Score:N0}";
            bool canRegister = recorder != null && recorder.CanRegister;
            registrationPanel.SetActive(canRegister);

            if (recorder == null || recorder.IsAborted)
                statusText.text = "RECORD UNAVAILABLE";
            else if (recorder.HasSubmitted)
                statusText.text = "RECORD SAVED";
            else if (!recorder.IsStorageReady)
                statusText.text = "STORAGE UNAVAILABLE";
            else if (result == null)
                statusText.text = "";
            else
                statusText.text = canRegister ? "A-Z, 0-9  /  1-8 CHARACTERS" : "RECORD UNAVAILABLE";
        }

        private void RegisterScore()
        {
            if (recorder == null || recorder.HasSubmitted)
                return;

            var result = recorder.SubmitRecord(playerNameInput.text);
            statusText.text = result switch
            {
                LeaderboardSubmitResult.Saved => "RECORD SAVED",
                LeaderboardSubmitResult.InvalidName => "USE 1-8 LETTERS OR NUMBERS",
                LeaderboardSubmitResult.DuplicateName => "NAME ALREADY TAKEN  /  TRY ANOTHER",
                LeaderboardSubmitResult.AlreadySubmitted => "RECORD SAVED",
                LeaderboardSubmitResult.StorageError => "SAVE FAILED  /  TRY AGAIN",
                _ => "RECORD UNAVAILABLE"
            };

            if (result == LeaderboardSubmitResult.Saved)
            {
                EventSystem.current?.SetSelectedGameObject(GetComponent<UIView>().menuButton.gameObject);
            }
            else if (result == LeaderboardSubmitResult.InvalidName || result == LeaderboardSubmitResult.DuplicateName)
            {
                EventSystem.current?.SetSelectedGameObject(playerNameInput.gameObject);
                playerNameInput.ActivateInputField();
            }
        }
    }
}
