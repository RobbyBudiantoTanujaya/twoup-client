using System;
using System.Collections.Generic;

namespace TwoUp.Logic
{
    public static class BattleshipPlacementGenerator
    {
        private const int MaxAttemptsPerShip = 1000;
        private static readonly int[] ShipLengths = { 5, 4, 3, 3, 2 };

        public static List<Twoup.V1.ShipPlacement> Generate(int seed)
        {
            var random = new Random(seed);
            var ships = new List<Twoup.V1.ShipPlacement>(ShipLengths.Length);

            foreach (var length in ShipLengths)
            {
                var placed = false;

                for (var attempt = 0; attempt < MaxAttemptsPerShip && !placed; attempt++)
                {
                    var horizontal = random.Next(2) == 0;
                    var maxRow = horizontal ? BattleshipPlacementValidator.GridSize : BattleshipPlacementValidator.GridSize - length + 1;
                    var maxCol = horizontal ? BattleshipPlacementValidator.GridSize - length + 1 : BattleshipPlacementValidator.GridSize;

                    var row = random.Next(maxRow);
                    var col = random.Next(maxCol);

                    var candidate = new Twoup.V1.ShipPlacement
                    {
                        Length = length,
                        Row = row,
                        Col = col,
                        Horizontal = horizontal,
                    };

                    if (!OverlapsAny(ships, candidate))
                    {
                        ships.Add(candidate);
                        placed = true;
                    }
                }

                if (!placed)
                    throw new InvalidOperationException($"Failed to place ship of length {length} within {MaxAttemptsPerShip} attempts (seed {seed}).");
            }

            return ships;
        }

        private static bool OverlapsAny(List<Twoup.V1.ShipPlacement> placedShips, Twoup.V1.ShipPlacement candidate)
        {
            for (var i = 0; i < candidate.Length; i++)
            {
                var row = candidate.Horizontal ? candidate.Row : candidate.Row + i;
                var col = candidate.Horizontal ? candidate.Col + i : candidate.Col;

                if (BattleshipPlacementValidator.CellsOccupied(placedShips, row, col))
                    return true;
            }

            return false;
        }
    }
}
