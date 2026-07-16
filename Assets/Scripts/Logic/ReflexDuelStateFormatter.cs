namespace TwoUp.Logic
{
    public static class ReflexDuelStateFormatter
    {
        private const string NoReactionMarker = "-";

        public static string FormatReactionMs(int ms)
        {
            if (ms > 0)
                return ms + " ms";

            return NoReactionMarker;
        }
    }
}
