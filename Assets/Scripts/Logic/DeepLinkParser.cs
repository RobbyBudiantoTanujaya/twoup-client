using System.Text.RegularExpressions;

namespace TwoUp.Logic
{
    public static class DeepLinkParser
    {
        private static readonly Regex RoomLinkPattern = new Regex(
            @"^(?:https?://[^/]+/r/|twoup://r/)([^/?#]+)",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string ExtractRoomCode(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            var match = RoomLinkPattern.Match(url);
            if (!match.Success)
                return null;

            var code = RoomCodeSanitizer.Sanitize(match.Groups[1].Value);
            return string.IsNullOrEmpty(code) ? null : code;
        }
    }
}
