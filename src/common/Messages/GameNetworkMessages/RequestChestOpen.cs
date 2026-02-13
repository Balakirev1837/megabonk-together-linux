using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class RequestChestOpen : IGameNetworkMessage
    {
        public uint ChestId { get; set; }
        public uint RequestingPlayerId { get; set; }
    }
}
