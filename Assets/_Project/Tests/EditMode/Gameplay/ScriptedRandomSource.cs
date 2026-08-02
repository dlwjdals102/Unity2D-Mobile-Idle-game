using Game.Core;

namespace Game.Gameplay.Tests
{
    /// <summary>고정된 값을 돌려줘, 테스트에서 치명타를 항상 터뜨리거나 항상 막을 수 있게 한다.</summary>
    internal sealed class ScriptedRandomSource : IRandomSource
    {
        private readonly double _value;

        private ScriptedRandomSource(double value) => _value = value;

        /// <summary>0보다 큰 어떤 치명타 확률보다도 작으므로 매번 치명타가 난다.</summary>
        public static ScriptedRandomSource AlwaysCritical => new ScriptedRandomSource(0d);

        /// <summary>1 미만인 어떤 치명타 확률보다도 크거나 같으므로 치명타가 나지 않는다.</summary>
        public static ScriptedRandomSource NeverCritical => new ScriptedRandomSource(0.999999d);

        public double NextDouble() => _value;
    }
}
