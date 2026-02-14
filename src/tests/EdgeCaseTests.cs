using MegabonkTogether.Common;
using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Common.Models;
using MemoryPack;
using FluentAssertions;
using Xunit;

namespace MegabonkTogether.Tests
{
    public class EdgeCaseTests
    {
        public EdgeCaseTests()
        {
            QuantizerCore.ResetToDefaults();
        }

        [Fact]
        public void Deserialization_CorruptedBytes_ShouldHandleGracefully()
        {
            var corruptedBytes = new byte[] { 0xFF, 0xFE, 0xFD, 0xFC };
            
            var result = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(corruptedBytes);
            
            result.Should().BeNull();
        }

        [Fact]
        public void Deserialization_EmptyBytes_ShouldThrow()
        {
            var emptyBytes = Array.Empty<byte>();
            
            Action act = () => MemoryPackSerializer.Deserialize<IGameNetworkMessage>(emptyBytes);
            
            act.Should().Throw<Exception>();
        }

        [Fact]
        public void LobbyUpdates_EmptyCollections_ShouldRoundTrip()
        {
            var original = new LobbyUpdates
            {
                Enemies = new List<EnemyModel>(),
                Players = new List<Player>(),
                BossOrbs = new List<BossOrbModel>(),
                RecentDeaths = new List<uint>()
            };

            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

            parsed.Should().BeOfType<LobbyUpdates>();
            var typed = (LobbyUpdates)parsed!;
            typed.Enemies.Should().BeEmpty();
            typed.RecentDeaths.Should().BeEmpty();
        }

        [Fact]
        public void PlayerUpdate_BoundaryValues_ShouldRoundTrip()
        {
            var original = new PlayerUpdate
            {
                ConnectionId = uint.MaxValue,
                Hp = uint.MaxValue,
                MaxHp = uint.MaxValue,
                Shield = uint.MaxValue,
                MaxShield = uint.MaxValue,
                Name = new string('X', 1000)
            };

            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

            parsed.Should().BeOfType<PlayerUpdate>();
            var typed = (PlayerUpdate)parsed!;
            typed.ConnectionId.Should().Be(uint.MaxValue);
            typed.Hp.Should().Be(uint.MaxValue);
            typed.Name.Should().Be(new string('X', 1000));
        }

        [Fact]
        public void SpawnedEnemy_NegativeHp_ShouldRoundTrip()
        {
            var original = new SpawnedEnemy
            {
                Hp = -1f,
                Id = uint.MaxValue,
                Name = int.MinValue
            };

            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

            parsed.Should().BeOfType<SpawnedEnemy>();
            var typed = (SpawnedEnemy)parsed!;
            typed.Hp.Should().Be(-1f);
        }

        [Fact]
        public void QuantizedVector3_BoundaryValues_ShouldRoundTrip()
        {
            var original = new QuantizedVector3
            {
                QuantizedX = short.MinValue,
                QuantizedY = short.MaxValue,
                QuantizedZ = 0
            };

            var bytes = MemoryPackSerializer.Serialize(original);
            var parsed = MemoryPackSerializer.Deserialize<QuantizedVector3>(bytes);

            parsed.QuantizedX.Should().Be(short.MinValue);
            parsed.QuantizedY.Should().Be(short.MaxValue);
            parsed.QuantizedZ.Should().Be(0);
        }

        [Fact]
        public void QuantizedRotation_BoundaryValues_ShouldRoundTrip()
        {
            var original = new QuantizedRotation
            {
                QuantizedYaw = ushort.MaxValue
            };

            var bytes = MemoryPackSerializer.Serialize(original);
            var parsed = MemoryPackSerializer.Deserialize<QuantizedRotation>(bytes);

            parsed.QuantizedYaw.Should().Be(ushort.MaxValue);
        }

        [Fact]
        public void Quantize_ShortMin_ShouldBeBelowWorldMin()
        {
            var result = QuantizerCore.Dequantize(short.MinValue);
            result.Should().BeLessThan(QuantizerCore.WorldMin);
        }

        [Fact]
        public void Quantize_ShortMax_ShouldBeAtWorldMax()
        {
            var result = QuantizerCore.Dequantize(short.MaxValue);
            result.Should().BeApproximately(QuantizerCore.WorldMax, 0.05f);
        }

        [Fact]
        public void Yaw_ZeroAndMax_ShouldBeAtExtremes()
        {
            var yaw0 = QuantizerCore.DequantizeYaw((ushort)0);
            var yawMax = QuantizerCore.DequantizeYaw(ushort.MaxValue);

            yaw0.Should().BeApproximately(0f, 0.01f);
            yawMax.Should().BeApproximately(360f, 0.01f);
        }

        [Fact]
        public void PlayerUpdate_EmptyName_ShouldRoundTrip()
        {
            var original = new PlayerUpdate
            {
                ConnectionId = 1,
                Name = ""
            };

            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

            parsed.Should().BeOfType<PlayerUpdate>();
            var typed = (PlayerUpdate)parsed!;
            typed.Name.Should().Be("");
        }

        [Fact]
        public void InventoryInfo_EmptyCollections_ShouldRoundTrip()
        {
            var original = new InventoryInfo
            {
                WeaponInfos = new List<WeaponInfo>(),
                TomeInfos = new List<TomeInfo>()
            };

            var bytes = MemoryPackSerializer.Serialize(original);
            var parsed = MemoryPackSerializer.Deserialize<InventoryInfo>(bytes);

            parsed.WeaponInfos.Should().BeEmpty();
            parsed.TomeInfos.Should().BeEmpty();
        }

        [Fact]
        public void EnemyModel_ZeroValues_ShouldRoundTrip()
        {
            var original = new EnemyModel
            {
                Id = 0,
                Hp = 0f,
                Position = new QuantizedVector3(),
                Yaw = new QuantizedRotation()
            };

            var bytes = MemoryPackSerializer.Serialize(original);
            var parsed = MemoryPackSerializer.Deserialize<EnemyModel>(bytes);

            parsed.Id.Should().Be(0);
            parsed.Hp.Should().Be(0f);
        }

        [Fact]
        public void Position_Zero_ShouldRoundTripToMiddle()
        {
            var quantized = QuantizerCore.Quantize(0f);
            var result = QuantizerCore.Dequantize(quantized);

            result.Should().BeApproximately(0f, 0.05f);
        }

        [Fact]
        public void Position_SymmetricValues_ShouldBeSymmetric()
        {
            var posResult = QuantizerCore.Dequantize(QuantizerCore.Quantize(250f));
            var negResult = QuantizerCore.Dequantize(QuantizerCore.Quantize(-250f));

            posResult.Should().BeApproximately(-negResult, 0.1f);
        }

        [Theory]
        [InlineData(0f)]
        [InlineData(90f)]
        [InlineData(180f)]
        [InlineData(270f)]
        public void Yaw_CardinalDirections_ShouldRoundTrip(float yaw)
        {
            var quantized = QuantizerCore.QuantizeYaw(yaw);
            var result = QuantizerCore.DequantizeYaw(quantized);

            result.Should().BeApproximately(yaw, 0.01f);
        }

        [Fact]
        public void TruncatedMessage_ShouldThrowOnDeserialization()
        {
            var original = new PlayerUpdate
            {
                ConnectionId = 42,
                Name = "Test"
            };
            
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var truncatedBytes = bytes.Take(bytes.Length / 2).ToArray();

            Action act = () => MemoryPackSerializer.Deserialize<IGameNetworkMessage>(truncatedBytes);

            act.Should().Throw<Exception>();
        }
    }
}
