using System.Linq;
using PartyGame.Match;
using PartyGame.Networking;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.MiniGames
{
    public sealed class MiniGameTemplateDebugController : NetworkBehaviour
    {
        [SerializeField] Transform playerButtonList;
        [SerializeField] Button playerButtonPrefab;
        [SerializeField] TMP_Text statusLabel;

        public override void OnNetworkSpawn()
        {
            if (statusLabel) statusLabel.text = IsServer ? "HOST: choose a test winner" : "Waiting for Host to choose a winner";
            BuildButtons();
        }
        void BuildButtons()
        {
            if (!IsServer || !playerButtonList || !playerButtonPrefab) return;
            foreach (var player in FindObjectsByType<NetworkPlayer>(FindObjectsSortMode.None).OrderBy(x => x.OwnerClientId))
            {
                var capturedId = player.OwnerClientId; var button = Instantiate(playerButtonPrefab, playerButtonList); button.name = $"AwardWin_Player_{capturedId}";
                var label = button.GetComponentInChildren<TMP_Text>(); if (label) label.text = $"AWARD WIN: {player.DisplayName.Value}";
                button.onClick.AddListener(() => Award(capturedId));
            }
        }
        void Award(ulong clientId)
        {
            if (!IsServer || MatchManager.Instance == null) return;
            foreach (var button in playerButtonList.GetComponentsInChildren<Button>()) button.interactable = false;
            MatchManager.Instance.CompleteMiniGameServer(clientId);
        }
    }
}
