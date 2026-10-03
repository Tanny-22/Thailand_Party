using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Authentication;
using PartyGame.Persistence;

namespace PartyGame.Networking
{
    public sealed class NetworkPlayer : NetworkBehaviour
    {
        public readonly NetworkVariable<FixedString64Bytes> DisplayName = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<FixedString64Bytes> CharacterId = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<FixedString128Bytes> AuthenticationId = new(writePerm: NetworkVariableWritePermission.Server);
        public readonly NetworkVariable<int> Wins = new(writePerm: NetworkVariableWritePermission.Server);
        public override void OnNetworkSpawn()
        {
            if (IsOwner) SubmitProfileServerRpc(SaveManager.Instance.PlayerName, SaveManager.Instance.CharacterId, AuthenticationService.Instance.PlayerId);
        }
        [ServerRpc]
        void SubmitProfileServerRpc(string displayName, string characterId, string authenticationId)
        {
            DisplayName.Value = string.IsNullOrWhiteSpace(displayName) ? "Player" : displayName.Trim();
            CharacterId.Value = string.IsNullOrWhiteSpace(characterId) ? "default" : characterId;
            AuthenticationId.Value = authenticationId ?? string.Empty;
        }
        [ServerRpc] public void RequestProfileChangeServerRpc(string displayName, string characterId) => SubmitProfileServerRpc(displayName, characterId, AuthenticationId.Value.ToString());
        public void AwardWinServer() { if (IsServer) Wins.Value++; }
    }
}
