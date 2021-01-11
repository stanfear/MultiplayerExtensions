using MultiplayerExtensions.Beatmaps;
using MultiplayerExtensions.Packets;
using MultiplayerExtensions.Sessions;
using MultiplayerExtensions.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace MultiplayerExtensions.OverrideClasses
{
    class GameStateControllerStub : LobbyGameStateController, ILobbyHostGameStateController, ILobbyGameStateController, IDisposable
    {
        [Inject]
        protected readonly SessionManager _sessionManager;

        [Inject]
        protected readonly PacketManager _packetManager;

        public new void Activate()
        {
            _sessionManager.playerStateChangedEvent += OnPlayerStateChanged;
            base.Activate();
        }

        public new void Deactivate()
        {
            _sessionManager.playerStateChangedEvent -= OnPlayerStateChanged;
            _menuRpcManager.startedLevelEvent -= HandleRpcStartedLevel;
            _menuRpcManager.cancelledLevelStartEvent -= HandleRpcCancelledLevel;
            base.Deactivate();
        }

        public new void StartListeningToGameStart()
        {
            base.StartListeningToGameStart();
            _menuRpcManager.startedLevelEvent -= HandleRpcStartedLevel;
            _menuRpcManager.startedLevelEvent += HandleRpcStartedLevel;
            _menuRpcManager.startedLevelEvent -= base.HandleMenuRpcManagerStartedLevel;
            _menuRpcManager.cancelledLevelStartEvent -= HandleRpcCancelledLevel;
            _menuRpcManager.cancelledLevelStartEvent += HandleRpcCancelledLevel;
            _menuRpcManager.cancelledLevelStartEvent -= base.HandleMenuRpcManagerCancelledLevelStart;
        }

        public override void StopListeningToGameStart()
        {
            _menuRpcManager.startedLevelEvent -= HandleRpcStartedLevel;
            base.StopListeningToGameStart();
        }

        private void OnPlayerStateChanged(IConnectedPlayer player)
        {
            if (starting)
            {
                if (player.HasState("start_primed"))
                {
                    Plugin.Log.Debug($"Player {player.userId} is ready.");
                }

                if (_sessionManager.connectedPlayers.All((x) => x.HasState("start_primed") || (!x.HasState("modded") && x.HasState("is_active") || !x.HasState("player") || x.HasState("dedicated_server"))) && _sessionManager.LocalPlayerHasState("start_primed"))
                {
                    Plugin.Log.Debug("All players ready, starting game.");
                    StartLevel();
                }
            }
        }

        public new void StartGame()
        {
            _sessionManager.SetLocalPlayerState("start_primed", false);
            starting = true;
            base.StartGame();
            _multiplayerLevelLoader.countdownFinishedEvent -= base.HandleMultiplayerLevelLoaderCountdownFinished;
            _multiplayerLevelLoader.countdownFinishedEvent += HandleCountdown;
        }

        public new void CancelGame()
        {
            starting = false;
            _sessionManager.SetLocalPlayerState("start_primed", false);
            _multiplayerLevelLoader.countdownFinishedEvent -= HandleCountdown;
            _multiplayerLevelLoader.countdownFinishedEvent += base.HandleMultiplayerLevelLoaderCountdownFinished;
            base.CancelGame();
        }

        public void HandleRpcStartedLevel(string userId, BeatmapIdentifierNetSerializable beatmapId, GameplayModifiers gameplayModifiers, float startTime)
        {
            
            _sessionManager.SetLocalPlayerState("start_primed", false);
            starting = true;

            BeatmapIdentifierNetSerializable bmId = beatmapId;
            if (Plugin.Config.CustomMatchmake)
            {
                ILobbyPlayerDataModel playerData = _lobbyPlayersDataModel.playersData.Values.ToList().Find(x => x.beatmapLevel is QuickplayBeatmapStub qpPreview && qpPreview.spoofedLevelID == beatmapId.levelID);
                if (playerData != null)
                    bmId = new BeatmapIdentifierNetSerializable(playerData.beatmapLevel.levelID, beatmapId.beatmapCharacteristicSerializedName, beatmapId.difficulty);
                Plugin.Log.Info($"Swapped starting level ID with '{playerData.beatmapLevel.levelID}'");
            }
            Plugin.Log.Info(bmId.levelID);

            base.HandleMenuRpcManagerStartedLevel(userId, bmId, gameplayModifiers, startTime);
            _multiplayerLevelLoader.countdownFinishedEvent -= base.HandleMultiplayerLevelLoaderCountdownFinished;
            _multiplayerLevelLoader.countdownFinishedEvent += HandleCountdown;
        }

        public void HandleRpcCancelledLevel(string userId)
        {
            starting = false;
            _sessionManager.SetLocalPlayerState("start_primed", false);
            _multiplayerLevelLoader.countdownFinishedEvent -= HandleCountdown;
            _multiplayerLevelLoader.countdownFinishedEvent += base.HandleMultiplayerLevelLoaderCountdownFinished;
            base.HandleMenuRpcManagerCancelledLevelStart(userId);
        }

        public void HandleCountdown(IPreviewBeatmapLevel previewBeatmapLevel, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristicSO beatmapCharacteristic, IDifficultyBeatmap difficultyBeatmap, GameplayModifiers gameplayModifiers)
        {
            Plugin.Log?.Debug("Map finished loading, waiting for other players...");

            this.previewBeatmapLevel = previewBeatmapLevel;
            this.beatmapDifficulty = beatmapDifficulty;
            this.beatmapCharacteristic = beatmapCharacteristic;
            this.difficultyBeatmap = difficultyBeatmap;
            this.gameplayModifiers = gameplayModifiers;

            _sessionManager.SetLocalPlayerState("start_primed", true);
            if (this._levelStartedOnTime && difficultyBeatmap != null && this._multiplayerSessionManager.localPlayer.WantsToPlayNextLevel())
            {
                OnPlayerStateChanged(_sessionManager.localPlayer);
            }
            else
            {
                StartLevel();
            }
        }

        public void StartLevel()
        {
            starting = false;

            Plugin.Log.Info($"'{nameof(previewBeatmapLevel)}' is {(previewBeatmapLevel == null ? "null" : "not null")}");
            Plugin.Log.Info($"'{nameof(beatmapDifficulty)}' is {(beatmapDifficulty == null ? "null" : "not null")}");
            Plugin.Log.Info($"'{nameof(beatmapCharacteristic)}' is {(beatmapCharacteristic == null ? "null" : "not null")}");
            Plugin.Log.Info($"'{nameof(difficultyBeatmap)}' is {(difficultyBeatmap == null ? "null" : "not null")}");
            Plugin.Log.Info($"'{nameof(gameplayModifiers)}' is {(gameplayModifiers == null ? "null" : "not null")}");

            base.HandleMultiplayerLevelLoaderCountdownFinished(previewBeatmapLevel, beatmapDifficulty, beatmapCharacteristic, difficultyBeatmap, gameplayModifiers);
        }

        private bool starting;

        private IPreviewBeatmapLevel previewBeatmapLevel;
        private BeatmapDifficulty beatmapDifficulty;
        private BeatmapCharacteristicSO beatmapCharacteristic;
        private IDifficultyBeatmap difficultyBeatmap;
        private GameplayModifiers gameplayModifiers;
    }
}
