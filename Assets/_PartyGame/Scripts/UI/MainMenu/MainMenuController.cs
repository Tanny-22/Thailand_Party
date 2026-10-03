using System.Threading.Tasks;
using PartyGame.Core;
using PartyGame.Networking;
using PartyGame.UI.Shared;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.MainMenu
{
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] GameObject mainPanel, createRoomPanel, joinRoomPanel, settingsPanel, customizePanel;
        [SerializeField] Button startButton, joinButton, settingsButton, customizeButton, quitButton;
        [SerializeField] ModalDialogController modal;
        void Awake()
        {
            startButton?.onClick.AddListener(() => Show(createRoomPanel)); joinButton?.onClick.AddListener(() => Show(joinRoomPanel));
            settingsButton?.onClick.AddListener(() => Show(settingsPanel)); customizeButton?.onClick.AddListener(() => Show(customizePanel)); quitButton?.onClick.AddListener(Quit);
            Show(mainPanel);
        }
        void OnEnable() { if (SessionManager.Instance) SessionManager.Instance.OperationFailed += ShowError; }
        void Start()
        {
            var pending = SessionManager.Instance?.ConsumePendingMessage();
            if (!string.IsNullOrWhiteSpace(pending)) ShowError(pending);
        }
        void OnDisable() { if (SessionManager.Instance) SessionManager.Instance.OperationFailed -= ShowError; }
        public void ShowMain() => Show(mainPanel);
        void Show(GameObject panel) { foreach (var p in new[]{mainPanel, createRoomPanel, joinRoomPanel, settingsPanel, customizePanel}) if (p) p.SetActive(p == panel); }
        void ShowError(string value) => modal?.Show(value);
        void Quit()
        {
#if UNITY_EDITOR
            Debug.Log("Quit requested (ignored safely in Editor).");
#else
            Application.Quit();
#endif
        }
    }
}
