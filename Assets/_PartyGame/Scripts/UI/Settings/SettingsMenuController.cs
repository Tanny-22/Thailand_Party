using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Settings
{
    public sealed class SettingsMenuController : MonoBehaviour
    {
        [SerializeField] GameObject audioPanel, controlsPanel, graphicsPanel;
        [SerializeField] Button audioButton, controlsButton, graphicsButton;
        void Awake() { audioButton?.onClick.AddListener(ShowAudio); controlsButton?.onClick.AddListener(ShowControls); graphicsButton?.onClick.AddListener(ShowGraphics); }
        void OnDestroy() { audioButton?.onClick.RemoveListener(ShowAudio); controlsButton?.onClick.RemoveListener(ShowControls); graphicsButton?.onClick.RemoveListener(ShowGraphics); }
        public void ShowAudio() => Show(audioPanel); public void ShowControls() => Show(controlsPanel); public void ShowGraphics() => Show(graphicsPanel);
        void OnEnable() => ShowAudio();
        void Show(GameObject selected) { foreach (var panel in new[]{audioPanel, controlsPanel, graphicsPanel}) if (panel) panel.SetActive(panel == selected); }
    }
}
