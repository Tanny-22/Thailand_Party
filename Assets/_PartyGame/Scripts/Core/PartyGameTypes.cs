using System;
using System.Collections.Generic;
using UnityEngine;

namespace PartyGame.Core
{
    public enum RoomVisibility { Public, Private }
    public enum RoomState { WaitingInLobby, Starting, Randomizing, Playing, ShowingResults, MatchFinished, Closing }

    [Serializable]
    public sealed class RoomSettingsData
    {
        public string RoomName = "Party Room";
        public RoomVisibility Visibility = RoomVisibility.Public;
        [Range(2, 10)] public int MaximumPlayers = 4;
        [Min(1)] public int WinsRequired = 3;
        public bool RandomSelection = true;
        public List<string> MiniGameIds = new();

        public RoomSettingsData Clone() => new()
        {
            RoomName = RoomName, Visibility = Visibility, MaximumPlayers = MaximumPlayers,
            WinsRequired = WinsRequired, RandomSelection = RandomSelection,
            MiniGameIds = new List<string>(MiniGameIds)
        };
    }

    [Serializable]
    public sealed class PartyPlayerData
    {
        public ulong ClientId;
        public string AuthenticationId;
        public string DisplayName;
        public string CharacterId;
        public bool IsHost;
        public int Wins;
        public bool IsConnected;
    }
}
