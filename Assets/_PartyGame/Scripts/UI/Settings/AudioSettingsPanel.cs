using PartyGame.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Settings
{
    public sealed class AudioSettingsPanel : MonoBehaviour
    {
        [SerializeField] Slider master, music, sfx;
        void Awake() { master?.onValueChanged.AddListener(_ => Apply()); music?.onValueChanged.AddListener(_ => Apply()); sfx?.onValueChanged.AddListener(_ => Apply()); }
        void OnDestroy() { master?.onValueChanged.RemoveAllListeners(); music?.onValueChanged.RemoveAllListeners(); sfx?.onValueChanged.RemoveAllListeners(); }
        void OnEnable() { if (!SaveManager.Instance) return; master.value = SaveManager.Instance.MasterVolume; music.value = SaveManager.Instance.MusicVolume; sfx.value = SaveManager.Instance.SfxVolume; }
        public void Apply() => PartyGame.Settings.SettingsManager.Instance.ApplyAudio(master.value, music.value, sfx.value);
    }
}
