using System.Collections;
using System.Linq;
using PartyGame.MiniGames;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PartyGame.UI.Randomizer
{
    public sealed class MiniGameRandomizerUI : MonoBehaviour
    {
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text countdown;
        [SerializeField] Image thumbnail;
        [SerializeField, Min(1f)] float revealSeconds = 4f;
        IEnumerator Start()
        {
            var manager = MiniGameManager.Instance;
            if (!manager) yield break;
            if (manager.IsServer && string.IsNullOrEmpty(manager.SelectedMiniGameId.Value.ToString())) manager.SelectNextServer();
            while (string.IsNullOrEmpty(manager.SelectedMiniGameId.Value.ToString())) yield return null;
            var finalId = manager.SelectedMiniGameId.Value.ToString();
            var pool = PartyGame.Networking.SessionManager.Instance.ActiveRoom.MiniGameIds.Select(manager.Registry.Find).Where(x => x).ToList();
            var elapsed = 0f; var step = 0;
            while (elapsed < revealSeconds)
            {
                var definition = pool.Count == 0 ? manager.Registry.Find(finalId) : pool[step++ % pool.Count];
                Show(definition, "CHOOSING...");
                var normalized = elapsed / revealSeconds; var delay = Mathf.Lerp(.08f, .55f, normalized * normalized);
                yield return new WaitForSecondsRealtime(delay); elapsed += delay;
            }
            var selected = manager.Registry.Find(finalId); Show(selected, selected ? selected.DisplayName : finalId);
            for (var i = 3; i > 0; i--) { if (countdown) countdown.text = $"Starting in {i}"; yield return new WaitForSecondsRealtime(1f); }
            if (countdown) countdown.text = "GO!";
            if (manager.IsServer) manager.LoadSelectedServer();
        }
        void Show(MiniGameDefinition definition, string text)
        {
            if (title) title.text = text;
            if (thumbnail) { thumbnail.sprite = definition ? definition.Thumbnail : null; thumbnail.enabled = definition && definition.Thumbnail; }
        }
    }
}
