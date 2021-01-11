using BeatSaverSharp;
using MultiplayerExtensions.Beatmaps;
using MultiplayerExtensions.HarmonyPatches;
using MultiplayerExtensions.Packets;
using MultiplayerExtensions.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Zenject;

namespace MultiplayerExtensions.OverrideClasses
{
    class PlayersDataModelStub : LobbyPlayersDataModel, ILobbyPlayersDataModel
    {
        [Inject]
        protected readonly SessionManager _sessionManager;

        [Inject]
        protected readonly PacketManager _packetManager;

        public PlayersDataModelStub() { }

        private List<string> vanillaLevelIds = new List<string>();

        public new void Activate()
        {
            _packetManager.RegisterCallback<PreviewBeatmapPacket>(HandlePreviewBeatmapPacket);
            _packetManager.RegisterCallback<QuickplayBeatmapPacket>(HandleQuickplayBeatmapPacket);
            _sessionManager.playerStateChangedEvent += HandlePlayerStateChanged;
            base.Activate();

            _menuRpcManager.selectedBeatmapEvent -= base.HandleMenuRpcManagerSelectedBeatmap;
            _menuRpcManager.selectedBeatmapEvent += this.HandleMenuRpcManagerSelectedBeatmap;

            _menuRpcManager.getSelectedBeatmapEvent -= base.HandleMenuRpcManagerGetSelectedBeatmap;
            _menuRpcManager.getSelectedBeatmapEvent += this.HandleMenuRpcManagerGetSelectedBeatmap;

            vanillaLevelIds = (from pack in _beatmapLevelsModel.ostAndExtrasPackCollection.beatmapLevelPacks
                               from level in pack.beatmapLevelCollection.beatmapLevels
                               select level.levelID).ToList();
        }

        private void HandlePlayerStateChanged(IConnectedPlayer player)
        {
            if (player.HasState("beatmap_downloaded"))
            {
                this.NotifyModelChange(player.userId);
            }
        }

        public void HandlePreviewBeatmapPacket(PreviewBeatmapPacket packet, IConnectedPlayer player)
        {
            string? hash = Utilities.Utils.LevelIdToHash(packet.levelId);
            if (hash != null)
            {
                Plugin.Log?.Debug($"'{player.userId}' selected song '{hash}'.");
                BeatmapCharacteristicSO characteristic = _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(packet.characteristic);
                PreviewBeatmapStub preview = new PreviewBeatmapStub(packet);
                HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(player.userId, preview, packet.difficulty, characteristic));
            }
        }

