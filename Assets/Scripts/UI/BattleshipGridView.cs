using System;
using UnityEngine;
using UnityEngine.UI;

namespace TwoUp.UI
{
    /// <summary>
    /// Reusable 10x10 grid of tappable cells (row-major index = row*10+col). Used three times per
    /// Battleship screen: the player's own fleet during placement, the firing target grid, and the
    /// small my-board overlay during firing.
    /// </summary>
    public class BattleshipGridView : MonoBehaviour
    {
        public const int Size = 10;

        [SerializeField] private Image[] cells;
        [SerializeField] private Button[] cellButtons;

        public event Action<int, int> CellTapped;

        private bool wired;

        private void Awake()
        {
            Wire();
        }

        private void Wire()
        {
            if (wired || cellButtons == null)
                return;
            wired = true;
            for (int i = 0; i < cellButtons.Length; i++)
            {
                int index = i;
                cellButtons[index].onClick.AddListener(() => CellTapped?.Invoke(index / Size, index % Size));
            }
        }

        public void SetCell(int row, int col, Color color)
        {
            cells[row * Size + col].color = color;
        }

        public void SetInteractable(int row, int col, bool interactable)
        {
            cellButtons[row * Size + col].interactable = interactable;
        }
    }
}
