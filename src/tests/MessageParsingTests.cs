using MegabonkTogether.Common.Messages;
using MegabonkTogether.Common.Messages.GameNetworkMessages;
using MegabonkTogether.Common.Models;
using MemoryPack;
using FluentAssertions;
using Xunit;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MegabonkTogether.Tests
{
    public class MessageParsingTests
    {
        private static void AssertRoundTrip<T>(T original) where T : class, IGameNetworkMessage
        {
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<T>();
        }

        [Fact]
        public void RequestChestOpen_ShouldRoundTripCorrectly()
        {
            var original = new RequestChestOpen { ChestId = 42, RequestingPlayerId = 7 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<RequestChestOpen>();
            var typedParsed = (RequestChestOpen)parsed!;
            typedParsed.ChestId.Should().Be(original.ChestId);
            typedParsed.RequestingPlayerId.Should().Be(original.RequestingPlayerId);
        }

        [Fact]
        public void GrantChestOpen_ShouldRoundTripCorrectly()
        {
            var original = new GrantChestOpen { ChestId = 100, GrantedPlayerId = 1 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<GrantChestOpen>();
            var typedParsed = (GrantChestOpen)parsed!;
            typedParsed.ChestId.Should().Be(original.ChestId);
            typedParsed.GrantedPlayerId.Should().Be(original.GrantedPlayerId);
        }

        [Fact]
        public void LobbyUpdates_ShouldRoundTripWithCollections()
        {
            var original = new LobbyUpdates
            {
                Enemies = new List<EnemyModel>
                {
                    new EnemyModel { Id = 1, Position = new QuantizedVector3 { QuantizedX = 10 } },
                    new EnemyModel { Id = 2, Position = new QuantizedVector3 { QuantizedY = 20 } }
                }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<LobbyUpdates>();
            var typedParsed = (LobbyUpdates)parsed!;
            typedParsed.Enemies.Should().HaveCount(2);
            typedParsed.Enemies.ElementAt(0).Id.Should().Be(1);
            typedParsed.Enemies.ElementAt(0).Position.QuantizedX.Should().Be(10);
            typedParsed.Enemies.ElementAt(1).Id.Should().Be(2);
            typedParsed.Enemies.ElementAt(1).Position.QuantizedY.Should().Be(20);
        }

        [Fact]
        public void PlayerUpdate_ShouldParseComplexStateCorrectly()
        {
            var original = new PlayerUpdate
            {
                ConnectionId = 9,
                Name = "Tester",
                Hp = 100,
                Position = new Vector3(1.23f, 4.56f, 7.89f),
                AnimatorState = new AnimatorState { IsJumping = true, IsGrounded = false }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PlayerUpdate>();
            var typedParsed = (PlayerUpdate)parsed!;
            typedParsed.ConnectionId.Should().Be(9);
            typedParsed.Name.Should().Be("Tester");
            typedParsed.Position.X.Should().BeApproximately(1.23f, 0.01f);
            typedParsed.AnimatorState.IsJumping.Should().BeTrue();
            typedParsed.AnimatorState.IsGrounded.Should().BeFalse();
        }

        [Fact]
        public void ClientInGameReady_ShouldRoundTripCorrectly()
        {
            var original = new ClientInGameReady { ConnectionId = 42 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ClientInGameReady>();
            var typedParsed = (ClientInGameReady)parsed!;
            typedParsed.ConnectionId.Should().Be(42u);
        }

        [Fact]
        public void SpawnedObject_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedObject
            {
                Position = new Vector3(1, 2, 3),
                Rotation = Quaternion.Identity,
                Scale = new Vector3(2, 2, 2),
                PrefabName = "TestPrefab",
                Id = 99,
                SpecificData = new Specific { ShadyGuyRarity = 5 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedObject>();
            var typedParsed = (SpawnedObject)parsed!;
            typedParsed.Position.X.Should().Be(1f);
            typedParsed.PrefabName.Should().Be("TestPrefab");
            typedParsed.Id.Should().Be(99u);
            typedParsed.SpecificData.ShadyGuyRarity.Should().Be(5);
        }

        [Fact]
        public void SpawnedEnemy_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedEnemy
            {
                Name = 1,
                Id = 42,
                ShouldForce = true,
                Flag = 3,
                Position = new Vector3(10, 20, 30),
                Wave = 5,
                CanBeElite = true,
                TargetId = 7,
                Hp = 1000f,
                ExtraSizeMultiplier = 1.5f,
                ReviverId = 99
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedEnemy>();
            var typedParsed = (SpawnedEnemy)parsed!;
            typedParsed.Name.Should().Be(1);
            typedParsed.Id.Should().Be(42u);
            typedParsed.ShouldForce.Should().BeTrue();
            typedParsed.Hp.Should().Be(1000f);
            typedParsed.ReviverId.Should().Be(99u);
        }

        [Fact]
        public void SelectedCharacter_ShouldRoundTripCorrectly()
        {
            var original = new SelectedCharacter
            {
                ConnectionId = 1,
                Character = 5,
                Skin = "CoolSkin"
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SelectedCharacter>();
            var typedParsed = (SelectedCharacter)parsed!;
            typedParsed.ConnectionId.Should().Be(1u);
            typedParsed.Character.Should().Be(5u);
            typedParsed.Skin.Should().Be("CoolSkin");
        }

        [Fact]
        public void EnemyDied_ShouldRoundTripCorrectly()
        {
            var original = new EnemyDied { EnemyId = 42, DiedByOwnerId = 7 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<EnemyDied>();
            var typedParsed = (EnemyDied)parsed!;
            typedParsed.EnemyId.Should().Be(42u);
            typedParsed.DiedByOwnerId.Should().Be(7u);
        }

        [Fact]
        public void SpawnedProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 100, QuantizedY = 200, QuantizedZ = 300 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedX = 1, QuantizedY = 0, QuantizedZ = 0 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedProjectile>();
            var typedParsed = (SpawnedProjectile)parsed!;
            typedParsed.Id.Should().Be(1u);
            typedParsed.OwnerId.Should().Be(5u);
            typedParsed.Weapon.Should().Be(3);
        }

        [Fact]
        public void ProjectileDone_ShouldRoundTripCorrectly()
        {
            var original = new ProjectileDone { ProjectileId = 42 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ProjectileDone>();
            var typedParsed = (ProjectileDone)parsed!;
            typedParsed.ProjectileId.Should().Be(42u);
        }

        [Fact]
        public void SpawnedPickupOrb_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedPickupOrb
            {
                Pickup = 3,
                Position = new Vector3(5, 10, 15)
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedPickupOrb>();
            var typedParsed = (SpawnedPickupOrb)parsed!;
            typedParsed.Pickup.Should().Be(3);
            typedParsed.Position.X.Should().Be(5f);
        }

        [Fact]
        public void SpawnedPickup_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedPickup
            {
                Id = 1,
                Pickup = 5,
                Position = new Vector3(10, 20, 30),
                Value = 100
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedPickup>();
            var typedParsed = (SpawnedPickup)parsed!;
            typedParsed.Id.Should().Be(1u);
            typedParsed.Pickup.Should().Be(5);
            typedParsed.Value.Should().Be(100);
        }

        [Fact]
        public void PickupApplied_ShouldRoundTripCorrectly()
        {
            var original = new PickupApplied { PickupId = 10, OwnerId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PickupApplied>();
            var typedParsed = (PickupApplied)parsed!;
            typedParsed.PickupId.Should().Be(10u);
            typedParsed.OwnerId.Should().Be(5u);
        }

        [Fact]
        public void PickupFollowingPlayer_ShouldRoundTripCorrectly()
        {
            var original = new PickupFollowingPlayer { PickupId = 1, PlayerId = 2 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PickupFollowingPlayer>();
            var typedParsed = (PickupFollowingPlayer)parsed!;
            typedParsed.PickupId.Should().Be(1u);
            typedParsed.PlayerId.Should().Be(2u);
        }

        [Fact]
        public void SpawnedChest_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedChest
            {
                Position = new Vector3(1, 2, 3),
                Rotation = Quaternion.CreateFromYawPitchRoll(0.5f, 0, 0),
                ChestId = 42
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedChest>();
            var typedParsed = (SpawnedChest)parsed!;
            typedParsed.ChestId.Should().Be(42u);
            typedParsed.Position.X.Should().Be(1f);
        }

        [Fact]
        public void ChestOpened_ShouldRoundTripCorrectly()
        {
            var original = new ChestOpened { ChestId = 10, OwnerId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ChestOpened>();
            var typedParsed = (ChestOpened)parsed!;
            typedParsed.ChestId.Should().Be(10u);
            typedParsed.OwnerId.Should().Be(5u);
        }

        [Fact]
        public void WeaponAdded_ShouldRoundTripCorrectly()
        {
            var original = new WeaponAdded
            {
                Weapon = 3,
                OwnerId = 7,
                Upgrades = new List<StatModifierModel>
                {
                    new StatModifierModel { StatType = 1, Value = 10f, ModificationType = 0 }
                }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<WeaponAdded>();
            var typedParsed = (WeaponAdded)parsed!;
            typedParsed.Weapon.Should().Be(3);
            typedParsed.OwnerId.Should().Be(7u);
            typedParsed.Upgrades.Should().HaveCount(1);
        }

        [Fact]
        public void InteractableUsed_ShouldRoundTripCorrectly()
        {
            var original = new InteractableUsed
            {
                NetplayId = 42,
                Action = InteractableAction.Interact,
                IsPortal = true,
                IsFinalPortal = false,
                IsCryptKey = false,
                OwnerId = 5
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<InteractableUsed>();
            var typedParsed = (InteractableUsed)parsed!;
            typedParsed.NetplayId.Should().Be(42u);
            typedParsed.Action.Should().Be(InteractableAction.Interact);
            typedParsed.IsPortal.Should().BeTrue();
        }

        [Fact]
        public void StartingChargingShrine_ShouldRoundTripCorrectly()
        {
            var original = new StartingChargingShrine { ShrineNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StartingChargingShrine>();
            var typedParsed = (StartingChargingShrine)parsed!;
            typedParsed.ShrineNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void StoppingChargingShrine_ShouldRoundTripCorrectly()
        {
            var original = new StoppingChargingShrine { ShrineNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StoppingChargingShrine>();
            var typedParsed = (StoppingChargingShrine)parsed!;
            typedParsed.ShrineNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void EnemyExploder_ShouldRoundTripCorrectly()
        {
            var original = new EnemyExploder { EnemyId = 10, SenderId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<EnemyExploder>();
            var typedParsed = (EnemyExploder)parsed!;
            typedParsed.EnemyId.Should().Be(10u);
            typedParsed.SenderId.Should().Be(5u);
        }

        [Fact]
        public void EnemyDamaged_ShouldRoundTripCorrectly()
        {
            var original = new EnemyDamaged
            {
                EnemyId = 1,
                Damage = 50f,
                DamageEffect = 2,
                DamageBlockedByArmor = 10,
                DamageSource = "Sword",
                DamageProcCoefficient = 1.0f,
                DamageElement = 0,
                DamageFlags = 1,
                DamageKnockback = 5f,
                DamageIsCrit = true,
                AttackerId = 7
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<EnemyDamaged>();
            var typedParsed = (EnemyDamaged)parsed!;
            typedParsed.EnemyId.Should().Be(1u);
            typedParsed.Damage.Should().Be(50f);
            typedParsed.DamageIsCrit.Should().BeTrue();
            typedParsed.DamageSource.Should().Be("Sword");
        }

        [Fact]
        public void SpawnedEnemySpecialAttack_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedEnemySpecialAttack
            {
                EnemyId = 1,
                AttackName = "Fireball",
                TargetId = 5
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedEnemySpecialAttack>();
            var typedParsed = (SpawnedEnemySpecialAttack)parsed!;
            typedParsed.EnemyId.Should().Be(1u);
            typedParsed.AttackName.Should().Be("Fireball");
            typedParsed.TargetId.Should().Be(5u);
        }

        [Fact]
        public void StartingChargingPylon_ShouldRoundTripCorrectly()
        {
            var original = new StartingChargingPylon { PylonNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StartingChargingPylon>();
            var typedParsed = (StartingChargingPylon)parsed!;
            typedParsed.PylonNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void StoppingChargingPylon_ShouldRoundTripCorrectly()
        {
            var original = new StoppingChargingPylon { PylonNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StoppingChargingPylon>();
            var typedParsed = (StoppingChargingPylon)parsed!;
            typedParsed.PylonNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void FinalBossOrbSpawned_ShouldRoundTripCorrectly()
        {
            var original = new FinalBossOrbSpawned
            {
                OrbType = Orb.Following,
                Target = 5,
                OrbId = 10
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<FinalBossOrbSpawned>();
            var typedParsed = (FinalBossOrbSpawned)parsed!;
            typedParsed.OrbType.Should().Be(Orb.Following);
            typedParsed.Target.Should().Be(5u);
            typedParsed.OrbId.Should().Be(10u);
        }

        [Fact]
        public void FinalBossOrbDestroyed_ShouldRoundTripCorrectly()
        {
            var original = new FinalBossOrbDestroyed { OrbId = 10, SenderId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<FinalBossOrbDestroyed>();
            var typedParsed = (FinalBossOrbDestroyed)parsed!;
            typedParsed.OrbId.Should().Be(10u);
            typedParsed.SenderId.Should().Be(5u);
        }

        [Fact]
        public void StartedSwarmEvent_ShouldRoundTripCorrectly()
        {
            var original = new StartedSwarmEvent { Duration = 30.5f };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StartedSwarmEvent>();
            var typedParsed = (StartedSwarmEvent)parsed!;
            typedParsed.Duration.Should().Be(30.5f);
        }

        [Fact]
        public void GameOver_ShouldRoundTripCorrectly()
        {
            var original = new GameOver();
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<GameOver>();
        }

        [Fact]
        public void PlayerDied_ShouldRoundTripCorrectly()
        {
            var original = new PlayerDied { PlayerId = 42 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PlayerDied>();
            var typedParsed = (PlayerDied)parsed!;
            typedParsed.PlayerId.Should().Be(42u);
        }

        [Fact]
        public void RetargetedEnemies_ShouldRoundTripCorrectly()
        {
            var original = new RetargetedEnemies
            {
                Enemy_NewTargetids = new List<(uint, uint)> { (1, 5), (2, 6) }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<RetargetedEnemies>();
            var typedParsed = (RetargetedEnemies)parsed!;
            typedParsed.Enemy_NewTargetids.Should().HaveCount(2);
        }

        [Fact]
        public void Introduced_ShouldRoundTripCorrectly()
        {
            var original = new Introduced
            {
                ConnectionId = 1,
                Name = "Player1",
                IsHost = true
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<Introduced>();
            var typedParsed = (Introduced)parsed!;
            typedParsed.ConnectionId.Should().Be(1u);
            typedParsed.Name.Should().Be("Player1");
            typedParsed.IsHost.Should().BeTrue();
        }

        [Fact]
        public void PlayerDisconnected_ShouldRoundTripCorrectly()
        {
            var original = new PlayerDisconnected { ConnectionId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PlayerDisconnected>();
            var typedParsed = (PlayerDisconnected)parsed!;
            typedParsed.ConnectionId.Should().Be(5u);
        }

        [Fact]
        public void RunStarted_ShouldRoundTripCorrectly()
        {
            var original = new RunStarted
            {
                MapData = 1,
                StageData = "Stage1",
                MapTierIndex = 2,
                MusicTrackIndex = 3,
                ChallengeName = "Challenge1"
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<RunStarted>();
            var typedParsed = (RunStarted)parsed!;
            typedParsed.MapData.Should().Be(1);
            typedParsed.StageData.Should().Be("Stage1");
            typedParsed.MapTierIndex.Should().Be(2);
            typedParsed.MusicTrackIndex.Should().Be(3);
            typedParsed.ChallengeName.Should().Be("Challenge1");
        }

        [Fact]
        public void ProjectilesUpdate_ShouldRoundTripCorrectly()
        {
            var original = new ProjectilesUpdate
            {
                Projectiles = new List<Projectile>
                {
                    new Projectile { Id = 1, Position = new QuantizedVector3 { QuantizedX = 10 } },
                    new Projectile { Id = 2, Position = new QuantizedVector3 { QuantizedY = 20 } }
                }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ProjectilesUpdate>();
            var typedParsed = (ProjectilesUpdate)parsed!;
            typedParsed.Projectiles.Should().HaveCount(2);
        }

        [Fact]
        public void TomeAdded_ShouldRoundTripCorrectly()
        {
            var original = new TomeAdded
            {
                Tome = 1,
                OwnerId = 5,
                Upgrades = new List<StatModifierModel>
                {
                    new StatModifierModel { StatType = 1, Value = 5f, ModificationType = 0 }
                },
                Rarity = 3
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TomeAdded>();
            var typedParsed = (TomeAdded)parsed!;
            typedParsed.Tome.Should().Be(1);
            typedParsed.OwnerId.Should().Be(5u);
            typedParsed.Rarity.Should().Be(3);
            typedParsed.Upgrades.Should().HaveCount(1);
        }

        [Fact]
        public void LightningStrike_ShouldRoundTripCorrectly()
        {
            var original = new LightningStrike
            {
                EnemyId = 1,
                Bounces = 3,
                Damage = 50f,
                DamageEffect = 0,
                DamageBlockedByArmor = 0,
                DamageSource = "Lightning",
                DamageIsCrit = true,
                DamageProcCoefficient = 1.0f,
                DamageElement = 2,
                DamageFlags = 0,
                DamageKnockback = 0f,
                BounceRange = 10f,
                BounceProcCoefficient = 0.5f,
                OwnerId = 5
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<LightningStrike>();
            var typedParsed = (LightningStrike)parsed!;
            typedParsed.EnemyId.Should().Be(1u);
            typedParsed.Bounces.Should().Be(3);
            typedParsed.Damage.Should().Be(50f);
            typedParsed.DamageIsCrit.Should().BeTrue();
        }

        [Fact]
        public void TornadoesSpawned_ShouldRoundTripCorrectly()
        {
            var original = new TornadoesSpawned { Amount = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TornadoesSpawned>();
            var typedParsed = (TornadoesSpawned)parsed!;
            typedParsed.Amount.Should().Be(5);
        }

        [Fact]
        public void StormStarted_ShouldRoundTripCorrectly()
        {
            var original = new StormStarted { StormOverAtTime = 60.5f };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StormStarted>();
            var typedParsed = (StormStarted)parsed!;
            typedParsed.StormOverAtTime.Should().Be(60.5f);
        }

        [Fact]
        public void StormStopped_ShouldRoundTripCorrectly()
        {
            var original = new StormStopped();
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StormStopped>();
        }

        [Fact]
        public void TumbleWeedSpawned_ShouldRoundTripCorrectly()
        {
            var original = new TumbleWeedSpawned
            {
                NetplayId = 1,
                Position = new QuantizedVector3 { QuantizedX = 10, QuantizedY = 20, QuantizedZ = 30 },
                Velocity = new QuantizedVector3 { QuantizedX = 1, QuantizedY = 0, QuantizedZ = 0 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TumbleWeedSpawned>();
            var typedParsed = (TumbleWeedSpawned)parsed!;
            typedParsed.NetplayId.Should().Be(1u);
            typedParsed.Position.QuantizedX.Should().Be((short)10);
            typedParsed.Velocity.QuantizedX.Should().Be((short)1);
        }

        [Fact]
        public void TumbleWeedsUpdate_ShouldRoundTripCorrectly()
        {
            var original = new TumbleWeedsUpdate
            {
                TumbleWeeds = new[]
                {
                    new TumbleWeedModel { NetplayId = 1, Position = new QuantizedVector3 { QuantizedX = 10 } },
                    new TumbleWeedModel { NetplayId = 2, Position = new QuantizedVector3 { QuantizedY = 20 } }
                }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TumbleWeedsUpdate>();
            var typedParsed = (TumbleWeedsUpdate)parsed!;
            typedParsed.TumbleWeeds.Should().HaveCount(2);
        }

        [Fact]
        public void TumbleWeedDespawned_ShouldRoundTripCorrectly()
        {
            var original = new TumbleWeedDespawned { NetplayId = 42 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TumbleWeedDespawned>();
            var typedParsed = (TumbleWeedDespawned)parsed!;
            typedParsed.NetplayId.Should().Be(42u);
        }

        [Fact]
        public void InteractableCharacterFightEnemySpawned_ShouldRoundTripCorrectly()
        {
            var original = new InteractableCharacterFightEnemySpawned { NetplayId = 42 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<InteractableCharacterFightEnemySpawned>();
            var typedParsed = (InteractableCharacterFightEnemySpawned)parsed!;
            typedParsed.NetplayId.Should().Be(42u);
        }

        [Fact]
        public void WantToStartFollowingPickup_ShouldRoundTripCorrectly()
        {
            var original = new WantToStartFollowingPickup { PickupId = 1, OwnerId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<WantToStartFollowingPickup>();
            var typedParsed = (WantToStartFollowingPickup)parsed!;
            typedParsed.PickupId.Should().Be(1u);
            typedParsed.OwnerId.Should().Be(5u);
        }

        [Fact]
        public void ItemAdded_ShouldRoundTripCorrectly()
        {
            var original = new ItemAdded { EItem = 10, OwnerId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ItemAdded>();
            var typedParsed = (ItemAdded)parsed!;
            typedParsed.EItem.Should().Be(10);
            typedParsed.OwnerId.Should().Be(5u);
        }

        [Fact]
        public void ItemRemoved_ShouldRoundTripCorrectly()
        {
            var original = new ItemRemoved { EItem = 10, OwnerId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<ItemRemoved>();
            var typedParsed = (ItemRemoved)parsed!;
            typedParsed.EItem.Should().Be(10);
            typedParsed.OwnerId.Should().Be(5u);
        }

        [Fact]
        public void WeaponToggled_ShouldRoundTripCorrectly()
        {
            var original = new WeaponToggled { OwnerId = 5, Enabled = true, EWeapon = 3 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<WeaponToggled>();
            var typedParsed = (WeaponToggled)parsed!;
            typedParsed.OwnerId.Should().Be(5u);
            typedParsed.Enabled.Should().BeTrue();
            typedParsed.EWeapon.Should().Be(3);
        }

        [Fact]
        public void SpawnedObjectInCrypt_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedObjectInCrypt
            {
                NetplayId = 1,
                Position = new QuantizedVector3 { QuantizedX = 10, QuantizedY = 20, QuantizedZ = 30 },
                IsCryptLeave = true
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedObjectInCrypt>();
            var typedParsed = (SpawnedObjectInCrypt)parsed!;
            typedParsed.NetplayId.Should().Be(1u);
            typedParsed.Position.QuantizedX.Should().Be((short)10);
            typedParsed.IsCryptLeave.Should().BeTrue();
        }

        [Fact]
        public void StartingChargingLamp_ShouldRoundTripCorrectly()
        {
            var original = new StartingChargingLamp { LampNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StartingChargingLamp>();
            var typedParsed = (StartingChargingLamp)parsed!;
            typedParsed.LampNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void StoppingChargingLamp_ShouldRoundTripCorrectly()
        {
            var original = new StoppingChargingLamp { LampNetplayId = 1, PlayerChargingId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<StoppingChargingLamp>();
            var typedParsed = (StoppingChargingLamp)parsed!;
            typedParsed.LampNetplayId.Should().Be(1u);
            typedParsed.PlayerChargingId.Should().Be(5u);
        }

        [Fact]
        public void TimerStarted_ShouldRoundTripCorrectly()
        {
            var original = new TimerStarted { IsDungeonTimer = true, SenderId = 5 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<TimerStarted>();
            var typedParsed = (TimerStarted)parsed!;
            typedParsed.IsDungeonTimer.Should().BeTrue();
            typedParsed.SenderId.Should().Be(5u);
        }

        [Fact]
        public void HatChanged_ShouldRoundTripCorrectly()
        {
            var original = new HatChanged { OwnerId = 5, EHat = 10 };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<HatChanged>();
            var typedParsed = (HatChanged)parsed!;
            typedParsed.OwnerId.Should().Be(5u);
            typedParsed.EHat.Should().Be(10);
        }

        [Fact]
        public void SpawnedReviver_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedReviver
            {
                Position = new Vector3(10, 20, 30),
                OwnerConnectionId = 5,
                ReviverId = 1
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedReviver>();
            var typedParsed = (SpawnedReviver)parsed!;
            typedParsed.Position.X.Should().Be(10f);
            typedParsed.OwnerConnectionId.Should().Be(5u);
            typedParsed.ReviverId.Should().Be(1u);
        }

        [Fact]
        public void PlayerRespawned_ShouldRoundTripCorrectly()
        {
            var original = new PlayerRespawned
            {
                OwnerId = 5,
                Position = new QuantizedVector3 { QuantizedX = 10, QuantizedY = 20, QuantizedZ = 30 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<PlayerRespawned>();
            var typedParsed = (PlayerRespawned)parsed!;
            typedParsed.OwnerId.Should().Be(5u);
            typedParsed.Position.QuantizedX.Should().Be((short)10);
        }

        [Fact]
        public void SpawnedAxeProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedAxeProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                StartPosition = new QuantizedVector3 { QuantizedZ = 5 },
                DesiredPosition = new QuantizedVector3 { QuantizedX = 100 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedAxeProjectile>();
            var typedParsed = (SpawnedAxeProjectile)parsed!;
            typedParsed.Id.Should().Be(1u);
            typedParsed.StartPosition.QuantizedZ.Should().Be((short)5);
        }

        [Fact]
        public void SpawnedBlackHoleProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedBlackHoleProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                StartPosition = new QuantizedVector3 { QuantizedZ = 5 },
                DesiredPosition = new QuantizedVector3 { QuantizedX = 100 },
                StartScaleSize = new QuantizedVector3 { QuantizedX = 2 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedBlackHoleProjectile>();
            var typedParsed = (SpawnedBlackHoleProjectile)parsed!;
            typedParsed.StartScaleSize.QuantizedX.Should().Be((short)2);
        }

        [Fact]
        public void SpawnedRocketProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedRocketProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                RocketPosition = new QuantizedVector3 { QuantizedX = 50 },
                RocketRotation = new QuantizedVector3 { QuantizedZ = 90 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedRocketProjectile>();
            var typedParsed = (SpawnedRocketProjectile)parsed!;
            typedParsed.RocketPosition.QuantizedX.Should().Be((short)50);
        }

        [Fact]
        public void SpawnedShotgunProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedShotgunProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                MuzzlePosition = new QuantizedVector3 { QuantizedX = 5 },
                MuzzleRotation = new QuantizedVector3 { QuantizedZ = 10 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedShotgunProjectile>();
            var typedParsed = (SpawnedShotgunProjectile)parsed!;
            typedParsed.MuzzlePosition.QuantizedX.Should().Be((short)5);
        }

        [Fact]
        public void SpawnedDexecutionerProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedDexecutionerProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                ProjectileDistance = 10f,
                ForwardOffset = 1f,
                UpOffset = 0.5f,
                AttackDir = new QuantizedVector3 { QuantizedX = 1 },
                Chance = 0.5f,
                UseAudio = true
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedDexecutionerProjectile>();
            var typedParsed = (SpawnedDexecutionerProjectile)parsed!;
            typedParsed.ProjectileDistance.Should().Be(10f);
            typedParsed.UseAudio.Should().BeTrue();
        }

        [Fact]
        public void SpawnedFireFieldProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedFireFieldProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                ExpirationTime = 5.5f
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedFireFieldProjectile>();
            var typedParsed = (SpawnedFireFieldProjectile)parsed!;
            typedParsed.ExpirationTime.Should().Be(5.5f);
        }

        [Fact]
        public void SpawnedCringeSwordProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedCringeSwordProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                MovingProjectilePosition = new QuantizedVector3 { QuantizedX = 20 },
                MovingProjectileRotation = new QuantizedVector4 { QuantizedX = 1, QuantizedW = 1 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedCringeSwordProjectile>();
            var typedParsed = (SpawnedCringeSwordProjectile)parsed!;
            typedParsed.MovingProjectilePosition.QuantizedX.Should().Be((short)20);
            typedParsed.MovingProjectileRotation.QuantizedW.Should().Be((short)1);
        }

        [Fact]
        public void SpawnedHeroSwordProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedHeroSwordProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                MovingProjectilePosition = new QuantizedVector3 { QuantizedX = 20 },
                MovingProjectileRotation = new QuantizedVector4 { QuantizedY = 1 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedHeroSwordProjectile>();
            var typedParsed = (SpawnedHeroSwordProjectile)parsed!;
            typedParsed.MovingProjectileRotation.QuantizedY.Should().Be((short)1);
        }

        [Fact]
        public void SpawnedRevolverProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedRevolverProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                MuzzlePosition = new QuantizedVector3 { QuantizedZ = 5 },
                MuzzleRotation = new QuantizedVector3 { QuantizedX = 2 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedRevolverProjectile>();
            var typedParsed = (SpawnedRevolverProjectile)parsed!;
            typedParsed.MuzzlePosition.QuantizedZ.Should().Be((short)5);
        }

        [Fact]
        public void SpawnedSniperProjectile_ShouldRoundTripCorrectly()
        {
            var original = new SpawnedSniperProjectile
            {
                Position = new QuantizedVector3 { QuantizedX = 10 },
                Id = 1,
                OwnerId = 5,
                Weapon = 3,
                Rotation = new QuantizedVector3 { QuantizedY = 1 },
                MuzzlePosition = new QuantizedVector3 { QuantizedX = 3 },
                MuzzleRotation = new QuantizedVector3 { QuantizedY = 4 }
            };
            var bytes = MemoryPackSerializer.Serialize<IGameNetworkMessage>(original);
            var parsed = MemoryPackSerializer.Deserialize<IGameNetworkMessage>(bytes);
            parsed.Should().BeOfType<SpawnedSniperProjectile>();
            var typedParsed = (SpawnedSniperProjectile)parsed!;
            typedParsed.MuzzleRotation.QuantizedY.Should().Be((short)4);
        }
    }
}