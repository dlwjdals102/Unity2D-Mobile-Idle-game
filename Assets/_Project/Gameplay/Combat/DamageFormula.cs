using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 데미지 계산. 방어력은 비율 감소형이다.
    /// <para>
    /// 감산형(<c>공격력 - 방어력</c>)을 쓰지 않는 이유는, 방어력이 공격력을 넘는 순간
    /// 데미지가 0이 되어 무한 성장 게임에서 곧바로 깨지기 때문이다.
    /// </para>
    /// </summary>
    public static class DamageFormula
    {
        /// <summary>
        /// 방어 상수. 방어력이 이 값과 같으면 데미지가 절반이 된다.
        /// <b>(추후 조정)</b> 방어력 스케일이 확정된 뒤, 그 범위 중간값에서
        /// 감소율이 약 50%가 되도록 역산해 확정한다. 지금은 임시값이다.
        /// </summary>
        public const double DefenseConstant = 100d;

        /// <summary>방어력이 데미지를 얼마나 남기는지. 항상 0보다 크고 1 이하다.</summary>
        public static double MultiplierAgainst(double defense) => DefenseConstant / (DefenseConstant + defense);

        public static BigNumber Resolve(BigNumber attackPower, double defense, bool isCritical, double criticalMultiplier)
        {
            BigNumber damage = attackPower * MultiplierAgainst(defense);
            return isCritical ? damage * criticalMultiplier : damage;
        }
    }
}
