using TMPro;
using Twoup.V1;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>Settings (S10): sound/music/vibration toggles (PlayerPrefs), restore purchase, links, version.</summary>
    public class SettingsController : MonoBehaviour
    {
        private const string SoundKey = "twoup.sound";
        private const string MusicKey = "twoup.music";
        private const string VibrationKey = "twoup.vibration";

        [SerializeField] private Button backButton;
        [SerializeField] private Toggle soundToggle;
        [SerializeField] private Toggle musicToggle;
        [SerializeField] private Toggle vibrationToggle;
        [SerializeField] private Button restorePurchaseButton;
        [SerializeField] private TMP_Text linksText;
        [SerializeField] private TMP_Text versionText;
        [SerializeField] private TMP_Text toastText;

        private void Start()
        {
            backButton.onClick.AddListener(OnBack);
            restorePurchaseButton.onClick.AddListener(OnRestorePurchaseClicked);
            toastText.gameObject.SetActive(false);

            soundToggle.isOn = PlayerPrefs.GetInt(SoundKey, 1) == 1;
            musicToggle.isOn = PlayerPrefs.GetInt(MusicKey, 1) == 1;
            vibrationToggle.isOn = PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            AudioListener.volume = soundToggle.isOn ? 1f : 0f;

            soundToggle.onValueChanged.AddListener(OnSoundToggled);
            musicToggle.onValueChanged.AddListener(OnMusicToggled);
            vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);

            versionText.text = "v" + Application.version;

            NetworkClient.Instance.ProfileDataReceived += OnProfileDataReceived;
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.ProfileDataReceived -= OnProfileDataReceived;
        }

        private void OnBack() => AppStateMachine.Instance.ToHome();

        private void OnSoundToggled(bool on)
        {
            PlayerPrefs.SetInt(SoundKey, on ? 1 : 0);
            PlayerPrefs.Save();
            AudioListener.volume = on ? 1f : 0f;
        }

        private void OnMusicToggled(bool on)
        {
            PlayerPrefs.SetInt(MusicKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnVibrationToggled(bool on)
        {
            PlayerPrefs.SetInt(VibrationKey, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void OnRestorePurchaseClicked()
        {
            NetworkClient.Instance.Send(new Envelope { GetProfile = new GetProfile() });
        }

        private void OnProfileDataReceived(ProfileData data)
        {
            ShowToast(data.Premium ? "Premium restored" : "No purchase found");
        }

        private void ShowToast(string message)
        {
            toastText.text = message;
            toastText.gameObject.SetActive(true);
        }
    }
}
