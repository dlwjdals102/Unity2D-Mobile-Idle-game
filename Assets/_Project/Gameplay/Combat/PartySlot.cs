using System;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 파티의 세 자리. 선언 순서가 곧 몬스터에게 맞는 순서다.
    /// 전사가 앞에 있다는 사실이 이 게임에서 "탱커"의 의미 전부이며,
    /// 어그로나 도발 같은 별도 규칙은 두지 않는다.
    /// </summary>
    public enum PartySlot
    {
        Warrior,
        Archer,
        Mage
    }

    /// <summary>
    /// 직업별 공격 주기. <b>스탯이 아니라 상수다.</b>
    /// 공격 속도를 성장 가능한 스탯으로 만들면 공격력과 곱으로 작용해 DPS가 제곱으로 뛰고,
    /// 그 시점부터 밸런싱이 사실상 불가능해진다.
    /// </summary>
    public static class AttackIntervals
    {
        public static double ForSlot(PartySlot slot) => slot switch
        {
            PartySlot.Warrior => 1.0d,
            PartySlot.Archer => 0.6d,
            PartySlot.Mage => 1.4d,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, "모르는 슬롯이다.")
        };
    }
}
