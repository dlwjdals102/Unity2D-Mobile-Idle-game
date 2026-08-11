using Game.Core;
using Game.Gameplay.Combat;
using Game.Gameplay.Progression;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class BattleRunnerTests
    {
        private const double FirstFloorMonsterHealth = 10d;
        private const double FirstFloorGoldReward = 5d;

        /// <summary>초당 1회 공격. Tick(1.0) 한 번이 정확히 공격 한 번이 되도록 맞춘 값이다.</summary>
        private static CharacterStats Stats(double attackPower, double attacksPerSecond = 1d)
            => new CharacterStats
            {
                AttackPower = attackPower,
                AttacksPerSecond = attacksPerSecond,
                CriticalChance = 0.5d,
                CriticalMultiplier = 2d
            };

        private static BattleRunner CreateRunner(CharacterStats stats, IRandomSource random = null)
            => new BattleRunner(FloorFormula.Default, stats, random ?? ScriptedRandomSource.NeverCritical);

        private static void AssertValue(BigNumber actual, double expected)
            => Assert.AreEqual(expected, actual.ToDouble(), 1e-6);

        [Test]
        public void NewRunner_StartsOnFirstFloorWithFullMonster()
        {
            var runner = CreateRunner(Stats(1d));

            Assert.AreEqual(1, runner.Floor);
            Assert.AreEqual(0, runner.KillsOnFloor);
            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth);
            AssertValue(runner.Gold, 0d);
        }

        [Test]
        public void Tick_DealsDamageToMonster()
        {
            var runner = CreateRunner(Stats(3d));

            runner.Tick(1d);

            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 3d);
        }

        [Test]
        public void CriticalHit_AppliesMultiplier()
        {
            var runner = CreateRunner(Stats(3d), ScriptedRandomSource.AlwaysCritical);

            runner.Tick(1d);

            // 데미지 3에 치명타 배율 2배가 적용된다.
            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 6d);
        }

        [Test]
        public void KillingMonster_AwardsGoldAndSpawnsReplacement()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));

            runner.Tick(1d);

            Assert.AreEqual(1, runner.KillsOnFloor);
            AssertValue(runner.Gold, FirstFloorGoldReward);
            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth);
        }

        [Test]
        public void ClearingRequiredKills_AdvancesFloor()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));

            for (int i = 0; i < BattleRunner.KillsPerFloor; i++) runner.Tick(1d);

            Assert.AreEqual(2, runner.Floor);
            Assert.AreEqual(0, runner.KillsOnFloor);
            AssertValue(runner.Gold, FirstFloorGoldReward * BattleRunner.KillsPerFloor);
        }

        [Test]
        public void AdvancingFloor_SpawnsTougherMonster()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));

            for (int i = 0; i < BattleRunner.KillsPerFloor; i++) runner.Tick(1d);

            Assert.Greater(runner.MonsterHealth.ToDouble(), FirstFloorMonsterHealth);
            AssertValue(runner.MonsterHealth, FloorFormula.Default.MonsterHealth(2).ToDouble());
        }

        [Test]
        public void AttackRate_ResolvesEveryAttackWithinOneTick()
        {
            var runner = CreateRunner(Stats(1d, attacksPerSecond: 4d));

            runner.Tick(1d);

            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 4d);
        }

        [Test]
        public void LongDelta_DoesNotLoseAttacks()
        {
            var runner = CreateRunner(Stats(1d));

            runner.Tick(5d);

            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 5d);
        }

        [Test]
        public void SmallDeltas_AccumulateIntoAnAttack()
        {
            var runner = CreateRunner(Stats(1d));
            runner.Tick(0.4d);   // 대기시간이 음수가 되는 즉시 첫 공격이 나간다

            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 1d);

            runner.Tick(0.4d);   // 0.6초 남아 아직 공격 차례가 아니다
            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 1d);

            runner.Tick(0.4d);   // 이제 주기를 넘겼다
            AssertValue(runner.MonsterHealth, FirstFloorMonsterHealth - 2d);
        }

        // ---------- 보스 층 ----------

        /// <summary>목표 층에 도달할 때까지 한 방에 처치하는 공격을 반복한다.</summary>
        private static void AdvanceTo(BattleRunner runner, int targetFloor)
        {
            for (int i = 0; i < 10000 && runner.Floor < targetFloor; i++) runner.Tick(1d);
            Assert.AreEqual(targetFloor, runner.Floor, "목표 층에 도달하지 못했다");
        }

        /// <summary>첫 보스 층까지 올라간 뒤, 보스가 죽지 않도록 플레이어를 약하게 만든다.</summary>
        private static BattleRunner CreateRunnerStalledOnBoss()
        {
            var stats = Stats(1e6d);
            var runner = CreateRunner(stats);
            AdvanceTo(runner, BattleRunner.BossFloorInterval);

            stats.AttackPower = 1d;
            stats.AttacksPerSecond = 0.01d;
            return runner;
        }

        [Test]
        public void EveryTenthFloor_IsABossFloorNeedingOneKill()
        {
            var runner = CreateRunner(Stats(1e6d));

            Assert.IsFalse(runner.IsBossFloor);
            Assert.AreEqual(BattleRunner.KillsPerFloor, runner.RequiredKills);

            AdvanceTo(runner, BattleRunner.BossFloorInterval);

            Assert.IsTrue(runner.IsBossFloor);
            Assert.AreEqual(1, runner.RequiredKills);
        }

        [Test]
        public void Boss_HasMultipliedHealth()
        {
            var runner = CreateRunner(Stats(1e6d));
            AdvanceTo(runner, BattleRunner.BossFloorInterval);

            double normalHealth = FloorFormula.Default.MonsterHealth(BattleRunner.BossFloorInterval).ToDouble();

            Assert.AreEqual(normalHealth * 12d, runner.MonsterMaxHealth.ToDouble(), 1e-6);
        }

        [Test]
        public void KillingBoss_AdvancesPastTheBossFloor()
        {
            var runner = CreateRunner(Stats(1e6d));
            AdvanceTo(runner, BattleRunner.BossFloorInterval);

            runner.Tick(1d);

            Assert.AreEqual(BattleRunner.BossFloorInterval + 1, runner.Floor);
            Assert.IsFalse(runner.IsBossFloor);
            Assert.AreEqual(0, runner.KillsOnFloor);
        }

        [Test]
        public void BossTimer_CountsDown()
        {
            BattleRunner runner = CreateRunnerStalledOnBoss();

            runner.Tick(5d);

            Assert.AreEqual(BattleRunner.BossTimeLimitSeconds - 5d, runner.BossSecondsRemaining, 1e-6);
        }

        [Test]
        public void BossTimer_Expiring_RestoresBossAndRestartsTimer()
        {
            BattleRunner runner = CreateRunnerStalledOnBoss();

            runner.Tick(29d);
            Assert.Less(runner.MonsterHealth.ToDouble(), runner.MonsterMaxHealth.ToDouble(), "보스가 데미지를 받지 않았다");

            runner.Tick(2d);   // 제한시간을 0 아래로 넘긴다

            Assert.AreEqual(BattleRunner.BossTimeLimitSeconds, runner.BossSecondsRemaining, 1e-6);
            Assert.AreEqual(runner.MonsterMaxHealth.ToDouble(), runner.MonsterHealth.ToDouble(), 1e-6);
            Assert.AreEqual(BattleRunner.BossFloorInterval, runner.Floor, "시도에 실패하면 같은 층에 머물러야 한다");
        }

        [Test]
        public void KillingBoss_AwardsDiamonds()
        {
            var runner = CreateRunner(Stats(1e6d));
            AdvanceTo(runner, BattleRunner.BossFloorInterval);

            Assert.AreEqual(0, runner.Diamonds, "보스 층에 도달한 것만으로는 다이아가 나오지 않는다");

            runner.Tick(1d);

            Assert.AreEqual(BattleRunner.BossDiamondReward, runner.Diamonds);
        }

        [Test]
        public void KillingNormalMonsters_AwardsNoDiamonds()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));

            for (int i = 0; i < 5; i++) runner.Tick(1d);

            Assert.AreEqual(5, runner.KillsOnFloor);
            Assert.AreEqual(0, runner.Diamonds);
        }

        [Test]
        public void FailedBossAttempt_AwardsNoDiamonds()
        {
            BattleRunner runner = CreateRunnerStalledOnBoss();

            runner.Tick(29d);
            runner.Tick(2d);   // 제한시간 초과로 보스가 회복한다

            Assert.AreEqual(0, runner.Diamonds);
        }

        // ---------- 개발용 층 클리어 ----------

        [Test]
        public void ClearFloorImmediately_AdvancesExactlyOneFloor()
        {
            var runner = CreateRunner(Stats(1d));

            runner.ClearFloorImmediately();

            Assert.AreEqual(2, runner.Floor);
            Assert.AreEqual(0, runner.KillsOnFloor);
        }

        [Test]
        public void ClearFloorImmediately_AwardsGoldForEveryMonsterOnTheFloor()
        {
            var runner = CreateRunner(Stats(1d));

            runner.ClearFloorImmediately();

            AssertValue(runner.Gold, FirstFloorGoldReward * BattleRunner.KillsPerFloor);
        }

        [Test]
        public void ClearFloorImmediately_OnlyKillsTheRemainingMonsters()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));
            for (int i = 0; i < 4; i++) runner.Tick(1d);   // 4마리는 직접 잡는다

            runner.ClearFloorImmediately();

            Assert.AreEqual(2, runner.Floor);

            // 남은 6마리만 추가로 처치되므로 보상 총액은 10마리분이다.
            AssertValue(runner.Gold, FirstFloorGoldReward * BattleRunner.KillsPerFloor);
        }

        [Test]
        public void ClearFloorImmediately_OnABossFloor_AwardsDiamonds()
        {
            BattleRunner runner = CreateRunnerStalledOnBoss();

            runner.ClearFloorImmediately();

            Assert.AreEqual(BattleRunner.BossFloorInterval + 1, runner.Floor);
            Assert.AreEqual(BattleRunner.BossDiamondReward, runner.Diamonds);
        }

        [Test]
        public void ClearFloorImmediately_SpawnsTheNextFloorsMonster()
        {
            var runner = CreateRunner(Stats(1d));

            runner.ClearFloorImmediately();

            AssertValue(runner.MonsterMaxHealth, FloorFormula.Default.MonsterHealth(2).ToDouble());
            AssertValue(runner.MonsterHealth, runner.MonsterMaxHealth.ToDouble());
        }

        // ---------- 이벤트 ----------

        [Test]
        public void DamageDealt_ReportsAmountAndCriticalFlag()
        {
            var runner = CreateRunner(Stats(3d), ScriptedRandomSource.AlwaysCritical);
            DamageResult? captured = null;
            runner.DamageDealt += result => captured = result;

            runner.Tick(1d);

            Assert.IsTrue(captured.HasValue, "DamageDealt 이벤트가 발생하지 않았다");
            Assert.AreEqual(6d, captured.Value.Amount.ToDouble(), 1e-6);
            Assert.IsTrue(captured.Value.IsCritical);
        }

        [Test]
        public void DamageDealt_FlagsNonCriticalHits()
        {
            var runner = CreateRunner(Stats(3d), ScriptedRandomSource.NeverCritical);
            DamageResult? captured = null;
            runner.DamageDealt += result => captured = result;

            runner.Tick(1d);

            Assert.IsTrue(captured.HasValue);
            Assert.AreEqual(3d, captured.Value.Amount.ToDouble(), 1e-6);
            Assert.IsFalse(captured.Value.IsCritical);
        }

        [Test]
        public void MonsterKilled_RaisedOncePerKill()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));
            int kills = 0;
            runner.MonsterKilled += () => kills++;

            for (int i = 0; i < 3; i++) runner.Tick(1d);

            Assert.AreEqual(3, kills);
        }

        [Test]
        public void MonsterKilled_NotRaisedWhenMonsterSurvives()
        {
            var runner = CreateRunner(Stats(1d));
            int kills = 0;
            runner.MonsterKilled += () => kills++;

            runner.Tick(1d);

            Assert.AreEqual(0, kills);
        }

        // ---------- 골드 사용 ----------

        [Test]
        public void TrySpendGold_WithEnoughGold_Deducts()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));
            runner.Tick(1d);   // 5골드 획득

            Assert.IsTrue(runner.TrySpendGold(2d));
            AssertValue(runner.Gold, FirstFloorGoldReward - 2d);
        }

        [Test]
        public void TrySpendGold_WithoutEnoughGold_ChangesNothing()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));
            runner.Tick(1d);

            Assert.IsFalse(runner.TrySpendGold(FirstFloorGoldReward + 1d));
            AssertValue(runner.Gold, FirstFloorGoldReward);
        }

        [Test]
        public void TrySpendGold_ExactBalance_Succeeds()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));
            runner.Tick(1d);

            Assert.IsTrue(runner.TrySpendGold(FirstFloorGoldReward));
            AssertValue(runner.Gold, 0d);
        }

        // ---------- 진행 상태 저장과 복원 ----------

        [Test]
        public void NewRunner_StartsFromStartProgress()
        {
            var runner = CreateRunner(Stats(1d));

            Assert.AreEqual(1, runner.Progress.Floor);
            Assert.AreEqual(0, runner.Progress.KillsOnFloor);
            AssertValue(runner.Progress.Gold, 0d);
        }

        [Test]
        public void Progress_ReflectsCurrentState()
        {
            var runner = CreateRunner(Stats(FirstFloorMonsterHealth));

            runner.Tick(1d);
            runner.Tick(1d);

            Assert.AreEqual(1, runner.Progress.Floor);
            Assert.AreEqual(2, runner.Progress.KillsOnFloor);
            AssertValue(runner.Progress.Gold, FirstFloorGoldReward * 2d);
        }

        [Test]
        public void Constructor_WithProgress_RestoresState()
        {
            var progress = new BattleProgress(37, 4, new BigNumber(1.5d, 20), 9);

            var runner = new BattleRunner(FloorFormula.Default, Stats(1d), ScriptedRandomSource.NeverCritical, progress);

            Assert.AreEqual(37, runner.Floor);
            Assert.AreEqual(4, runner.KillsOnFloor);
            Assert.AreEqual(20, runner.Gold.Exponent);
            Assert.AreEqual(1.5d, runner.Gold.Mantissa, 1e-9);
            Assert.AreEqual(9, runner.Diamonds);
        }

        [Test]
        public void RestoredRunner_SpawnsMonsterForTheRestoredFloor()
        {
            var progress = new BattleProgress(37, 4, BigNumber.Zero, 0);

            var runner = new BattleRunner(FloorFormula.Default, Stats(1d), ScriptedRandomSource.NeverCritical, progress);

            AssertValue(runner.MonsterMaxHealth, FloorFormula.Default.MonsterHealth(37).ToDouble());
            AssertValue(runner.MonsterHealth, runner.MonsterMaxHealth.ToDouble());
        }

        [Test]
        public void RestoringOntoABossFloor_RestartsTheBossTimer()
        {
            var progress = new BattleProgress(BattleRunner.BossFloorInterval, 0, BigNumber.Zero, 0);

            var runner = new BattleRunner(FloorFormula.Default, Stats(1d), ScriptedRandomSource.NeverCritical, progress);

            Assert.IsTrue(runner.IsBossFloor);
            Assert.AreEqual(BattleRunner.BossTimeLimitSeconds, runner.BossSecondsRemaining, 1e-6);
        }

        [Test]
        public void Progress_SurvivesARoundTripThroughANewRunner()
        {
            var original = CreateRunner(Stats(FirstFloorMonsterHealth));
            for (int i = 0; i < 25; i++) original.Tick(1d);

            var restored = new BattleRunner(
                FloorFormula.Default, Stats(1d), ScriptedRandomSource.NeverCritical, original.Progress);

            Assert.AreEqual(original.Floor, restored.Floor);
            Assert.AreEqual(original.KillsOnFloor, restored.KillsOnFloor);
            Assert.AreEqual(original.Gold.Mantissa, restored.Gold.Mantissa, 1e-12);
            Assert.AreEqual(original.Gold.Exponent, restored.Gold.Exponent);
        }
    }
}
