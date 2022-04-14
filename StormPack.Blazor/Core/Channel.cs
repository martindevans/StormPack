using Stormpack;

namespace StormPack.Blazor.Core
{
    public class Channel
    {
        /// <summary>
        /// Get the indices of the number for each bit
        /// </summary>
        public IReadOnlyList<int> Bits { get; }

        public Channel(PackChannel channel)
        {
            var bits = new int[64];
            for (var i = 0; i < bits.Length; i++)
                bits[i] = -1;

            var bit = 63;
            foreach (var fragment in channel.Fragments)
                for (var i = 0; i < fragment.BitCount; i++)
                    bits[bit--] = fragment.Index;

            Bits = bits;
        }
    }
}
