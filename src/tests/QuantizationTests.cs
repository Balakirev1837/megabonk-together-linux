using MegabonkTogether.Common.Models;
using FluentAssertions;
using Xunit;
using System;

namespace MegabonkTogether.Tests
{
    public static class TestQuantizer
    {
        // Mocking world size to [-500, 500]
        private static float GetWorldMin() => -500f;
        private static float GetWorldMax() => 500f;
        private static float GetRange() => 1000f;

        public static short Quantize(float value)
        {
            float t = (value - GetWorldMin()) / GetRange();
            return (short)(t * short.MaxValue);
        }

        public static float Dequantize(short q)
        {
            float t = q / (float)short.MaxValue;
            return GetWorldMin() + t * GetRange();
        }

        public static QuantizedRotation QuantizeYaw(float yawDeg)
        {
            // Manual Repeat logic to avoid UnityEngine.Mathf dependency
            float repeatYaw = yawDeg % 360f;
            if (repeatYaw < 0) repeatYaw += 360f;
            
            var res = (ushort)(repeatYaw / 360f * ushort.MaxValue);
            return new QuantizedRotation { QuantizedYaw = res };
        }

        public static float DequantizeYaw(QuantizedRotation qRot)
        {
            return (qRot.QuantizedYaw / (float)ushort.MaxValue) * 360f;
        }

        public static float ManualRepeat(float value, float length)
        {
            float res = value % length;
            if (res < 0) res += length;
            return res;
        }
    }

    public class QuantizationTests
    {
        [Theory]
        [InlineData(0f)]
        [InlineData(180f)]
        [InlineData(359.9f)]
        [InlineData(-10f)]
        [InlineData(720f)]
        public void Yaw_ShouldRoundTripWithAcceptablePrecision(float inputYaw)
        {
            // Act
            var quantized = TestQuantizer.QuantizeYaw(inputYaw);
            var result = TestQuantizer.DequantizeYaw(quantized);

            // Assert
            // Precision should be within 360 / 65535 ~= 0.005 degrees
            result.Should().BeApproximately(TestQuantizer.ManualRepeat(inputYaw, 360f), 0.01f);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(100f)]
        [InlineData(-250f)]
        [InlineData(499f)]
        public void Position_ShouldRoundTripWithAcceptablePrecision(float inputPos)
        {
            // Act
            var quantized = TestQuantizer.Quantize(inputPos);
            var result = TestQuantizer.Dequantize(quantized);

            // Assert
            // Range is 1000, short is 32767. Precision ~= 1000 / 32767 ~= 0.03
            result.Should().BeApproximately(inputPos, 0.05f);
        }

        [Fact]
        public void Quantize_ShouldBeClampedByRange()
        {
             // Test boundaries
             TestQuantizer.Quantize(-500f).Should().Be(0);
             // short.MaxValue is the max t=1.0
             TestQuantizer.Quantize(500f).Should().Be(short.MaxValue);
        }
    }
}