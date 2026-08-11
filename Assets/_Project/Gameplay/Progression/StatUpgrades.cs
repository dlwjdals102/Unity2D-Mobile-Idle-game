using System;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Progression
{
    /// <summary>
    /// 스탯 강화 묶음. 골드를 차감하고 단계를 올리는 것까지가 책임이다.
    /// <para>
    /// 전투 스탯에 반영하는 일은 하지 않는다. 장비도 같은 스탯에 기여하므로
    /// 여러 곳에서 대입하면 서로를 덮어쓴다. 반영은 <c>StatComposer</c>가 혼자 한다.
    /// </para>
    /// </summary>
    public sealed class StatUpgrades
    {
        private readonly BattleRunner _battle;

        public StatUpgrades(BattleRunner battle, StatUpgrade attackPower, StatUpgrade criticalMultiplier)
        {
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            AttackPower = attackPower ?? throw new ArgumentNullException(nameof(attackPower));
            CriticalMultiplier = criticalMultiplier ?? throw new ArgumentNullException(nameof(criticalMultiplier));
        }

        public StatUpgrade AttackPower { get; }

        public StatUpgrade CriticalMultiplier { get; }

        /// <summary>기획서의 1차 밸런싱 값. 데이터 테이블이 생기면 그쪽으로 옮긴다.</summary>
        public static StatUpgrades CreateDefault(BattleRunner battle)
            => new StatUpgrades(
                battle,
                attackPower: new StatUpgrade(5d, 1.075d, 10d, 1.095d),
                criticalMultiplier: new StatUpgrade(2d, 1.02d, 100d, 1.15d));

        /// <summary>
        /// 골드가 충분하면 차감하고 한 단계 올린다. 부족하면 아무것도 하지 않고 <c>false</c>.
        /// </summary>
        public bool TryPurchase(StatUpgrade upgrade)
        {
            if (upgrade == null) throw new ArgumentNullException(nameof(upgrade));

            if (!_battle.TrySpendGold(upgrade.Cost)) return false;

            upgrade.Increase();
            return true;
        }

        /// <summary>세이브에서 불러온 단계를 그대로 적용한다. 골드는 차감하지 않는다.</summary>
        public void Restore(int attackPowerLevel, int criticalMultiplierLevel)
        {
            AttackPower.Restore(attackPowerLevel);
            CriticalMultiplier.Restore(criticalMultiplierLevel);
        }
    }
}
