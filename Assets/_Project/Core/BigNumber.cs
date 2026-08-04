using System;
using System.Globalization;

namespace Game.Core
{
    /// <summary>
    /// 가수(mantissa)와 상한 없는 10의 지수로 나눠 저장하는 수치 타입.
    /// 방치형 게임의 재화·데미지는 <see cref="double"/>의 한계인 약 1e308을 쉽게 넘기므로
    /// 지수를 별도 정수로 들고 간다.
    /// 항상 1 &lt;= |Mantissa| &lt; 10 으로 정규화되며, 0은 (0, 0) 한 가지 형태만 가진다.
    /// 입력값은 유한하다고 가정한다. NaN과 무한대는 처리하지 않는다.
    /// </summary>
    public readonly struct BigNumber : IComparable<BigNumber>, IEquatable<BigNumber>
    {
        /// <summary>
        /// double의 유효숫자는 15~17자리다. 이보다 더 많은 자릿수만큼 작은 값을 더해도
        /// 결과가 바뀌지 않으므로, 그런 덧셈은 계산을 건너뛴다.
        /// </summary>
        private const int NegligibleExponentGap = 17;

        /// <summary>double로 정확히 표현되는 10의 거듭제곱 값들.</summary>
        private static readonly double[] Pow10Cache = BuildPow10Cache();

        private static readonly string[] ShortSuffixes = { "", "K", "M", "B", "T" };

        public static readonly BigNumber Zero = default;
        public static readonly BigNumber One = new BigNumber(1d, 0);

        public double Mantissa { get; }
        public int Exponent { get; }

        public BigNumber(double mantissa, int exponent)
        {
            if (mantissa == 0d)
            {
                Mantissa = 0d;
                Exponent = 0;
                return;
            }

            int shift = (int)Math.Floor(Math.Log10(Math.Abs(mantissa)));
            double normalized = Scale(mantissa, -shift);

            // Log10의 오차가 1 ulp 생길 수 있어 가수가 [1, 10) 범위를 살짝 벗어날 때가 있다.
            if (Math.Abs(normalized) >= 10d)
            {
                normalized /= 10d;
                shift++;
            }
            else if (Math.Abs(normalized) < 1d)
            {
                normalized *= 10d;
                shift--;
            }

            Mantissa = normalized;
            Exponent = exponent + shift;
        }

        public static implicit operator BigNumber(double value) => new BigNumber(value, 0);

        /// <summary>double로 되돌린다. 약 1e308을 넘으면 무한대가 된다.</summary>
        public double ToDouble() => Scale(Mantissa, Exponent);

        /// <summary>
        /// <paramref name="baseValue"/>의 <paramref name="exponent"/> 제곱.
        /// 로그 공간에서 계산하므로 결과가 double 범위를 벗어나도 지수는 정확하게 유지된다.
        /// 가수의 유효숫자는 약 12자리다.
        /// </summary>
        public static BigNumber Pow(double baseValue, int exponent)
        {
            if (baseValue <= 0d)
                throw new ArgumentOutOfRangeException(nameof(baseValue), baseValue, "밑은 양수여야 한다.");

            return FromLog10(Math.Log10(baseValue) * exponent);
        }

        /// <summary>double 범위를 벗어날 수 있는 10의 거듭제곱.</summary>
        private static BigNumber FromLog10(double log10)
        {
            int exponent = (int)Math.Floor(log10);
            return new BigNumber(Math.Pow(10d, log10 - exponent), exponent);
        }

        // ---------- 사칙연산 ----------

        public static BigNumber operator +(BigNumber a, BigNumber b)
        {
            if (a.Mantissa == 0d) return b;
            if (b.Mantissa == 0d) return a;

            bool aIsLarger = a.Exponent >= b.Exponent;
            BigNumber high = aIsLarger ? a : b;
            BigNumber low = aIsLarger ? b : a;

            int gap = high.Exponent - low.Exponent;
            if (gap > NegligibleExponentGap) return high;

            return new BigNumber(high.Mantissa + low.Mantissa / Pow10(gap), high.Exponent);
        }

        public static BigNumber operator -(BigNumber value) => new BigNumber(-value.Mantissa, value.Exponent);

        public static BigNumber operator -(BigNumber a, BigNumber b) => a + -b;

        public static BigNumber operator *(BigNumber a, BigNumber b)
        {
            if (a.Mantissa == 0d || b.Mantissa == 0d) return Zero;
            return new BigNumber(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static BigNumber operator /(BigNumber a, BigNumber b)
        {
            if (b.Mantissa == 0d) throw new DivideByZeroException();
            if (a.Mantissa == 0d) return Zero;
            return new BigNumber(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
        }

        // ---------- 비교 ----------

        public int CompareTo(BigNumber other)
        {
            int sign = Math.Sign(Mantissa);
            int otherSign = Math.Sign(other.Mantissa);
            if (sign != otherSign) return sign.CompareTo(otherSign);
            if (sign == 0) return 0;

            // 음수는 지수가 클수록 작은 값이므로 비교 결과를 뒤집는다.
            if (Exponent != other.Exponent)
            {
                int byExponent = Exponent.CompareTo(other.Exponent);
                return sign > 0 ? byExponent : -byExponent;
            }

            return Mantissa.CompareTo(other.Mantissa);
        }

        public static bool operator <(BigNumber a, BigNumber b) => a.CompareTo(b) < 0;
        public static bool operator >(BigNumber a, BigNumber b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigNumber a, BigNumber b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigNumber a, BigNumber b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigNumber a, BigNumber b) => a.Equals(b);
        public static bool operator !=(BigNumber a, BigNumber b) => !a.Equals(b);

        /// <summary>
        /// 필드를 그대로 비교한다. 서로 다른 연산 경로로 도달한 값은 가수 끝자리가 어긋날 수 있으므로,
        /// 그 차이를 무시해야 한다면 <see cref="CompareTo"/>를 쓴다.
        /// </summary>
        public bool Equals(BigNumber other) => Mantissa == other.Mantissa && Exponent == other.Exponent;

        public override bool Equals(object obj) => obj is BigNumber other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Mantissa, Exponent);

        // ---------- 표시 형식 ----------

        public override string ToString()
        {
            if (Mantissa == 0d) return "0";

            int tier = Exponent < 3 ? 0 : Exponent / 3;
            double scaled = Mantissa * Pow10(Exponent - tier * 3);

            // 소수 둘째 자리로 반올림하면 999.999 같은 값이 1000이 되어 단위가 한 칸 밀린다.
            // 그대로 두면 999999가 "1M"이 아니라 "1000K"로 나오므로, 이때는 단위를 올린다.
            if (Math.Abs(Math.Round(scaled, 2)) >= 1000d)
            {
                scaled /= 1000d;
                tier++;
            }

            return Format(scaled) + SuffixFor(tier);
        }

        private static string Format(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);

        /// <summary>단위 접미사. "", K, M, B, T 다음부터는 aa, ab, ... az, ba, ... 순으로 이어진다.</summary>
        private static string SuffixFor(int tier)
        {
            if (tier < ShortSuffixes.Length) return ShortSuffixes[tier];

            int index = tier - ShortSuffixes.Length;
            return string.Concat((char)('a' + index / 26), (char)('a' + index % 26));
        }

        // ---------- 보조 함수 ----------

        /// <summary>
        /// 10^exponent를 곱한다. 지수가 음수일 때는 소수를 곱하는 대신 나누기로 처리해,
        /// 양방향 모두 캐시된 정확한 10의 거듭제곱을 쓰도록 한다.
        /// </summary>
        private static double Scale(double value, int exponent)
            => exponent >= 0 ? value * Pow10(exponent) : value / Pow10(-exponent);

        private static double Pow10(int exponent)
        {
            if (exponent >= 0 && exponent < Pow10Cache.Length) return Pow10Cache[exponent];
            return Math.Pow(10d, exponent);
        }

        private static double[] BuildPow10Cache()
        {
            // 1e22가 double로 정확히 표현되는 마지막 10의 거듭제곱이다.
            var cache = new double[23];
            double value = 1d;
            for (int i = 0; i < cache.Length; i++)
            {
                cache[i] = value;
                value *= 10d;
            }

            return cache;
        }
    }
}
