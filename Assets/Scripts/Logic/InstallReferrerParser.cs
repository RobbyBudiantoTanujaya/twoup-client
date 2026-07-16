namespace TwoUp.Logic
{
    public static class InstallReferrerParser
    {
        public static string ExtractRoomCode(string referrer)
        {
            if (string.IsNullOrEmpty(referrer))
                return null;

            foreach (var pair in referrer.Split('&'))
            {
                var kv = pair.Split(new[] { '=' }, 2);
                if (kv.Length == 2 && kv[0] == "utm_content")
                {
                    var code = RoomCodeSanitizer.Sanitize(kv[1]);
                    return string.IsNullOrEmpty(code) ? null : code;
                }
            }

            return null;
        }
    }
}
