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

        [Header("Active environment")]
        public Env activeEnvironment = Env.Dev;

        [Header("Endpoints")]
        [Tooltip("10.0.2.2 reaches the host machine's localhost from the Android emulator.")]
        public string devUrl = "ws://10.0.2.2:8080/ws";
        public string stagingUrl = ""; // TODO: fill in once a staging server exists

        [Header("Invite links")]
        [Tooltip("Placeholder — final domain is Blocker B2 in the TDD.")]
        public string inviteLinkBase = "https://2up.example/r/";

        public string ActiveUrl => activeEnvironment == Env.Staging ? stagingUrl : devUrl;
    }
}
