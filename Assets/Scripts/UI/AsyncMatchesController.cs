using System;
using TMPro;
using Twoup.V1;
using TwoUp.Logic;
using TwoUp.Net;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// AsyncList (S11): matches waiting on the local player's move (or the opponent's),
    /// sorted your-turn-first via AsyncMatchListSorter, resume-on-tap.
    /// </summary>
    public class AsyncMatchesController : MonoBehaviour
    {
        [SerializeField] private Button backButton;
        [SerializeField] private Transform content;
        [SerializeField] private AsyncMatchRowView rowTemplate;
        [SerializeField] private TMP_Text emptyText;

        private void Start()
        {
            backButton.onClick.AddListener(OnBack);
            emptyText.gameObject.SetActive(false);

            var net = NetworkClient.Instance;
            net.AsyncMatchListReceived += OnAsyncMatchListReceived;
            net.MatchResumedReceived += OnMatchResumed;
            net.Send(new Envelope { ListAsyncMatches = new ListAsyncMatches() });
        }

        private void OnDestroy()
        {
            var net = NetworkClient.Instance;
            if (net == null)
                return;
            net.AsyncMatchListReceived -= OnAsyncMatchListReceived;
            net.MatchResumedReceived -= OnMatchResumed;
        }

        private void OnBack() => AppStateMachine.Instance.ToHome();

        private void OnAsyncMatchListReceived(AsyncMatchList list)
        {
            var sorted = AsyncMatchListSorter.Sort(list.Matches);

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            emptyText.gameObject.SetActive(sorted.Count == 0);

            foreach (var match in sorted)
                CreateRow(match);
        }

        private void CreateRow(AsyncMatchSummary match)
        {
            var clone = Instantiate(rowTemplate, content);
            clone.gameObject.SetActive(true);
            clone.MatchId = match.MatchId;

            var entry = AppStateMachine.Instance.Catalog.Find(match.GameId);
            string gameName = entry != null ? entry.displayName : match.GameId;
            clone.TitleLabel.text = $"{gameName} vs {match.OpponentDisplayName}";

            string status = match.YourTurn ? "YOUR TURN" : "waiting...";
            if (match.ForfeitDeadlineUnixMs > 0)
            {
                long remainingMs = Math.Max(0, match.ForfeitDeadlineUnixMs - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                status += $" - {remainingMs / 3_600_000L}h left";
            }
            clone.StatusLabel.text = status;

            string matchId = match.MatchId;
            clone.Button.onClick.AddListener(() => OnResumeClicked(matchId));
        }

        private void OnResumeClicked(string matchId)
        {
            NetworkClient.Instance.Send(new Envelope { ResumeAsyncMatch = new ResumeAsyncMatch { MatchId = matchId } });
        }

        private void OnMatchResumed(MatchResumed msg)
        {
            MatchContext.MatchId = msg.MatchId;
            MatchContext.PendingResumeState = msg.State;
            AppStateMachine.Instance.ToGame(msg.GameId);
        }
    }
}
