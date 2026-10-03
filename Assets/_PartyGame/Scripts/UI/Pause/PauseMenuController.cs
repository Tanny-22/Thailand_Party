using PartyGame.Core;
using PartyGame.Input;
using PartyGame.Networking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PartyGame.UI.Pause
{
    public sealed class PauseMenuController : MonoBehaviour
    {
        [SerializeField] GameObject root;
        [SerializeField] GameObject mainPanel, settingsPanel, customizePanel;
        InputAction pause;
        void OnEnable() { pause = InputManager.Instance?.Actions?.FindAction("UI/Pause"); if (pause != null) { pause.performed += Toggle; pause.Enable(); } }
        void OnDisable() { if (pause != null) pause.performed -= Toggle; }
        void Toggle(InputAction.CallbackContext _) => root.SetActive(!root.activeSelf);
        public void Continue() => root.SetActive(false);
        public void ShowMain() => Show(mainPanel);
        public void ShowSettings() => Show(settingsPanel);
        public void ShowCustomize() => Show(customizePanel);
        void Show(GameObject selected) { foreach (var panel in new[]{mainPanel, settingsPanel, customizePanel}) if (panel) panel.SetActive(panel == selected); }
        public async void LeaveToMenu() { await SessionManager.Instance.LeaveAsync(); await SceneFlowManager.Instance.LoadLocalAsync("01_MainMenu"); }
        public async void QuitToWindow()
        {
            await SessionManager.Instance.LeaveAsync();
#if UNITY_EDITOR
            Debug.Log("Quit requested (ignored in Editor).");
#else
            Application.Quit();
#endif
        }
    }
}
