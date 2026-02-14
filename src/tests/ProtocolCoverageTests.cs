using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Common.Models;
using MemoryPack;
using FluentAssertions;
using Xunit;
using System.Reflection;
using System.Numerics;

namespace MegabonkTogether.Tests
{
    public class ProtocolCoverageTests
    {
        private static readonly Assembly CommonAssembly = typeof(IGameNetworkMessage).Assembly;

        [Fact]
        public void AllMessageTypes_ShouldBeRegisteredInUnion()
        {
            var interfaceType = typeof(IGameNetworkMessage);

            var registeredTypes = interfaceType.GetCustomAttributes<MemoryPackUnionAttribute>()
                .Select(attr => attr.Type)
                .ToHashSet();

            var implementingTypes = CommonAssembly.GetTypes()
                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToHashSet();

            var missingFromUnion = implementingTypes.Except(registeredTypes).ToList();
            var extraInUnion = registeredTypes.Except(implementingTypes).ToList();

            missingFromUnion.Should().BeEmpty(
                $"these types implement IGameNetworkMessage but aren't in MemoryPackUnion: {string.Join(", ", missingFromUnion.Select(t => t.Name))}");

            extraInUnion.Should().BeEmpty(
                $"these types are in MemoryPackUnion but don't implement IGameNetworkMessage: {string.Join(", ", extraInUnion.Select(t => t.Name))}");
        }

        [Fact]
        public void AllMessageTypes_ShouldRoundTripWithPopulatedData()
        {
            var interfaceType = typeof(IGameNetworkMessage);
            var messageTypes = CommonAssembly.GetTypes()
                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .ToList();

            var failures = new List<string>();

            foreach (var messageType in messageTypes)
            {
                try
                {
                    var instance = TestDataBuilder.BuildPopulated(messageType);
                    var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>((IGameNetworkMessage)instance);
                    var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

                    parsed.Should().NotBeNull($"deserialization returned null for {messageType.Name}");
                    parsed.Should().BeOfType(messageType, $"{messageType.Name} round-tripped as {parsed?.GetType().Name}");
                }
                catch (Exception ex)
                {
                    failures.Add($"{messageType.Name}: {ex.Message}");
                }
            }

            failures.Should().BeEmpty(
                $"these message types failed to round-trip: {Environment.NewLine}{string.Join(Environment.NewLine, failures)}");
        }

        [Fact]
        public void AllMessageTypes_ShouldHaveUniqueUnionCodes()
        {
            var interfaceType = typeof(IGameNetworkMessage);
            var unions = interfaceType.GetCustomAttributes<MemoryPackUnionAttribute>().ToList();

            var codes = unions.Select(u => u.Tag).ToList();
            var uniqueCodes = codes.Distinct().ToList();

            codes.Should().HaveSameCount(uniqueCodes,
                $"duplicate union codes found: {string.Join(", ", codes.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key))}");
        }

        [Theory]
        [MemberData(nameof(GetAllMessageTypes))]
        public void MessageType_ShouldRoundTrip(Type messageType)
        {
            var instance = TestDataBuilder.BuildPopulated(messageType);
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>((IGameNetworkMessage)instance);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);

            parsed.Should().NotBeNull();
            parsed.Should().BeOfType(messageType);
        }

        public static IEnumerable<object[]> GetAllMessageTypes()
        {
            var interfaceType = typeof(IGameNetworkMessage);
            return CommonAssembly.GetTypes()
                .Where(t => interfaceType.IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                .Select(t => new object[] { t });
        }
    }

    internal static class TestDataBuilder
    {
        private static readonly Dictionary<Type, object> DefaultValues = new()
        {
            { typeof(int), 42 },
            { typeof(uint), 42u },
            { typeof(long), 42L },
            { typeof(ulong), 42ul },
            { typeof(short), (short)42 },
            { typeof(ushort), (ushort)42 },
            { typeof(byte), (byte)42 },
            { typeof(sbyte), (sbyte)42 },
            { typeof(float), 3.14f },
            { typeof(double), 3.14 },
            { typeof(decimal), 3.14m },
            { typeof(bool), true },
            { typeof(char), 'X' },
            { typeof(string), "TestData" },
        };

        public static object BuildPopulated(Type type)
        {
            if (DefaultValues.TryGetValue(type, out var defaultValue))
            {
                return defaultValue;
            }

            if (type.IsEnum)
            {
                var enumValues = Enum.GetValues(type);
                return enumValues.GetValue(enumValues.Length > 1 ? 1 : 0)!;
            }

            if (type == typeof(QuantizedVector3))
            {
                return new QuantizedVector3 { QuantizedX = 100, QuantizedY = 200, QuantizedZ = 300 };
            }

