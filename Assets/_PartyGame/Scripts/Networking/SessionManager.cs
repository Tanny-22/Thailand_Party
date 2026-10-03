using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PartyGame.Core;
using PartyGame.Persistence;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using Unity.Netcode;
using UnityEngine;

namespace PartyGame.Networking
{
    public sealed class SessionManager : PersistentService<SessionManager>
    {
        public const string StateKey = "state", WinsKey = "wins", PoolKey = "pool", RandomKey = "random", RemovalKey = "remove";
        [SerializeField] GameConfig gameConfig;
        [SerializeField] RoomSettingsData activeRoom = new();
        readonly HashSet<string> roomBanList = new();
        bool busy;
        ISession activeSession;
        string pendingUserMessage;
        RoomState currentRoomState = RoomState.WaitingInLobby;
        public event Action StateChanged;
        public event Action<string> OperationFailed;
        public bool IsBusy => busy;
        public bool IsHost => activeSession?.IsHost == true;
        public ISession ActiveSession => activeSession;
        public RoomSettingsData ActiveRoom => activeRoom;
        public string JoinCode => activeSession?.Code ?? string.Empty;
        public RoomState CurrentRoomState => currentRoomState;
        public string ConsumePendingMessage() { var value = pendingUserMessage; pendingUserMessage = null; return value; }

