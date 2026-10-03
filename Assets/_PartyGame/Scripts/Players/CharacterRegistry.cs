using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PartyGame.Players
{
    [CreateAssetMenu(menuName = "Party Game/Character Registry", fileName = "CharacterRegistry")]
    public sealed class CharacterRegistry : ScriptableObject
    {
        [SerializeField] List<CharacterDefinition> entries = new();
        public IReadOnlyList<CharacterDefinition> Entries => entries;
        public CharacterDefinition Find(string id) => entries.FirstOrDefault(x => x && x.Id == id);
        public IEnumerable<CharacterDefinition> Enabled => entries.Where(x => x && x.EnabledForSelection);
    }
}
