using Twoup.V1;
using TwoUp.Net;
using UnityEngine;

namespace TwoUp
{
    /// <summary>Registers the FCM push token with the server once the player is identified (called by BootController after ServerHello).</summary>
    public class PushTokenClient : MonoBehaviour
    {
        public void OnIdentified()
        {
#if TWOUP_FIREBASE
            Firebase.Messaging.FirebaseMessaging.TokenReceived += OnTokenReceived;
            Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(dependencyTask =>
            {
                if (dependencyTask.Result != Firebase.DependencyStatus.Available)
                    return;

                Firebase.Messaging.FirebaseMessaging.GetTokenAsync().ContinueWith(tokenTask =>
                {
                    if (!tokenTask.IsFaulted && !tokenTask.IsCanceled)
                        SendToken(tokenTask.Result);
                });
            });
#else
            Debug.Log("[Push] TWOUP_FIREBASE off - token registration skipped");
#endif
        }

#if TWOUP_FIREBASE
        private void OnTokenReceived(object sender, Firebase.Messaging.TokenReceivedEventArgs e)
        {
            SendToken(e.Token);
        }

        private void SendToken(string token)
        {
            NetworkClient.Instance.Send(new Envelope
            {
                RegisterPushToken = new RegisterPushToken { FcmToken = token }
            });
        }

        private void OnDestroy()
        {
            Firebase.Messaging.FirebaseMessaging.TokenReceived -= OnTokenReceived;
        }
#endif
    }
}
