using Game.Core;
using Game.Gameplay.Progression;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class FloorFormulaTests
    {
        private const double BaseHealth = 10d;
        private const double HealthGrowth = 1.16d;
        private const double BaseGold = 5d;
        private const double GoldGrowth = 1.14d;

        private static FloorFormula CreateFormula()
            => new FloorFormula(BaseHealth, HealthGrowth, BaseGold, GoldGrowth);

        private static double Ratio(BigNumber numerator, BigNumber denominator)
            => (numerator / denominator).ToDouble();

        [Test]
        public void FirstFloor_UsesBaseValues()
        {
            var formula = CreateFormula();
            Assert.AreEqual(BaseHealth, formula.MonsterHealth(1).ToDouble(), 1e-6);
            Assert.AreEqual(BaseGold, formula.GoldReward(1).ToDouble(), 1e-6);
        }

        [Test]
        public void MonsterHealth_GrowsByConfiguredRatePerFloor()
        {
            var formula = CreateFormula();
            Assert.AreEqual(HealthGrowth, Ratio(formula.MonsterHealth(2), formula.MonsterHealth(1)), 1e-9);
            Assert.AreEqual(HealthGrowth, Ratio(formula.MonsterHealth(51), formula.MonsterHealth(50)), 1e-9);
        }

        [Test]
        public void GoldReward_GrowsByConfiguredRatePerFloor()
        {
            var formula = CreateFormula();
            Assert.AreEqual(GoldGrowth, Ratio(formula.GoldReward(2), formula.GoldReward(1)), 1e-9);
            Assert.AreEqual(GoldGrowth, Ratio(formula.GoldReward(51), formula.GoldReward(50)), 1e-9);
        }

        [Test]
        public void MonsterHealth_OutgrowsGold_SoProgressDecelerates()
        {
            var formula = CreateFormula();
            double earlyCost = Ratio(formula.MonsterHealth(1), formula.GoldReward(1));
            double lateCost = Ratio(formula.MonsterHealth(100), formula.GoldReward(100));
            Assert.Greater(lateCost, earlyCost);
        }

        [Test]
        public void DeepFloor_ExceedsDoubleRange()
        {
            var formula = CreateFormula();
            BigNumber health = formula.MonsterHealth(5000);

            // double은 약 1e308에서 한계에 도달한다. 지수는 그 너머로도 계속 올라가야 한다.
            Assert.Greater(health.Exponent, 308);
        }
    }
}
