using System;
using System.Numerics;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Stormpack.ToLanguageExtensions;

namespace Stormpack.Tests;

[TestClass]
public class UnitTest1
{
    [TestMethod]
    public void TestMethod1()
    {
        var spec = new PackSpec(
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
            new PackSpec.Number(min: 0,     max: 1, precision: 0.000000000000004)
        );

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

    [TestMethod]
    public void TestMethod2()
    {
        var spec = new PackSpec(
            // 8 bytes, no tearing
            new PackSpec.Number(min: 0, max: 256, precision: 1),
            new PackSpec.Number(min: 0, max: 256, precision: 1),
            new PackSpec.Number(min: 0, max: 65536, precision: 1),
            new PackSpec.Number(min: 0, max: 65536, precision: 1),
            new PackSpec.Number(min: 0, max: 65536, precision: 1),

            // 10 bytes, final value torn over 2 channels
            new PackSpec.Number(min: 0, max: 65536, precision: 1),
            new PackSpec.Number(min: 0, max: 65536, precision: 1),
            new PackSpec.Number(min: 0, max: 65536, precision: 1),
            new PackSpec.Number(min: 0, max: 2147483648, precision: 0.5),

            // Ludicrous precision (pad out the rest of this channel)
            new PackSpec.Number(min: 0, max: 1, precision: 0.000000000000004)
        );

        var json = JsonSerializer.Serialize(spec);

        var spec2 = JsonSerializer.Deserialize<PackSpec>(json);
    }

    [TestMethod]
    public void MatricesYPR()
    {
        var m = Matrix4x4.CreateFromYawPitchRoll(Rads(45), 0, 0);
        var p = new Vector3(100, 0, 0);
        var r = Vector3.Transform(p, m);

        Console.WriteLine(m);
        Console.WriteLine(r);
    }

    [TestMethod]
    public void MatricesMul()
    {
        var a = new Matrix4x4(
            1, 2, 3, 0,
            1, 2, 3, 0,
            1, 2, 3, 0,
            0, 0, 0, 0
        );
        var b = new Matrix4x4(
            1, 0, 1, 0,
            0, 2, 0, 2,
            1, 0, 3, 0,
            0, 2, 0, 4
        );

        var c = a * b;

        Console.WriteLine(c);
    }

    [TestMethod]
    public void MatricesTranslate()
    {
        var a = new Matrix4x4(
            1, 2, 3, 0,
            4, 5, 6, 0,
            7, 8, 9, 0,
            0, 0, 0, 1
        );

        a.Translation += new Vector3(11, 12, 13);

        Console.WriteLine(a);
    }

    [TestMethod]
    public void MatricesInvert()
    {
        var a = Matrix4x4.CreateFromYawPitchRoll(Rads(45), 0, 0);
        var c = Matrix4x4.Invert(a, out var b);

        Console.WriteLine(c);
        Console.WriteLine(b);
    }

    public static float Rads(float angle)
    {
        return (float)((Math.PI / 180) * angle);
    }
}