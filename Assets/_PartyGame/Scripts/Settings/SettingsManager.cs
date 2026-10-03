using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core;
using PartyGame.Persistence;
using UnityEngine;
using UnityEngine.Audio;

namespace PartyGame.Settings
{
    public sealed class SettingsManager : PersistentService<SettingsManager>
    {
        [SerializeField] AudioMixer audioMixer;
        public event Action SettingsChanged;
        public void ApplyAudio(float master, float music, float sfx)
        {
            master = Mathf.Clamp01(master); music = Mathf.Clamp01(music); sfx = Mathf.Clamp01(sfx);
            SaveManager.Instance.MasterVolume = master; SaveManager.Instance.MusicVolume = music; SaveManager.Instance.SfxVolume = sfx;
            SetMixer("MasterVolume", master); SetMixer("MusicVolume", music); SetMixer("SFXVolume", sfx);
            SettingsChanged?.Invoke();
        }
        void SetMixer(string parameter, float linear) { if (audioMixer) audioMixer.SetFloat(parameter, linear <= 0.0001f ? -80f : Mathf.Log10(linear) * 20f); }
        public IReadOnlyList<Resolution> GetUniqueResolutions() => Screen.resolutions.GroupBy(r => (r.width, r.height)).Select(g => g.Last()).ToList();
        public void ApplyGraphics(int width, int height, bool fullscreen, bool vsync)
        {
            QualitySettings.vSyncCount = vsync ? 1 : 0;
            Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
            SaveManager.Instance.SetInt("Width", width); SaveManager.Instance.SetInt("Height", height);
            SaveManager.Instance.SetInt("Fullscreen", fullscreen ? 1 : 0); SaveManager.Instance.SetInt("VSync", vsync ? 1 : 0);
            SettingsChanged?.Invoke();
        }
    }
}
