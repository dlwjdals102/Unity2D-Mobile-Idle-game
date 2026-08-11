using System;
using Game.Gameplay.Combat;

namespace Game.Gameplay.Progression
{
    /// <summary>
    /// 스탯 강화 묶음. 구매하면 골드를 차감하고 단계를 올린 뒤 실제 전투 스탯에 반영한다.
    /// 이 세 가지가 항상 같이 일어나도록 한곳에 묶어두었다.
    /// </summary>
    public sealed class StatUpgrades
    {
        private readonly CharacterStats _stats;
        private readonly BattleRunner _battle;

        public StatUpgrades(CharacterStats stats, BattleRunner battle, StatUpgrade attackPower, StatUpgrade criticalMultiplier)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            _battle = battle ?? throw new ArgumentNullException(nameof(battle));
            AttackPower = attackPower ?? throw new ArgumentNullException(nameof(attackPower));
            CriticalMultiplier = criticalMultiplier ?? throw new ArgumentNullException(nameof(criticalMultiplier));

            Apply();
        }

        public StatUpgrade AttackPower { get; }

        public StatUpgrade CriticalMultiplier { get; }

        /// <summary>기획서의 1차 밸런싱 값. 데이터 테이블이 생기면 그쪽으로 옮긴다.</summary>
        public static StatUpgrades CreateDefault(CharacterStats stats, BattleRunner battle)
            => new StatUpgrades(
                stats,
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
            Apply();
            return true;
        }

        /// <summary>세이브에서 불러온 단계를 그대로 적용한다. 골드는 차감하지 않는다.</summary>
        public void Restore(int attackPowerLevel, int criticalMultiplierLevel)
        {
            AttackPower.Restore(attackPowerLevel);
            CriticalMultiplier.Restore(criticalMultiplierLevel);
            Apply();
        }

        private void Apply()
        {
            _stats.AttackPower = AttackPower.Value;

            // 치명타 배율은 항상 한 자릿수 근처라 double로 내려도 정밀도 문제가 없다.
            _stats.CriticalMultiplier = CriticalMultiplier.Value.ToDouble();
        }
    }
}
