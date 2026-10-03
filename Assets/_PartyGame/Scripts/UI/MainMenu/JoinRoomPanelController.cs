using PartyGame.Core;
using PartyGame.Networking;
using PartyGame.UI.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.MainMenu
{
    public sealed class JoinRoomPanelController : MonoBehaviour
    {
        [SerializeField] TMP_InputField codeInput, searchInput;
        [SerializeField] Toggle includeFullToggle;
        [SerializeField] Button joinCodeButton, refreshButton;
        [SerializeField] TMP_Text browserResults;
        [SerializeField] PublicRoomEntryView roomEntryPrefab;
        [SerializeField] Transform roomList;
        [SerializeField] LoadingOverlayController loading;
        [SerializeField] ModalDialogController modal;
        readonly System.Collections.Generic.List<PublicRoomEntryView> entries = new();
        void Awake() { joinCodeButton?.onClick.AddListener(JoinCode); refreshButton?.onClick.AddListener(Refresh); }
        void OnDestroy() { joinCodeButton?.onClick.RemoveListener(JoinCode); refreshButton?.onClick.RemoveListener(Refresh); }
        void OnEnable() { if (SessionManager.Instance) SessionManager.Instance.OperationFailed += ShowError; }
        void OnDisable() { if (SessionManager.Instance) SessionManager.Instance.OperationFailed -= ShowError; }
        async void JoinCode()
        {
            if (SessionManager.Instance == null || SessionManager.Instance.IsBusy) return;
            joinCodeButton.interactable = false; loading?.Show("Joining...");
            if (await SessionManager.Instance.JoinByCodeAsync(codeInput ? codeInput.text : string.Empty)) await SceneFlowManager.Instance.LoadLocalAsync("02_Lobby");
            if (loading) loading.Hide();
            if (joinCodeButton) joinCodeButton.interactable = true;
        }
        async void Refresh()
        {
            if (SessionManager.Instance == null || SessionManager.Instance.IsBusy) return;
            refreshButton.interactable = false; loading?.Show("Refreshing rooms..."); ClearEntries();
            try
            {
                var rooms = await SessionManager.Instance.QueryPublicRoomsAsync(searchInput ? searchInput.text : string.Empty, includeFullToggle && includeFullToggle.isOn);
                if (browserResults) browserResults.text = rooms.Count == 0 ? "No matching public rooms." : string.Empty;
                foreach (var room in rooms)
                {
                    if (!roomEntryPrefab || !roomList) continue;
                    var entry = Instantiate(roomEntryPrefab, roomList); entries.Add(entry); entry.name = $"Room_{room.Id}";
                    var current = room.MaxPlayers - room.AvailableSlots;
                    entry.Bind(room.Name, current, room.MaxPlayers, room.AvailableSlots > 0, () => JoinSelected(room.Id, entry));
                }
            }
            catch (System.Exception e) { Debug.LogException(e); if (browserResults) browserResults.text = "Room refresh failed."; modal?.Show("Unable to refresh public rooms."); }
            if (loading) loading.Hide();
            if (refreshButton) refreshButton.interactable = true;
        }
        async void JoinSelected(string id, PublicRoomEntryView entry)
        {
            if (SessionManager.Instance == null || SessionManager.Instance.IsBusy) return;
            entry.SetBusy(true); loading?.Show("Joining room...");
            if (await SessionManager.Instance.JoinByIdAsync(id)) await SceneFlowManager.Instance.LoadLocalAsync("02_Lobby");
            else entry.SetBusy(false);
            if (loading) loading.Hide();
        }
        void ShowError(string message) { if (loading) loading.Hide(); modal?.Show(message); }
        void ClearEntries() { foreach (var entry in entries) if (entry) Destroy(entry.gameObject); entries.Clear(); }
    }
}
