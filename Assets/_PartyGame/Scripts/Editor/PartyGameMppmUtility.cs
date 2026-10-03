#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PartyGame.EditorTools
{
    // Editor-only helpers for repeatable Multiplayer Play Mode QA scenarios.
    public static class PartyGameMppmUtility
    {
        const string HostTag = "party-smoke-host";
        const string ClientTag = "party-smoke-client";

        public static void ConfigureTwoPlayerQaScenario(string scenarioTag)
        {
            var type = FindPlaymodeType();
            var playerOne = type?.GetProperty("PlayerOne", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            var playerTwo = type?.GetProperty("PlayerTwo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            if (playerOne == null || playerTwo == null) { Debug.LogError("MPPM players are not initialized."); return; }
            ClearAndAdd(playerOne, HostTag); Add(playerOne, scenarioTag);
            ClearAndAdd(playerTwo, ClientTag); Add(playerTwo, scenarioTag);
            Debug.Log($"MPPM QA scenario configured: {scenarioTag}.");
        }

        [MenuItem("Party Game/Testing/Enable Multiplayer Play Mode")]
        public static void EnableMultiplayerPlayMode()
        {
            var settings = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Unity.Multiplayer.Playmode.Workflow.Editor.MultiplayerPlayModeSettings")).FirstOrDefault(x => x != null);
            settings?.GetMethod("SetIsMppmActive", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(null, new object[] { true });
            Debug.Log("Multiplayer Play Mode enabled.");
        }

        [MenuItem("Party Game/Testing/Configure Two Player Smoke Tags")]
        public static void ConfigureTwoPlayerSmokeTags()
        {
            var type = FindPlaymodeType();
            var playerOne = type?.GetProperty("PlayerOne", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            var playerTwo = type?.GetProperty("PlayerTwo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            if (playerOne == null || playerTwo == null) { Debug.LogError("MPPM players are not initialized."); return; }
            ClearAndAdd(playerOne, HostTag);
            ClearAndAdd(playerTwo, ClientTag);
            Debug.Log("MPPM smoke tags configured: Player 1 = host, Player 2 = client.");
        }

        [MenuItem("Party Game/Testing/Clear Two Player Smoke Tags")]
        public static void ClearTwoPlayerSmokeTags()
        {
            var type = FindPlaymodeType();
            Clear(type?.GetProperty("PlayerOne", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null));
            Clear(type?.GetProperty("PlayerTwo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null));
            Debug.Log("MPPM smoke tags cleared.");
        }

        [MenuItem("Party Game/Testing/Activate Player 2")]
        public static void ActivatePlayerTwo()
        {
            var type = FindPlaymodeType();
            if (type == null) { Debug.LogError("Multiplayer Play Mode API was not found."); return; }
            var player = type.GetProperty("PlayerTwo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            if (player == null) { Debug.LogError("Multiplayer Play Mode Player 2 is not initialized. Open the Multiplayer Play Mode window once and retry."); return; }
            var activate = player.GetType().GetMethod("Activate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { null };
            var activated = activate != null && (bool)activate.Invoke(player, args);
            Debug.Log($"MPPM Player 2 activation: {activated}; result: {args[0] ?? "None"}.");
        }

        [MenuItem("Party Game/Testing/Deactivate Player 2")]
        public static void DeactivatePlayerTwo()
        {
            var type = FindPlaymodeType();
            var player = type?.GetProperty("PlayerTwo", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            var deactivate = player?.GetType().GetMethod("Deactivate", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { null };
            var deactivated = deactivate != null && (bool)deactivate.Invoke(player, args);
            Debug.Log($"MPPM Player 2 deactivation: {deactivated}; result: {args[0] ?? "None"}.");
        }

        static Type FindPlaymodeType() => AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Unity.Multiplayer.Playmode.Workflow.Editor.MultiplayerPlaymode")).FirstOrDefault(x => x != null);
        static void ClearAndAdd(object player, string tag)
        {
            Clear(player);
            Add(player, tag);
        }
        static void Add(object player, string tag)
        {
            var add = player.GetType().GetMethod("AddTag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { tag, null };
            if (add == null || !(bool)add.Invoke(player, args)) Debug.LogError($"Could not add MPPM tag '{tag}': {args[1] ?? "Unknown"}.");
        }
        static void Clear(object player)
        {
            if (player == null) return;
            var clear = player.GetType().GetMethod("ClearTags", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var args = new object[] { null };
            if (clear != null && !(bool)clear.Invoke(player, args)) Debug.LogError($"Could not clear MPPM tags: {args[0] ?? "Unknown"}.");
        }
    }
}
#endif


