using System;
using NUnit.Framework;

namespace Game.Core.Tests
{
    public class BigNumberTests
    {
        private const double MantissaTolerance = 1e-9;

        private static void AssertValue(BigNumber actual, double expectedMantissa, int expectedExponent)
        {
            Assert.AreEqual(expectedExponent, actual.Exponent, "exponent");
            Assert.AreEqual(expectedMantissa, actual.Mantissa, MantissaTolerance, "mantissa");
        }

        // ---------- 생성과 정규화 ----------

        [Test]
        public void Constructor_NormalizesMantissaIntoRange()
        {
            AssertValue(new BigNumber(12345d, 0), 1.2345d, 4);
        }

        [Test]
        public void Constructor_KeepsAlreadyNormalizedValue()
        {
            AssertValue(new BigNumber(5.5d, 7), 5.5d, 7);
        }

        [Test]
        public void Constructor_NormalizesValueBelowOne()
        {
            AssertValue(new BigNumber(0.001d, 0), 1d, -3);
        }

        [Test]
        public void Constructor_NormalizesNegativeValue()
        {
            AssertValue(new BigNumber(-2500d, 0), -2.5d, 3);
        }

        [Test]
        public void Constructor_CollapsesZeroToCanonicalForm()
        {
            AssertValue(new BigNumber(0d, 42), 0d, 0);
        }

        [Test]
        public void ImplicitConversion_FromDouble_Normalizes()
        {
            BigNumber value = 1500d;
            AssertValue(value, 1.5d, 3);
        }

        // ---------- 덧셈 ----------

        [Test]
        public void Add_SameExponent()
        {
            AssertValue(new BigNumber(2d, 5) + new BigNumber(3d, 5), 5d, 5);
        }

        [Test]
        public void Add_DifferentExponent()
        {
            // 1e5 + 5e4 = 1.5e5
            AssertValue(new BigNumber(1d, 5) + new BigNumber(5d, 4), 1.5d, 5);
        }

        [Test]
        public void Add_CarriesIntoNextExponent()
        {
            // 9e5 + 9e5 = 1.8e6
            AssertValue(new BigNumber(9d, 5) + new BigNumber(9d, 5), 1.8d, 6);
        }

        [Test]
        public void Add_ZeroIsIdentity()
        {
            AssertValue(BigNumber.Zero + new BigNumber(7d, 3), 7d, 3);
            AssertValue(new BigNumber(7d, 3) + BigNumber.Zero, 7d, 3);
        }

        [Test]
        public void Add_NegligibleOperandDoesNotChangeResult()
        {
            // 지수 차이가 double 정밀도를 넘어서므로 합은 큰 쪽 값과 같다.
            // 조기 반환이 실제로 동작했는지는 최적화 세부사항이라 이 테스트로는 관측할 수 없다.
            AssertValue(new BigNumber(1d, 100) + new BigNumber(1d, 10), 1d, 100);
        }

        [Test]
        public void Add_NegativeOperand()
        {
            AssertValue(new BigNumber(5d, 3) + new BigNumber(-2d, 3), 3d, 3);
        }

        // ---------- 뺄셈 ----------

        [Test]
        public void Subtract_Basic()
        {
            AssertValue(new BigNumber(5d, 6) - new BigNumber(2d, 6), 3d, 6);
        }

        [Test]
        public void Subtract_EqualValuesYieldsZero()
        {
            AssertValue(new BigNumber(3.25d, 12) - new BigNumber(3.25d, 12), 0d, 0);
        }

        [Test]
        public void Subtract_CanProduceNegativeResult()
        {
            AssertValue(new BigNumber(2d, 3) - new BigNumber(5d, 3), -3d, 3);
        }

        [Test]
        public void Negate_FlipsSign()
        {
            AssertValue(-new BigNumber(4d, 8), -4d, 8);
        }

        [Test]
        public void Negate_ZeroStaysZero()
        {
            AssertValue(-BigNumber.Zero, 0d, 0);
        }

        // ---------- 곱셈 ----------

        [Test]
        public void Multiply_AddsExponents()
        {
            AssertValue(new BigNumber(2d, 5) * new BigNumber(3d, 4), 6d, 9);
        }

        [Test]
        public void Multiply_NormalizesMantissaOverflow()
        {
            // 5e2 * 4e2 = 2e5
            AssertValue(new BigNumber(5d, 2) * new BigNumber(4d, 2), 2d, 5);
        }

        [Test]
        public void Multiply_ByZeroYieldsZero()
        {
            AssertValue(new BigNumber(9d, 50) * BigNumber.Zero, 0d, 0);
        }

        [Test]
        public void Multiply_NegativeOperand()
        {
            AssertValue(new BigNumber(2d, 3) * new BigNumber(-3d, 3), -6d, 6);
        }

        [Test]
        public void Multiply_ExceedsDoubleRange()
        {
            // 1e200 * 1e200 = 1e400. double이라면 무한대가 되는 값이다.
            AssertValue(new BigNumber(1d, 200) * new BigNumber(1d, 200), 1d, 400);
        }

        // ---------- 나눗셈 ----------

        [Test]
        public void Divide_SubtractsExponents()
        {
            AssertValue(new BigNumber(6d, 9) / new BigNumber(3d, 4), 2d, 5);
        }

        [Test]
        public void Divide_NormalizesMantissaUnderflow()
        {
            // 2e5 / 4e2 = 5e2
            AssertValue(new BigNumber(2d, 5) / new BigNumber(4d, 2), 5d, 2);
        }

