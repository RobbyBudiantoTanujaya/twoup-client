using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>Single row in Profile's pair list (S8), cloned per PairSummary from an inactive template.</summary>
    public class PairRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text scoreLabel;
        [SerializeField] private TMP_Text badgeLabel;
        [SerializeField] private Button button;

        public string OtherPlayerId;

        public TMP_Text NameLabel => nameLabel;
        public TMP_Text ScoreLabel => scoreLabel;
        public TMP_Text BadgeLabel => badgeLabel;
        public Button Button => button;
    }
}
