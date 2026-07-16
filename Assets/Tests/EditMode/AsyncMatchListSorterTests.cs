using System.Collections.Generic;
using NUnit.Framework;
using TwoUp.Logic;
using Twoup.V1;

namespace TwoUp.Tests.EditMode
{
    public class AsyncMatchListSorterTests
    {
        private static AsyncMatchSummary Make(string matchId, bool yourTurn, long deadlineMs)
        {
            return new AsyncMatchSummary
            {
                MatchId = matchId,
                YourTurn = yourTurn,
                ForfeitDeadlineUnixMs = deadlineMs,
            };
        }

        [Test]
        public void Sort_YourTurnFirst_ThenByDeadlineAscending()
        {
            var items = new List<AsyncMatchSummary>
            {
                Make("theirTurnLate", false, 5000),
                Make("yourTurnLate", true, 9000),
                Make("theirTurnEarly", false, 1000),
                Make("yourTurnEarly", true, 2000),
            };

            var sorted = AsyncMatchListSorter.Sort(items);

            Assert.AreEqual(new[] { "yourTurnEarly", "yourTurnLate", "theirTurnEarly", "theirTurnLate" },
                sorted.ConvertAll(x => x.MatchId).ToArray());
        }

        [Test]
        public void Sort_ZeroDeadlineLast()
        {
            var items = new List<AsyncMatchSummary>
            {
                Make("placement", true, 0),
                Make("withDeadline", true, 4000),
                Make("theirPlacement", false, 0),
                Make("theirWithDeadline", false, 3000),
            };

            var sorted = AsyncMatchListSorter.Sort(items);

            Assert.AreEqual(new[] { "withDeadline", "placement", "theirWithDeadline", "theirPlacement" },
                sorted.ConvertAll(x => x.MatchId).ToArray());
        }
    }
}
