using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.MainMenu
{
    public sealed class PublicRoomEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text roomNameLabel;
        [SerializeField] TMP_Text playerCountLabel;
        [SerializeField] Button joinButton;
        Action joinAction;

        void Awake() => joinButton?.onClick.AddListener(Join);
        void OnDestroy() => joinButton?.onClick.RemoveListener(Join);

        public void Bind(string roomName, int currentPlayers, int maximumPlayers, bool joinable, Action onJoin)
        {
            if (roomNameLabel) roomNameLabel.text = roomName;
            if (playerCountLabel) playerCountLabel.text = $"{currentPlayers}/{maximumPlayers}";
            joinAction = onJoin;
            if (joinButton) joinButton.interactable = joinable;
        }

        public void SetBusy(bool value) { if (joinButton) joinButton.interactable = !value; }
        void Join() => joinAction?.Invoke();
    }
}
