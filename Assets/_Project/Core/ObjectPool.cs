using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// 인스턴스를 재사용해 자주 호출되는 경로에서 할당이 발생하지 않게 한다.
    /// 가장 큰 대상은 데미지 팝업이다.
    /// 게임 루프에서만 접근하므로 스레드 안전하지 않다.
    /// </summary>
    public sealed class ObjectPool<T> where T : class
    {
        private readonly Func<T> _factory;
        private readonly Action<T> _onRent;
        private readonly Action<T> _onReturn;
        private readonly Stack<T> _available = new Stack<T>();
        private readonly HashSet<T> _rented = new HashSet<T>();

        /// <param name="factory">풀이 비었을 때 인스턴스를 새로 만든다.</param>
        /// <param name="onRent">인스턴스를 꺼낼 때 실행. 활성화 처리 등.</param>
        /// <param name="onReturn">인스턴스를 되돌려받을 때 실행. 비활성화 처리 등.</param>
        public ObjectPool(Func<T> factory, Action<T> onRent = null, Action<T> onReturn = null)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _onRent = onRent;
            _onReturn = onReturn;
        }

        public int AvailableCount => _available.Count;

        public int RentedCount => _rented.Count;

        /// <summary>인스턴스를 미리 만들어둬, 초반의 바쁜 프레임에서 할당이 일어나지 않게 한다.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++) _available.Push(_factory());
        }

        public T Rent()
        {
            T instance = _available.Count > 0 ? _available.Pop() : _factory();
            _rented.Add(instance);
            _onRent?.Invoke(instance);
            return instance;
        }

        /// <summary>
        /// 인스턴스를 반납한다. 현재 대여 중이 아닌 것은 거부하는데,
        /// 이렇게 해야 같은 인스턴스가 두 주인에게 동시에 넘어가는 이중 반납을 잡아낼 수 있다.
        /// </summary>
        public void Return(T instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            if (!_rented.Remove(instance))
                throw new InvalidOperationException("이 풀에서 대여한 인스턴스가 아니거나, 이미 반납된 인스턴스다.");

            _onReturn?.Invoke(instance);
            _available.Push(instance);
        }
    }
}
