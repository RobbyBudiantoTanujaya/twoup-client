using System;
using UnityEngine;

namespace TwoUp
{
    /// <summary>Stable per-install device id (generated once, kept in PlayerPrefs).</summary>
    public static class DeviceIdentity
    {
        private const string DeviceIdKey = "twoup.device_id";

        public static string GetOrCreateDeviceId()
        {
            if (!PlayerPrefs.HasKey(DeviceIdKey))
            {
                PlayerPrefs.SetString(DeviceIdKey, Guid.NewGuid().ToString("N"));
                PlayerPrefs.Save();
            }
            return PlayerPrefs.GetString(DeviceIdKey);
        }

        // TODO: user-editable display name; derived from the device id for now.
        public static string DisplayName =>
            "Player-" + GetOrCreateDeviceId().Substring(0, 4).ToUpperInvariant();
    }
}
