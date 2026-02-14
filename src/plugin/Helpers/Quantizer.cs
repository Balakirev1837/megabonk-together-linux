using MegabonkTogether.Common;
using MegabonkTogether.Common.Models;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MegabonkTogether.Helpers
{
    public static class Quantizer
    {
        public static void InitializeFromWorldSize(Vector3 worldSize)
        {
            QuantizerCore.ConfigureWorldBounds(-worldSize.x / 2f, worldSize.x / 2f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Dequantize(QuantizedRotation rotation)
        {
            float yaw = QuantizerCore.DequantizeYaw(rotation.QuantizedYaw);
            return Quaternion.Euler(0, yaw, 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Quantize(float value)
        {
            return QuantizerCore.Quantize(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector4 Quantize(Quaternion rotation)
        {
            return new QuantizedVector4
            {
                QuantizedX = QuantizerCore.Quantize(rotation.x),
                QuantizedY = QuantizerCore.Quantize(rotation.y),
                QuantizedZ = QuantizerCore.Quantize(rotation.z),
                QuantizedW = QuantizerCore.Quantize(rotation.w)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion Dequantize(QuantizedVector4 qRot)
        {
            return new Quaternion(
                QuantizerCore.Dequantize(qRot.QuantizedX),
                QuantizerCore.Dequantize(qRot.QuantizedY),
                QuantizerCore.Dequantize(qRot.QuantizedZ),
                QuantizerCore.Dequantize(qRot.QuantizedW)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector3 Quantize(Vector3 position)
        {
            return new QuantizedVector3
            {
                QuantizedX = QuantizerCore.Quantize(position.x),
                QuantizedY = QuantizerCore.Quantize(position.y),
                QuantizedZ = QuantizerCore.Quantize(position.z)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Dequantize(QuantizedVector3 qPos)
        {
            return new Vector3(
                QuantizerCore.Dequantize(qPos.QuantizedX),
                QuantizerCore.Dequantize(qPos.QuantizedY),
                QuantizerCore.Dequantize(qPos.QuantizedZ)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedVector2 Quantize(Vector2 position)
        {
            return new QuantizedVector2
            {
                QuantizedX = QuantizerCore.Quantize(position.x),
                QuantizedY = QuantizerCore.Quantize(position.y)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Dequantize(QuantizedVector2 qPos)
        {
            return new Vector2(
                QuantizerCore.Dequantize(qPos.QuantizedX),
                QuantizerCore.Dequantize(qPos.QuantizedY)
            );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Dequantize(short q)
        {
            return QuantizerCore.Dequantize(q);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QuantizedRotation QuantizeYaw(float yawDeg)
        {
            return QuantizerCore.QuantizeYaw(yawDeg);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(QuantizedRotation qRot)
        {
            return QuantizerCore.DequantizeYaw(qRot);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DequantizeYaw(ushort qYaw)
        {
            return QuantizerCore.DequantizeYaw(qYaw);
        }
    }
}
