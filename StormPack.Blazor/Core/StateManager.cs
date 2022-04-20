using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Stormpack;
using Stormpack.ToLanguageExtensions;

namespace StormPack.Blazor.Core
{
    public class StateManager
    {
        private static readonly PackSpec Default = new(
            new PackSpec.Number(min: 0,     max: 256,       precision: 1,   name: "Alpha"),
            new PackSpec.Number(min: 0,     max: 256,       precision: 0.5, name: "Beta"),
            new PackSpec.Number(min: 0,     max: 32768,     precision: 1,   name: "Gamma"),
            new PackSpec.Number(min: 0,     max: 474836483, precision: 0.1, name: "Delta")
        );

        private readonly NavigationManager _navManager;

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

        public StateManager(NavigationManager navManager)
        {
            _navManager = navManager;

            _spec = new PackSpec();
            _result = _spec.Generate();
            Lua = "";
            SetSpec(Default);
        }

        public void SetSpec(PackSpec spec)
        {
            _spec = spec;
            NotifyChanged();
        }

        public void AddNumber()
        {
            _spec.Add(new PackSpec.Number(0, 256, 1));
            NotifyChanged();
        }

        public void Remove(PackSpec.Number number)
        {
            _spec.Remove(number);
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

            UpdateUrl();
            OnStateChange?.Invoke();
        }

        private void UpdateUrl()
        {
            var serialised = Serialize();
            var uri = _navManager.GetUriWithQueryParameter("state", string.IsNullOrEmpty(serialised) ? null : serialised);
            _navManager.NavigateTo(uri);
        }

        private static PackSpec Deserialize(string urlEncoded)
        {
            if (string.IsNullOrWhiteSpace(urlEncoded))
                return Default;

            var compressed = Convert.FromBase64String(WebUtility.UrlDecode(urlEncoded));
            var bytes = Decompress(compressed);
            var json = Encoding.UTF8.GetString(bytes);

            return JsonSerializer.Deserialize<PackSpec>(json) ?? Default;

            static byte[] Decompress(byte[] data)
            {
                var input = new MemoryStream(data);
                var output = new MemoryStream();
                using (var dstream = new DeflateStream(input, CompressionMode.Decompress))
                    dstream.CopyTo(output);
                return output.ToArray();
            }
        }

        private string Serialize()
        {
            var utf8 = JsonSerializer.SerializeToUtf8Bytes(_spec);
            var compressed = Compress(utf8);
            return WebUtility.UrlEncode(Convert.ToBase64String(compressed));

            static byte[] Compress(byte[] data)
            {
                var output = new MemoryStream();
                using (var dstream = new DeflateStream(output, CompressionLevel.Optimal))
                    dstream.Write(data, 0, data.Length);
                return output.ToArray();
            }
        }

        public void Load(string? stateString)
        {
            if (string.IsNullOrWhiteSpace(stateString))
                return;
            SetSpec(Deserialize(stateString));
        }
    }
}
