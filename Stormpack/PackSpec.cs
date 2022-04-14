﻿using System.Collections;
using System.Diagnostics;

namespace Stormpack;

public class PackSpec
    : IEnumerable<PackSpec.Number>
{
    private readonly List<Number> _numbers = new();
    public IEnumerable<Number> Numbers => _numbers;

    public void Add(Number number) => _numbers.Add(number);

    public PackResult Generate()
    {
        foreach (var number in Numbers)
        {
            if (number.Max < number.Min)
                throw new InvalidOperationException("Number `Max` must be > Number `Min`");
        }

        // Calculate scale factors for numbers
        var scaling = new List<PackScaling>();
        for (var i = 0; i < _numbers.Count; i++)
        {
            var n = _numbers[i];

            // Extend precision to exactly occupy however many bits this number is already using
            var range = n.Max - n.Min;
            var bits = Math.Log2(range / n.Precision);
            var intBits = Math.Ceiling(bits);
            var precision = Math.Pow(2, -intBits) * range;

            // Sanity check extra precision
            var bits2 = Math.Log2(range / precision);
            Debug.Assert(bits2 - (int)bits2 == 0);
            Debug.Assert((int)bits2 == (int)Math.Ceiling(bits));

            scaling.Add(new PackScaling(i, -n.Min, 1 / precision));
        }

        // Calculate number fragments packed into channels (each 64 bits)
        var channels = new List<PackChannel>();
        var fragmentsBitsSpare = PackChannel.ChannelBits;
        var fragments = new List<PackFragment>();
        for (var i = 0; i < _numbers.Count; i++)
        {
            var number = _numbers[i];

            if (fragments == null)
            {
                fragments = new List<PackFragment>();
                fragmentsBitsSpare = PackChannel.ChannelBits;
            }

            // Add this number to the fragments list
            if (number.Bits > fragmentsBitsSpare)
            {
                // Add the first part of the number
                var firstBits = fragmentsBitsSpare;
                fragments.Add(new PackFragment(i, Mask(fragmentsBitsSpare), 0, firstBits, PackChannel.ChannelBits - fragmentsBitsSpare));

                // Output this channel and start a new one
                channels.Add(new PackChannel(channels.Count, fragments));
                fragments = new List<PackFragment>();
                fragmentsBitsSpare = PackChannel.ChannelBits - (number.Bits - firstBits);

                // Output the rest of the number
                fragments.Add(new PackFragment(i, Mask(number.Bits - firstBits), firstBits, number.Bits - firstBits, PackChannel.ChannelBits - fragmentsBitsSpare));
            }
            else
            {
                fragments.Add(new PackFragment(i, 0, 0, number.Bits, PackChannel.ChannelBits - fragmentsBitsSpare));
                fragmentsBitsSpare -= number.Bits;
            }

            // Once the channel is out of bits output it
            if (fragmentsBitsSpare == 0)
            {
                channels.Add(new PackChannel(channels.Count, fragments));
                fragments = null;
            }
        }

        if (fragments != null)
            channels.Add(new PackChannel(channels.Count, fragments));

        return new PackResult(scaling, channels);
    }

    private static int Mask(int bits)
    {
        return (1 << bits) - 1;
    }


    public IEnumerator<Number> GetEnumerator() => _numbers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public class Number
    {
        public double Min { get; set; }
        public double Max { get; set; }
        public double Precision { get; set; }

        public int Bits => (int)Math.Ceiling(Math.Log2((Max - Min) / Precision));

        public Number(double min, double max, double precision)
        {
            Min = min;
            Max = max;
            Precision = precision;
        }

        public override string ToString()
        {
            return $"[{Min},{Max}]@{Precision}";
        }
    };
}