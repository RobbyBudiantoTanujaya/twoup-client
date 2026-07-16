namespace TwoUp.Logic
{
    public static class LedgerDeltaFormatter
    {
        private const string Arrow = "→";

        public static string FormatHeadline(int winsMine, int winsTheirs, string opponentName)
        {
            return $"You {winsMine} : {winsTheirs} {opponentName}";
        }

        public static string FormatStreak(bool holderIsMe, string opponentName, int count)
        {
            if (count < 2)
                return "";

            return holderIsMe
                ? $"You're on a {count} win streak!"
                : $"{opponentName} on a {count} win streak";
        }

        public static string FormatDuo(int score, int best, bool newBest)
        {
            if (newBest)
                return $"New duo best! {best} {Arrow} {score}";

            return $"Score {score} (best {best})";
        }
    }
}
