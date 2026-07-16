using System.Collections.Generic;
using System.Linq;

namespace TwoUp.Logic
{
    public static class AsyncMatchListSorter
    {
        public static List<Twoup.V1.AsyncMatchSummary> Sort(IEnumerable<Twoup.V1.AsyncMatchSummary> items)
        {
            return items
                .OrderByDescending(x => x.YourTurn)
                .ThenBy(x => x.ForfeitDeadlineUnixMs == 0 ? long.MaxValue : x.ForfeitDeadlineUnixMs)
                .ToList();
        }
    }
}
