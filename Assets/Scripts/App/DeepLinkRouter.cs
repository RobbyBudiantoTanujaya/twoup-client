using TwoUp.Logic;
using UnityEngine;

namespace TwoUp
{
    /// <summary>Routes room-code deep links (cold-start URL, runtime activation, install referrer) into MatchContext.</summary>
    public class DeepLinkRouter : MonoBehaviour
    {
        private void Awake()
        {
            Application.deepLinkActivated += OnDeepLink;

            var code = DeepLinkParser.ExtractRoomCode(Application.absoluteURL);
            if (code != null)
                MatchContext.PendingRoomCode = code;

            InstallReferrerReader.ReadOnce(referrerCode => MatchContext.PendingRoomCode = referrerCode);
        }

        private void OnDestroy()
        {
            Application.deepLinkActivated -= OnDeepLink;
        }

        private void OnDeepLink(string url)
        {
            var code = DeepLinkParser.ExtractRoomCode(url);
            if (code != null)
                MatchContext.PendingRoomCode = code;
        }
    }
}
