using System.Text;

namespace Stormpack.ToLanguageExtensions;

public static class PackResultLuaExtensions
{
    public static string ToLua(this PackResult result, string? roundFunc = null)
    {
        var output = new StringBuilder();
        output.Append(EmitPackFunction(result, roundFunc));
        output.AppendLine();
        output.Append(EmitUnpackFunction(result));
        return output.ToString();
    }

    private static StringBuilder EmitPackFunction(PackResult result, string? roundFunc)
    {
        var output = new StringBuilder();

        output.Append("function pk(v)");

        // Prescale all of the values
        output.Append(EmitPackScaling(result, roundFunc));

        // Pack into channels
        output.Append("return{");
        output.AppendJoin(",", result.Channels.Select(EmitPackChannel));
        output.Append("}end");

        return output;

        static StringBuilder EmitPackChannel(PackChannel channel)
        {
            var output = new StringBuilder();
            output.AppendJoin('+', channel.Fragments.Select(ToLuaExpr));
            return output;

            static StringBuilder ToLuaExpr(PackFragment frag)
            {
                var expr = new StringBuilder(ToLuaExprInner(frag));
                if (frag.Offset > 0)
                {
                    expr.Insert(0, '(');
                    expr.Append($"<<{frag.Offset})");
                }
                return expr;

                static string ToLuaExprInner(PackFragment frag)
                {
                    var str = $"v[{frag.Index + 1}]";

                    if (frag.ShiftRight > 0)
                        str = $"{str}>>{FormatNumber(frag.ShiftRight)}";

                    if (frag.Mask > 0)
                        str = $"{str}&{FormatNumber(frag.Mask)}";

                    if (frag.ShiftRight != 0 || frag.Mask > 0)
                        return $"({str})";
                    return str;
                }
            }
        }

        static StringBuilder EmitPackScaling(PackResult result, string? roundFunc)
        {
            var builder = new StringBuilder();

            // Only apply scale operations which actually do something
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            var scaling = result.Scaling.Where(a => a.Add != 0 || a.Multiply != 1).ToList();
            if (scaling.Count == 0)
                return builder;

            // If we haven't been given a round func define one now
            if (roundFunc == null)
            {
                builder.Append("v.r=function(x)return math.floor(x+0.5)end ");
                roundFunc = "v.r";
            }

            // Output scaling operations:
            // v[1] = round((v[1] + Add) * Mul)
            foreach (var (index, add, multiply) in scaling)
            {
                builder.Append($"v[{index + 1}]={roundFunc}((v[{index + 1}]");
                if (add != 0)
                {
                    if (add > 0)
                        builder.Append('+');
                    builder.Append($"{FormatNumber(add)}");
                }

                builder.Append(')');
                if (Math.Abs(multiply - 1) > double.Epsilon)
                    builder.Append($"*{FormatNumber(multiply)}");
                builder.Append(')');
            }

            return builder;
        }
    }

    private static StringBuilder EmitUnpackFunction(PackResult result)
    {
        var output = new StringBuilder();

        var channels = result.Channels.Select((_, i) => (char)(i + 97)).ToList();

        // Accept an argument per channel (a, b, c...)
        output.Append("function upk(");
        output.AppendJoin(",", channels);
        output.Append(')');

        // Aggregate fragments into whole numbers (grouping up fragments of the same number torn across two channels
        var numbers = from channel in result.Channels
                      from fragment in channel.Fragments
                      group (channel, fragment) by fragment.Index into nums
                      orderby nums.Key
                      let extract = ExtractNumber(channels, nums.ToList())
                      select UnpackScaling(result.Scaling[nums.Key], extract);

        output.Append("return{");
        output.AppendJoin(',', numbers);
        output.Append('}');

        output.Append("end");

        return output;

        static StringBuilder ExtractNumber(IReadOnlyList<char> channelNames, IReadOnlyList<(PackChannel channel, PackFragment fragment)> fragments)
        {
            var builder = new StringBuilder();

            if (fragments.Count == 1)
            {
                var (channel, fragment) = fragments[0];
                builder.Append(ExtractFragment(channelNames, channel, fragment));
            }
            else
            {
                var frags = from f in fragments
                            select ShiftFragment(ExtractFragment(channelNames, f.channel, f.fragment), f.fragment);
                builder.AppendJoin("|", frags);
            }

            return builder;

            static StringBuilder ExtractFragment(IReadOnlyList<char> channelNames, PackChannel channel, PackFragment fragment)
            {
                var builder = new StringBuilder();

                if (fragment.Offset == 0)
                    builder.Append(channelNames[channel.Index]);
                else
                {
                    builder.Append(channelNames[channel.Index]);
                    if (fragment.Offset > 0)
                    {
                        builder.Append(">>");
                        builder.Append(fragment.Offset);
                    }
                }

                builder.Append('&');
                builder.Append(FormatNumber(Mask(fragment.BitCount)));

                return builder;
            }

            static StringBuilder ShiftFragment(StringBuilder builder, PackFragment fragment)
            {
                builder.Insert(0, '(');
                builder.Append(')');

                if (fragment.ShiftRight != 0)
                {
                    builder.Insert(0, '(');
                    builder.Append($"<<{FormatNumber(fragment.ShiftRight)}");
                    builder.Append(')');
                }

                return builder;
            }
        }

        static StringBuilder UnpackScaling(PackScaling scale, StringBuilder inner)
        {
            if (scale.Add == 0 && Math.Abs(scale.Multiply - 1) < double.Epsilon)
                return inner;

            // Wrap inner expr in brackets
            inner.Insert(0, "(");
            inner.Append(")");

            // Rescale
            if (Math.Abs(scale.Multiply - 1) > double.Epsilon)
            {
                inner.Insert(0, "(");
                inner.Append($"/{FormatNumber(scale.Multiply)})");
            }

            // Offset
            if (scale.Add != 0)
            {
                var add = -scale.Add;

                if (add > 0)
                    inner.Append('+');
                inner.Append($"{FormatNumber(add)}");
            }

            return inner;
        }
    }

    private static int Mask(int bits)
    {
        return (1 << bits) - 1;
    }

    private static string FormatNumber(double n)
    {
        // Special case for integers
        if (n - (int)n == 0)
            return FormatNumber((long)n);

        return n.ToString("0.####");
    }

    private static string FormatNumber(long n)
    {
        var options = new List<string> {
            n.ToString(),
            $"0x{Convert.ToString(n, 16)}"
        };

        // Special formatting for pow2
        var log2 = Math.Log2(n);
        if (log2 % 1 == 0)
            options.Add($"2^{log2}");

        return options.MinBy(a => a.Length)!;
    }
}