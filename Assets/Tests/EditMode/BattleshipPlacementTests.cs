using System.Collections.Generic;
using NUnit.Framework;
using TwoUp.Logic;
using Twoup.V1;

namespace TwoUp.Tests.EditMode
{
    public class BattleshipPlacementTests
    {
        private static List<ShipPlacement> KnownGoodLayout()
        {
            return new List<ShipPlacement>
            {
                new ShipPlacement { Length = 5, Row = 0, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 4, Row = 2, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 3, Row = 4, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 3, Row = 6, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 2, Row = 8, Col = 0, Horizontal = true },
            };
        }

        [Test]
        public void Validator_AcceptsKnownGoodLayout()
        {
            Assert.IsTrue(BattleshipPlacementValidator.IsValid(KnownGoodLayout()));
        }

        [Test]
        public void Validator_RejectsOverlap()
        {
            var ships = KnownGoodLayout();
            ships[1] = new ShipPlacement { Length = 4, Row = 0, Col = 1, Horizontal = true };

            Assert.IsFalse(BattleshipPlacementValidator.IsValid(ships));
        }

        [Test]
        public void Validator_RejectsOutOfBounds()
        {
            var ships = KnownGoodLayout();
            ships[0] = new ShipPlacement { Length = 5, Row = 0, Col = 6, Horizontal = true };

            Assert.IsFalse(BattleshipPlacementValidator.IsValid(ships));
        }

        [Test]
        public void Validator_RejectsWrongShipSet()
        {
            var fourShips = KnownGoodLayout();
            fourShips.RemoveAt(0);
            Assert.IsFalse(BattleshipPlacementValidator.IsValid(fourShips));

            var wrongLengths = new List<ShipPlacement>
            {
                new ShipPlacement { Length = 5, Row = 0, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 4, Row = 2, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 3, Row = 4, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 3, Row = 6, Col = 0, Horizontal = true },
                new ShipPlacement { Length = 1, Row = 8, Col = 0, Horizontal = true },
            };
            Assert.IsFalse(BattleshipPlacementValidator.IsValid(wrongLengths));
        }

        [Test]
        public void Generator_ProducesValidLayout()
        {
            for (var seed = 1; seed <= 20; seed++)
            {
                var ships = BattleshipPlacementGenerator.Generate(seed);
                Assert.IsTrue(BattleshipPlacementValidator.IsValid(ships), $"seed {seed} produced an invalid layout");
            }
        }

        [Test]
        public void Generator_IsDeterministic()
        {
            var first = BattleshipPlacementGenerator.Generate(7);
            var second = BattleshipPlacementGenerator.Generate(7);

            Assert.AreEqual(first.Count, second.Count);
            for (var i = 0; i < first.Count; i++)
            {
                Assert.AreEqual(first[i].Length, second[i].Length);
                Assert.AreEqual(first[i].Row, second[i].Row);
                Assert.AreEqual(first[i].Col, second[i].Col);
                Assert.AreEqual(first[i].Horizontal, second[i].Horizontal);
            }
        }
    }
}
