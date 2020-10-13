using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Text.Json;

namespace SideCarCLI
{
    class SideCarData : IValidatableObject
    {
        
        private ConcurrentDictionary<string,Process> allProcesses;
        private readonly Interceptors interceptors;
        const int RunAppTillTheEnd = -1;
        private List<ValidationResult> validations;
        private string WorkingDirectory;

        public SideCarData(Interceptors interceptors)
        {
            
            this.interceptors = interceptors;
            MaxSeconds = RunAppTillTheEnd;
            validations = new List<ValidationResult>();
            allProcesses = new ConcurrentDictionary<string, Process>();
        }
       

        public int MaxSeconds { get; internal set; }
        public string FullAppPath { get; internal set; }
        public string Arguments { get; internal set; }
        public bool ExistRunningProcess { 
            get
            {
                return allProcesses.Where(it => !it.Value.HasExited).Any();
            }
        }

        internal void ParseSeconds(CommandOption cmd)
        {
            
            if (cmd.HasValue())
            {
                string valSeconds = cmd.Value();
                if (!int.TryParse(valSeconds,out int result))
                {
                    MaxSeconds = result;
                }
                else
                {
                    this.MaxSeconds = RunAppTillTheEnd;
                    string[] names = new[] { nameof(cmd.ShortName) };
                    
                    validations.Add(new ValidationResult($"cannot parse {valSeconds}", names));
                }
            }
        }
        
        public int ExecuteApp()
        {
            var validations = Validate(null).ToArray();
            if (validations.Length > 0)
            {
                foreach (var item in validations)
                {
                    Console.Error.WriteLine($"{item.MemberNames?.FirstOrDefault()} ->  {item.ErrorMessage} ");
                }
                return -1;
            }
            var pi = new ProcessStartInfo(this.FullAppPath);
            pi.Arguments = this.Arguments;
            pi.WorkingDirectory = this.WorkingDirectory;
            pi.UseShellExecute = false;
            pi.CreateNoWindow = false;
            pi.RedirectStandardError = true;
            pi.RedirectStandardOutput = true;
            Process p = new Process()
            {
                StartInfo = pi
            };

          
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;
            
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
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
            return exitCode;
            //TODO: finish  with p.ExitCode

            //TODO: backgroud job with timer interceptor

            //TODO: wait maxSeconds if != RunAppTillTheEnd 

        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            P_OutputDataReceived(sender, e);
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
                    var local = item;
                    var pi = new ProcessStartInfo(item.FullPath);
                    pi.Arguments = e.Data;
                    string wd = item.FolderToExecute;
                    if (string.IsNullOrWhiteSpace(wd))
                    {
                        wd = Path.GetDirectoryName(Path.GetFullPath(item.Name));
                    }
                    string arguments = item.Arguments;
                    if (string.IsNullOrWhiteSpace(arguments))
                    {
                        arguments = "{line}";
                    }
                    pi.Arguments = arguments.Replace("{line}",e.Data);
                    pi.RedirectStandardError = item.InterceptOutput;
                    pi.RedirectStandardOutput = item.InterceptOutput;
                   
                    pi.WorkingDirectory = wd;

                    pi.UseShellExecute = !item.InterceptOutput;
                    pi.CreateNoWindow = item.InterceptOutput;

                    var p = new Process()
                    {
                        StartInfo = pi
                    };
                    p.EnableRaisingEvents = item.InterceptOutput;
                    allProcesses.TryAdd(local.Name, p);
                    p.Start();
                    if (item.InterceptOutput)
                    {
                        p.BeginOutputReadLine();
                        p.BeginErrorReadLine();
                        p.OutputDataReceived += (sender,args)=>
                        {
                            Console.WriteLine($"Interceptor : {local.Name} => {args.Data}");
                        };
                        p.ErrorDataReceived+= (sender, args) =>
                        {
                            Console.WriteLine($"Interceptor : {local.Name} ERROR => {args.Data}");
                        };
                        p.Exited += (sender, args) =>
                          {
                              var process = this.allProcesses[local.Name];                              
                              Console.WriteLine($"Interceptor : {local.Name} FINISH");
                          };
                        

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("error:!!!" + ex.Message);
                }
            }
        }

        

        Func<Interceptor, DataReceivedEventHandler> a = (item) =>
        {
            return delegate (object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine("!!!!");
                Console.WriteLine($"[{item.Name}] {e.Data}");
            };
        };
        

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
