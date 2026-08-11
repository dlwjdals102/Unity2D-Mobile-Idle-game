using System;
using Game.Gameplay.Combat;
using Game.Gameplay.Equipment;

namespace Game.Gameplay.Progression
{
    /// <summary>
    /// 강화 단계와 착용 장비를 합쳐 전투 스탯을 만든다.
    /// <para>
    /// <see cref="CharacterStats"/>에 값을 쓰는 것은 이 클래스뿐이다.
    /// 기여자가 여럿인데 각자 대입하면 서로를 덮어쓰므로, 작성자를 하나로 묶었다.
    /// </para>
    /// <para>
    /// 강화 구매와 장비 착용을 여기서 감싸는 것도 같은 이유다.
    /// 상태를 바꾼 뒤 반영을 빠뜨릴 수 있는 경로를 남기지 않는다.
    /// </para>
    /// </summary>
    public sealed class StatComposer
    {
        /// <summary>치명타 확률 상한. 장비를 모두 갖춰도 확정 치명타가 되지는 않는다.</summary>
        public const double MaxCriticalChance = 0.75d;

        private readonly CharacterStats _stats;
        private readonly double _baseCriticalChance;

        public StatComposer(CharacterStats stats, StatUpgrades upgrades, Inventory inventory, double baseCriticalChance)
        {
            _stats = stats ?? throw new ArgumentNullException(nameof(stats));
            Upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            Inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _baseCriticalChance = baseCriticalChance;

            Recalculate();
        }

        public StatUpgrades Upgrades { get; }

        public Inventory Inventory { get; }

        public bool TryPurchaseUpgrade(StatUpgrade upgrade)
        {
            if (!Upgrades.TryPurchase(upgrade)) return false;

            Recalculate();
            return true;
        }

        public bool TryEquip(EquipmentDefinition definition)
        {
            if (!Inventory.TryEquip(definition)) return false;

            Recalculate();
            return true;
        }

        /// <summary>세이브에서 불러온 강화 단계를 적용한다.</summary>
        public void RestoreUpgrades(int attackPowerLevel, int criticalMultiplierLevel)
        {
            Upgrades.Restore(attackPowerLevel, criticalMultiplierLevel);
            Recalculate();
        }

        private void Recalculate()
        {
            EquipmentDefinition weapon = Inventory.GetEquipped(EquipmentSlot.Weapon);
            EquipmentDefinition charm = Inventory.GetEquipped(EquipmentSlot.Charm);
            EquipmentDefinition ring = Inventory.GetEquipped(EquipmentSlot.Ring);

            _stats.AttackPower = Upgrades.AttackPower.Value * Multiplier(weapon);

            // 치명타 배율은 항상 한 자릿수 근처라 double로 내려도 정밀도 문제가 없다.
            _stats.CriticalMultiplier = Upgrades.CriticalMultiplier.Value.ToDouble();

            _stats.GoldMultiplier = Multiplier(charm);

            // 반지만 가산으로 처리한다. 확률이라 곱하면 금방 1을 넘는다.
            double criticalChance = _baseCriticalChance + (ring?.Value ?? 0d);
            _stats.CriticalChance = Math.Min(criticalChance, MaxCriticalChance);
        }

        /// <summary>착용한 것이 없으면 배율은 1이다.</summary>
        private static double Multiplier(EquipmentDefinition definition) => definition?.Value ?? 1d;
    }
}
