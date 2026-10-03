using PartyGame.Match;
using Unity.Netcode;
using UnityEngine;

namespace PartyGame.MiniGames
{
    public abstract class MiniGameController : NetworkBehaviour
    {
        [SerializeField] bool allowDebugWinner;
        protected void ReportWinner(ulong clientId) { if (IsServer) MatchManager.Instance.ReportWinnerServer(clientId); }
        [ContextMenu("Debug: Host Wins")]
        void DebugHostWins() { if (allowDebugWinner && IsServer) ReportWinner(NetworkManager.ServerClientId); }
    }
}
