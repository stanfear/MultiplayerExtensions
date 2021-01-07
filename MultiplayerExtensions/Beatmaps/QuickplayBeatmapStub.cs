using BeatSaverSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerExtensions.Beatmaps
{
    class QuickplayBeatmapStub : PreviewBeatmapStub
    {
        public string spoofedLevelID { get; private set; }

        public QuickplayBeatmapStub(string levelHash, string spoofedLevelID, IPreviewBeatmapLevel preview) : base(levelHash, preview)
        {
            this.spoofedLevelID = spoofedLevelID;
            base._downloadable = DownloadableState.True;
        }

        public QuickplayBeatmapStub(QuickplayBeatmapPacket packet) : base(packet)
        {
            bool isCustom = Utilities.Utils.LevelIdToHash(packet.levelId) != null;
            this.spoofedLevelID = packet.spoofedLevelId;
            base.isDownloaded = !isCustom;
            base._downloadable = !isCustom ? DownloadableState.True : DownloadableState.Unchecked;
        }

        public QuickplayBeatmapStub(Beatmap bm, string spoofedLevelID) : base(bm)
            => this.spoofedLevelID = spoofedLevelID;
    }
}