            if (type == typeof(QuantizedVector2))
            {
                return new QuantizedVector2 { QuantizedX = 10, QuantizedY = 20 };
            }

            if (type == typeof(QuantizedRotation))
            {
                return new QuantizedRotation { QuantizedYaw = 45 };
            }

            if (type == typeof(AnimatorState))
            {
                return new AnimatorState { State = 0b00011111 };
            }

            if (type == typeof(MovementState))
            {
                return new MovementState
                {
                    AxisInput = new QuantizedVector2 { QuantizedX = 1, QuantizedY = 1 },
                    CameraForward = new QuantizedVector3 { QuantizedX = 0, QuantizedY = 100, QuantizedZ = 0 },
                    CameraRight = new QuantizedVector3 { QuantizedX = 100, QuantizedY = 0, QuantizedZ = 0 }
                };
            }

            if (type == typeof(Vector3))
            {
                return new Vector3(1, 2, 3);
            }

            if (type == typeof(Quaternion))
            {
                return Quaternion.Identity;
            }

            if (type == typeof(Specific))
            {
                return new Specific { ShadyGuyRarity = 5 };
            }

            if (type == typeof(InventoryInfo))
            {
                return new InventoryInfo
                {
                    WeaponInfos = new List<WeaponInfo> { new WeaponInfo { EWeapon = 1, Level = 2 } },
                    TomeInfos = new List<TomeInfo> { new TomeInfo { ETome = 1, Level = 3 } }
                };
            }

            if (type == typeof(WeaponInfo))
            {
                return new WeaponInfo { EWeapon = 1, Level = 5 };
            }

            if (type == typeof(TomeInfo))
            {
                return new TomeInfo { ETome = 2, Level = 3 };
            }

            if (type == typeof(StatModifierModel))
            {
                return new StatModifierModel { StatType = 1, Value = 10f, ModificationType = 0 };
            }

            if (type == typeof(EnemyModel))
            {
                return new EnemyModel
                {
                    Id = 99,
                    Hp = 500f,
                    Position = new QuantizedVector3 { QuantizedX = 1, QuantizedY = 2, QuantizedZ = 3 },
                    Yaw = new QuantizedRotation { QuantizedYaw = 90 }
                };
            }

            if (type == typeof(BossOrbModel))
            {
                return new BossOrbModel
                {
                    Id = 7,
                    Position = new QuantizedVector3 { QuantizedX = 1, QuantizedY = 2, QuantizedZ = 3 }
                };
            }

            if (type == typeof(Player))
            {
                return new Player
                {
                    ConnectionId = 1,
                    IsHost = false,
                    Character = 2,
                    Name = "TestPlayer",
                    Hp = 100,
                    MaxHp = 100
                };
            }

            if (type == typeof(Projectile))
            {
                return new Projectile
                {
                    Id = 1,
                    Position = new QuantizedVector3 { QuantizedX = 1, QuantizedY = 2, QuantizedZ = 3 },
                    FordwardVector = new QuantizedVector3 { QuantizedX = 0, QuantizedY = 0, QuantizedZ = 1 }
                };
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = type.GetGenericArguments()[0];
                var list = (System.Collections.IList)Activator.CreateInstance(type)!;
                list.Add(BuildPopulated(itemType));
                return list;
            }

            if (typeof(IEnumerable<>).IsAssignableFrom(type) && type.IsInterface)
            {
                var itemType = type.GetGenericArguments()[0];
                var listType = typeof(List<>).MakeGenericType(itemType);
                var list = (System.Collections.IList)Activator.CreateInstance(listType)!;
                list.Add(BuildPopulated(itemType));
                return list;
            }

            var instance = Activator.CreateInstance(type);
            if (instance == null)
            {
                throw new InvalidOperationException($"Failed to create instance of {type.Name}");
            }

            PopulateProperties(instance, type);

            return instance;
        }

        private static void PopulateProperties(object instance, Type type)
        {
            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanWrite || prop.SetMethod == null)
                    continue;

                if (prop.GetCustomAttributes(typeof(MemoryPackIgnoreAttribute), false).Any())
                    continue;

                try
                {
                    var value = BuildPopulated(prop.PropertyType);
                    prop.SetValue(instance, value);
                }
                catch
                {
                }
            }

            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.IsInitOnly)
                    continue;

                if (field.GetCustomAttributes(typeof(MemoryPackIgnoreAttribute), false).Any())
                    continue;

                try
                {
                    var value = BuildPopulated(field.FieldType);
                    field.SetValue(instance, value);
                }
                catch
                {
                }
            }
        }
    }
}
