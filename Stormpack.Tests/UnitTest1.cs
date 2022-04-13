using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stormpack.ToLanguageExtensions;

using static System.Math;

namespace Stormpack.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var spec = new PackSpec
        {
            // 8 bytes, no tearing
            new PackSpec.Number(min: 0,     max: 256,   precision: 1),
            new PackSpec.Number(min: 0,     max: 256,   precision: 1),
            new PackSpec.Number(min: 0,     max: 65536, precision: 1),
            new PackSpec.Number(min: 0,     max: 65536, precision: 1),
            new PackSpec.Number(min: 0,     max: 65536, precision: 1),

            // 10 bytes, final value torn over 2 channels
            new PackSpec.Number(min: 0,     max: 65536,      precision: 1),
            new PackSpec.Number(min: 0,     max: 65536,      precision: 1),
            new PackSpec.Number(min: 0,     max: 65536,      precision: 1),
            new PackSpec.Number(min: 0,     max: 2147483648, precision: 0.5),

            // Ludicrous precision (pad out the rest of this channel)
            new PackSpec.Number(min: 0,     max: 1, precision: 0.000000000000004),
        };

        var result = spec.Generate();

        foreach (var channel in result.Channels)
        {
            Console.WriteLine($"## Channel {channel.Index}");
            foreach (var fragment in channel.Fragments)
                Console.WriteLine($"[n:{fragment.Index}] (bits:{fragment.BitCount}, Shift:{fragment.ShiftRight}) ");
            Console.WriteLine();
        }

        Console.WriteLine($"Channels: {result.Channels.Count}");
        Console.WriteLine($"Unused Bits: {result.SpareBits}");
        Console.WriteLine();
        Console.WriteLine(result.ToLua());
    }
}