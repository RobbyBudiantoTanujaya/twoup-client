using Google.Protobuf;
using NUnit.Framework;
using Twoup.V1;

namespace TwoUp.Tests.EditMode
{
    public class MatchContextTests
    {
        [TearDown]
        public void TearDown()
        {
            MatchContext.Clear();
            MatchContext.PendingRoomCode = null;
        }

        private static void SetAllFields()
        {
            MatchContext.Match = new MatchFound { MatchId = "m1", GameId = "connect_four" };
            MatchContext.MatchId = "m1";
            MatchContext.GameId = "connect_four";
            MatchContext.MyPlayerId = "p1";
            MatchContext.PendingGameStart = new GameStart { MatchId = "m1" };
            MatchContext.PairId = "pair1";
            MatchContext.PendingRoomCode = "ABCD";
            MatchContext.VsBotMode = true;
            MatchContext.LastGameOver = new GameOver();
            MatchContext.PendingResumeState = ByteString.CopyFromUtf8("state");
            MatchContext.SeriesWinsMine = 2;
            MatchContext.SeriesWinsTheirs = 1;
        }

        [Test]
        public void Clear_ResetsMatchFieldsButKeepsPendingRoomCode()
        {
            SetAllFields();

            MatchContext.Clear();

            Assert.AreEqual("ABCD", MatchContext.PendingRoomCode);
            Assert.IsNull(MatchContext.Match);
            Assert.IsNull(MatchContext.MatchId);
            Assert.IsNull(MatchContext.GameId);
            Assert.IsNull(MatchContext.MyPlayerId);
            Assert.IsNull(MatchContext.PendingGameStart);
            Assert.IsNull(MatchContext.PairId);
            Assert.IsFalse(MatchContext.VsBotMode);
            Assert.IsNull(MatchContext.LastGameOver);
            Assert.IsNull(MatchContext.PendingResumeState);
        }

        [Test]
        public void Clear_ResetsSeriesCounters()
        {
            SetAllFields();

            MatchContext.Clear();

            Assert.AreEqual(0, MatchContext.SeriesWinsMine);
            Assert.AreEqual(0, MatchContext.SeriesWinsTheirs);
        }
    }
}
