using System;

namespace Game.Core
{
    /// <summary>
    /// 난수를 인터페이스 뒤로 감춰, 테스트에서 전투 결과를 결정론적으로 만들 수 있게 한다.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>[0, 1) 범위의 값.</summary>
        double NextDouble();
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        private readonly Random _random;

        public SystemRandomSource() : this(Environment.TickCount) { }

        public SystemRandomSource(int seed) => _random = new Random(seed);

        public double NextDouble() => _random.NextDouble();
    }
}
