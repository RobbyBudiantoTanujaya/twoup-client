using UnityEngine;

namespace TwoUp.Net
{
    /// <summary>
    /// The only source of server endpoint URLs. Logic must read ActiveUrl and never
    /// hardcode an endpoint. Asset lives at Assets/Config/ServerConfig.asset.
    /// </summary>
    [CreateAssetMenu(fileName = "ServerConfig", menuName = "2UP/Server Config")]
    public class ServerConfig : ScriptableObject
    {
        public enum Env
        {
            Dev,
            Staging,
        }

        /// <summary>Where this build/editor session is running, so the correct loopback
        /// address to the dev server can be picked without hand-editing a URL string.</summary>
        public enum DevTarget
        {
            [InspectorName("Unity Editor (localhost)")]
            UnityEditor,
            [InspectorName("Android Emulator (10.0.2.2)")]
            AndroidEmulator,
            [InspectorName("LAN device — physical phone (custom IP)")]
            LanDevice,
        }

        [Header("Active environment")]
        public Env activeEnvironment = Env.Dev;

        [Header("Dev target (used when environment = Dev)")]
        public DevTarget devTarget = DevTarget.UnityEditor;
        [Tooltip("Matches PORT in twoup-server/.env.")]
        public int devPort = 8080;
        [Tooltip("Only used when Dev Target = LAN device. Get it from `./dev.sh myip` in twoup-server.")]
        public string lanIp = "";

        [Header("Staging")]
        public string stagingUrl = ""; // TODO: fill in once a staging server exists

        [Header("Invite links")]
        [Tooltip("Placeholder — final domain is Blocker B2 in the TDD.")]
        public string inviteLinkBase = "https://2up.example/r/";

        public string ActiveUrl
        {
            get
            {
                if (activeEnvironment == Env.Staging)
                    return stagingUrl;

                return devTarget switch
                {
                    DevTarget.AndroidEmulator => $"ws://10.0.2.2:{devPort}/ws",
                    DevTarget.LanDevice => $"ws://{lanIp}:{devPort}/ws",
                    _ => $"ws://localhost:{devPort}/ws",
                };
            }
        }
    }
}
