using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>Single row in Shop's item list (S9), cloned per ShopItem from an inactive template.</summary>
    public class ShopItemRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text priceLabel;
        [SerializeField] private Button buyButton;

        public string ItemKey;

        public TMP_Text NameLabel => nameLabel;
        public TMP_Text PriceLabel => priceLabel;
        public Button BuyButton => buyButton;
    }
}
