using System;
using Game.Core;
using Game.Gameplay.Combat;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class DamageFormulaTests
    {
        [Test]
        public void NoDefense_LetsFullDamageThrough()
        {
            Assert.AreEqual(1d, DamageFormula.MultiplierAgainst(0d), 1e-9);
        }

        [Test]
        public void DefenseEqualToTheConstant_HalvesDamage()
        {
            Assert.AreEqual(0.5d, DamageFormula.MultiplierAgainst(DamageFormula.DefenseConstant), 1e-9);
        }

        [Test]
        public void Multiplier_NeverReachesZero()
        {
            // 비율 감소형을 쓰는 이유. 감산형이었다면 여기서 데미지가 0이 된다.
            Assert.Greater(DamageFormula.MultiplierAgainst(1e6d), 0d);
        }

        [Test]
        public void MoreDefense_AlwaysMeansLessDamage()
        {
            double low = DamageFormula.MultiplierAgainst(10d);
            double high = DamageFormula.MultiplierAgainst(200d);

            Assert.Greater(low, high);
        }

        [Test]
        public void Resolve_AppliesDefenseThenCritical()
        {
            BigNumber normal = DamageFormula.Resolve(1000d, DamageFormula.DefenseConstant, false, 2d);
            BigNumber critical = DamageFormula.Resolve(1000d, DamageFormula.DefenseConstant, true, 2d);

            Assert.AreEqual(500d, normal.ToDouble(), 1e-6);
            Assert.AreEqual(1000d, critical.ToDouble(), 1e-6);
        }

        [Test]
        public void Resolve_KeepsHugeAttackPowerIntact()
        {
            // 공격력이 double 범위를 넘어도 비율 감소는 지수를 건드리지 않는다.
            BigNumber damage = DamageFormula.Resolve(new BigNumber(1d, 400), 0d, false, 2d);

            Assert.AreEqual(400, damage.Exponent);
        }
    }

    public class CombatUnitTests
    {
        private static UnitStats Stats(double attack = 10d, double health = 100d, double defense = 0d)
            => new UnitStats
            {
                AttackPower = attack,
                MaxHealth = health,
                Defense = defense,
                CriticalChance = 0d,
                CriticalMultiplier = 2d
            };

        [Test]
        public void NewUnit_StartsAtFullHealth()
        {
            var unit = new CombatUnit(PartySlot.Warrior, Stats(health: 250d));

            Assert.AreEqual(250d, unit.Health.ToDouble(), 1e-9);
            Assert.IsTrue(unit.IsAlive);
        }

        [Test]
        public void AttackInterval_ComesFromTheSlot()
        {
            Assert.AreEqual(AttackIntervals.ForSlot(PartySlot.Archer),
                new CombatUnit(PartySlot.Archer, Stats()).AttackInterval, 1e-9);

            // 궁수가 가장 빠르고 마법사가 가장 느리다.
            Assert.Less(AttackIntervals.ForSlot(PartySlot.Archer), AttackIntervals.ForSlot(PartySlot.Warrior));
            Assert.Less(AttackIntervals.ForSlot(PartySlot.Warrior), AttackIntervals.ForSlot(PartySlot.Mage));
        }

        [Test]
        public void TakingDamage_ReducesHealth()
        {
            var unit = new CombatUnit(PartySlot.Mage, Stats(health: 100d));

            unit.TakeDamage(30d);

            Assert.AreEqual(70d, unit.Health.ToDouble(), 1e-9);
            Assert.IsTrue(unit.IsAlive);
        }

        [Test]
        public void LethalDamage_ClampsHealthToZero()
        {
            var unit = new CombatUnit(PartySlot.Mage, Stats(health: 100d));

            unit.TakeDamage(500d);

            Assert.AreEqual(0d, unit.Health.ToDouble(), 1e-9);
            Assert.IsFalse(unit.IsAlive);
        }

        [Test]
        public void DeadUnit_TakesNoFurtherDamage()
        {
            var unit = new CombatUnit(PartySlot.Mage, Stats(health: 100d));
            unit.TakeDamage(500d);

            unit.TakeDamage(500d);

            Assert.AreEqual(0d, unit.Health.ToDouble(), 1e-9);
        }

        [Test]
        public void Restore_RefillsHealth()
        {
            var unit = new CombatUnit(PartySlot.Warrior, Stats(health: 100d));
            unit.TakeDamage(500d);

            unit.Restore();

            Assert.IsTrue(unit.IsAlive);
            Assert.AreEqual(100d, unit.Health.ToDouble(), 1e-9);
        }

        [Test]
        public void FirstAttack_FiresImmediately()
        {
            var unit = new CombatUnit(PartySlot.Warrior, Stats());   // 주기 1.0초

            unit.AdvanceCooldown(0.01d);

            // 대기시간 0에서 시작하므로 전투가 시작되자마자 한 대 친다.
            Assert.IsTrue(unit.IsAttackDue);
        }

        [Test]
        public void AfterAttacking_TheUnitWaitsItsInterval()
        {
            var unit = new CombatUnit(PartySlot.Warrior, Stats());   // 주기 1.0초
            unit.AdvanceCooldown(0.5d);
            unit.ConsumeAttack();

            Assert.IsFalse(unit.IsAttackDue, "0.5초 지났으므로 아직 다음 차례가 아니다");

            unit.AdvanceCooldown(0.6d);
            Assert.IsTrue(unit.IsAttackDue);
        }

        [Test]
        public void LongDelta_LetsSeveralAttacksBeConsumed()
        {
            var unit = new CombatUnit(PartySlot.Warrior, Stats());   // 주기 1.0초
            unit.AdvanceCooldown(3d);

            int attacks = 0;
            while (unit.IsAttackDue)
            {
                attacks++;
                unit.ConsumeAttack();
            }

            Assert.AreEqual(3, attacks);
        }
    }

    public class PartyTests
    {
        private static CombatUnit Unit(PartySlot slot, double health = 100d)
            => new CombatUnit(slot, new UnitStats
            {
                AttackPower = 10d,
                MaxHealth = health,
                Defense = 0d,
                CriticalChance = 0d,
                CriticalMultiplier = 2d
            });

        private static Party CreateParty(double health = 100d)
            => new Party(Unit(PartySlot.Warrior, health), Unit(PartySlot.Archer, health), Unit(PartySlot.Mage, health));

        [Test]
        public void Units_AreOrderedBySlot()
        {
            Party party = CreateParty();

            Assert.AreEqual(PartySlot.Warrior, party.Units[0].Slot);
            Assert.AreEqual(PartySlot.Archer, party.Units[1].Slot);
            Assert.AreEqual(PartySlot.Mage, party.Units[2].Slot);
        }

        [Test]
        public void Get_ReturnsTheUnitInThatSlot()
        {
            Party party = CreateParty();

            Assert.AreEqual(PartySlot.Mage, party.Get(PartySlot.Mage).Slot);
        }

        [Test]
        public void Constructor_RejectsAUnitInTheWrongSlot()
        {
            Assert.Throws<ArgumentException>(() =>
                new Party(Unit(PartySlot.Mage), Unit(PartySlot.Archer), Unit(PartySlot.Warrior)));
        }

        [Test]
        public void FirstAlive_IsTheWarriorWhileHeStands()
        {
            Party party = CreateParty();

            Assert.AreEqual(PartySlot.Warrior, party.FirstAlive.Slot);
        }

        [Test]
        public void FirstAlive_MovesDownTheOrderAsUnitsFall()
        {
            Party party = CreateParty();

            party.Get(PartySlot.Warrior).TakeDamage(999d);
            Assert.AreEqual(PartySlot.Archer, party.FirstAlive.Slot);

            party.Get(PartySlot.Archer).TakeDamage(999d);
            Assert.AreEqual(PartySlot.Mage, party.FirstAlive.Slot);
        }

        [Test]
        public void Party_IsWipedOnlyWhenEveryoneIsDown()
        {
            Party party = CreateParty();

            party.Get(PartySlot.Warrior).TakeDamage(999d);
            party.Get(PartySlot.Archer).TakeDamage(999d);
            Assert.IsFalse(party.IsWiped);

            party.Get(PartySlot.Mage).TakeDamage(999d);
            Assert.IsTrue(party.IsWiped);
            Assert.IsNull(party.FirstAlive);
        }

        [Test]
        public void RestoreAll_BringsEveryoneBack()
        {
            Party party = CreateParty();
            foreach (CombatUnit unit in party.Units) unit.TakeDamage(999d);

            party.RestoreAll();

            Assert.IsFalse(party.IsWiped);
            Assert.AreEqual(PartySlot.Warrior, party.FirstAlive.Slot);
        }
    }
}
