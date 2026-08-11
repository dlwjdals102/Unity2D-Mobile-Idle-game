using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Equipment;
using Game.Gameplay.Progression;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class StatComposerTests
    {
        private const double BaseCriticalChance = 0.15d;

        private CharacterStats _stats;
        private BattleRunner _battle;
        private Inventory _inventory;
        private StatComposer _composer;

        private void StartWithGold(double gold)
        {
            _stats = new CharacterStats { AttacksPerSecond = 1d };

            _battle = new BattleRunner(
                FloorFormula.Default,
                _stats,
                ScriptedRandomSource.NeverCritical,
                new BattleProgress(1, 0, gold, 0));

            _inventory = new Inventory();
            _composer = new StatComposer(_stats, StatUpgrades.CreateDefault(_battle), _inventory, BaseCriticalChance);
        }

        private EquipmentDefinition Own(int id)
        {
            EquipmentDefinition definition = EquipmentTable.Default.Find(id);
            _inventory.Add(definition);
            return definition;
        }

        // ---------- 강화만 있을 때 ----------

        [Test]
        public void Construction_WritesLevelZeroValuesIntoStats()
        {
            StartWithGold(0d);

            Assert.AreEqual(_composer.Upgrades.AttackPower.Value.ToDouble(), _stats.AttackPower.ToDouble(), 1e-9);
            Assert.AreEqual(BaseCriticalChance, _stats.CriticalChance, 1e-9);
            Assert.AreEqual(1d, _stats.GoldMultiplier, 1e-9, "장비가 없으면 골드 배율은 1이다");
        }

        [Test]
        public void PurchasingUpgrade_RaisesTheStatUsedInCombat()
        {
            StartWithGold(1000d);
            double before = _stats.AttackPower.ToDouble();

            Assert.IsTrue(_composer.TryPurchaseUpgrade(_composer.Upgrades.AttackPower));

            Assert.Greater(_stats.AttackPower.ToDouble(), before);
        }

        [Test]
        public void FailedPurchase_LeavesStatsUnchanged()
        {
            StartWithGold(0d);
            double before = _stats.AttackPower.ToDouble();

            Assert.IsFalse(_composer.TryPurchaseUpgrade(_composer.Upgrades.AttackPower));

            Assert.AreEqual(before, _stats.AttackPower.ToDouble(), 1e-9);
        }

        [Test]
        public void RestoreUpgrades_AppliesLoadedLevels()
        {
            StartWithGold(0d);

            _composer.RestoreUpgrades(attackPowerLevel: 10, criticalMultiplierLevel: 4);

            Assert.AreEqual(_composer.Upgrades.AttackPower.Value.ToDouble(), _stats.AttackPower.ToDouble(), 1e-9);
            Assert.AreEqual(_composer.Upgrades.CriticalMultiplier.Value.ToDouble(), _stats.CriticalMultiplier, 1e-9);
        }

        // ---------- 장비가 더해질 때 ----------

        [Test]
        public void EquippingWeapon_MultipliesAttackPower()
        {
            StartWithGold(0d);
            double withoutWeapon = _stats.AttackPower.ToDouble();
            EquipmentDefinition weapon = Own(103);   // 룬 블레이드, 1.7배

            Assert.IsTrue(_composer.TryEquip(weapon));

            Assert.AreEqual(withoutWeapon * weapon.Value, _stats.AttackPower.ToDouble(), 1e-9);
        }

        [Test]
        public void EquippingCharm_SetsGoldMultiplier()
        {
            StartWithGold(0d);
            EquipmentDefinition charm = Own(205);   // 금화의 심장, 2.5배

            _composer.TryEquip(charm);

            Assert.AreEqual(charm.Value, _stats.GoldMultiplier, 1e-9);
        }

        [Test]
        public void EquippingRing_AddsToCriticalChance()
        {
            StartWithGold(0d);
            EquipmentDefinition ring = Own(302);   // 예리한 반지, +0.06

            _composer.TryEquip(ring);

            Assert.AreEqual(BaseCriticalChance + ring.Value, _stats.CriticalChance, 1e-9);
        }

        [Test]
        public void CriticalChance_IsCapped()
        {
            StartWithGold(0d);

            // 기본 확률 자체가 상한을 넘도록 만든 뒤 반지까지 끼운다.
            _stats = new CharacterStats { AttacksPerSecond = 1d };
            _inventory = new Inventory();
            var composer = new StatComposer(
                _stats, StatUpgrades.CreateDefault(_battle), _inventory, baseCriticalChance: 0.9d);

            EquipmentDefinition ring = EquipmentTable.Default.Find(305);
            _inventory.Add(ring);
            composer.TryEquip(ring);

            Assert.AreEqual(StatComposer.MaxCriticalChance, _stats.CriticalChance, 1e-9);
        }

        [Test]
        public void EquippingUnownedItem_Fails_AndLeavesStatsUnchanged()
        {
            StartWithGold(0d);
            double before = _stats.AttackPower.ToDouble();
            EquipmentDefinition weapon = EquipmentTable.Default.Find(105);   // 보유하지 않았다

            Assert.IsFalse(_composer.TryEquip(weapon));

            Assert.AreEqual(before, _stats.AttackPower.ToDouble(), 1e-9);
        }

        [Test]
        public void EquippingInTheSameSlot_ReplacesThePreviousItem()
        {
            StartWithGold(0d);
            double bare = _stats.AttackPower.ToDouble();

            _composer.TryEquip(Own(101));                       // 1.10배
            EquipmentDefinition better = Own(105);              // 3.50배
            _composer.TryEquip(better);

            // 배율이 겹쳐 곱해지지 않고 새 무기 것만 적용된다.
            Assert.AreEqual(bare * better.Value, _stats.AttackPower.ToDouble(), 1e-9);
        }

        [Test]
        public void UpgradesAndEquipment_CompoundTogether()
        {
            StartWithGold(1e9d);
            _composer.RestoreUpgrades(attackPowerLevel: 30, criticalMultiplierLevel: 0);

            EquipmentDefinition weapon = Own(104);   // 2.4배
            _composer.TryEquip(weapon);

            double expected = _composer.Upgrades.AttackPower.Value.ToDouble() * weapon.Value;
            Assert.AreEqual(expected, _stats.AttackPower.ToDouble(), 1e-6);
        }
    }
}
