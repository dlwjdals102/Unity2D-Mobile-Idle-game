using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Progression;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class StatUpgradesTests
    {
        private CharacterStats _stats;
        private BattleRunner _battle;
        private StatUpgrades _upgrades;

        private void StartWithGold(double gold)
        {
            _stats = new CharacterStats
            {
                AttacksPerSecond = 1d,
                CriticalChance = 0d,
                CriticalMultiplier = 2d
            };

            _battle = new BattleRunner(
                FloorFormula.Default,
                _stats,
                ScriptedRandomSource.NeverCritical,
                new BattleProgress(1, 0, gold, 0));

            _upgrades = StatUpgrades.CreateDefault(_stats, _battle);
        }

        [Test]
        public void Construction_AppliesLevelZeroValuesToStats()
        {
            StartWithGold(0d);

            Assert.AreEqual(_upgrades.AttackPower.Value.ToDouble(), _stats.AttackPower.ToDouble(), 1e-9);
            Assert.AreEqual(_upgrades.CriticalMultiplier.Value.ToDouble(), _stats.CriticalMultiplier, 1e-9);
            Assert.AreEqual(0, _upgrades.AttackPower.Level);
        }

        [Test]
        public void Purchase_WithoutEnoughGold_ChangesNothing()
        {
            StartWithGold(0d);
            BigNumber goldBefore = _battle.Gold;

            Assert.IsFalse(_upgrades.TryPurchase(_upgrades.AttackPower));

            Assert.AreEqual(0, _upgrades.AttackPower.Level);
            Assert.AreEqual(goldBefore.ToDouble(), _battle.Gold.ToDouble(), 1e-9);
        }

        [Test]
        public void Purchase_DeductsGoldAndRaisesLevel()
        {
            StartWithGold(1000d);
            double cost = _upgrades.AttackPower.Cost.ToDouble();

            Assert.IsTrue(_upgrades.TryPurchase(_upgrades.AttackPower));

            Assert.AreEqual(1, _upgrades.AttackPower.Level);
            Assert.AreEqual(1000d - cost, _battle.Gold.ToDouble(), 1e-9);
        }

        [Test]
        public void Purchase_RaisesTheStatUsedInCombat()
        {
            StartWithGold(1000d);
            double attackBefore = _stats.AttackPower.ToDouble();

            _upgrades.TryPurchase(_upgrades.AttackPower);

            Assert.Greater(_stats.AttackPower.ToDouble(), attackBefore);
            Assert.AreEqual(_upgrades.AttackPower.Value.ToDouble(), _stats.AttackPower.ToDouble(), 1e-9);
        }

        [Test]
        public void Purchase_RaisesTheCostOfTheNextLevel()
        {
            StartWithGold(1000d);
            double costBefore = _upgrades.AttackPower.Cost.ToDouble();

            _upgrades.TryPurchase(_upgrades.AttackPower);

            Assert.Greater(_upgrades.AttackPower.Cost.ToDouble(), costBefore);
        }

        [Test]
        public void Purchase_OnlyAffectsTheChosenUpgrade()
        {
            StartWithGold(1000d);
            double criticalBefore = _stats.CriticalMultiplier;

            _upgrades.TryPurchase(_upgrades.AttackPower);

            Assert.AreEqual(0, _upgrades.CriticalMultiplier.Level);
            Assert.AreEqual(criticalBefore, _stats.CriticalMultiplier, 1e-9);
        }

        [Test]
        public void Restore_SetsLevelsAndAppliesThemWithoutCharging()
        {
            StartWithGold(1000d);

            _upgrades.Restore(attackPowerLevel: 12, criticalMultiplierLevel: 3);

            Assert.AreEqual(12, _upgrades.AttackPower.Level);
            Assert.AreEqual(3, _upgrades.CriticalMultiplier.Level);
            Assert.AreEqual(_upgrades.AttackPower.Value.ToDouble(), _stats.AttackPower.ToDouble(), 1e-9);
            Assert.AreEqual(1000d, _battle.Gold.ToDouble(), 1e-9, "복원은 골드를 쓰지 않는다");
        }

        [Test]
        public void Cost_GrowsFasterThanTheEffect()
        {
            StartWithGold(0d);
            StatUpgrade upgrade = _upgrades.AttackPower;

            BigNumber costAtStart = upgrade.Cost;
            BigNumber valueAtStart = upgrade.Value;

            _upgrades.Restore(attackPowerLevel: 20, criticalMultiplierLevel: 0);

            double costRatio = (upgrade.Cost / costAtStart).ToDouble();
            double valueRatio = (upgrade.Value / valueAtStart).ToDouble();

            // 이 관계가 뒤집히면 강화가 갈수록 싸져 성장이 발산한다. 설계 자체를 테스트로 고정한다.
            Assert.Greater(costRatio, valueRatio);
        }
    }
}
