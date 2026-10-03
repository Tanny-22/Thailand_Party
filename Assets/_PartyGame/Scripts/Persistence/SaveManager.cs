using UnityEngine;

namespace PartyGame.Persistence
{
    public sealed class SaveManager : MonoBehaviour
    {
        const string Prefix = "PartyGame.";
        public static SaveManager Instance { get; private set; }
        public string PlayerName { get => GetString("PlayerName", GeneratePlayerName()); set => SetString("PlayerName", value); }
        public string CharacterId { get => GetString("CharacterId", "default"); set => SetString("CharacterId", value); }
        public float MasterVolume { get => GetFloat("MasterVolume", 0.8f); set => SetFloat("MasterVolume", value); }
        public float MusicVolume { get => GetFloat("MusicVolume", 0.8f); set => SetFloat("MusicVolume", value); }
        public float SfxVolume { get => GetFloat("SfxVolume", 0.8f); set => SetFloat("SfxVolume", value); }
        public string BindingOverrides { get => GetString("BindingOverrides", string.Empty); set => SetString("BindingOverrides", value); }
        void Awake() { if (Instance && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        public string GetString(string key, string fallback) => PlayerPrefs.GetString(Prefix + key, fallback);
        public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(Prefix + key, fallback);
        public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(Prefix + key, fallback);
        public void SetString(string key, string value) { PlayerPrefs.SetString(Prefix + key, value ?? string.Empty); PlayerPrefs.Save(); }
        public void SetInt(string key, int value) { PlayerPrefs.SetInt(Prefix + key, value); PlayerPrefs.Save(); }
        public void SetFloat(string key, float value) { PlayerPrefs.SetFloat(Prefix + key, value); PlayerPrefs.Save(); }
        public static string GeneratePlayerName() => $"Player-{Random.Range(1000, 9999)}";
    }
}
