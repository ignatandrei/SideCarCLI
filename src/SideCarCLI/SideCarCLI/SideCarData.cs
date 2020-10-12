using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;

namespace SideCarCLI
{
    class SideCarData
    {
        private Interceptors interceptors;
        public SideCarData()
        {
            string fileInterceptors = Path.Combine("cmdInterceptors", "interceptors.json");
            interceptors = JsonSerializer.Deserialize<Interceptors>(File.ReadAllText(fileInterceptors));

            parsingErrors = new List<KeyValuePair<string, string>>();
        }
        List<KeyValuePair<string, string>> parsingErrors;
        public long MaxSeconds { get; set; }

        internal void ParseSeconds(CommandOption cmd)
        {
            MaxSeconds = 0;
            if (cmd.HasValue())
            {
                string valSeconds = cmd.Value();
                if (!long.TryParse(valSeconds,out long result))
                {
                    MaxSeconds = result;
                }
                else
                {
                    Console.Error.WriteLine($"cannot parse {valSeconds}");
                    parsingErrors.Add(new KeyValuePair<string, string>( nameof(MaxSeconds), $"cannot parse {valSeconds}"));
                }
            }
        }
    }
}
