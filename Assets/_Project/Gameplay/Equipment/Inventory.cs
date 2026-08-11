using System;
using System.Collections.Generic;

namespace Game.Gameplay.Equipment
{
    /// <summary>
    /// 보유한 장비와 슬롯별 착용 상태.
    /// 합성 승급이 아직 없으므로 중복 획득은 그대로 쌓인다.
    /// </summary>
    public sealed class Inventory
    {
        private readonly List<EquipmentDefinition> _owned = new List<EquipmentDefinition>();
        private readonly Dictionary<EquipmentSlot, EquipmentDefinition> _equipped =
            new Dictionary<EquipmentSlot, EquipmentDefinition>();

        public IReadOnlyList<EquipmentDefinition> Owned => _owned;

        public void Add(EquipmentDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            _owned.Add(definition);
        }

        /// <summary>해당 슬롯에 착용 중인 장비. 없으면 <c>null</c>.</summary>
        public EquipmentDefinition GetEquipped(EquipmentSlot slot)
            => _equipped.TryGetValue(slot, out EquipmentDefinition found) ? found : null;

        /// <summary>
        /// 보유한 장비를 자기 슬롯에 착용한다. 갖고 있지 않으면 <c>false</c>.
        /// 같은 슬롯에 이미 착용 중이던 것은 벗겨져 보유 목록에 그대로 남는다.
        /// </summary>
        public bool TryEquip(EquipmentDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (!_owned.Contains(definition)) return false;

            _equipped[definition.Slot] = definition;
            return true;
        }
    }
}
