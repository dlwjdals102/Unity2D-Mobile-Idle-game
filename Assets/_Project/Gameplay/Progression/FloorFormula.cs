using Game.Core;

namespace Game.Gameplay.Progression
{
    /// <summary>
    /// 층별 몬스터 체력과 골드 보상. 둘 다 등비로 증가하되 골드가 더 느리게 자라도록 두어,
    /// 층이 오를수록 진행이 느려지고 플레이어가 다른 성장 축으로 옮겨가게 만든다.
    /// 층 번호는 1부터 시작하며, 1층이 기본값이다.
    /// </summary>
    public sealed class FloorFormula
    {
        private readonly BigNumber _baseMonsterHealth;
        private readonly double _monsterHealthGrowth;
        private readonly BigNumber _baseGoldReward;
        private readonly double _goldRewardGrowth;

        public FloorFormula(
            BigNumber baseMonsterHealth,
            double monsterHealthGrowth,
            BigNumber baseGoldReward,
            double goldRewardGrowth)
        {
            _baseMonsterHealth = baseMonsterHealth;
            _monsterHealthGrowth = monsterHealthGrowth;
            _baseGoldReward = baseGoldReward;
            _goldRewardGrowth = goldRewardGrowth;
        }

        /// <summary>기획서의 1차 밸런싱 값. 데이터 테이블이 생기면 그쪽으로 옮긴다.</summary>
        public static FloorFormula Default => new FloorFormula(10d, 1.16d, 5d, 1.14d);

        public BigNumber MonsterHealth(int floor) => _baseMonsterHealth * BigNumber.Pow(_monsterHealthGrowth, floor - 1);

        public BigNumber GoldReward(int floor) => _baseGoldReward * BigNumber.Pow(_goldRewardGrowth, floor - 1);
    }
}
