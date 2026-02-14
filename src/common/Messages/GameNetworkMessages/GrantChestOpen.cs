using MemoryPack;

namespace MegabonkTogether.Common.Messages
{
    [MemoryPackable]
    public partial class GrantChestOpen : IGameNetworkMessage
    {
        public uint ChestId { get; set; }
        public uint GrantedPlayerId { get; set; }
    }
}
