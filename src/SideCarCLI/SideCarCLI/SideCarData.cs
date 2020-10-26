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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SideCarCLI
{
    class SideCarData : IValidatableObject
    {
        
        private ConcurrentDictionary<string,Process> allProcesses;
        private ConcurrentDictionary<string, Process> timerProcesses;
        private Dictionary<string, Timer> timers;
        private readonly Interceptors allInterceptors;
        private readonly Interceptors runningInterceptors;
        const int RunAppTillTheEnd = -1;
        private List<ValidationResult> validations;
        private string WorkingDirectory;
        private bool waitForTimersToFinish;


        public SideCarData()
        {
            this.argsRegex = new Dictionary<string, string>();
            string fileInterceptors = Path.Combine("cmdInterceptors", "interceptors.json");
            allInterceptors = JsonSerializer.Deserialize<Interceptors>(File.ReadAllText(fileInterceptors));
            runningInterceptors = new Interceptors();
            MaxSeconds = RunAppTillTheEnd;
            validations = new List<ValidationResult>();
            allProcesses = new ConcurrentDictionary<string, Process>();
            timerProcesses = new ConcurrentDictionary<string, Process>();
            timers = new Dictionary<string, Timer>();
        }
       

        public int MaxSeconds { get; internal set; }
        public string FullAppPath { get; internal set; }
        public string Arguments { get; internal set; }
        public bool ExistRunningProcess { 
            get
            {
                var exists= allProcesses.Where(it => !it.Value.HasExited).Any();
                if (exists)
                    return true;

                if(waitForTimersToFinish)
                    return timerProcesses.Where(it => !it.Value.HasExited).Any();

                return false;
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

            if (!string.IsNullOrWhiteSpace(regEx))
            {
                var regex = new Regex(this.regEx);
                var names = regex.
                    GetGroupNames().
                    Where(it => !int.TryParse(it, out var _)).
                    ToArray();

                var matches = regex.Matches(this.Arguments);
                if (matches.Count == 0)
                    throw new ArgumentException($" the regex {regEx} has no matches for {Arguments}");

                var m = matches.FirstOrDefault();
                foreach(var g in names)
                {
                    if (m.Groups[g].Success)
                    {
                        argsRegex[g]=m.Groups[g].Value;
                    }
                    else
                    {
                        throw new ArgumentException($"cannot find {g}  in regex {regEx} when parsing {Arguments}");
                    }
                }

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
            
            ShowSummary(pi, this);
            if (justTest)
                return 0;
            
            p.OutputDataReceived += P_OutputDataReceived;
            p.ErrorDataReceived += P_ErrorDataReceived;
            
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            RunTimerProcesses();
            var res=p.WaitForExit(this.MaxSeconds);
            var exitCode = 0;
            foreach(var item in this.timers)
            {
                item.Value.Dispose();
            }
            if (res)
            {
                exitCode = p.ExitCode;
                RunFinishInterceptors(exitCode);
            }
            else
            {
                Console.WriteLine($"timeout {MaxSeconds} elapsed");
                exitCode = int.MinValue;
            }

            if (!waitForTimersToFinish)
            {
                foreach(var item in this.timerProcesses)
                {
                    try
                    {
                        item.Value.Kill(true);
                    }
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine($"cannot kill {item.Value}");
                    }
                }
            }

            return exitCode;
            

            //TODO: backgroud job with timer interceptor

           

        }

        internal void SetTest(bool justTest)
        {
            this.justTest = justTest;
        }

        internal void SetRegex(string regEx)
        {
            this.regEx = regEx;
        }

        internal void ParseWaitForTimersToFinish(CommandOption wait)
        {
            this.waitForTimersToFinish = false;
            if (wait.HasValue())
            {
                if (wait.Value() != "0")
                    waitForTimersToFinish = true;
            }
        }

        private void ExecuteTimerProcess(object state)
        {
            var local = state as TimerInterceptor;
            var dataToBeParsed = new Dictionary<string, string>();

            if (this.argsRegex.Count > 0)
            {
                foreach (var reg in argsRegex)
                {
                    dataToBeParsed.Add("{" + reg.Key + "}", reg.Value);
                }
            }
            
            string name = local.Name + DateTime.Now.ToString("o");
            timerProcesses.TryAdd(name, local.RunTimerInterceptor(local.Name, dataToBeParsed));

        }
        private int RunTimerProcesses()
        {
            if (!(runningInterceptors?.TimerInterceptors?.Length > 0))
                return 0;
            
            foreach (var item in runningInterceptors.TimerInterceptors)
            {
                var local = item;
                var t = new Timer( ExecuteTimerProcess,local,0,item.intervalRepeatSeconds * 1000);
                timers.Add(local.Name, t);
            }
            

            return timers.Count;
        }

        private void T_Elapsed()
        {
            throw new NotImplementedException();
        }

        internal void ParseInterceptors(CommandOption lineInterceptorsNames, CommandOption timerInterceptorsNames, CommandOption finishInterceptorsNames)
        {
            if (lineInterceptorsNames.HasValue())
            {
                var names = lineInterceptorsNames.Values.Where(it => !string.IsNullOrWhiteSpace(it)).ToArray();
                var inter = this.allInterceptors.LineInterceptors?.Where(it => names.Contains(it.Name)).ToArray();
                this.runningInterceptors.LineInterceptors = inter;
                if(names.Length > inter?.Length)
                {
                    var diff = names.Except(inter.Select(it => it.Name)).ToArray();
                    string[] namesMember = new[] { lineInterceptorsNames.ShortName, lineInterceptorsNames.LongName };
                    foreach (var item in diff)
                    {

                        validations.Add(new ValidationResult($"cannot find line interceptor {item}", namesMember));
                    }
                }
            }
            if (timerInterceptorsNames.HasValue())
            {
                var names = timerInterceptorsNames.Values.Where(it => !string.IsNullOrWhiteSpace(it)).ToArray();
                var inter = this.allInterceptors.TimerInterceptors?.Where(it => names.Contains(it.Name)).ToArray();
                this.runningInterceptors.TimerInterceptors= inter;
                if (names.Length > inter?.Length)
                {
                    var diff = names.Except(inter?.Select(it => it.Name)).ToArray();
                    string[] namesMember = new[] { timerInterceptorsNames.ShortName, timerInterceptorsNames.LongName };
                    foreach (var item in diff)
                    {

                        validations.Add(new ValidationResult($"cannot find timer interceptor {item}", namesMember));
                    }
                }
            }
            if (finishInterceptorsNames.HasValue())
            {
                var names = finishInterceptorsNames.Values.Where(it => !string.IsNullOrWhiteSpace(it)).ToArray();
                var inter = this.allInterceptors.FinishInterceptors?.Where(it => names.Contains(it.Name)).ToArray();
                this.runningInterceptors.FinishInterceptors = inter;
                if (names.Length > inter?.Length)
                {
                    var diff = names.Except(inter?.Select(it => it.Name)).ToArray();
                    string[] namesMember = new[] { finishInterceptorsNames.ShortName, finishInterceptorsNames.LongName };
                    foreach (var item in diff)
                    {

                        validations.Add(new ValidationResult($"cannot find timer interceptor {item}", namesMember));
                    }
                }
            }
        }

        private void ShowSummary(ProcessStartInfo pi, SideCarData sideCarData)
        {
            Console.WriteLine($"I will start {pi.FileName} in {pi.WorkingDirectory} with {pi.Arguments}");
            if (argsRegex.Count > 0)
            {
                Console.WriteLine("---->replacements arguments ");
                foreach (var ar in argsRegex)
                {
                    Console.WriteLine($"{ar.Key}=>{ar.Value}");
                }
            }

            Console.WriteLine($"---->LineInterceptors:{sideCarData?.runningInterceptors?.LineInterceptors?.Length}"); 
            if (sideCarData.runningInterceptors?.LineInterceptors?.Length > 0)
            foreach (var item in sideCarData?.runningInterceptors?.LineInterceptors)
            {
                Console.WriteLine($"LineInterceptor:{item.Name} with arguments {item.Arguments}");
            }
            
            Console.WriteLine($"---->TimerInterceptors:{sideCarData?.runningInterceptors?.TimerInterceptors?.Length}");
            if(sideCarData.runningInterceptors?.TimerInterceptors?.Length>0)
            foreach (var item in sideCarData.runningInterceptors.TimerInterceptors)
            {
                Console.WriteLine($"TimerInterceptor:{item.Name} with arguments {item.Arguments}");
            }
            Console.WriteLine($"---->FinishInterceptors:{sideCarData?.runningInterceptors?.FinishInterceptors?.Length}");
            if(sideCarData.runningInterceptors?.FinishInterceptors?.Length>0)
            foreach (var item in sideCarData.runningInterceptors.FinishInterceptors)
            {
                Console.WriteLine($"FinishInterceptor:{item.Name} with arguments {item.Arguments}");
            }
        }

        private void RunFinishInterceptors(int exitCode)
        {
            if (!(this.runningInterceptors?.FinishInterceptors?.Length > 0))
                return;

            foreach(var item in this.runningInterceptors.FinishInterceptors)
            {
                try
                {
                    var local = item;
                    var dataToBeParsed = new Dictionary<string, string>();

                    if (this.argsRegex.Count > 0)
                    {
                        foreach (var reg in argsRegex)
                        {
                            dataToBeParsed.Add("{" + reg.Key + "}", reg.Value);
                        }
                    }
                    dataToBeParsed["{exitCode}"] = exitCode.ToString();

                    allProcesses.TryAdd(local.Name, local.RunFinishInterceptor(local.Name, dataToBeParsed));;

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"FinishInterceptor {item?.Name} error:!!!" + ex.Message);
                }
            }
        }

        private void P_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            P_OutputDataReceived(sender, e);
        }
        //static object lockMe = new object(); 

        private void P_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            
            //TODO: line interceptors to 
            //e.Data
            if (!(this.runningInterceptors?.LineInterceptors?.Length > 0))
            {
                //default write the process data
                Console.WriteLine(e.Data);
                return;
            }
            //lock(lockMe)
            foreach(var item in runningInterceptors.LineInterceptors)
            {
                try
                {

                    var local = item;
                    var dataToBeParsed=new Dictionary<string, string>();
                    
                    if(this.argsRegex.Count>0)
                    {
                        foreach (var reg in argsRegex)
                        {
                            dataToBeParsed.Add("{"+reg.Key+"}", reg.Value);
                        }
                    }
                    dataToBeParsed["{line}"]= e.Data;
                    allProcesses.TryAdd(local.Name, local.RunLineInterceptor(local.Name,dataToBeParsed));
                
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"LineInterceptor:{item?.Name} error:!!!" + ex.Message);
                }
            }
        }

        

        Func<Interceptor, DataReceivedEventHandler> a1 = (item) =>
        {
            return delegate (object sender, DataReceivedEventArgs e)
            {
                Console.WriteLine("!!!!");
                Console.WriteLine($"[{item.Name}] {e.Data}");
            };
        };
        private string regEx;
        Dictionary<string, string> argsRegex;
        private bool justTest;

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
