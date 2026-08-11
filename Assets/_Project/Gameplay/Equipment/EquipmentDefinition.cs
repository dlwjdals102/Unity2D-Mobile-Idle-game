namespace Game.Gameplay.Equipment
{
    /// <summary>장비를 착용하는 자리. 자리마다 기여하는 스탯이 다르다.</summary>
    public enum EquipmentSlot
    {
        /// <summary>공격력 배율.</summary>
        Weapon,

        /// <summary>골드 획득량 배율.</summary>
        Charm,

        /// <summary>치명타 확률 가산치.</summary>
        Ring
    }

    public enum EquipmentGrade
    {
        Common,
        Rare,
        Epic,
        Unique,
        Legendary
    }

    /// <summary>
    /// 장비 한 종류의 정의. 값이 아니라 정의라서 불변이고, 인벤토리는 이것을 참조만 한다.
    /// </summary>
    public sealed class EquipmentDefinition
    {
        public EquipmentDefinition(int id, string name, EquipmentSlot slot, EquipmentGrade grade, double value)
        {
            Id = id;
            Name = name;
            Slot = slot;
            Grade = grade;
            Value = value;
        }

        public int Id { get; }

        public string Name { get; }

        public EquipmentSlot Slot { get; }

        public EquipmentGrade Grade { get; }

        /// <summary>
        /// 슬롯에 따라 의미가 다르다.
        /// 무기와 부적은 곱해지는 배율, 반지는 치명타 확률에 더해지는 값이다.
        /// </summary>
        public double Value { get; }
    }
}
