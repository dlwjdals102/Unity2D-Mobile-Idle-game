using System.Collections.Generic;
using Game.Gameplay.Equipment;
using NUnit.Framework;

namespace Game.Gameplay.Tests
{
    public class InventoryTests
    {
        private static EquipmentDefinition Definition(int id) => EquipmentTable.Default.Find(id);

        [Test]
        public void NewInventory_IsEmptyAndWearsNothing()
        {
            var inventory = new Inventory();

            Assert.AreEqual(0, inventory.Owned.Count);
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Weapon));
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Charm));
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Ring));
        }

        [Test]
        public void Add_KeepsDuplicates()
        {
            var inventory = new Inventory();
            EquipmentDefinition sword = Definition(101);

            inventory.Add(sword);
            inventory.Add(sword);

            // 합성 승급이 없으므로 중복은 그대로 쌓인다.
            Assert.AreEqual(2, inventory.Owned.Count);
        }

        [Test]
        public void Equip_RequiresOwningTheItem()
        {
            var inventory = new Inventory();

            Assert.IsFalse(inventory.TryEquip(Definition(101)));
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Weapon));
        }

        [Test]
        public void Equip_PutsTheItemInItsOwnSlot()
        {
            var inventory = new Inventory();
            EquipmentDefinition ring = Definition(303);
            inventory.Add(ring);

            Assert.IsTrue(inventory.TryEquip(ring));

            Assert.AreSame(ring, inventory.GetEquipped(EquipmentSlot.Ring));
            Assert.IsNull(inventory.GetEquipped(EquipmentSlot.Weapon), "다른 슬롯은 비어 있어야 한다");
        }

        [Test]
        public void Equip_ReplacesWhatWasInTheSlot()
        {
            var inventory = new Inventory();
            EquipmentDefinition oldSword = Definition(101);
            EquipmentDefinition newSword = Definition(104);
            inventory.Add(oldSword);
            inventory.Add(newSword);

            inventory.TryEquip(oldSword);
            inventory.TryEquip(newSword);

            Assert.AreSame(newSword, inventory.GetEquipped(EquipmentSlot.Weapon));
            Assert.AreEqual(2, inventory.Owned.Count, "벗은 장비는 보유 목록에 남는다");
        }
    }

    public class EquipmentTableTests
    {
        [Test]
        public void Default_HasEveryGradeInEverySlot()
        {
            EquipmentTable table = EquipmentTable.Default;

            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Charm, EquipmentSlot.Ring })
            {
                foreach (EquipmentGrade grade in new[]
                {
                    EquipmentGrade.Common, EquipmentGrade.Rare, EquipmentGrade.Epic,
                    EquipmentGrade.Unique, EquipmentGrade.Legendary
                })
                {
                    Assert.AreEqual(1, CountOf(table, slot, grade), $"{slot} / {grade}");
                }
            }
        }

        [Test]
        public void Find_ReturnsNullForAnUnknownId()
        {
            Assert.IsNull(EquipmentTable.Default.Find(999));
        }

        [Test]
        public void HigherGrades_AreWorthMore()
        {
            EquipmentTable table = EquipmentTable.Default;

            foreach (EquipmentSlot slot in new[] { EquipmentSlot.Weapon, EquipmentSlot.Charm, EquipmentSlot.Ring })
            {
                var byGrade = new List<EquipmentDefinition>();
                foreach (EquipmentDefinition definition in table.All)
                {
                    if (definition.Slot == slot) byGrade.Add(definition);
                }

                byGrade.Sort((a, b) => a.Grade.CompareTo(b.Grade));

                for (int i = 1; i < byGrade.Count; i++)
                {
                    Assert.Greater(byGrade[i].Value, byGrade[i - 1].Value,
                        $"{slot}: {byGrade[i].Grade}가 {byGrade[i - 1].Grade}보다 낮다");
                }
            }
        }

        private static int CountOf(EquipmentTable table, EquipmentSlot slot, EquipmentGrade grade)
        {
            int count = 0;
            foreach (EquipmentDefinition definition in table.All)
            {
                if (definition.Slot == slot && definition.Grade == grade) count++;
            }

            return count;
        }
    }
}
