using System;
using TwoUp.Logic;
using UnityEngine;

namespace TwoUp
{
    /// <summary>Reads the Play Install Referrer once per install; pure parsing lives in InstallReferrerParser.</summary>
    public static class InstallReferrerReader
    {
        private const string ConsumedKey = "twoup.referrer_consumed";

        public static void ReadOnce(Action<string> onRoomCode)
        {
            if (PlayerPrefs.GetInt(ConsumedKey, 0) == 1)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var clientClass = new AndroidJavaClass("com.android.installreferrer.api.InstallReferrerClient"))
                using (var builder = clientClass.CallStatic<AndroidJavaObject>("newBuilder", activity))
                {
                    var client = builder.Call<AndroidJavaObject>("build");
                    client.Call("startConnection", new ReferrerStateListener(client, onRoomCode));
                }
            }
            catch
            {
                MarkConsumed();
            }
#else
            MarkConsumed();
#endif
        }

        private static void MarkConsumed()
        {
            PlayerPrefs.SetInt(ConsumedKey, 1);
            PlayerPrefs.Save();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private class ReferrerStateListener : AndroidJavaProxy
        {
            private readonly AndroidJavaObject client;
            private readonly Action<string> onRoomCode;

            public ReferrerStateListener(AndroidJavaObject client, Action<string> onRoomCode)
                : base("com.android.installreferrer.api.InstallReferrerStateListener")
            {
                this.client = client;
                this.onRoomCode = onRoomCode;
            }

            public void onInstallReferrerSetupFinished(int responseCode)
            {
                try
                {
                    const int ResponseOk = 0;
                    if (responseCode == ResponseOk)
                    {
                        using (var details = client.Call<AndroidJavaObject>("getInstallReferrer"))
                        {
                            var referrer = details.Call<string>("getInstallReferrer");
                            var roomCode = InstallReferrerParser.ExtractRoomCode(referrer);
                            if (!string.IsNullOrEmpty(roomCode))
                                onRoomCode?.Invoke(roomCode);
                        }
                    }
                }
                catch
                {
                    // silent per spec
                }
                finally
                {
                    try { client.Call("endConnection"); } catch { /* silent */ }
                    MarkConsumed();
                }
            }

            public void onInstallReferrerServiceDisconnected()
            {
            }
        }
#endif
    }
}
