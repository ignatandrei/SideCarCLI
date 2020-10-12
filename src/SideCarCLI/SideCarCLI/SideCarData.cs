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
        private string WorkingDirectory;

        public SideCarData(Interceptors interceptors)
        {
            
            this.interceptors = interceptors;
            MaxSeconds = RunAppTillTheEnd;
            validations = new List<ValidationResult>();
        }
       

        public int MaxSeconds { get; internal set; }
        public string FullAppPath { get; internal set; }
        public string Arguments { get; internal set; }
        internal void ParseSeconds(CommandOption cmd)
        {
            MaxSeconds = 0;
            if (cmd.HasValue())
            {
                string valSeconds = cmd.Value();
                if (!int.TryParse(valSeconds,out int result))
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
            var pi = new ProcessStartInfo(this.FullAppPath);
            pi.Arguments = this.Arguments;
            pi.WorkingDirectory = this.WorkingDirectory;
            pi.UseShellExecute = true;
            pi.CreateNoWindow = true;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true;
            
            var p = Process.Start(pi);
            p.OutputDataReceived += P_OutputDataReceived;
            var res=p.WaitForExit(this.MaxSeconds);
            var exitCode = 0;
            if (res)
            {
                exitCode = p.ExitCode;                
            }
            else
            {
                exitCode = int.MinValue;
            }

            //TODO: finish  with p.ExitCode

            //TODO: backgroud job with timer interceptor

            //TODO: wait maxSeconds if != RunAppTillTheEnd 

        }

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            //TODO: line interceptors to 
            //e.Data
            if (!(this.interceptors?.LineInterceptors?.Length > 0))
                return;

            foreach(var item in this.interceptors.LineInterceptors)
            {
                try
                {
                    var pi = new ProcessStartInfo(item.FullPath);
                    pi.Arguments = e.Data;
                    string wd = item.FolderToExecute; ;
                    if (string.IsNullOrWhiteSpace(wd)) {
                        wd = Path.GetDirectoryName(Path.GetFullPath(item.FullPath));
                    }
                    
                    pi.WorkingDirectory = wd;
                    pi.UseShellExecute = true;
                    pi.CreateNoWindow = false;                    
                    var p = Process.Start(pi);                    

                }
                catch (Exception ex)
                {

                }
            }
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

        internal void ParseArguments(CommandOption argExe)
        {
            if (!argExe.HasValue())
                return;

            this.Arguments = string.Join(" ", argExe.Values);
            
        }
        internal void ParseWorkingDirectory(CommandOption wd)
        {
            if (!wd.HasValue())
            {
                this.WorkingDirectory = Environment.CurrentDirectory;
            }
            else
            {
                this.WorkingDirectory = wd.Value();
            }

        }
    }
}
