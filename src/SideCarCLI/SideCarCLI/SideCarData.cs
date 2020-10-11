using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace SideCarCLI
{
    class SideCarData
    {
        public SideCarData()
        {
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
