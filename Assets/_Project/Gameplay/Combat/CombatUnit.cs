using System;
using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 전투에 참여 중인 아군 한 명. 현재 체력과 다음 공격까지 남은 시간을 들고 있다.
    /// 죽은 유닛은 그 층이 끝날 때까지 되살아나지 않는다.
    /// </summary>
    public sealed class CombatUnit
    {
        private double _secondsUntilNextAttack;

        public CombatUnit(PartySlot slot, UnitStats stats)
        {
            Slot = slot;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));

            Health = stats.MaxHealth;
        }

        public PartySlot Slot { get; }

        public UnitStats Stats { get; }

        public BigNumber Health { get; private set; }

        public bool IsAlive => Health > BigNumber.Zero;

        public double AttackInterval => AttackIntervals.ForSlot(Slot);

        /// <summary>공격 주기가 돌아왔는지. 긴 델타를 따라잡을 수 있도록 여러 번 참이 될 수 있다.</summary>
        public bool IsAttackDue => _secondsUntilNextAttack < 0d;

        public void AdvanceCooldown(double deltaSeconds) => _secondsUntilNextAttack -= deltaSeconds;

        /// <summary>공격 한 번을 소비한다. <see cref="IsAttackDue"/>가 참일 때만 부른다.</summary>
        public void ConsumeAttack() => _secondsUntilNextAttack += AttackInterval;

        public void TakeDamage(BigNumber amount)
        {
            if (!IsAlive) return;

            Health -= amount;
            if (Health < BigNumber.Zero) Health = BigNumber.Zero;
        }

        /// <summary>체력을 가득 채우고 공격 주기를 초기화한다. 층이 바뀔 때 호출된다.</summary>
        public void Restore()
        {
            Health = Stats.MaxHealth;
            _secondsUntilNextAttack = 0d;
        }
    }
}
