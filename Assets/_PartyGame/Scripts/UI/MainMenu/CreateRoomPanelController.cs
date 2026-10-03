using System.Collections.Generic;
using PartyGame.Core;
using PartyGame.MiniGames;
using PartyGame.Networking;
using PartyGame.UI.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.MainMenu
{
    public sealed class CreateRoomPanelController : MonoBehaviour
    {
        [SerializeField] TMP_InputField roomNameInput;
        [SerializeField] Toggle privateToggle;
        [SerializeField] TMP_Dropdown playerCountDropdown, winsDropdown;
        [SerializeField] Toggle randomToggle;
        [SerializeField] MiniGameRegistry registry;
        [SerializeField] SelectionCardView miniGameCardPrefab;
        [SerializeField] Transform miniGameList;
        [SerializeField] TMP_Text validationLabel;
        [SerializeField] Button createButton;
        [SerializeField] LoadingOverlayController loading;
        [SerializeField] ModalDialogController modal;
        readonly List<string> selectedIds = new();
        void Awake() { createButton?.onClick.AddListener(Create); BuildMiniGameList(); }
        void OnDestroy() => createButton?.onClick.RemoveListener(Create);
        public void ToggleMiniGame(string id, bool selected) { if (selected && !selectedIds.Contains(id)) selectedIds.Add(id); else if (!selected) selectedIds.Remove(id); }
        void BuildMiniGameList()
        {
            selectedIds.Clear();
            if (!registry) return;
            foreach (var entry in registry.Enabled)
            {
                selectedIds.Add(entry.Id);
                if (!miniGameCardPrefab || !miniGameList) continue;
                var card = Instantiate(miniGameCardPrefab, miniGameList);
                card.name = $"MiniGame_{entry.Id}";
                card.Bind(entry.Id, entry.DisplayName, entry.Thumbnail, true, ToggleMiniGame);
            }
        }
        async void Create()
        {
            if (SessionManager.Instance == null) return;
            if (string.IsNullOrWhiteSpace(roomNameInput ? roomNameInput.text : null)) { ShowValidation("Room name cannot be empty."); return; }
            if (selectedIds.Count == 0) { ShowValidation("Select at least one MiniGame."); return; }
            ShowValidation(string.Empty); createButton.interactable = false; loading?.Show("Creating room...");
            var data = new RoomSettingsData { RoomName = roomNameInput ? roomNameInput.text : "Party Room", Visibility = privateToggle && privateToggle.isOn ? RoomVisibility.Private : RoomVisibility.Public, MaximumPlayers = 2 + (playerCountDropdown ? playerCountDropdown.value : 2), WinsRequired = 1 + (winsDropdown ? winsDropdown.value : 2), RandomSelection = !randomToggle || randomToggle.isOn, MiniGameIds = new List<string>(selectedIds) };
            if (await SessionManager.Instance.CreateRoomAsync(data)) await SceneFlowManager.Instance.LoadLocalAsync("02_Lobby");
            if (loading) loading.Hide();
            if (createButton) createButton.interactable = true;
        }
        void ShowValidation(string value) { if (validationLabel) validationLabel.text = value; if (!string.IsNullOrEmpty(value)) modal?.Show(value); }
    }
}
