using System.Linq;
using PartyGame.Networking;
using Unity.Collections;
using UnityEngine;

namespace PartyGame.Players
{
    /// <summary>
    /// Instantiates a local presentation prefab for the character ID already
    /// synchronized by NetworkPlayer. The visual itself is intentionally not a
    /// NetworkObject; every client derives the same presentation from CharacterId.
    /// </summary>
    public sealed class NetworkPlayerCharacterVisual : MonoBehaviour
    {
        [SerializeField] NetworkPlayer networkPlayer;
        [SerializeField] CharacterRegistry registry;
        [SerializeField] Transform visualRoot;

        GameObject activeVisual;

        void Awake()
        {
            if (!networkPlayer) networkPlayer = GetComponent<NetworkPlayer>();
            if (!visualRoot) visualRoot = transform;
        }

        void OnEnable()
        {
            if (!networkPlayer) return;
            networkPlayer.CharacterId.OnValueChanged += OnCharacterChanged;
            Refresh(networkPlayer.CharacterId.Value.ToString());
        }

        void OnDisable()
        {
            if (networkPlayer)
                networkPlayer.CharacterId.OnValueChanged -= OnCharacterChanged;
        }

        void OnCharacterChanged(FixedString64Bytes previous, FixedString64Bytes current) =>
            Refresh(current.ToString());

        void Refresh(string characterId)
        {
            if (activeVisual) Destroy(activeVisual);
            activeVisual = null;

            if (!registry) return;
            var definition = registry.Find(characterId) ?? registry.Enabled.FirstOrDefault();
            if (!definition || !definition.CharacterPrefab) return;

            activeVisual = Instantiate(definition.CharacterPrefab, visualRoot, false);
            activeVisual.name = $"CharacterVisual_{definition.Id}";
            activeVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            activeVisual.transform.localScale = Vector3.one;
        }
    }
}
