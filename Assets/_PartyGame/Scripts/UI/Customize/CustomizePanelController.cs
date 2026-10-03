using PartyGame.Persistence;
using PartyGame.Players;
using PartyGame.Networking;
using PartyGame.Core;
using PartyGame.UI.Shared;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PartyGame.UI.Customize
{
    public sealed class CustomizePanelController : MonoBehaviour
    {
        [SerializeField] TMP_InputField displayNameInput;
        [SerializeField] CharacterRegistry registry;
        [SerializeField] GameConfig gameConfig;
        [SerializeField] SelectionCardView characterCardPrefab;
        [SerializeField] Transform characterList;
        [SerializeField] UnityEngine.UI.Button saveButton;
        [SerializeField] TMP_Text feedbackLabel;
        string selectedCharacter;
        void Awake() { saveButton?.onClick.AddListener(Save); BuildCards(); }
        void OnDestroy() => saveButton?.onClick.RemoveListener(Save);
        void OnEnable() { if (!SaveManager.Instance) return; displayNameInput.text = SaveManager.Instance.PlayerName; selectedCharacter = SaveManager.Instance.CharacterId; BuildCards(); }
        public void SelectCharacter(string id) { if (registry && registry.Find(id)) { selectedCharacter = id; BuildCards(); } }
        void SelectCharacter(string id, bool selected) { if (selected) SelectCharacter(id); }
        void BuildCards()
        {
            if (!registry || !characterCardPrefab || !characterList) return;
            for (var i = characterList.childCount - 1; i >= 0; i--) Destroy(characterList.GetChild(i).gameObject);
            foreach (var entry in registry.Enabled)
            {
                var card = Instantiate(characterCardPrefab, characterList); card.name = $"Character_{entry.Id}";
                card.Bind(entry.Id, entry.DisplayName, entry.Thumbnail, entry.Id == selectedCharacter, SelectCharacter);
            }
        }
        public void Save()
        {
            if (!SaveManager.Instance) return;
            var name = displayNameInput ? displayNameInput.text.Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(name)) { if (feedbackLabel) feedbackLabel.text = "Display name cannot be empty."; return; }
            var max = gameConfig ? gameConfig.MaximumDisplayNameLength : 16;
            if (name.Length > max) { if (feedbackLabel) feedbackLabel.text = $"Display name must be {max} characters or fewer."; return; }
            SaveManager.Instance.PlayerName = name;
            SaveManager.Instance.CharacterId = string.IsNullOrWhiteSpace(selectedCharacter) ? "default" : selectedCharacter;
            var localPlayer = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).FirstOrDefault(x => x.IsOwner);
            if (localPlayer && localPlayer.IsSpawned) localPlayer.RequestProfileChangeServerRpc(name, SaveManager.Instance.CharacterId);
            if (feedbackLabel) feedbackLabel.text = "Profile saved.";
        }
    }
}
