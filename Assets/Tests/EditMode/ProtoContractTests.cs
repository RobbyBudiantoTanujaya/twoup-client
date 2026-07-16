using Google.Protobuf;
using Google.Protobuf.Collections;
using NUnit.Framework;

namespace TwoUp.Tests.EditMode
{
    public class ProtoContractTests
    {
        [Test]
        public void Envelope_HasVotingPayloads()
        {
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.PairFound,
                new Twoup.V1.Envelope { PairFound = new Twoup.V1.PairFound() }.PayloadCase);
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.VotingLocked,
                new Twoup.V1.Envelope { VotingLocked = new Twoup.V1.VotingLocked() }.PayloadCase);
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.StartBotMatch,
                new Twoup.V1.Envelope { StartBotMatch = new Twoup.V1.StartBotMatch() }.PayloadCase);
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.MatchWentAsync,
                new Twoup.V1.Envelope { MatchWentAsync = new Twoup.V1.MatchWentAsync() }.PayloadCase);
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.GetProfile,
                new Twoup.V1.Envelope { GetProfile = new Twoup.V1.GetProfile() }.PayloadCase);
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.RegisterPushToken,
                new Twoup.V1.Envelope { RegisterPushToken = new Twoup.V1.RegisterPushToken() }.PayloadCase);
        }

        [Test]
        public void ConnectFourContract_Unchanged()
        {
            var state = new Twoup.V1.ConnectFourState
            {
                NextPlayerId = "player-1",
            };
            state.Cells.Add(0);

            Assert.IsInstanceOf<RepeatedField<int>>(state.Cells);
            Assert.AreEqual("player-1", state.NextPlayerId);
        }

        [Test]
        public void BattleshipState_RoundTrips()
        {
            var original = new Twoup.V1.BattleshipState
            {
                Phase = Twoup.V1.BattleshipPhase.BsFiring,
                MyPlacementLocked = true,
                OpponentPlacementLocked = true,
                NextPlayerId = "player-2",
            };
            for (var i = 0; i < 100; i++)
            {
                original.MyFleet.Add(i % 6);
            }

            var roundTripped = Twoup.V1.BattleshipState.Parser.ParseFrom(original.ToByteArray());

            Assert.AreEqual(original, roundTripped);
            Assert.AreEqual(100, roundTripped.MyFleet.Count);
        }
    }
}
