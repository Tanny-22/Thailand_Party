using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Results
{
    public sealed class ResultsPlayerEntryView : MonoBehaviour
    {
        [SerializeField] Image characterThumbnail;
        [SerializeField] TMP_Text displayNameLabel;
        [SerializeField] TMP_Text winsLabel;
        [SerializeField] GameObject winnerHighlight;

        public void Bind(string displayName, Sprite character, int wins, bool lastWinner)
        {
            if (displayNameLabel) displayNameLabel.text = displayName;
            if (winsLabel) winsLabel.text = $"{wins} WIN{(wins == 1 ? string.Empty : "S")}";
            if (characterThumbnail) { characterThumbnail.sprite = character; characterThumbnail.enabled = character; }
            if (winnerHighlight) winnerHighlight.SetActive(lastWinner);
        }
    }
}
