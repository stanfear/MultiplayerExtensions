using HarmonyLib;
using IPA.Utilities;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using Polyglot;
using MultiplayerExtensions.Beatmaps;
#if DEBUG
using System.Collections.Generic;
#endif
/// <summary>
/// See https://github.com/pardeike/Harmony/wiki for a full reference on Harmony.
/// </summary>
namespace MultiplayerExtensions.HarmonyPatches
{
#if DEBUG
    [HarmonyPatch(typeof(SongPackMasksModel), MethodType.Constructor,
        new Type[] { // List the Types of the method's parameters.
        typeof(BeatmapLevelsModel) })]
    public class SongPackMasksModel_Constructor
    {
        /// <summary>
        /// Adds a level pack selection to Quick Play's picker. Unfortunately, the server doesn't allow custom songs to be played in Quick Play.
        /// Left here for testing.
        /// </summary>
        static void Postfix(SongPackMasksModel __instance, ref BeatmapLevelsModel beatmapLevelsModel, ref List<Tuple<SongPackMask, string>> ____songPackMaskData)
        {
            SongPackMask customs = new SongPackMask("custom_levelpack_CustomLevels");
            ____songPackMaskData.Add(customs, "Custom");
        }
    }
#endif

    [HarmonyPatch(typeof(MultiplayerLevelSelectionFlowCoordinator), "enableCustomLevels", MethodType.Getter)]
    public class EnableCustomLevelsPatch
    {
        /// <summary>
        /// Overrides getter for <see cref="MultiplayerLevelSelectionFlowCoordinator.enableCustomLevels"/>
        /// </summary>
        static bool Prefix(ref bool __result)
        {
            __result = (Plugin.Config.CustomSongs && LobbyJoinPatch.IsPrivate) || (Plugin.Config.CustomMatchmake && !LobbyJoinPatch.IsPrivate);
            Plugin.Log?.Debug($"CustomLevels are {(__result ? "enabled" : "disabled")}.");
            return false;
        }
    }

    [HarmonyPatch(typeof(JoinQuickPlayViewController), "multiplayerModeSettings", MethodType.Getter)]
    public class EnableCustomMatchmakingPatch
    {
        private static BeatmapLevelsModel? beatmapLevelsModel;
        public static SongPackMask customSongsMask;

        /// <summary>
        /// Overrides getter for <see cref="JoinQuickPlayViewController.multiplayerModeSettings"/>
        /// </summary>
        static void Postfix(ref MultiplayerModeSettings __result)
        {
            bool isCustom = Plugin.Config.CustomMatchmake;
            if (isCustom)
            {
                if (beatmapLevelsModel == null)
                    beatmapLevelsModel = Resources.FindObjectsOfTypeAll<BeatmapLevelsModel>().First();
                if (customSongsMask == new SongPackMask())
                    customSongsMask = new SongPackMask((from pack in beatmapLevelsModel.customLevelPackCollection.beatmapLevelPacks select pack.packID).ToBloomFilter());

                __result.quickPlaySongPackMask = __result.quickPlaySongPackMask | customSongsMask;
                if (Plugin.Config.AllDifficulties)
                    __result.quickPlayBeatmapDifficulty = BeatmapDifficultyMask.All;
            }
        }
    }

    [HarmonyPatch(typeof(MultiplayerLobbyConnectionController), "connectionType", MethodType.Setter)]
    class LobbyJoinPatch
    {
        public static MultiplayerLobbyConnectionController.LobbyConnectionType ConnectionType;

        public static bool IsPrivate { get { return ConnectionType != MultiplayerLobbyConnectionController.LobbyConnectionType.QuickPlay || false; } }
        public static bool IsHost { get { return ConnectionType == MultiplayerLobbyConnectionController.LobbyConnectionType.PartyHost || false; } }
        public static bool IsMultiplayer { get { return ConnectionType != MultiplayerLobbyConnectionController.LobbyConnectionType.None || false; } }

        /// <summary>
        /// Gets the current lobby type.
        /// </summary>
        static void Prefix(MultiplayerLobbyConnectionController __instance)
        {
            ConnectionType = __instance.GetProperty<MultiplayerLobbyConnectionController.LobbyConnectionType, MultiplayerLobbyConnectionController>("connectionType");
            Plugin.Log?.Debug($"Joining a {ConnectionType} lobby.");
        }
    }

    [HarmonyPatch(typeof(CenterStageScreenController), "SetHostDataManual", MethodType.Normal)]
    internal class CenterStageSetDataPatch
    {
        internal static FieldAccessor<CenterStageScreenController, ILobbyPlayersDataModel>.Accessor _lobbyPlayersDataModel = FieldAccessor<CenterStageScreenController, ILobbyPlayersDataModel>.GetAccessor("_lobbyPlayersDataModel");

        static void Prefix(CenterStageScreenController __instance, ref IPreviewBeatmapLevel previewBeatmapLevel)
        {
            if (!LobbyJoinPatch.IsPrivate && Plugin.Config.CustomMatchmake)
            {
                var levelId = previewBeatmapLevel.levelID;
                ILobbyPlayerDataModel playerData = _lobbyPlayersDataModel(ref __instance).playersData.Values.ToList().Find(x => x.beatmapLevel is QuickplayBeatmapStub qpPreview && qpPreview.spoofedLevelID == levelId);
                if (playerData != null)
                    previewBeatmapLevel = playerData.beatmapLevel;
            }
        }
    }
}
