using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SideCarCLI
{
    enum InterceptorType
    {
        None=0,
        LineInterceptor=1,
        FinishInterceptor=2,
        TimerInterceptor =3
    }
    public class Interceptor
    {
        public string Name { get; set; }
        public string Arguments { get; set; }
        public string FullPath { get; set; }
        public string FolderToExecute { get; set; }
        public bool InterceptOutput { get; set; }
        public string RegexCaptureArguments { get; set; }
        public Process RunTimerInterceptor(string name, Dictionary<string, string> data)
        {
            return RunInterceptor(name, data, InterceptorType.TimerInterceptor);
        }
        public Process RunLineInterceptor(string name, Dictionary<string, string> data)
        {
            return RunInterceptor(name, data, InterceptorType.LineInterceptor);
        }
        public Process RunFinishInterceptor(string name, Dictionary<string, string> data)
        {
            return RunInterceptor(name, data, InterceptorType.FinishInterceptor);
        }
        private string ReplaceForInterceptor(InterceptorType interceptorType)
        {
            switch (interceptorType)
            {
                case InterceptorType.LineInterceptor:
                    return "{line}";
                case InterceptorType.FinishInterceptor:
                    return "{exitCode}";
                case InterceptorType.TimerInterceptor:
                    return null;
                default:
                    throw new ArgumentException($"Cannot find {interceptorType.ToString()}");
            }
        }
        private Process RunInterceptor(string name, Dictionary<string, string> data, InterceptorType interceptorType)
        {
            string typeInterceptor =  interceptorType.ToString();
            //string replace = ReplaceForInterceptor(interceptorType);

            var pi = new ProcessStartInfo(FullPath);
            
            string wd = FolderToExecute;
            if (string.IsNullOrWhiteSpace(wd))
            {
                wd = Path.GetDirectoryName(Path.GetFullPath(Name));
            }
            string arguments = Arguments;
            if (string.IsNullOrWhiteSpace(arguments))
            {
                arguments = "{line}";
            }
            foreach (var item in data)
            {
                arguments = arguments.Replace(item.Key, item.Value);
            }
            
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
                    Console.WriteLine($"Interceptor {typeInterceptor}: {name} => {args.Data}");                    
                };
                p.ErrorDataReceived += (sender, args) =>
                {
                    Console.WriteLine($"Interceptor {typeInterceptor}: {name} ERROR => {args.Data}");
                };
                p.Exited += (sender, args) =>
                {                    
                    Console.WriteLine($"Interceptor {typeInterceptor}: {name} exited ");
                };
            }
            return p;
        }
    }
    public class TimerInterceptor: Interceptor
    {
        public long intervalRepeatSeconds { get; set; }
        public string arguments { get; set; }
    }
    public class Interceptors
    {
        public TimerInterceptor[] TimerInterceptors { get; set; }
        public Interceptor[] LineInterceptors { get; set; }
        public Interceptor[] FinishInterceptors { get; set; }
    }
}
