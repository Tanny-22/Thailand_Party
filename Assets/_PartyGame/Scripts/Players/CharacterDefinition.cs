using UnityEngine;

namespace PartyGame.Players
{
    [CreateAssetMenu(menuName = "Party Game/Character Definition", fileName = "Character_")]
    public sealed class CharacterDefinition : ScriptableObject
    {
        [SerializeField] string id = "default";
        [SerializeField] string displayName = "Default Character";
        [SerializeField] Sprite thumbnail;
        [SerializeField] GameObject characterPrefab;
        [SerializeField] bool enabledForSelection = true;
        public string Id => id;
        public string DisplayName => displayName;
        public Sprite Thumbnail => thumbnail;
        public GameObject CharacterPrefab => characterPrefab;
        public bool EnabledForSelection => enabledForSelection;
    }
}
