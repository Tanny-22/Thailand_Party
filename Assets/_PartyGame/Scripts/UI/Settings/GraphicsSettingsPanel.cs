using System.Collections.Generic;
using PartyGame.Persistence;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Settings
{
    public sealed class GraphicsSettingsPanel : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown resolutions;
        [SerializeField] Toggle fullscreen, vsync;
        IReadOnlyList<Resolution> options;
        void OnEnable()
        {
            options = PartyGame.Settings.SettingsManager.Instance.GetUniqueResolutions(); resolutions.ClearOptions(); resolutions.AddOptions(new List<string>(System.Linq.Enumerable.Select(options, r => $"{r.width} x {r.height}")));
            var current = 0; for (var i = 0; i < options.Count; i++) if (options[i].width == Screen.width && options[i].height == Screen.height) current = i;
            resolutions.SetValueWithoutNotify(current); fullscreen.SetIsOnWithoutNotify(Screen.fullScreen); vsync.SetIsOnWithoutNotify(QualitySettings.vSyncCount > 0);
        }
        public void Apply() { if (options == null || options.Count == 0) return; var value = options[Mathf.Clamp(resolutions.value, 0, options.Count - 1)]; PartyGame.Settings.SettingsManager.Instance.ApplyGraphics(value.width, value.height, fullscreen.isOn, vsync.isOn); }
    }
}
