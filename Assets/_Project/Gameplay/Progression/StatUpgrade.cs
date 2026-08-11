using Game.Core;

namespace Game.Gameplay.Progression
{
    /// <summary>
    /// 골드로 무한히 올릴 수 있는 스탯 하나.
    /// 비용 성장률을 효과 성장률보다 크게 잡아, 단계가 오를수록 같은 성장에 드는 골드가 늘어난다.
    /// 이 격차가 사실상의 상한처럼 작동해 플레이어를 다른 성장 축으로 밀어낸다.
    /// </summary>
    public sealed class StatUpgrade
    {
        private readonly BigNumber _baseValue;
        private readonly double _valueGrowth;
        private readonly BigNumber _baseCost;
        private readonly double _costGrowth;

        public StatUpgrade(BigNumber baseValue, double valueGrowth, BigNumber baseCost, double costGrowth)
        {
            _baseValue = baseValue;
            _valueGrowth = valueGrowth;
            _baseCost = baseCost;
            _costGrowth = costGrowth;
        }

        public int Level { get; private set; }

        /// <summary>다음 단계로 올리는 데 드는 골드.</summary>
        public BigNumber Cost => _baseCost * BigNumber.Pow(_costGrowth, Level);

        /// <summary>현재 단계의 효과값.</summary>
        public BigNumber Value => _baseValue * BigNumber.Pow(_valueGrowth, Level);

        // 단계를 바꾸는 것은 같은 어셈블리의 StatUpgrades만 할 수 있다.
        // 결제와 스탯 반영을 거치지 않고 단계만 오르는 경로를 막기 위해서다.
        internal void Increase() => Level++;

        internal void Restore(int level) => Level = level;
    }
}
