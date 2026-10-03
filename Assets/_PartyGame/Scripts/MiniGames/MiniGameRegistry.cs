using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PartyGame.MiniGames
{
    [CreateAssetMenu(menuName = "Party Game/Mini Game Registry", fileName = "MiniGameRegistry")]
    public sealed class MiniGameRegistry : ScriptableObject
    {
        [SerializeField] List<MiniGameDefinition> entries = new();
        public IReadOnlyList<MiniGameDefinition> Entries => entries;
        public MiniGameDefinition Find(string id) => entries.FirstOrDefault(x => x && x.Id == id);
        public IEnumerable<MiniGameDefinition> Enabled => entries.Where(x => x && x.EnabledForSelection);
    }
}
