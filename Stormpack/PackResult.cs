namespace Stormpack;

public class PackResult
{
    public IReadOnlyList<PackScaling> Scaling { get; }
    public IReadOnlyList<PackChannel> Channels { get; }

    public int SpareBits => Channels.Select(a => a.SpareBits).Sum();

    public PackResult(IReadOnlyList<PackScaling> scaling, IReadOnlyList<PackChannel> channels)
    {
        Scaling = scaling;
        Channels = channels;
    }
}

public class PackChannel
{
    public const int ChannelBits = 64;

    public int Index { get; }
    public IReadOnlyList<PackFragment> Fragments { get; }

    public int SpareBits => ChannelBits - Fragments.Select(a => a.BitCount).Sum();

    public PackChannel(int index, IReadOnlyList<PackFragment> fragments)
    {
        Index = index;
        Fragments = fragments;
    }
}

/// <summary>
/// Fragment of a number to pack
/// Fragment = (Value >> ShiftRight) &amp; Mask
/// </summary>
/// <param name="Index">Index of this number</param>
/// <param name="Mask">Mask to extract bits needed for this fragment</param>
/// <param name="ShiftRight">Amount to shift right before masking</param>
/// <param name="BitCount">Mumber of bits in this fragment</param>
/// <param name="Offset">Offset into channel to store this fragment</param>
public record PackFragment(int Index, long Mask, long ShiftRight, int BitCount, int Offset);

/// <summary>
/// Scaling to apply to numbers before packing
/// Value = round((Value + add) * multiply)
/// </summary>
/// <param name="Index">Index of the number to preprocess</param>
/// <param name="Add">Value to add</param>
/// <param name="Multiply">Value to multiply</param>
public record PackScaling(int Index, double Add, double Multiply);