using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>한 번의 타격 결과. 데미지 숫자를 어떻게 표시할지 정하려면 두 값이 모두 필요하다.</summary>
    public readonly struct DamageResult
    {
        public DamageResult(BigNumber amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }

        public BigNumber Amount { get; }

        public bool IsCritical { get; }
    }
}
