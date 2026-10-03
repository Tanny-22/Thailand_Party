using PartyGame.Core;
using PartyGame.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Multiplayer;
using PartyGame.MiniGames;
using PartyGame.Players;
using PartyGame.UI.Shared;
using System.Collections.Generic;
using System.Linq;

namespace PartyGame.UI.Lobby
{
    public sealed class LobbyUIController : MonoBehaviour
    {
        [SerializeField] TMP_Text roomName, joinCode, visibility, players, settingsSummary;
        [SerializeField] Transform playerList;
        [SerializeField] LobbyPlayerEntryView playerEntryPrefab;
        [SerializeField] CharacterRegistry characterRegistry;
        [SerializeField] GameObject hostSettingsRoot;
        [SerializeField] TMP_InputField roomNameInput;
        [SerializeField] Toggle privateToggle;
        [SerializeField] TMP_Dropdown capacityDropdown, winsDropdown;
        [SerializeField] Toggle randomToggle;
        [SerializeField] Transform miniGameList;
        [SerializeField] SelectionCardView miniGameCardPrefab;
        [SerializeField] MiniGameRegistry miniGameRegistry;
        [SerializeField] Button applySettingsButton, startButton, leaveButton;
        [SerializeField] ModalDialogController modal;
        [SerializeField] LoadingOverlayController loading;
        readonly List<LobbyPlayerEntryView> entries = new();
        readonly HashSet<string> selectedMiniGames = new();
        void Awake() { startButton?.onClick.AddListener(StartGame); leaveButton?.onClick.AddListener(Leave); applySettingsButton?.onClick.AddListener(ApplySettings); }
        void OnDestroy() { startButton?.onClick.RemoveListener(StartGame); leaveButton?.onClick.RemoveListener(Leave); applySettingsButton?.onClick.RemoveListener(ApplySettings); }
        void OnEnable() { if (SessionManager.Instance) { SessionManager.Instance.StateChanged += Refresh; SessionManager.Instance.OperationFailed += ShowError; } Refresh(); }
        void OnDisable() { if (SessionManager.Instance) { SessionManager.Instance.StateChanged -= Refresh; SessionManager.Instance.OperationFailed -= ShowError; } }
        void Refresh()
        {
            var sm = SessionManager.Instance; if (sm?.ActiveSession == null) return;
            if (roomName) roomName.text = sm.ActiveSession.Name; if (joinCode) joinCode.text = $"Join Code: {sm.JoinCode}"; if (visibility) visibility.text = sm.ActiveSession.IsPrivate ? "PRIVATE" : "PUBLIC";
            if (players) players.text = $"Players {sm.ActiveSession.Players.Count}/{sm.ActiveSession.MaxPlayers}"; if (settingsSummary) settingsSummary.text = $"First to {sm.ActiveRoom.WinsRequired} wins\nPool: {string.Join(", ", sm.ActiveRoom.MiniGameIds)}";
            if (startButton) startButton.gameObject.SetActive(sm.IsHost); if (hostSettingsRoot) hostSettingsRoot.SetActive(sm.IsHost);
            BindSettings(sm); RebuildRoster(sm);
        }
        void BindSettings(SessionManager sm)
        {
            if (!sm.IsHost) return;
            if (roomNameInput && !roomNameInput.isFocused) roomNameInput.text = sm.ActiveRoom.RoomName;
            privateToggle?.SetIsOnWithoutNotify(sm.ActiveRoom.Visibility == RoomVisibility.Private); capacityDropdown?.SetValueWithoutNotify(Mathf.Clamp(sm.ActiveRoom.MaximumPlayers - 2, 0, 8)); winsDropdown?.SetValueWithoutNotify(Mathf.Clamp(sm.ActiveRoom.WinsRequired - 1, 0, 9)); randomToggle?.SetIsOnWithoutNotify(sm.ActiveRoom.RandomSelection);
            if (selectedMiniGames.Count == 0) { selectedMiniGames.UnionWith(sm.ActiveRoom.MiniGameIds); RebuildMiniGameCards(); }
        }
        void RebuildRoster(SessionManager sm)
        {
            foreach (var entry in entries) if (entry) Destroy(entry.gameObject); entries.Clear(); if (!playerList || !playerEntryPrefab) return;
            var networkPlayers = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None);
            foreach (var player in sm.ActiveSession.Players)
            {
                var host = player.Id == sm.ActiveSession.Host; var np = networkPlayers.FirstOrDefault(x => x.AuthenticationId.Value.ToString() == player.Id);
                var display = np ? np.DisplayName.Value.ToString() : player.GetPlayerName() ?? "Player"; var character = np ? characterRegistry?.Find(np.CharacterId.Value.ToString()) : null;
                var view = Instantiate(playerEntryPrefab, playerList); entries.Add(view); view.name = $"Player_{player.Id}";
                view.Bind(display, character ? character.Thumbnail : null, host, sm.IsHost && !host, () => Moderate(player.Id, false), () => Moderate(player.Id, true));
            }
        }
        void RebuildMiniGameCards()
        {
            if (!miniGameList || !miniGameCardPrefab || !miniGameRegistry) return; for (var i = miniGameList.childCount - 1; i >= 0; i--) Destroy(miniGameList.GetChild(i).gameObject);
            foreach (var item in miniGameRegistry.Enabled) { var card = Instantiate(miniGameCardPrefab, miniGameList); card.Bind(item.Id, item.DisplayName, item.Thumbnail, selectedMiniGames.Contains(item.Id), ToggleMiniGame); }
        }
        void ToggleMiniGame(string id, bool selected) { if (selected) selectedMiniGames.Add(id); else selectedMiniGames.Remove(id); }
        async void ApplySettings()
        {
            var sm = SessionManager.Instance; if (sm == null || !sm.IsHost || sm.IsBusy) return; if (selectedMiniGames.Count == 0) { modal?.Show("Select at least one MiniGame."); return; }
            applySettingsButton.interactable = false; loading?.Show("Updating room..."); var value = sm.ActiveRoom.Clone(); value.RoomName = roomNameInput ? roomNameInput.text : value.RoomName; value.Visibility = privateToggle && privateToggle.isOn ? RoomVisibility.Private : RoomVisibility.Public; value.MaximumPlayers = 2 + (capacityDropdown ? capacityDropdown.value : 0); value.WinsRequired = 1 + (winsDropdown ? winsDropdown.value : 0); value.RandomSelection = !randomToggle || randomToggle.isOn; value.MiniGameIds = selectedMiniGames.ToList(); await sm.UpdateRoomAsync(value); loading?.Hide(); applySettingsButton.interactable = true;
        }
        async void Moderate(string playerId, bool ban) { var sm = SessionManager.Instance; if (sm == null || !sm.IsHost || sm.IsBusy) return; loading?.Show(ban ? "Banning player..." : "Removing player..."); await sm.KickAsync(playerId, ban); loading?.Hide(); }
        async void StartGame() { var sm = SessionManager.Instance; if (sm == null || !sm.IsHost || sm.IsBusy) return; if (sm.ActiveSession.Players.Count < 2) { modal?.Show("At least two players are required."); return; } if (sm.ActiveRoom.MiniGameIds.Count == 0) { modal?.Show("Select at least one MiniGame."); return; } startButton.interactable = false; await sm.SetRoomStateAsync(RoomState.Randomizing); if (!SceneFlowManager.Instance.LoadNetworked("03_MiniGameRandomizer")) { startButton.interactable = true; modal?.Show("Unable to start the network scene transition."); } }
        async void Leave() { loading?.Show("Leaving room..."); await SessionManager.Instance.LeaveAsync(); await SceneFlowManager.Instance.LoadLocalAsync("01_MainMenu"); }
        void ShowError(string message) { loading?.Hide(); modal?.Show(message); }
    }
}