        public void HandleQuickplayBeatmapPacket(QuickplayBeatmapPacket packet, IConnectedPlayer player)
        {
            string? hash = Utilities.Utils.LevelIdToHash(packet.levelId);
            Plugin.Log?.Debug($"'{player.userId}' selected song '{hash ?? packet.levelId}' spoofed with '{packet.spoofedLevelId}'.");
            BeatmapCharacteristicSO characteristic = _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(packet.characteristic);
            QuickplayBeatmapStub preview = new QuickplayBeatmapStub(packet);
            HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(player.userId, preview, packet.difficulty, characteristic));
        }

        public async override void HandleMenuRpcManagerGetSelectedBeatmap(string userId)
        {
            Plugin.Log.Debug($"'{userId}' wants to know what beatmap is selected");
            ILobbyPlayerDataModel lobbyPlayerDataModel = this.GetLobbyPlayerDataModel(this.localUserId);
            if (lobbyPlayerDataModel?.beatmapLevel != null) {
                string characteristic = lobbyPlayerDataModel.beatmapCharacteristic.serializedName;
                BeatmapDifficulty difficulty = lobbyPlayerDataModel.beatmapDifficulty;
                if (lobbyPlayerDataModel.beatmapLevel is QuickplayBeatmapStub quickplayBeatmap)
                {
                    Plugin.Log.Debug($"Responding with spoofed id '{quickplayBeatmap.spoofedLevelID}'");
                    _menuRpcManager.SelectBeatmap(new BeatmapIdentifierNetSerializable(quickplayBeatmap.spoofedLevelID, characteristic, difficulty));
                    //_packetManager.Send(await QuickplayBeatmapPacket.FromPreview(quickplayBeatmap, characteristic, difficulty));
                }
                else
                {
                    Plugin.Log.Debug($"Responding with levelid '{lobbyPlayerDataModel.beatmapLevel.levelID}'");
                    _menuRpcManager.SelectBeatmap(new BeatmapIdentifierNetSerializable(lobbyPlayerDataModel.beatmapLevel.levelID, characteristic, difficulty));
                    //if (lobbyPlayerDataModel.beatmapLevel is PreviewBeatmapStub previewBeatmap)
                        //_packetManager.Send(await PreviewBeatmapPacket.FromPreview(previewBeatmap, characteristic, difficulty));
                }
            }
        }

        public async override void HandleMenuRpcManagerSelectedBeatmap(string userId, BeatmapIdentifierNetSerializable beatmapId)
        {
            BeatmapCharacteristicSO characteristic = _beatmapCharacteristicCollection.GetBeatmapCharacteristicBySerializedName(beatmapId.beatmapCharacteristicSerializedName);

            if (!LobbyJoinPatch.IsPrivate && Plugin.Config.CustomMatchmake)
            {
                if (beatmapId != null)
                {
                    string? hash = Utilities.Utils.LevelIdToHash(beatmapId.levelID);
                    if (hash != null)
                    {
                        Plugin.Log?.Warn($"'{userId}': Custom song should not have been sent with vanilla packet.");
                    }
                    else
                    {
                        if (userId == hostUserId)
                        {
                            Plugin.Log.Info($"Host server seleted beatmap '{beatmapId.levelID}'");
                            ILobbyPlayerDataModel playerData = playersData.Values.ToList().Find(x => x.beatmapLevel is QuickplayBeatmapStub qpPreview && qpPreview.spoofedLevelID == beatmapId.levelID);
                            if (playerData != null)
                                HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(userId, playerData.beatmapLevel, beatmapId.difficulty, characteristic));
                            else
                                base.HandleMenuRpcManagerSelectedBeatmap(userId, beatmapId);
                        }
                    }
                }
                else
                {
                    base.HandleMenuRpcManagerSelectedBeatmap(userId, beatmapId);
                }
                return;
            }

            if (beatmapId != null)
            {
                string? hash = Utilities.Utils.LevelIdToHash(beatmapId.levelID);
                if (hash != null)
                {
                    Plugin.Log?.Debug($"'{userId}' selected song '{hash}'.");
                    PreviewBeatmapStub? preview = null;

                    if (_playersData.Values.Any(playerData => playerData.beatmapLevel?.levelID == beatmapId.levelID))
                    {
                        IPreviewBeatmapLevel playerPreview = _playersData.Values.Where(playerData => playerData.beatmapLevel?.levelID == beatmapId.levelID).First().beatmapLevel;
                        if (playerPreview is PreviewBeatmapStub playerPreviewStub)
                            preview = playerPreviewStub;
                    }

                    if (preview == null)
                    {
                        IPreviewBeatmapLevel localPreview = SongCore.Loader.GetLevelById(beatmapId.levelID);
                        if (localPreview != null)
                            preview = new PreviewBeatmapStub(hash, localPreview);
                    }

                    if (preview == null)
                    {
                        try
                        {
                            Beatmap bm = await Plugin.BeatSaver.Hash(hash);
                            preview = new PreviewBeatmapStub(bm);
                        }
                        catch
                        {
                            Plugin.Log.Error($"Beatsaver metadata fetch failed.");
                            return;
                        }
                    }

                    if (userId == base.hostUserId)
                        _sessionManager.SetLocalPlayerState("beatmap_downloaded", preview.isDownloaded);

                    HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(userId, preview, beatmapId.difficulty, characteristic));
                    return;
                }
            }

            base.HandleMenuRpcManagerSelectedBeatmap(userId, beatmapId);
        }

        public async new void SetLocalPlayerBeatmapLevel(string levelId, BeatmapDifficulty beatmapDifficulty, BeatmapCharacteristicSO characteristic)
        {
            string? hash = Utilities.Utils.LevelIdToHash(levelId);
            if (!LobbyJoinPatch.IsPrivate && Plugin.Config.CustomMatchmake)
            {
                Plugin.Log?.Debug($"Local user selected song '{hash ?? levelId}'.");
                QuickplayBeatmapStub? preview = null;

                if (_playersData.Values.Any(playerData => playerData.beatmapLevel?.levelID == levelId))
                {
                    IPreviewBeatmapLevel playerPreview = _playersData.Values.Where(playerData => playerData.beatmapLevel?.levelID == levelId).First().beatmapLevel;
                    if (playerPreview is QuickplayBeatmapStub playerPreviewStub)
                        preview = playerPreviewStub;
                }

                if (preview == null)
                {
                    IPreviewBeatmapLevel localPreview = hash != null ? SongCore.Loader.GetLevelById(levelId) : _beatmapLevelsModel.GetLevelPreviewForLevelId(levelId);
                    if (localPreview != null)
                        preview = new QuickplayBeatmapStub(hash ?? "", GetSpoofedLevelId(), localPreview);
                }

                if (preview == null && hash != null)
                {
                    try
                    {
                        Beatmap bm = await Plugin.BeatSaver.Hash(hash);
                        preview = new QuickplayBeatmapStub(bm, GetSpoofedLevelId());
                    }
                    catch
                    {
                        return;
                    }
                }

                _sessionManager.SetLocalPlayerState("beatmap_downloaded", preview.isDownloaded);

                HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(base.localUserId, preview, beatmapDifficulty, characteristic));
                _packetManager.Send(await QuickplayBeatmapPacket.FromPreview(preview, characteristic.serializedName, beatmapDifficulty));
                _menuRpcManager.SelectBeatmap(new BeatmapIdentifierNetSerializable(preview.spoofedLevelID, characteristic.serializedName, beatmapDifficulty));

                return;
            }

            if (hash != null)
            {
                Plugin.Log?.Debug($"Local user selected song '{hash}'.");
                PreviewBeatmapStub? preview = null;

                if (_playersData.Values.Any(playerData => playerData.beatmapLevel?.levelID == levelId))
                {
                    IPreviewBeatmapLevel playerPreview = _playersData.Values.Where(playerData => playerData.beatmapLevel?.levelID == levelId).First().beatmapLevel;
                    if (playerPreview is PreviewBeatmapStub playerPreviewStub)
                        preview = playerPreviewStub;
                }

                IPreviewBeatmapLevel localPreview = SongCore.Loader.GetLevelById(levelId);
                if (localPreview != null)
                    preview = new PreviewBeatmapStub(hash, localPreview);

                if (preview == null)
                {
                    try
                    {
                        Beatmap bm = await Plugin.BeatSaver.Hash(hash);
                        preview = new PreviewBeatmapStub(bm);
                    }
                    catch
                    {
                        return;
                    }
                }

                if (base.localUserId == base.hostUserId)
                    _sessionManager.SetLocalPlayerState("beatmap_downloaded", preview.isDownloaded);

                HMMainThreadDispatcher.instance.Enqueue(() => base.SetPlayerBeatmapLevel(base.localUserId, preview, beatmapDifficulty, characteristic));
                _packetManager.Send(await PreviewBeatmapPacket.FromPreview(preview, characteristic.serializedName, beatmapDifficulty));
                if (!_sessionManager.connectedPlayers.All(x => x.HasState("modded")))
                    _menuRpcManager.SelectBeatmap(new BeatmapIdentifierNetSerializable(levelId, characteristic.serializedName, beatmapDifficulty));

            }else
                base.SetLocalPlayerBeatmapLevel(levelId, beatmapDifficulty, characteristic);
        }

        public string GetSpoofedLevelId() 
        {
            string spoofedId = vanillaLevelIds.Find(vanillaLevelId => !playersData.Values.Any(playerDataModel =>
            {
                IPreviewBeatmapLevel playerPreview = playerDataModel.beatmapLevel;
                if (playerPreview is QuickplayBeatmapStub preview)
                    return preview.spoofedLevelID == vanillaLevelId;
                return false;
            }));

            Plugin.Log.Info($"Spoofing level id with '{spoofedId}'");
            return spoofedId;
        }
    }
}
