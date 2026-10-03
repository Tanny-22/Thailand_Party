#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PartyGame.Core;
using PartyGame.Match;
using PartyGame.Networking;
using PartyGame.Persistence;
using Unity.Multiplayer.Playmode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace PartyGame.Development
{
    /// <summary>Editor-only MPPM smoke driver, inert unless explicit test tags are assigned.</summary>
    public sealed class PartyGameMppmSmokeDriver : MonoBehaviour
    {
        const string HostTag = "party-smoke-host";
        const string ClientTag = "party-smoke-client";
        const string JoinCodeTag = "party-qa-joincode";
        const string ModerationTag = "party-qa-moderation";
        const string LeaveTag = "party-qa-leave";
        const string RoomName = "Codex MPPM Smoke";
        const int TimeoutSeconds = 45;
        static readonly string IpcPath = Path.Combine(Path.GetTempPath(), "PartyGameMppmQa.txt");
        bool isHost;

        async void Start()
        {
            var tags = CurrentPlayer.ReadOnlyTags();
            isHost = tags.Contains(HostTag);
            var isClient = tags.Contains(ClientTag);
            if (!isHost && !isClient) return;

            DontDestroyOnLoad(gameObject);
            try
            {
                await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu", "main menu");
                SaveManager.Instance.PlayerName = isHost ? "MPPM Host" : "MPPM Client";
                SaveManager.Instance.CharacterId = "default";
                if (tags.Contains(JoinCodeTag)) await RunJoinCodeQaAsync();
                else if (tags.Contains(ModerationTag)) await RunModerationQaAsync();
                else if (tags.Contains(LeaveTag)) await RunLeaveQaAsync();
                else if (isHost) await RunHostAsync();
                else await RunClientAsync();
            }
            catch (Exception exception)
            {
                Debug.LogError($"[MPPM SMOKE] {(isHost ? "HOST" : "CLIENT")} FAIL: {exception.Message}\n{exception}");
                CurrentPlayer.ReportResult(false, exception.Message);
            }
        }

        async Task RunHostAsync()
        {
            Log("creating public two-player room");
            var settings = new RoomSettingsData
            {
                RoomName = RoomName,
                Visibility = RoomVisibility.Public,
                MaximumPlayers = 2,
                WinsRequired = 1,
                RandomSelection = false,
                MiniGameIds = new List<string> { "template" }
            };
            Ensure(await SessionManager.Instance.CreateRoomAsync(settings), "host could not create the room");
            await SceneFlowManager.Instance.LoadLocalAsync("02_Lobby");
            await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 2, "second session player");
            await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.IsHost && NetworkManager.Singleton.ConnectedClients.Count == 2, "second NGO client");
            // Let the joining player's JoinByIdAsync finish its post-join room-state validation
            // before the automated host advances the room faster than a human could.
            await Task.Delay(1000);
            Log("two players connected; starting authoritative scene flow");
            await SessionManager.Instance.SetRoomStateAsync(RoomState.Randomizing);
            Ensure(SceneFlowManager.Instance.LoadNetworked("03_MiniGameRandomizer"), "randomizer scene transition did not start");
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "MiniGame_Template", "mini-game template scene", 60);
            await WaitUntilAsync(() => MatchManager.Instance && MatchManager.Instance.IsSpawned, "spawned match manager");
            Log("mini-game loaded; awarding the host one win");
            MatchManager.Instance.CompleteMiniGameServer(NetworkManager.ServerClientId);
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "04_Results", "results scene");
            await WaitUntilAsync(() => MatchManager.Instance && MatchManager.Instance.MatchFinished.Value && MatchManager.Instance.Standings.Count > 0, "synchronized final standings");
            Log("PASS — create, join, two-player NGO connection, randomizer, mini-game, and results completed");
            CurrentPlayer.ReportResult(true, "PartyGame host end-to-end smoke passed.");
        }

        async Task RunClientAsync()
        {
            Log("polling the public room browser query");
            Unity.Services.Multiplayer.ISessionInfo target = null;
            var deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
            while (DateTime.UtcNow < deadline && target == null)
            {
                var rooms = await SessionManager.Instance.QueryPublicRoomsAsync(RoomName, false);
                target = rooms.FirstOrDefault(room => string.Equals(room.Name, RoomName, StringComparison.Ordinal));
                if (target == null) await Task.Delay(750);
            }
            Ensure(target != null, "public room did not appear in the browser query");
            Ensure(await SessionManager.Instance.JoinByIdAsync(target.Id), "client could not join the selected public room");
            await SceneFlowManager.Instance.LoadLocalAsync("02_Lobby");
            await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsConnectedClient, "NGO client connection");
            Log("joined room and connected through NGO");
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "MiniGame_Template", "networked mini-game scene", 60);
            Log("received mini-game scene transition");
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "04_Results", "networked results scene");
            await WaitUntilAsync(() => MatchManager.Instance && MatchManager.Instance.MatchFinished.Value && MatchManager.Instance.Standings.Count > 0, "client final standings");
            Log("PASS — browser join, NGO connection, scene synchronization, and results replication completed");
            CurrentPlayer.ReportResult(true, "PartyGame client end-to-end smoke passed.");
        }

        async Task RunJoinCodeQaAsync()
        {
            var started = DateTime.UtcNow;
            if (isHost)
            {
                TryDeleteIpc();
                await CreateRoomThroughUiAsync("Join Code QA");
                File.WriteAllText(IpcPath, $"READY|{SessionManager.Instance.JoinCode}");
                await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 2, "join-code session membership", 60);
                await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.ConnectedClients.Count == 2, "join-code NGO client", 60);
                Ensure(UnityEngine.Object.FindObjectsByType<PartyGame.UI.Lobby.LobbyPlayerEntryView>(FindObjectsSortMode.None).Length == 2, "lobby did not contain exactly two player entries");
                Log("JOIN CODE HOST PASS — UI-created room has two session and NGO players with two roster entries");
                CurrentPlayer.ReportResult(true, "Join Code host QA passed.");
                return;
            }

            OpenJoinPanel();
            var codeInput = FindActive<TMP_InputField>("JoinCodeInput");
            var joinButton = FindActive<Button>("JoinCodeButton");
            codeInput.text = "INVALID-CODE";
            joinButton.onClick.Invoke();
            await Task.Yield();
            Ensure(!joinButton.interactable && IsActive("LoadingOverlay"), "invalid-code loading state was not visible");
            await WaitUntilAsync(() => !SessionManager.Instance.IsBusy && IsActive("ModalDialogPanel"), "invalid-code recovery modal", 60);
            Ensure(joinButton.interactable && !IsActive("LoadingOverlay") && SessionManager.Instance.ActiveSession == null, "invalid-code UI/session did not recover");
            Ensure(ModalText().Contains("Unable to join", StringComparison.OrdinalIgnoreCase), "invalid-code message was not user-facing");
            FindActive<Button>("CloseButton").onClick.Invoke();

            var code = await ReadJoinCodeAsync(started);
            codeInput.text = code;
            joinButton.onClick.Invoke();
            await Task.Yield();
            Ensure(!joinButton.interactable && IsActive("LoadingOverlay"), "valid join loading state was not visible");
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "02_Lobby", "join-code lobby", 60);
            await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.IsConnectedClient, "join-code NGO connection", 60);
            await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 2, "client join-code membership update", 60);
            await WaitUntilAsync(() => UnityEngine.Object.FindObjectsByType<PartyGame.UI.Lobby.LobbyPlayerEntryView>(FindObjectsSortMode.None).Length == 2, "client lobby roster update", 60);
            Log("JOIN CODE CLIENT PASS — invalid code recovered, then exact Join Code connected through real UI");
            CurrentPlayer.ReportResult(true, "Join Code client QA passed.");
        }

        async Task RunModerationQaAsync()
        {
            var started = DateTime.UtcNow;
            if (isHost)
            {
                TryDeleteIpc();
                await CreateRoomThroughUiAsync("Moderation QA");
                File.WriteAllText(IpcPath, $"READY|{SessionManager.Instance.JoinCode}");
                await WaitForTwoPlayersAsync();
                Ensure(ActiveButtons("KickButton").Count == 1 && ActiveButtons("BanButton").Count == 1, "moderation buttons were not limited to the remote player");
                ActiveButtons("KickButton")[0].onClick.Invoke();
                await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 1 && NetworkManager.Singleton.ConnectedClients.Count == 1, "kicked player cleanup", 60);
                Log("KICK HOST PASS — roster/session/NGO returned to one player");
                File.WriteAllText(IpcPath, $"REJOIN|{SessionManager.Instance.JoinCode}");
                await WaitForTwoPlayersAsync();
                ActiveButtons("BanButton")[0].onClick.Invoke();
                await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 1 && NetworkManager.Singleton.ConnectedClients.Count == 1, "banned player cleanup", 60);
                File.WriteAllText(IpcPath, $"BANNED|{SessionManager.Instance.JoinCode}");
                await Task.Delay(4000);
                Ensure(SessionManager.Instance.ActiveSession?.Players.Count == 1 && NetworkManager.Singleton.ConnectedClients.Count == 1, "banned rejoin disturbed host room");
                Log("BAN HOST PASS — identity ban removed client and rejected rejoin while host room stayed healthy");
                CurrentPlayer.ReportResult(true, "Kick/Ban host QA passed.");
                return;
            }

            var code = await ReadIpcCodeAsync("READY", started);
            await JoinCodeThroughUiAsync(code);
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu", "kicked client main menu", 60);
            Ensure(ModalText().Contains("kicked", StringComparison.OrdinalIgnoreCase), "kick message was missing");
            Ensure(SessionManager.Instance.ActiveSession == null && (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening), "kick did not clear client state");
            Log("KICK CLIENT PASS — returned to menu with message and clean session/network state");
            code = await ReadIpcCodeAsync("REJOIN", started);
            CloseModalIfOpen();
            await JoinCodeThroughUiAsync(code);
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu", "banned client main menu", 60);
            Ensure(ModalText().Contains("banned", StringComparison.OrdinalIgnoreCase), "ban message was missing");
            Ensure(SessionManager.Instance.ActiveSession == null, "ban did not clear client session");
            code = await ReadIpcCodeAsync("BANNED", started);
            CloseModalIfOpen();
            OpenJoinPanel();
            FindActive<TMP_InputField>("JoinCodeInput").text = code;
            FindActive<Button>("JoinCodeButton").onClick.Invoke();
            await WaitUntilAsync(() => !SessionManager.Instance.IsBusy && SceneManager.GetActiveScene().name == "01_MainMenu" && IsActive("ModalDialogPanel"), "banned rejoin rejection", 60);
            Ensure(SessionManager.Instance.ActiveSession == null && !IsActive("LoadingOverlay"), "banned rejoin left stale client state");
            Log("BAN CLIENT PASS — banned identity could not rejoin and UI recovered");
            CurrentPlayer.ReportResult(true, "Kick/Ban client QA passed.");
        }

        async Task RunLeaveQaAsync()
        {
            var started = DateTime.UtcNow;
            if (isHost)
            {
                TryDeleteIpc();
                await CreateRoomThroughUiAsync("Leave QA");
                File.WriteAllText(IpcPath, $"READY|{SessionManager.Instance.JoinCode}");
                await WaitForTwoPlayersAsync();
                await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 1, "normal client leave", 60);
                Ensure(SessionManager.Instance.IsHost && NetworkManager.Singleton.IsHost, "host room did not survive normal client leave");
                File.WriteAllText(IpcPath, $"REJOIN|{SessionManager.Instance.JoinCode}");
                await WaitForTwoPlayersAsync();
                FindActive<Button>("LeaveButton").onClick.Invoke();
                await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu" && SessionManager.Instance.ActiveSession == null, "host leave cleanup", 60);
                Log("LEAVE HOST PASS — survived client leave/rejoin, then closed room and returned to menu");
                CurrentPlayer.ReportResult(true, "Leave host QA passed.");
                return;
            }

            var code = await ReadIpcCodeAsync("READY", started);
            await JoinCodeThroughUiAsync(code);
            FindActive<Button>("LeaveButton").onClick.Invoke();
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu" && SessionManager.Instance.ActiveSession == null, "client leave main menu", 60);
            Ensure(!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening, "normal client leave did not stop NGO");
            code = await ReadIpcCodeAsync("REJOIN", started);
            await JoinCodeThroughUiAsync(code);
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu", "host-closed client main menu", 60);
            Ensure(ModalText().Contains("Host disconnected", StringComparison.OrdinalIgnoreCase) || ModalText().Contains("Room closed", StringComparison.OrdinalIgnoreCase), "host-close message was missing");
            Ensure(SessionManager.Instance.ActiveSession == null && (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening), "host close left stale client state");
            Log("LEAVE CLIENT PASS — normal leave/rejoin and host-close cleanup completed without restart");
            CurrentPlayer.ReportResult(true, "Leave client QA passed.");
        }

        async Task CreateRoomThroughUiAsync(string roomName)
        {
            FindActive<Button>("STARTButton").onClick.Invoke();
            await Task.Yield();
            FindActive<TMP_InputField>("RoomNameInput").text = roomName;
            FindActive<TMP_Dropdown>("PlayerCountDropdown").value = 0;
            FindActive<TMP_Dropdown>("WinsDropdown").value = 0;
            var create = FindActive<Button>("CreateButton");
            create.onClick.Invoke();
            await Task.Yield();
            Ensure(!create.interactable && IsActive("LoadingOverlay"), "create loading state was not visible");
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "02_Lobby" && SessionManager.Instance.ActiveSession != null, "UI-created lobby", 60);
        }

        async Task JoinCodeThroughUiAsync(string code)
        {
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "01_MainMenu", "main menu before join", 60);
            CloseModalIfOpen();
            OpenJoinPanel();
            FindActive<TMP_InputField>("JoinCodeInput").text = code;
            FindActive<Button>("JoinCodeButton").onClick.Invoke();
            await WaitUntilAsync(() => SceneManager.GetActiveScene().name == "02_Lobby", "joined lobby", 60);
            await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.IsConnectedClient, "joined NGO client", 60);
        }

        async Task WaitForTwoPlayersAsync()
        {
            await WaitUntilAsync(() => SessionManager.Instance.ActiveSession?.Players.Count == 2, "two session players", 60);
            await WaitUntilAsync(() => NetworkManager.Singleton && NetworkManager.Singleton.ConnectedClients.Count == 2, "two NGO players", 60);
            await WaitUntilAsync(() => ActiveButtons("KickButton").Count == 1, "remote moderation entry", 60);
        }

        static void OpenJoinPanel() { FindActive<Button>("JOINButton").onClick.Invoke(); }
        static T FindActive<T>(string name) where T : Component => Resources.FindObjectsOfTypeAll<T>().First(item => item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy && item.gameObject.name == name);
        static List<Button> ActiveButtons(string name) => Resources.FindObjectsOfTypeAll<Button>().Where(item => item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy && item.gameObject.name == name).ToList();
        static bool IsActive(string name) => Resources.FindObjectsOfTypeAll<Transform>().Any(item => item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy && item.gameObject.name == name);
        static string ModalText() { var modal = Resources.FindObjectsOfTypeAll<PartyGame.UI.Shared.ModalDialogController>().FirstOrDefault(item => item.gameObject.scene.IsValid() && item.gameObject.activeInHierarchy); return modal ? modal.GetComponentInChildren<TMP_Text>(true)?.text ?? string.Empty : string.Empty; }
        static void CloseModalIfOpen() { var close = ActiveButtons("CloseButton").FirstOrDefault(); close?.onClick.Invoke(); }
        static void TryDeleteIpc() { try { if (File.Exists(IpcPath)) File.Delete(IpcPath); } catch { } }
        static async Task<string> ReadJoinCodeAsync(DateTime started) => await ReadIpcCodeAsync("READY", started);
        static async Task<string> ReadIpcCodeAsync(string expectedState, DateTime started)
        {
            string value = null;
            await WaitUntilAsync(() =>
            {
                if (!File.Exists(IpcPath) || File.GetLastWriteTimeUtc(IpcPath) < started.AddSeconds(-2)) return false;
                try { value = File.ReadAllText(IpcPath); } catch { return false; }
                return value.StartsWith(expectedState + "|", StringComparison.Ordinal);
            }, $"IPC state {expectedState}", 60);
            return value.Split('|')[1];
        }

        static async Task WaitUntilAsync(Func<bool> predicate, string description, int timeoutSeconds = TimeoutSeconds)
        {
            var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            while (!predicate())
            {
                if (DateTime.UtcNow >= deadline) throw new TimeoutException($"Timed out waiting for {description}.");
                await Task.Delay(100);
            }
        }

        static void Ensure(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        static void Log(string message) => Debug.Log($"[MPPM SMOKE] {message}");
    }
}
#endif

