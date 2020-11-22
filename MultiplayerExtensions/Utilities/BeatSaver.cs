using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MultiplayerExtensions.Utilities
{
    public static class BeatSaver
    {
        public static readonly string BeatSaverBaseUrl = "https://beatsaver.com";
        public static readonly string BeatSaverDetailsFromKeyBaseUrl = $"{BeatSaverBaseUrl}/api/maps/detail/";
        public static readonly string BeatSaverDetailsFromHashBaseUrl = $"{BeatSaverBaseUrl}/api/maps/by-hash/";
        private static Uri GetBeatSaverDetailsByKey(string key)
        {
            return new Uri(BeatSaverDetailsFromKeyBaseUrl + key.ToLower());
        }

        private static Uri GetBeatSaverDetailsByHash(string hash)
        {
            return new Uri(BeatSaverDetailsFromHashBaseUrl + hash.ToLower());
        }



    }
}
