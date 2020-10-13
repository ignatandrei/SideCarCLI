using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SideCarCLI
{
    public class Interceptor
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
        public string FullPath { get; set; }
        public string FolderToExecute { get; set; }
        public bool InterceptOutput { get; set; }
        public Process RunLineInterceptor(string name, string data)
        {
            return RunInterceptor(name, data, "{line}");
        }
        public Process RunFinishInterceptor(string name, string data)
        {
            return RunInterceptor(name, data, "{exitCode}");
        }

        private Process RunInterceptor(string name, string data, string replace)
        {
            var pi = new ProcessStartInfo(FullPath);
            pi.Arguments = data;
            string wd = FolderToExecute;
            if (string.IsNullOrWhiteSpace(wd))
            {
                wd = Path.GetDirectoryName(Path.GetFullPath(Name));
            }
            string arguments = Arguments;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                arguments = replace;
            }
            pi.Arguments = arguments.Replace(replace, data);
            pi.RedirectStandardError = InterceptOutput;
            pi.RedirectStandardOutput = InterceptOutput;

            pi.WorkingDirectory = wd;

            pi.UseShellExecute = !InterceptOutput;
            pi.CreateNoWindow = InterceptOutput;

            var p = new Process()
            {
                StartInfo = pi
            };
            p.EnableRaisingEvents = InterceptOutput;
            
            p.Start();
            if (InterceptOutput)
            {
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.OutputDataReceived += (sender, args) =>
                {
                    Console.WriteLine($"Interceptor : {name} => {args.Data}");
                };
                p.ErrorDataReceived += (sender, args) =>
                {
                    Console.WriteLine($"Interceptor : {name} ERROR => {args.Data}");
                };
                p.Exited += (sender, args) =>
                {
                    
                    Console.WriteLine($"Interceptor : {name} FINISH");
                };
            }
            return p;
        }
    }
    public class TimerInterceptor: Interceptor
    {
        public long intervalRepeatSeconds { get; set; }
    }
    public class Interceptors
    {
        public TimerInterceptor[] TimerInterceptors { get; set; }
        public Interceptor[] LineInterceptors { get; set; }
        public Interceptor[] FinishInterceptors { get; set; }
    }
}
