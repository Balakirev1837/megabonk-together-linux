using MegabonkTogether.Common.Models;
using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class PlayerUpdate : IGameNetworkMessage
    {
        public uint ConnectionId { get; set; }
        public QuantizedVector3 Position { get; set; } = new();
        public MovementState MovementState { get; set; } = new();

        public AnimatorState AnimatorState { get; set; } = new();

        public InventoryInfo Inventory { get; set; } = new();

        public string Name { get; set; } = "";
        public uint Hp { get; set; }
        public uint MaxHp { get; set; }
        //public uint Xp { get; set; }
        public uint Shield { get; set; }
        public uint MaxShield { get; set; }
    }

    [MemoryPackable]
    public partial class AnimatorState
    {
        public const byte IsGroundedBit = 1 << 0;
        public const byte IsMovingBit = 1 << 1;
        public const byte IsIdleBit = 1 << 2;
        public const byte IsGrindingBit = 1 << 3;
        public const byte IsJumpingBit = 1 << 4;

        public byte State { get; set; }

        [MemoryPackIgnore]
        public bool IsGrounded
        {
            get => (State & IsGroundedBit) != 0;
            set => State = (byte)(value ? (State | IsGroundedBit) : (State & ~IsGroundedBit));
        }

        [MemoryPackIgnore]
        public bool IsMoving
        {
            get => (State & IsMovingBit) != 0;
            set => State = (byte)(value ? (State | IsMovingBit) : (State & ~IsMovingBit));
        }

        [MemoryPackIgnore]
        public bool IsIdle
        {
            get => (State & IsIdleBit) != 0;
            set => State = (byte)(value ? (State | IsIdleBit) : (State & ~IsIdleBit));
        }

        [MemoryPackIgnore]
        public bool IsGrinding
        {
            get => (State & IsGrindingBit) != 0;
            set => State = (byte)(value ? (State | IsGrindingBit) : (State & ~IsGrindingBit));
        }

        [MemoryPackIgnore]
        public bool IsJumping
        {
            get => (State & IsJumpingBit) != 0;
            set => State = (byte)(value ? (State | IsJumpingBit) : (State & ~IsJumpingBit));
        }
    }

    [MemoryPackable]
    public partial class MovementState
    {
        public QuantizedVector2 AxisInput { get; set; } = new();
        public QuantizedVector3 CameraForward { get; set; } = new();
        public QuantizedVector3 CameraRight { get; set; } = new();
    }
}
