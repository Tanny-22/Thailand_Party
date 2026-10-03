using UnityEngine;

namespace PartyGame.Core
{
    [CreateAssetMenu(menuName = "Party Game/Game Config", fileName = "GameConfig")]
    public sealed class GameConfig : ScriptableObject
    {
        [Header("Rooms")]
        [SerializeField, Range(2, 10)] int defaultRoomCapacity = 4;
        [SerializeField, Range(2, 10)] int minimumPlayersToStart = 2;
        [SerializeField, Min(1)] int defaultWinsRequired = 3;
        [SerializeField, Min(1)] int maximumWinsRequired = 10;
        [SerializeField, Range(3, 32)] int maximumRoomNameLength = 24;
        [SerializeField, Range(3, 24)] int maximumDisplayNameLength = 16;
        [Header("Flow")]
        [SerializeField] string mainMenuScene = "01_MainMenu";
        [SerializeField] string lobbyScene = "02_Lobby";
        [SerializeField] string randomizerScene = "03_MiniGameRandomizer";
        [SerializeField] string resultsScene = "04_Results";
        [SerializeField] bool verboseDevelopmentLogs = true;

        public int DefaultRoomCapacity => defaultRoomCapacity;
        public int MinimumPlayersToStart => minimumPlayersToStart;
        public int DefaultWinsRequired => defaultWinsRequired;
        public int MaximumWinsRequired => maximumWinsRequired;
        public int MaximumRoomNameLength => maximumRoomNameLength;
        public int MaximumDisplayNameLength => maximumDisplayNameLength;
        public string MainMenuScene => mainMenuScene;
        public string LobbyScene => lobbyScene;
        public string RandomizerScene => randomizerScene;
        public string ResultsScene => resultsScene;
        public bool VerboseDevelopmentLogs => verboseDevelopmentLogs;
    }
}
