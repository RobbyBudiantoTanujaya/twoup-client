using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Single selectable game card in the Voting (S5) grid. Also reused as the two showdown
    /// slot occupants — showdown re-parents the existing card transform, it never instantiates.
    /// </summary>
    public class GameCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text tagsLabel;
        [SerializeField] private Image background;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private Button getCoinsButton;

        public string GameId;

        public TMP_Text NameLabel => nameLabel;
        public TMP_Text TagsLabel => tagsLabel;
        public Image Background => background;
        public Button Button => button;
        public TMP_Text CostText => costText;
        public Button GetCoinsButton => getCoinsButton;
    }
}