        [Test]
        public void Divide_ByOneIsIdentity()
        {
            AssertValue(new BigNumber(7.5d, 11) / BigNumber.One, 7.5d, 11);
        }

        [Test]
        public void Divide_ZeroNumeratorYieldsZero()
        {
            AssertValue(BigNumber.Zero / new BigNumber(3d, 5), 0d, 0);
        }

        [Test]
        public void Divide_ByZeroThrows()
        {
            Assert.Throws<DivideByZeroException>(() => { var _ = BigNumber.One / BigNumber.Zero; });
        }

        // ---------- 거듭제곱 ----------

        [Test]
        public void Pow_ZeroExponentIsOne()
        {
            AssertValue(BigNumber.Pow(1.16d, 0), 1d, 0);
        }

        [Test]
        public void Pow_PowerOfTen()
        {
            AssertValue(BigNumber.Pow(10d, 5), 1d, 5);
        }

        [Test]
        public void Pow_MatchesDoubleWithinRange()
        {
            Assert.AreEqual(Math.Pow(1.16d, 10), BigNumber.Pow(1.16d, 10).ToDouble(), 1e-9);
        }

        [Test]
        public void Pow_ExceedsDoubleRange()
        {
            // 2^2000은 약 1e602로, double 한계를 한참 넘어선다.
            Assert.AreEqual(602, BigNumber.Pow(2d, 2000).Exponent);
        }

        [Test]
        public void Pow_NonPositiveBaseThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigNumber.Pow(0d, 2));
            Assert.Throws<ArgumentOutOfRangeException>(() => BigNumber.Pow(-2d, 2));
        }

        // ---------- 비교 ----------

        [Test]
        public void Compare_GreaterByExponent()
        {
            Assert.IsTrue(new BigNumber(1d, 10) > new BigNumber(9d, 9));
        }

        [Test]
        public void Compare_GreaterByMantissa()
        {
            Assert.IsTrue(new BigNumber(5d, 7) > new BigNumber(4.9d, 7));
        }

        [Test]
        public void Compare_EqualValues()
        {
            Assert.AreEqual(0, new BigNumber(3d, 8).CompareTo(new BigNumber(3d, 8)));
            Assert.IsTrue(new BigNumber(3d, 8) == new BigNumber(3d, 8));
        }

        [Test]
        public void Compare_PositiveBeatsNegative()
        {
            Assert.IsTrue(new BigNumber(1d, 0) > new BigNumber(-1d, 50));
        }

        [Test]
        public void Compare_NegativesOrderByMagnitudeReversed()
        {
            // -1e3이 -1e5보다 크다
            Assert.IsTrue(new BigNumber(-1d, 3) > new BigNumber(-1d, 5));
        }

        [Test]
        public void Compare_NegativesWithSameExponent()
        {
            Assert.IsTrue(new BigNumber(-2d, 5) < new BigNumber(-1d, 5));
        }

        [Test]
        public void Compare_ZeroAgainstSignedValues()
        {
            Assert.IsTrue(BigNumber.Zero < new BigNumber(1d, -30));
            Assert.IsTrue(BigNumber.Zero > new BigNumber(-1d, -30));
        }

        [Test]
        public void Compare_LessThanOrEqualOperators()
        {
            Assert.IsTrue(new BigNumber(2d, 4) <= new BigNumber(2d, 4));
            Assert.IsTrue(new BigNumber(2d, 4) >= new BigNumber(2d, 4));
            Assert.IsTrue(new BigNumber(1d, 4) != new BigNumber(2d, 4));
        }

        // ---------- 표시 형식 ----------

        [Test]
        public void ToString_Zero()
        {
            Assert.AreEqual("0", BigNumber.Zero.ToString());
        }

        [Test]
        public void ToString_BelowThousandIsPlain()
        {
            Assert.AreEqual("123", new BigNumber(1.23d, 2).ToString());
            Assert.AreEqual("0.5", new BigNumber(5d, -1).ToString());
        }

        [Test]
        public void ToString_ShortScaleSuffixes()
        {
            Assert.AreEqual("1.23K", new BigNumber(1.23d, 3).ToString());
            Assert.AreEqual("12.3K", new BigNumber(1.23d, 4).ToString());
            Assert.AreEqual("1M", new BigNumber(1d, 6).ToString());
            Assert.AreEqual("1B", new BigNumber(1d, 9).ToString());
            Assert.AreEqual("1T", new BigNumber(1d, 12).ToString());
        }

        [Test]
        public void ToString_AlphabeticSuffixesBeyondTrillion()
        {
            Assert.AreEqual("1aa", new BigNumber(1d, 15).ToString());
            Assert.AreEqual("1ab", new BigNumber(1d, 18).ToString());
            Assert.AreEqual("1ba", new BigNumber(1d, 93).ToString());
        }

        [Test]
        public void ToString_NegativeValue()
        {
            Assert.AreEqual("-1.5M", new BigNumber(-1.5d, 6).ToString());
        }

        // ---------- 동등성 규약 ----------

        [Test]
        public void Equals_MatchesHashCodeForIdenticalValues()
        {
            var a = new BigNumber(2.5d, 9);
            var b = new BigNumber(2500000000d, 0);
            Assert.IsTrue(a.Equals(b));
            Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
        }
    }
}
