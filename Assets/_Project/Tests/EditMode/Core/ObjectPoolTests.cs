using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class ObjectPoolTests
    {
        private sealed class Tracked
        {
            public int Id;
            public bool IsActive;
        }

        private static ObjectPool<Tracked> CreatePool(List<Tracked> created = null)
        {
            int nextId = 0;
            return new ObjectPool<Tracked>(
                () =>
                {
                    var instance = new Tracked { Id = nextId++ };
                    created?.Add(instance);
                    return instance;
                },
                onRent: instance => instance.IsActive = true,
                onReturn: instance => instance.IsActive = false);
        }

        [Test]
        public void Rent_CreatesInstanceWhenEmpty()
        {
            var pool = CreatePool();

            Tracked instance = pool.Rent();

            Assert.IsNotNull(instance);
            Assert.AreEqual(1, pool.RentedCount);
            Assert.AreEqual(0, pool.AvailableCount);
        }

        [Test]
        public void Rent_ReusesReturnedInstanceInsteadOfAllocating()
        {
            var created = new List<Tracked>();
            var pool = CreatePool(created);

            Tracked first = pool.Rent();
            pool.Return(first);
            Tracked second = pool.Rent();

            Assert.AreSame(first, second);
            Assert.AreEqual(1, created.Count, "재사용하지 않고 인스턴스를 새로 만들었다");
        }

        [Test]
        public void Prewarm_CreatesInstancesUpFront()
        {
            var created = new List<Tracked>();
            var pool = CreatePool(created);

            pool.Prewarm(8);

            Assert.AreEqual(8, created.Count);
            Assert.AreEqual(8, pool.AvailableCount);
            Assert.AreEqual(0, pool.RentedCount);
        }

        [Test]
        public void Prewarm_ThenRent_DoesNotAllocate()
        {
            var created = new List<Tracked>();
            var pool = CreatePool(created);
            pool.Prewarm(3);

            for (int i = 0; i < 3; i++) pool.Rent();

            Assert.AreEqual(3, created.Count);
        }

        [Test]
        public void Callbacks_RunOnRentAndReturn()
        {
            var pool = CreatePool();

            Tracked instance = pool.Rent();
            Assert.IsTrue(instance.IsActive, "onRent 콜백이 실행되지 않았다");

            pool.Return(instance);
            Assert.IsFalse(instance.IsActive, "onReturn 콜백이 실행되지 않았다");
        }

        [Test]
        public void Return_Twice_Throws()
        {
            var pool = CreatePool();
            Tracked instance = pool.Rent();
            pool.Return(instance);

            Assert.Throws<InvalidOperationException>(() => pool.Return(instance));
        }

        [Test]
        public void Return_ForeignInstance_Throws()
        {
            var pool = CreatePool();

            Assert.Throws<InvalidOperationException>(() => pool.Return(new Tracked()));
        }

        [Test]
        public void Counts_TrackRentAndReturn()
        {
            var pool = CreatePool();
            Tracked a = pool.Rent();
            Tracked b = pool.Rent();
            pool.Rent();

            Assert.AreEqual(3, pool.RentedCount);
            Assert.AreEqual(0, pool.AvailableCount);

            pool.Return(a);
            pool.Return(b);

            Assert.AreEqual(1, pool.RentedCount);
            Assert.AreEqual(2, pool.AvailableCount);
        }
    }
}
