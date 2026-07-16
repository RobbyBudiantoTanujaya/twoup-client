using System.Text;

namespace TwoUp.Logic
{
    public static class RoomCodeSanitizer
    {
        public const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        private const int MaxLength = 6;

        public static string Sanitize(string raw)
        {
            if (string.IsNullOrEmpty(raw))
                return "";

            var sb = new StringBuilder(MaxLength);
            foreach (var c in raw.ToUpperInvariant())
            {
                if (sb.Length >= MaxLength)
                    break;

                if (Alphabet.IndexOf(c) >= 0)
                    sb.Append(c);
            }

            return sb.ToString();
        }
    }
}
