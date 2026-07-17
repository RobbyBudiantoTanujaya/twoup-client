using Google.Protobuf;
using NUnit.Framework;

namespace TwoUp.Tests.EditMode
{
    public class EconomyContractTests
    {
        [Test]
        public void EconomyPayloads_RoundTrip()
        {
            var economyConfig = new Twoup.V1.Envelope
            {
                EconomyConfig = new Twoup.V1.EconomyConfig
                {
                    AdRewardCoins = 3,
                    AdDailyCap = 5,
                    StartingBalance = 20,
                },
            };
            economyConfig.EconomyConfig.GameCoinCost["battleship"] = 3;
            economyConfig.EconomyConfig.DailyStreakRewards.AddRange(new[] { 5, 6, 7, 8, 10, 12, 15 });

            var roundTrippedConfig = Twoup.V1.Envelope.Parser.ParseFrom(economyConfig.ToByteArray());
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.EconomyConfig, roundTrippedConfig.PayloadCase);
            Assert.AreEqual(3, roundTrippedConfig.EconomyConfig.GameCoinCost["battleship"]);
            Assert.AreEqual(3, roundTrippedConfig.EconomyConfig.AdRewardCoins);
            Assert.AreEqual(5, roundTrippedConfig.EconomyConfig.AdDailyCap);
            Assert.AreEqual(20, roundTrippedConfig.EconomyConfig.StartingBalance);
            CollectionAssert.AreEqual(new[] { 5, 6, 7, 8, 10, 12, 15 }, roundTrippedConfig.EconomyConfig.DailyStreakRewards);

            var claimed = new Twoup.V1.Envelope
            {
                DailyRewardClaimed = new Twoup.V1.DailyRewardClaimed
                {
                    RewardCoins = 7,
                    StreakCount = 3,
                    CoinBalance = 27,
                },
            };

            var roundTrippedClaimed = Twoup.V1.Envelope.Parser.ParseFrom(claimed.ToByteArray());
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.DailyRewardClaimed, roundTrippedClaimed.PayloadCase);
            Assert.AreEqual(7, roundTrippedClaimed.DailyRewardClaimed.RewardCoins);
            Assert.AreEqual(3, roundTrippedClaimed.DailyRewardClaimed.StreakCount);
            Assert.AreEqual(27, roundTrippedClaimed.DailyRewardClaimed.CoinBalance);

            var wallet = new Twoup.V1.Envelope
            {
                WalletUpdate = new Twoup.V1.WalletUpdate
                {
                    CoinBalance = 20,
                    DailyRewardAvailable = true,
                },
            };

            var roundTrippedWallet = Twoup.V1.Envelope.Parser.ParseFrom(wallet.ToByteArray());
            Assert.AreEqual(Twoup.V1.Envelope.PayloadOneofCase.WalletUpdate, roundTrippedWallet.PayloadCase);
            Assert.AreEqual(20, roundTrippedWallet.WalletUpdate.CoinBalance);
            Assert.IsTrue(roundTrippedWallet.WalletUpdate.DailyRewardAvailable);
        }

        [Test]
        public void TicketFields_StillPresent()
        {
            Assert.AreEqual(0, new Twoup.V1.WalletUpdate().TicketBalance);
        }
    }
}