        public async Task InitializeServicesAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized) await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn) await AuthenticationService.Instance.SignInAnonymouslyAsync();
            if (AuthenticationService.Instance.PlayerName != SafePlayerName())
                await AuthenticationService.Instance.UpdatePlayerNameAsync(SafePlayerName());
        }

        public async Task<bool> CreateRoomAsync(RoomSettingsData settings)
        {
            if (busy) return false;
            if (!Validate(settings, out var error)) return Fail(error);
            busy = true; StateChanged?.Invoke();
            try
            {
                await InitializeServicesAsync();
                activeRoom = settings.Clone();
                var options = new SessionOptions
                {
                    Name = activeRoom.RoomName.Trim(), MaxPlayers = activeRoom.MaximumPlayers,
                    IsPrivate = activeRoom.Visibility == RoomVisibility.Private,
                    SessionProperties = BuildProperties(activeRoom)
                }.WithRelayNetwork().WithPlayerName(VisibilityPropertyOptions.Public);
                activeSession = await MultiplayerService.Instance.CreateSessionAsync(options);
                Subscribe(activeSession);
                return true;
            }
            catch (Exception e) { Debug.LogException(e); return Fail("Failed to create room. Check services and network connection."); }
            finally { busy = false; StateChanged?.Invoke(); }
        }

        public async Task<bool> JoinByCodeAsync(string code)
        {
            if (busy || string.IsNullOrWhiteSpace(code)) return Fail("Enter a valid Join Code.");
            busy = true; StateChanged?.Invoke();
            try
            {
                await InitializeServicesAsync();
                activeSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(code.Trim().ToUpperInvariant(), new JoinSessionOptions().WithPlayerName(VisibilityPropertyOptions.Public));
                if (!CanJoinActiveSession(out var joinError)) { await activeSession.LeaveAsync(); activeSession = null; return Fail(joinError); }
                Subscribe(activeSession); ReadRoomProperties(); return true;
            }
            catch (Exception e) { Debug.LogWarning($"Join by code failed: {e.Message}"); return Fail("Unable to join. The room may not exist or may be full."); }
            finally { busy = false; StateChanged?.Invoke(); }
        }

        public async Task<IReadOnlyList<ISessionInfo>> QueryPublicRoomsAsync(string search, bool includeFull)
        {
            await InitializeServicesAsync();
            var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions { Count = 50 });
            return results.Sessions.Where(x => !x.IsLocked && (includeFull || x.AvailableSlots > 0)
                && (string.IsNullOrWhiteSpace(search) || x.Name.IndexOf(search.Trim(), StringComparison.OrdinalIgnoreCase) >= 0)
                && (!x.Properties.TryGetValue(StateKey, out var p) || p.Value == RoomState.WaitingInLobby.ToString())).ToList();
        }

        public async Task<bool> JoinByIdAsync(string id)
        {
            if (busy || string.IsNullOrWhiteSpace(id)) return false;
            busy = true;
            try
            {
                await InitializeServicesAsync();
                activeSession = await MultiplayerService.Instance.JoinSessionByIdAsync(id, new JoinSessionOptions().WithPlayerName(VisibilityPropertyOptions.Public));
                if (!CanJoinActiveSession(out var joinError)) { await activeSession.LeaveAsync(); activeSession = null; return Fail(joinError); }
                Subscribe(activeSession); ReadRoomProperties(); return true;
            }
            catch (Exception e) { Debug.LogException(e); return Fail("Unable to join selected room."); }
            finally { busy = false; StateChanged?.Invoke(); }
        }

        public async Task UpdateRoomAsync(RoomSettingsData settings)
        {
            if (busy) { Fail("A room operation is already in progress."); return; }
            if (!IsHost) { Fail("Only the Host can edit room settings."); return; }
            if (!Validate(settings, out var error)) { Fail(error); return; }
            if (activeSession.Players.Count > settings.MaximumPlayers) { Fail("Capacity cannot be lower than the connected player count."); return; }
            busy = true; StateChanged?.Invoke();
            try
            {
                activeRoom = settings.Clone(); var host = activeSession.AsHost(); host.Name = activeRoom.RoomName.Trim(); host.IsPrivate = activeRoom.Visibility == RoomVisibility.Private;
                host.SetProperties(BuildProperties(activeRoom, currentRoomState)); await host.SavePropertiesAsync(); StateChanged?.Invoke();
            }
            catch (Exception e) { Debug.LogException(e); Fail("Unable to update room settings."); }
            finally { busy = false; StateChanged?.Invoke(); }
        }

        public async Task<bool> KickAsync(string playerId, bool ban)
        {
            if (busy || !IsHost || string.IsNullOrEmpty(playerId) || playerId == activeSession.Host) return false;
            busy = true; StateChanged?.Invoke();
            if (ban) roomBanList.Add(playerId);
            try
            {
                var host = activeSession.AsHost();
                host.SetProperty(RemovalKey, Public($"{playerId}|{(ban ? "ban" : "kick")}"));
                await host.SavePropertiesAsync();
                await host.RemovePlayerAsync(playerId);
                return true;
            }
            catch (Exception e) { Debug.LogException(e); Fail(ban ? "Unable to ban that player." : "Unable to kick that player."); return false; }
            finally { busy = false; StateChanged?.Invoke(); }
        }

        public bool IsBanned(string authenticationId) => !string.IsNullOrEmpty(authenticationId) && roomBanList.Contains(authenticationId);
        public async Task SetRoomStateAsync(RoomState state)
        {
            if (!IsHost) return; currentRoomState = state; var host = activeSession.AsHost(); host.SetProperty(StateKey, Public(state.ToString(), PropertyIndex.String1)); await host.SavePropertiesAsync(); StateChanged?.Invoke();
        }

        public async Task LeaveAsync(string reason = null)
        {
            if (busy) return; busy = true;
            try
            {
                if (activeSession != null) { Unsubscribe(activeSession); if (activeSession.IsHost) await activeSession.AsHost().DeleteAsync(); else await activeSession.LeaveAsync(); }
            }
            catch (Exception e) { Debug.LogWarning($"Session cleanup: {e.Message}"); }
            finally { activeSession = null; roomBanList.Clear(); currentRoomState = RoomState.WaitingInLobby; if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown(); busy = false; StateChanged?.Invoke(); }
        }

        Dictionary<string, SessionProperty> BuildProperties(RoomSettingsData r, RoomState state = RoomState.WaitingInLobby) => new()
        {
            [StateKey] = Public(state.ToString(), PropertyIndex.String1),
            [WinsKey] = Public(r.WinsRequired.ToString(), PropertyIndex.Number1),
            [PoolKey] = Public(string.Join(",", r.MiniGameIds)), [RandomKey] = Public(r.RandomSelection ? "1" : "0")
        };
        static SessionProperty Public(string value, PropertyIndex index = PropertyIndex.None) => new(value, VisibilityPropertyOptions.Public, index);
        static string Property(ISession session, string key) => session?.Properties != null && session.Properties.TryGetValue(key, out var p) ? p.Value : string.Empty;
        string SafePlayerName()
        {
            var raw = SaveManager.Instance ? SaveManager.Instance.PlayerName : SaveManager.GeneratePlayerName();
            raw = string.IsNullOrWhiteSpace(raw) ? SaveManager.GeneratePlayerName() : raw.Trim();
            raw = raw.Substring(0, Mathf.Min(raw.Length, gameConfig ? gameConfig.MaximumDisplayNameLength : 16));
            var serviceSafe = new string(raw.Select(character => char.IsLetterOrDigit(character) || character == '-' || character == '_' || character == '.' ? character : '-').ToArray());
            return string.IsNullOrWhiteSpace(serviceSafe) ? SaveManager.GeneratePlayerName() : serviceSafe;
        }
        bool Validate(RoomSettingsData value, out string error)
        {
            error = string.Empty; if (value == null) { error = "Room settings are missing."; return false; }
            if (string.IsNullOrWhiteSpace(value.RoomName)) { error = "Room name cannot be empty."; return false; }
            if (value.RoomName.Trim().Length > (gameConfig ? gameConfig.MaximumRoomNameLength : 24)) { error = "Room name is too long."; return false; }
            if (value.MaximumPlayers < 2 || value.MaximumPlayers > 10) { error = "Player count must be between 2 and 10."; return false; }
            if (value.WinsRequired < 1 || value.WinsRequired > (gameConfig ? gameConfig.MaximumWinsRequired : 10)) { error = "Wins Required is outside the allowed range."; return false; }
            if (value.MiniGameIds == null || value.MiniGameIds.Count == 0) { error = "Select at least one MiniGame."; return false; }
            return true;
        }
        bool Fail(string message) { if (!string.IsNullOrEmpty(message)) OperationFailed?.Invoke(message); return false; }
        void Subscribe(ISession s) { s.Changed += OnChanged; s.Deleted += OnClosed; s.RemovedFromSession += OnRemoved; s.SessionHostChanged += OnHostChanged; }
        void Unsubscribe(ISession s) { s.Changed -= OnChanged; s.Deleted -= OnClosed; s.RemovedFromSession -= OnRemoved; s.SessionHostChanged -= OnHostChanged; }
        async void OnChanged()
        {
            ReadRoomProperties();
            if (IsHost && roomBanList.Count > 0)
            {
                foreach (var player in activeSession.Players.Where(p => roomBanList.Contains(p.Id)).ToArray())
                    await activeSession.AsHost().RemovePlayerAsync(player.Id);
            }
            StateChanged?.Invoke();
        }
        async void OnClosed() { await HandleRemoteClose("Room Closed."); }
        async void OnRemoved()
        {
            var marker = Property(activeSession, RemovalKey);
            var localId = AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : string.Empty;
            var message = marker.StartsWith(localId + "|ban", StringComparison.Ordinal) ? "You were banned from this room." : "You were kicked by the Host.";
            await HandleRemoteClose(message);
        }
        async void OnHostChanged(string _) { await HandleRemoteClose("Host disconnected. Room closed."); }
        async Task HandleRemoteClose(string message)
        {
            if (activeSession != null) Unsubscribe(activeSession);
            activeSession = null; pendingUserMessage = message;
            if (NetworkManager.Singleton && NetworkManager.Singleton.IsListening) NetworkManager.Singleton.Shutdown();
            OperationFailed?.Invoke(message);
            if (SceneFlowManager.Instance) await SceneFlowManager.Instance.LoadLocalAsync(gameConfig ? gameConfig.MainMenuScene : "01_MainMenu");
        }
        bool CanJoinActiveSession(out string error)
        {
            error = null;
            if (activeSession == null) { error = "Room was not found."; return false; }
            if (Property(activeSession, StateKey) != RoomState.WaitingInLobby.ToString()) { error = "Room is already in a match."; return false; }
            return true;
        }
        void ReadRoomProperties()
        {
            if (activeSession == null) return;
            activeRoom.RoomName = activeSession.Name; activeRoom.MaximumPlayers = activeSession.MaxPlayers; activeRoom.Visibility = activeSession.IsPrivate ? RoomVisibility.Private : RoomVisibility.Public;
            if (Enum.TryParse(Property(activeSession, StateKey), out RoomState state)) currentRoomState = state;
            if (int.TryParse(Property(activeSession, WinsKey), out var wins)) activeRoom.WinsRequired = wins;
            activeRoom.RandomSelection = Property(activeSession, RandomKey) != "0";
            activeRoom.MiniGameIds = Property(activeSession, PoolKey).Split(new[]{','}, StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}
