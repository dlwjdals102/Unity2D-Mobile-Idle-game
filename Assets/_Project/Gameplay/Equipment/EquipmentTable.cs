using System;
using System.Collections.Generic;

namespace Game.Gameplay.Equipment
{
    /// <summary>
    /// 존재하는 장비 정의 전부. id로 찾을 수 있어 세이브에는 id만 남기면 된다.
    /// </summary>
    public sealed class EquipmentTable
    {
        private readonly Dictionary<int, EquipmentDefinition> _byId = new Dictionary<int, EquipmentDefinition>();
        private readonly List<EquipmentDefinition> _all = new List<EquipmentDefinition>();

        public EquipmentTable(IEnumerable<EquipmentDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            foreach (EquipmentDefinition definition in definitions)
            {
                _byId.Add(definition.Id, definition);
                _all.Add(definition);
            }
        }

        public IReadOnlyList<EquipmentDefinition> All => _all;

        /// <summary>없는 id면 <c>null</c>. 옛 세이브에 지금은 사라진 장비가 남아 있을 수 있다.</summary>
        public EquipmentDefinition Find(int id) => _byId.TryGetValue(id, out EquipmentDefinition found) ? found : null;

        /// <summary>
        /// 기획서의 1차 밸런싱 값. 데이터 파이프라인이 생기면 그쪽으로 옮긴다.
        /// id는 슬롯별로 100단위를 쓴다.
        /// </summary>
        public static EquipmentTable Default => new EquipmentTable(new[]
        {
            new EquipmentDefinition(101, "낡은 검", EquipmentSlot.Weapon, EquipmentGrade.Common, 1.10d),
            new EquipmentDefinition(102, "강철 검", EquipmentSlot.Weapon, EquipmentGrade.Rare, 1.30d),
            new EquipmentDefinition(103, "룬 블레이드", EquipmentSlot.Weapon, EquipmentGrade.Epic, 1.70d),
            new EquipmentDefinition(104, "용살자", EquipmentSlot.Weapon, EquipmentGrade.Unique, 2.40d),
            new EquipmentDefinition(105, "탑의 유산", EquipmentSlot.Weapon, EquipmentGrade.Legendary, 3.50d),

            new EquipmentDefinition(201, "구리 부적", EquipmentSlot.Charm, EquipmentGrade.Common, 1.10d),
            new EquipmentDefinition(202, "은빛 부적", EquipmentSlot.Charm, EquipmentGrade.Rare, 1.25d),
            new EquipmentDefinition(203, "황금 부적", EquipmentSlot.Charm, EquipmentGrade.Epic, 1.50d),
            new EquipmentDefinition(204, "탐욕의 인장", EquipmentSlot.Charm, EquipmentGrade.Unique, 1.90d),
            new EquipmentDefinition(205, "금화의 심장", EquipmentSlot.Charm, EquipmentGrade.Legendary, 2.50d),

            new EquipmentDefinition(301, "무딘 반지", EquipmentSlot.Ring, EquipmentGrade.Common, 0.03d),
            new EquipmentDefinition(302, "예리한 반지", EquipmentSlot.Ring, EquipmentGrade.Rare, 0.06d),
            new EquipmentDefinition(303, "매의 눈", EquipmentSlot.Ring, EquipmentGrade.Epic, 0.10d),
            new EquipmentDefinition(304, "처형자의 반지", EquipmentSlot.Ring, EquipmentGrade.Unique, 0.15d),
            new EquipmentDefinition(305, "일격필살", EquipmentSlot.Ring, EquipmentGrade.Legendary, 0.22d)
        });
    }
}
