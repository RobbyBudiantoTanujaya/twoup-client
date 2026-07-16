using System.Collections.Generic;

namespace TwoUp.Logic
{
    public static class BattleshipPlacementValidator
    {
        public const int GridSize = 10;
        private static readonly int[] ExpectedLengths = { 5, 4, 3, 3, 2 };

        public static bool IsValid(IReadOnlyList<Twoup.V1.ShipPlacement> ships)
        {
            if (ships == null || ships.Count != ExpectedLengths.Length)
                return false;

            var remainingLengths = new List<int>(ExpectedLengths);
            var occupied = new HashSet<(int row, int col)>();

            foreach (var ship in ships)
            {
                if (!remainingLengths.Remove(ship.Length))
                    return false;

                if (ship.Row < 0 || ship.Row >= GridSize || ship.Col < 0 || ship.Col >= GridSize)
                    return false;

                if (ship.Horizontal)
                {
                    if (ship.Col + ship.Length - 1 >= GridSize)
                        return false;
                }
                else
                {
                    if (ship.Row + ship.Length - 1 >= GridSize)
                        return false;
                }

                for (var i = 0; i < ship.Length; i++)
                {
                    var cell = ship.Horizontal
                        ? (ship.Row, ship.Col + i)
                        : (ship.Row + i, ship.Col);

                    if (!occupied.Add(cell))
                        return false;
                }
            }

            return true;
        }

        public static bool CellsOccupied(IReadOnlyList<Twoup.V1.ShipPlacement> ships, int row, int col)
        {
            if (ships == null)
                return false;

            foreach (var ship in ships)
            {
                for (var i = 0; i < ship.Length; i++)
                {
                    var cellRow = ship.Horizontal ? ship.Row : ship.Row + i;
                    var cellCol = ship.Horizontal ? ship.Col + i : ship.Col;

                    if (cellRow == row && cellCol == col)
                        return true;
                }
            }

            return false;
        }
    }
}
