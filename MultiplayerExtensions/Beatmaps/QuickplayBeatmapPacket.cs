using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MultiplayerExtensions.Beatmaps
{
    class QuickplayBeatmapPacket : PreviewBeatmapPacket, INetSerializable, IPoolablePacket
    {
        public string spoofedLevelId;

        public new void Serialize(NetDataWriter writer)
        {
            base.Serialize(writer);
            writer.Put(spoofedLevelId);
        }

        public new void Deserialize(NetDataReader reader)
        {
            base.Deserialize(reader);
            this.spoofedLevelId = reader.GetString();
        }

        static async public Task<QuickplayBeatmapPacket> FromPreview(QuickplayBeatmapStub preview, string characteristic, BeatmapDifficulty difficulty)
        {
            QuickplayBeatmapPacket packet = new QuickplayBeatmapPacket();

            packet.levelId = preview.levelID;
            packet.songName = preview.songName;
            packet.songSubName = preview.songSubName;
            packet.songAuthorName = preview.songAuthorName;
            packet.levelAuthorName = preview.levelAuthorName;
            packet.beatsPerMinute = preview.beatsPerMinute;
            packet.songDuration = preview.songDuration;

            packet.coverImage = await preview.GetRawCoverAsync(CancellationToken.None);

            packet.characteristic = characteristic;
            packet.difficulty = difficulty;

            packet.spoofedLevelId = preview.spoofedLevelID;

            return packet;
        }

        public new void Release()
        {
            ThreadStaticPacketPool<QuickplayBeatmapPacket>.pool.Release(this);
        }
    }
}
