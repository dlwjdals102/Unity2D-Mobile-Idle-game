using Game.Core;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 전투 진행 상태의 스냅샷. 저장할 때 꺼내고 불러올 때 넣는 값이라,
    /// <see cref="BattleRunner"/>가 세이브 계층을 알 필요가 없다.
    /// </summary>
    public readonly struct BattleProgress
    {
        public BattleProgress(int floor, int killsOnFloor, BigNumber gold, int diamonds)
        {
            Floor = floor;
            KillsOnFloor = killsOnFloor;
            Gold = gold;
            Diamonds = diamonds;
        }

        public int Floor { get; }

        public int KillsOnFloor { get; }

        public BigNumber Gold { get; }

        /// <summary>보스에서만 나오는 재화. 보스 하나당 지급량이 고정이라 int로 충분하다.</summary>
        public int Diamonds { get; }

        /// <summary>새 게임의 시작 상태.</summary>
        public static BattleProgress Start => new BattleProgress(1, 0, BigNumber.Zero, 0);
    }
}
