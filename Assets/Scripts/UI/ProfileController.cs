using System.Linq;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Profile (S8): my display name + bot ledger, per-pair versus/co-op ledger list, and a
    /// pair-detail drilldown (versus lines + co-op lines simplified as two text blocks per plan).
    /// </summary>
    public class ProfileController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text myNameText;
        [SerializeField] private TMP_InputField editNameInput;
        [SerializeField] private Button editNameButton;
        [SerializeField] private TMP_Text botStatsText;
        [SerializeField] private Transform content;
        [SerializeField] private PairRowView rowTemplate;
        [SerializeField] private GameObject pairDetailPanel;
        [SerializeField] private TMP_Text detailHeaderText;
        [SerializeField] private TMP_Text detailAggregateText;
        [SerializeField] private TMP_Text detailStreakText;
        [SerializeField] private TMP_Text versusLinesText;
        [SerializeField] private TMP_Text coopLinesText;
        [SerializeField] private Button closeDetailButton;

        private void Start()
        {
            backButton.onClick.AddListener(OnBack);
            editNameButton.onClick.AddListener(OnEditNameClicked);
            editNameInput.onEndEdit.AddListener(OnNameEndEdit);
            closeDetailButton.onClick.AddListener(OnCloseDetail);
            editNameInput.gameObject.SetActive(false);
            pairDetailPanel.SetActive(false);

            var net = NetworkClient.Instance;
            net.ProfileDataReceived += OnProfileDataReceived;
            net.PairDetailReceived += OnPairDetailReceived;
            net.Send(new Envelope { GetProfile = new GetProfile() });
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.ProfileDataReceived -= OnProfileDataReceived;
            net.PairDetailReceived -= OnPairDetailReceived;
        }

        private void OnBack() => AppStateMachine.Instance.ToHome();

        private void OnProfileDataReceived(ProfileData data)
        {
            myNameText.text = data.Me.DisplayName;
            botStatsText.text = $"vs Bots: {data.BotWins}W {data.BotLosses}L {data.BotDraws}D";

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            foreach (var pair in data.Pairs)
                CreateRow(pair);
        }

        private void CreateRow(PairSummary pair)
        {
            var clone = Instantiate(rowTemplate, content);
            clone.gameObject.SetActive(true);
            clone.OtherPlayerId = pair.OtherPlayerId;

            clone.NameLabel.text = pair.OtherDisplayName;
            clone.ScoreLabel.text = LedgerDeltaFormatter.FormatHeadline(pair.WinsMine, pair.WinsTheirs, pair.OtherDisplayName);
            clone.BadgeLabel.text = pair.MilestoneTier == "none"
                ? $"Duo Lv{pair.DuoLevel}"
                : $"{pair.MilestoneTier} - Duo Lv{pair.DuoLevel}";

            string otherPlayerId = pair.OtherPlayerId;
            clone.Button.onClick.AddListener(() => OnRowClicked(otherPlayerId));
        }

        private void OnRowClicked(string otherPlayerId)
        {
            NetworkClient.Instance.Send(new Envelope { GetPairDetail = new GetPairDetail { OtherPlayerId = otherPlayerId } });
        }

        private void OnPairDetailReceived(PairDetail detail)
        {
            pairDetailPanel.SetActive(true);

            var summary = detail.Summary;
            detailHeaderText.text = summary.OtherDisplayName;
            detailAggregateText.text = LedgerDeltaFormatter.FormatHeadline(summary.WinsMine, summary.WinsTheirs, summary.OtherDisplayName);

            bool holderIsMe = detail.StreakHolderPlayerId == NetworkClient.Instance.PlayerId;
            detailStreakText.text = LedgerDeltaFormatter.FormatStreak(holderIsMe, summary.OtherDisplayName, detail.StreakCount);

            versusLinesText.text = string.Join("\n", detail.VersusLines.Select(
                l => $"{l.GameId}: {l.WinsMine} : {l.WinsTheirs} ({l.Draws} draws)"));
            coopLinesText.text = string.Join("\n", detail.CoopLines.Select(
                l => $"{l.GameId}: best {l.BestScore} ({l.TotalMatches} runs)"));
        }

        private void OnEditNameClicked()
        {
            editNameInput.gameObject.SetActive(!editNameInput.gameObject.activeSelf);
        }

        private void OnNameEndEdit(string value)
        {
            string trimmed = value.Trim();
            if (string.IsNullOrEmpty(trimmed))
                return;

            NetworkClient.Instance.Send(new Envelope { SetProfile = new SetProfile { DisplayName = trimmed } });
            editNameInput.gameObject.SetActive(false);
        }

        private void OnCloseDetail() => pairDetailPanel.SetActive(false);
    }
}
