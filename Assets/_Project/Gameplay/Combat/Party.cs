using System;
using System.Collections.Generic;

namespace Game.Gameplay.Combat
{
    /// <summary>
    /// 세 자리로 고정된 아군 편성. 자리 순서가 곧 몬스터에게 맞는 순서다.
    /// 빈 자리는 없다. 시작할 때 각 자리에 기본 캐릭터가 배치된 상태로 출발한다.
    /// </summary>
    public sealed class Party
    {
        private readonly CombatUnit[] _units;

        public Party(CombatUnit warrior, CombatUnit archer, CombatUnit mage)
        {
            _units = new[]
            {
                Verify(warrior, PartySlot.Warrior, nameof(warrior)),
                Verify(archer, PartySlot.Archer, nameof(archer)),
                Verify(mage, PartySlot.Mage, nameof(mage))
            };
        }

        /// <summary>자리 순서대로. 이 순서가 피격 순서이기도 하다.</summary>
        public IReadOnlyList<CombatUnit> Units => _units;

        public bool IsWiped => FirstAlive == null;

        /// <summary>몬스터가 노리는 대상. 전멸했다면 <c>null</c>.</summary>
        public CombatUnit FirstAlive
        {
            get
            {
                foreach (CombatUnit unit in _units)
                {
                    if (unit.IsAlive) return unit;
                }

                return null;
            }
        }

        /// <summary>배열 순서가 열거형 값과 같도록 생성자에서 보장한다.</summary>
        public CombatUnit Get(PartySlot slot) => _units[(int)slot];

        /// <summary>
        /// 인자 세 개가 모두 같은 타입이라 자리를 바꿔 넣기 쉽다.
        /// 그대로 두면 피격 순서가 조용히 뒤바뀌므로 생성 시점에 잡는다.
        /// </summary>
        private static CombatUnit Verify(CombatUnit unit, PartySlot expected, string parameterName)
        {
            if (unit == null) throw new ArgumentNullException(parameterName);

            if (unit.Slot != expected)
                throw new ArgumentException($"{expected} 자리에 {unit.Slot} 유닛이 들어왔다.", parameterName);

            return unit;
        }

        /// <summary>전원을 되살리고 체력을 가득 채운다. 층이 바뀌거나 전멸했을 때 호출된다.</summary>
        public void RestoreAll()
        {
            foreach (CombatUnit unit in _units) unit.Restore();
        }
    }
}
