using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 유닛 한 명의 전투 수치. 강화 단계와 장비에서 계산되어 채워진다.
    /// 공격 주기는 여기 없다. 직업별 고정 상수이므로 <see cref="AttackIntervals"/>가 들고 있다.
    /// </summary>
    public sealed class UnitStats
    {
        public BigNumber AttackPower { get; set; }

        public BigNumber MaxHealth { get; set; }

        /// <summary>
        /// 비율 감소에 쓰이는 방어력. 체력과 달리 지수로 자라지 않는다.
        /// 무한히 자라면 감소율이 1에 수렴해 데미지가 사실상 0이 되고,
        /// 그걸 막으려 방어 상수까지 키우면 밸런싱 축이 하나 더 늘어난다.
        /// 생존은 체력이 담당하고 방어력은 초중반 스탯으로 둔다.
        /// </summary>
        public double Defense { get; set; }

        /// <summary>치명타 확률. [0, 1] 범위.</summary>
        public double CriticalChance { get; set; }

        /// <summary>치명타 시 적용되는 데미지 배율.</summary>
        public double CriticalMultiplier { get; set; }
    }
}
