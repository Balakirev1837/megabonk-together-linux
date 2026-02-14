using MegabonkTogether.Common;
using MegabonkTogether.Common.Models;
using FluentAssertions;
using Xunit;

namespace MegabonkTogether.Tests
{
    public class QuantizationTests
    {
        public QuantizationTests()
        {
            QuantizerCore.ResetToDefaults();
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(180f)]
        [InlineData(359.9f)]
        [InlineData(-10f)]
        [InlineData(720f)]
        public void Yaw_ShouldRoundTripWithAcceptablePrecision(float inputYaw)
        {
            var quantized = QuantizerCore.QuantizeYaw(inputYaw);
            var result = QuantizerCore.DequantizeYaw(quantized);

            result.Should().BeApproximately(QuantizerCore.Repeat(inputYaw, 360f), 0.01f);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(100f)]
        [InlineData(-250f)]
        [InlineData(499f)]
        public void Position_ShouldRoundTripWithAcceptablePrecision(float inputPos)
        {
            var quantized = QuantizerCore.Quantize(inputPos);
            var result = QuantizerCore.Dequantize(quantized);

            result.Should().BeApproximately(inputPos, 0.05f);
        }

        [Fact]
        public void Quantize_ShouldBeClampedByRange()
        {
            QuantizerCore.Quantize(-500f).Should().Be((short)0);
            QuantizerCore.Quantize(500f).Should().Be(short.MaxValue);
        }

        [Fact]
        public void Quantize_AtWorldMin_ShouldBeZero()
        {
            var result = QuantizerCore.Quantize(QuantizerCore.WorldMin);
            result.Should().Be((short)0);
        }

        [Fact]
        public void Quantize_AtWorldMax_ShouldBeShortMax()
        {
            var result = QuantizerCore.Quantize(QuantizerCore.WorldMax);
            result.Should().Be(short.MaxValue);
        }

        [Fact]
        public void Dequantize_AtZero_ShouldBeWorldMin()
        {
            var result = QuantizerCore.Dequantize(0);
            result.Should().BeApproximately(QuantizerCore.WorldMin, 0.001f);
        }

        [Fact]
        public void Dequantize_AtShortMax_ShouldBeWorldMax()
        {
            var result = QuantizerCore.Dequantize(short.MaxValue);
            result.Should().BeApproximately(QuantizerCore.WorldMax, 0.05f);
        }

        [Theory]
        [InlineData(450f)]
        [InlineData(600f)]
        [InlineData(-600f)]
        public void Quantize_OutOfBounds_ShouldStillProduceValues(float input)
        {
            var quantized = QuantizerCore.Quantize(input);
            var result = QuantizerCore.Dequantize(quantized);
            
            short.MaxValue.Should().BeGreaterThan(quantized);
            short.MinValue.Should().BeLessThan(quantized);
        }

        [Fact]
        public void ConfigureWorldBounds_ShouldAffectQuantization()
        {
            QuantizerCore.ConfigureWorldBounds(-1000f, 1000f);

            QuantizerCore.Quantize(-1000f).Should().Be((short)0);
            QuantizerCore.Quantize(1000f).Should().Be(short.MaxValue);

            var result = QuantizerCore.Dequantize((short)(short.MaxValue / 2));
            result.Should().BeApproximately(0f, 0.1f);

            QuantizerCore.ResetToDefaults();
        }

        [Fact]
        public void Yaw_NegativeAngle_ShouldWrapToPositive()
        {
            var quantized = QuantizerCore.QuantizeYaw(-90f);
            var result = QuantizerCore.DequantizeYaw(quantized);

            result.Should().BeApproximately(270f, 0.01f);
        }

        [Fact]
        public void Yaw_LargePositiveAngle_ShouldWrap()
        {
            var quantized = QuantizerCore.QuantizeYaw(450f);
            var result = QuantizerCore.DequantizeYaw(quantized);

            result.Should().BeApproximately(90f, 0.01f);
        }
    }
}
