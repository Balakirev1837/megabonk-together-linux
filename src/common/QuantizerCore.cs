using MegabonkTogether.Common.Models;
using System.Runtime.CompilerServices;

namespace MegabonkTogether.Common
{
    public static class QuantizerCore
    {
        public const float DEFAULT_WORLD_MIN = -500f;
        public const float DEFAULT_WORLD_MAX = 500f;
        public const float DEFAULT_RANGE = 1000f;

        private static float worldMin = DEFAULT_WORLD_MIN;
        private static float worldMax = DEFAULT_WORLD_MAX;
        private static float range = DEFAULT_RANGE;

        public static void ConfigureWorldBounds(float min, float max)
        {
            worldMin = min;
            worldMax = max;
            range = max - min;
        }

        public static void ResetToDefaults()
        {
            worldMin = DEFAULT_WORLD_MIN;
            worldMax = DEFAULT_WORLD_MAX;
            range = DEFAULT_RANGE;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Quantize(float value)
        {
            float t = (value - worldMin) / range;
            return (short)(t * short.MaxValue);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dequantize(short q)
        {
            float t = q / (float)short.MaxValue;
            return worldMin + t * range;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedRotation QuantizeYaw(float yawDeg)
        {
            yawDeg = Repeat(yawDeg, 360f);
            var res = (ushort)(yawDeg / 360f * ushort.MaxValue);
            return new QuantizedRotation { QuantizedYaw = res };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(ushort qYaw)
        {
            return (qYaw / (float)ushort.MaxValue) * 360f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(QuantizedRotation qRot)
        {
            return DequantizeYaw(qRot.QuantizedYaw);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Repeat(float value, float length)
        {
            float res = value % length;
            if (res < 0) res += length;
            return res;
        }

        public static float WorldMin => worldMin;
        public static float WorldMax => worldMax;
        public static float Range => range;
    }
}
