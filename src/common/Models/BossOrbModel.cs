using MemoryPack;

namespace MegabonkTogether.Common.Models
{
    [MemoryPackable]
    public partial class BossOrbModel
    {
        public uint Id { get; set; }

        public QuantizedVector3 Position = new();

        public void UpdateFrom(BossOrbModel other)
        {
            Id = other.Id;
            Position = other.Position;
        }
    }
}
