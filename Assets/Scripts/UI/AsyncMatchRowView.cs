using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>Single row in the AsyncList (S11) list, cloned per AsyncMatchSummary from an inactive template.</summary>
    public class AsyncMatchRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text statusLabel;
        [SerializeField] private Button button;

        public string MatchId;

        public TMP_Text TitleLabel => titleLabel;
        public TMP_Text StatusLabel => statusLabel;
        public Button Button => button;
    }
}
