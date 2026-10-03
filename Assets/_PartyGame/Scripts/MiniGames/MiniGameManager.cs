using System;
using System.Linq;
using PartyGame.Core;
using PartyGame.Networking;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace PartyGame.MiniGames
{
    public sealed class MiniGameManager : NetworkBehaviour
    {
        [SerializeField] MiniGameRegistry registry;
        public static MiniGameManager Instance { get; private set; }
        public readonly NetworkVariable<FixedString64Bytes> SelectedMiniGameId = new();
        public MiniGameRegistry Registry => registry;
        public event Action<string> SelectionChanged;
        void Awake() { if (Instance && Instance != this) { Destroy(gameObject); return; } Instance = this; }
        public override void OnNetworkSpawn() => SelectedMiniGameId.OnValueChanged += (_, value) => SelectionChanged?.Invoke(value.ToString());
        public string SelectNextServer()
        {
            if (!IsServer) return string.Empty;
            var ids = SessionManager.Instance.ActiveRoom.MiniGameIds.Where(id => registry.Find(id)).ToList();
            if (ids.Count == 0) return string.Empty;
            var id = SessionManager.Instance.ActiveRoom.RandomSelection ? ids[UnityEngine.Random.Range(0, ids.Count)] : ids[0]; SelectedMiniGameId.Value = id; return id;
        }
        public bool LoadSelectedServer() { var definition = registry.Find(SelectedMiniGameId.Value.ToString()); return definition && SceneFlowManager.Instance.LoadNetworked(definition.SceneName); }
    }
}
