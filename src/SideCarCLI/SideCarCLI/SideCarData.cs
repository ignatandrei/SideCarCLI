﻿using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;

namespace SideCarCLI
{
    class SideCarData : IValidatableObject
    {
        
        private readonly Interceptors interceptors;
        const int RunAppTillTheEnd = -1;
        private List<ValidationResult> validations;
        public SideCarData(Interceptors interceptors)
        {
            
            this.interceptors = interceptors;
            MaxSeconds = RunAppTillTheEnd;
            validations = new List<ValidationResult>();
        }
       

        public long MaxSeconds { get; internal set; }
        public string FullAppPath { get; internal set; }
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
                    string[] names = new[] { nameof(cmd.ShortName) };
                    
                    validations.Add(new ValidationResult($"cannot parse {valSeconds}", names));
                }
            }
        }
        
        public void ExecuteApp()
        {
            var validations = Validate(null).ToArray();
            if (validations.Length > 0)
            {
                foreach (var item in validations)
                {
                    Console.Error.WriteLine($"{item.MemberNames?.FirstOrDefault()} ->  {item.ErrorMessage} ");
                }
                return;
            }
            Process.Start(FullAppPath);
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            return validations;
        }

        internal void ParseCommandName(CommandOption nameExe)
        {
            if (!nameExe.HasValue())
            {
                string[] names = new[] { nameof(nameExe.ShortName) };

                validations.Add(new ValidationResult($"cannot find name exe", names));
                return;
            }
            this.FullAppPath = nameExe.Value();
            //TODO : verify that path exists 
            //or the command can execute ( it is in %PATH%")

        }
    }
}
