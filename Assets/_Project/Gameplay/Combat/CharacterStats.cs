using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>플레이어의 전투 수치. 강화가 이 값을 직접 변경한다.</summary>
    public sealed class CharacterStats
    {
        public BigNumber AttackPower { get; set; }

        public double AttacksPerSecond { get; set; }

        /// <summary>치명타 확률. [0, 1] 범위.</summary>
        public double CriticalChance { get; set; }

        /// <summary>치명타 시 적용되는 데미지 배율.</summary>
        public double CriticalMultiplier { get; set; }

        /// <summary>골드 획득량에 곱해지는 배율. 아무 보정이 없으면 1이다.</summary>
        public double GoldMultiplier { get; set; } = 1d;
    }
}
