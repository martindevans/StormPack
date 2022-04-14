using System.Collections;
using Stormpack;
using Stormpack.ToLanguageExtensions;

namespace StormPack.Blazor.Core
{
    public class StateManager
    {
        private PackSpec _spec;
        private PackResult _result;

        public IEnumerable<Channel> Channels
        {
            get
            {
                foreach (var channel in _result.Channels)
                    yield return new Channel(channel);
            }
        }

        public string Lua
        {
            get;
            private set;
        }

        public IEnumerable<PackSpec.Number> Numbers
        {
            get
            {
                foreach (var number in _spec.Numbers)
                    yield return number;
            }
        }

        public string? Error { get; set; }

        public event Action? OnStateChange;

        public StateManager()
        {
            _spec = new PackSpec();
            _result = _spec.Generate();
            Lua = "";

            // todo: temp set a spec
            SetSpec(new PackSpec
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
                new PackSpec.Number(min: 0,     max: 1, precision: 0.0000000004),
            });
        }

        public void SetSpec(PackSpec spec)
        {
            _spec = spec;
            NotifyChanged();
        }

        public void NotifyChanged()
        {
            Error = null;
            try
            {
                _result = _spec.Generate();
                Lua = _result.ToLua();
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }

            OnStateChange?.Invoke();
        }
    }
}
