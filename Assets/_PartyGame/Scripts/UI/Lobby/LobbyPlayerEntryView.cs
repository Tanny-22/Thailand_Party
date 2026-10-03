using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Lobby
{
    public sealed class LobbyPlayerEntryView : MonoBehaviour
    {
        [SerializeField] Image characterThumbnail;
        [SerializeField] TMP_Text displayNameLabel;
        [SerializeField] GameObject hostBadge;
        [SerializeField] Button kickButton;
        [SerializeField] Button banButton;
        Action kickAction, banAction;

        void Awake() { kickButton?.onClick.AddListener(Kick); banButton?.onClick.AddListener(Ban); }
        void OnDestroy() { kickButton?.onClick.RemoveListener(Kick); banButton?.onClick.RemoveListener(Ban); }

        public void Bind(string displayName, Sprite character, bool isHost, bool canModerate, Action onKick, Action onBan)
        {
            if (displayNameLabel) displayNameLabel.text = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName;
            if (characterThumbnail) { characterThumbnail.sprite = character; characterThumbnail.enabled = character; }
            if (hostBadge) hostBadge.SetActive(isHost);
            if (kickButton) kickButton.gameObject.SetActive(canModerate);
            if (banButton) banButton.gameObject.SetActive(canModerate);
            kickAction = onKick; banAction = onBan;
        }

        void Kick() => kickAction?.Invoke();
        void Ban() => banAction?.Invoke();
    }
}
