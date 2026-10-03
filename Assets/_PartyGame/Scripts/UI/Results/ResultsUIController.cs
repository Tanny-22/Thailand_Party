using PartyGame.Core;
using PartyGame.Match;
using PartyGame.MiniGames;
using PartyGame.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PartyGame.Players;
using System.Collections.Generic;
using System.Linq;

namespace PartyGame.UI.Results
{
    public sealed class ResultsUIController : MonoBehaviour
    {
        [SerializeField] TMP_Text title, scoreboard;
        [SerializeField] Transform resultList;
        [SerializeField] ResultsPlayerEntryView resultEntryPrefab;
        [SerializeField] CharacterRegistry characterRegistry;
        [SerializeField] Button continueButton;
        void OnEnable() { continueButton?.onClick.AddListener(Continue); Render(); }
        void OnDisable() => continueButton?.onClick.RemoveListener(Continue);
        void Render()
        {
            if (!MatchManager.Instance) return;
            title.text = MatchManager.Instance.MatchFinished.Value ? "MATCH WINNER" : "RESULTS";
            var players = FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).ToDictionary(x => x.OwnerClientId);
            var rows = MatchManager.Instance.Standings.OrderByDescending(x => x.Value).ThenBy(x => x.Key).ToList();
            scoreboard.text = rows.Count == 0 ? "Waiting for synchronized standings..." : string.Join("\n", rows.Select(x => $"{Name(players, x.Key)}: {x.Value} wins{(MatchManager.Instance.LastWinner.Value.ToString() == x.Key.ToString() ? "  ★" : string.Empty)}"));
            if (resultList && resultEntryPrefab)
            {
                for (var i = resultList.childCount - 1; i >= 0; i--) Destroy(resultList.GetChild(i).gameObject);
                foreach (var row in rows)
                {
                    players.TryGetValue(row.Key, out var player); var definition = player ? characterRegistry?.Find(player.CharacterId.Value.ToString()) : null;
                    Instantiate(resultEntryPrefab, resultList).Bind(Name(players, row.Key), definition ? definition.Thumbnail : null, row.Value, MatchManager.Instance.LastWinner.Value.ToString() == row.Key.ToString());
                }
            }
            continueButton.gameObject.SetActive(SessionManager.Instance?.IsHost == true);
        }
        static string Name(IReadOnlyDictionary<ulong, NetworkPlayer> players, ulong id) => players.TryGetValue(id, out var player) ? player.DisplayName.Value.ToString() : $"Player {id}";
        async void Continue() { if (MatchManager.Instance.MatchFinished.Value) { await SessionManager.Instance.SetRoomStateAsync(RoomState.WaitingInLobby); MatchManager.Instance.ResetMatchServer(); SceneFlowManager.Instance.LoadNetworked("02_Lobby"); } else { await SessionManager.Instance.SetRoomStateAsync(RoomState.Randomizing); SceneFlowManager.Instance.LoadNetworked("03_MiniGameRandomizer"); } }
    }
}
