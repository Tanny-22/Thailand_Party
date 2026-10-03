using System;
using System.Collections.Generic;
using System.Linq;
using PartyGame.Core;
using PartyGame.Networking;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PartyGame.Match
{
    public sealed class MatchManager : NetworkBehaviour
    {
        public static MatchManager Instance { get; private set; }
        public readonly NetworkVariable<FixedString64Bytes> LastWinner = new();
        public readonly NetworkVariable<bool> MatchFinished = new();
        readonly Dictionary<ulong, int> wins = new();
        public event Action StandingsChanged;
        void Awake() { if (Instance && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        public IReadOnlyDictionary<ulong, int> Standings => wins;
        public void ReportWinnerServer(ulong clientId)
        {
            if (!IsServer || SessionManager.Instance?.ActiveRoom == null) return;
            wins[clientId] = wins.TryGetValue(clientId, out var count) ? count + 1 : 1;
            LastWinner.Value = clientId.ToString(); MatchFinished.Value = wins[clientId] >= SessionManager.Instance.ActiveRoom.WinsRequired;
            BroadcastResultsClientRpc(wins.Keys.ToArray(), wins.Values.ToArray(), MatchFinished.Value);
        }
        public async void CompleteMiniGameServer(ulong clientId)
        {
            if (!IsServer) return;
            ReportWinnerServer(clientId);
            await SessionManager.Instance.SetRoomStateAsync(RoomState.ShowingResults);
            SceneFlowManager.Instance.LoadNetworked("04_Results");
        }
        [ClientRpc] void BroadcastResultsClientRpc(ulong[] ids, int[] values, bool finished) { wins.Clear(); for (var i = 0; i < ids.Length; i++) wins[ids[i]] = values[i]; StandingsChanged?.Invoke(); }
        public void ResetMatchServer() { if (!IsServer) return; wins.Clear(); LastWinner.Value = default; MatchFinished.Value = false; BroadcastResultsClientRpc(Array.Empty<ulong>(), Array.Empty<int>(), false); }
    }
}
