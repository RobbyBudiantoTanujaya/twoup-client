using System;
using System.Linq;
using Google.Protobuf;
using Twoup.V1;

namespace TwoUp
{
    /// <summary>
    /// Session data for the current match, filled from MatchFound in the lobby and
    /// consumed by the game scene. Cleared when leaving a match or disconnecting.
    /// </summary>
    public static class MatchContext
    {
        public static string MatchId;
        public static string GameId;
        public static MatchFound Match;
        public static string MyPlayerId;

        /// <summary>Set when GameStart arrives in the lobby; consumed by the game scene for its first render.</summary>
        public static GameStart PendingGameStart;

        /// <summary>Set from PairFound, before the game_id for the current match is known.</summary>
        public static string PairId;

        /// <summary>Set from a deep link / install referrer; consumed once after ServerHello. Survives Clear() pre-match.</summary>
        public static string PendingRoomCode;

        public static bool VsBotMode;
        public static GameOver LastGameOver;
        public static ByteString PendingResumeState;
        public static int SeriesWinsMine;
        public static int SeriesWinsTheirs;

        public static PlayerInfo Me => Find(p => p.PlayerId == MyPlayerId);
        public static PlayerInfo Opponent => Find(p => p.PlayerId != MyPlayerId);
        public static int MySeat => Me?.Seat ?? 0;

        private static PlayerInfo Find(Func<PlayerInfo, bool> predicate) =>
            Match?.Players?.FirstOrDefault(predicate);

        public static void Set(MatchFound found, string myPlayerId)
        {
            Match = found;
            MatchId = found.MatchId;
            GameId = found.GameId;
            MyPlayerId = myPlayerId;
        }

        public static void Clear()
        {
            Match = null;
            MatchId = null;
            GameId = null;
            MyPlayerId = null;
            PendingGameStart = null;
            PairId = null;
            VsBotMode = false;
            LastGameOver = null;
            PendingResumeState = null;
            SeriesWinsMine = 0;
            SeriesWinsTheirs = 0;
        }
    }
}
