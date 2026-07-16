namespace TwoUp.Logic
{
    public static class VotingCountdownFormatter
    {
        public static string Format(int remainingMs)
        {
            if (remainingMs <= 0)
                return "0";

            int seconds = (remainingMs + 999) / 1000;
            return seconds.ToString();
        }
    }
}
