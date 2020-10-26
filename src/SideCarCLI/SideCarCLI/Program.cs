using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SideCarCLI
{
    class Program
    {
        
        static async Task<int> Main(string[] args)
        {

            //var data = "x=y z=t";
            //var expression = @"(?<FirstArg>\w+)=((\w+)) (?<LastArg>\w+)=((\w+))";
            //var data = "-n 20 \"www.yahoo.com\"";
            //var expression = @"(?<FirstArg>.+) (?<site>.+)";
            //var options = RegexOptions.Singleline;
            //var regex = new Regex(expression);
            //var names = regex.
            //    GetGroupNames().
            //    Where(it => !int.TryParse(it, out var _)).
            //    ToArray();

            //var matches = regex.Matches(data);

            ////Console.WriteLine(matches.Count);
            //var m = matches.FirstOrDefault();
            //Console.WriteLine(m.Groups?.Count);
            //foreach (var g in names)
            //{
            //    Console.WriteLine(g);
            //    Console.WriteLine(m.Groups[g].Success);
            //    Console.WriteLine(m.Groups[g].Value);

            //}
            //return 1;
            var app = new CommandLineApplication()
            {
                MakeSuggestionsInErrorMessage = true,
                FullName = "SideCar for any other application",
                ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated,
                ExtendedHelpText = @"
                The most simplest form it is: 
                    SideCarCLI startApp --name YourApp
                "
            };


            app.HelpOption("-h|--help", inherited: true);
            
            app.Command("about", cmd =>
            {
                cmd.OnExecute(() =>
                {
                    app.ShowVersion();
                    Console.WriteLine("made by Andrei Ignat, http://msprogrammer.serviciipeweb.ro/category/sidecar/");
                });
            });

            app.Command("StartAPP", cmdStartApp =>
            {
                cmdStartApp.FullName = "start the CLI application that you need to intercept";
                cmdStartApp.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                var nameExe=cmdStartApp.Option("-n|--name <fullPathToApplication>", "Path to the StartApp", CommandOptionType.SingleValue);
                var argExe = cmdStartApp.Option("-a|--arguments <arguments_to_the_app>", "StartApp arguments", CommandOptionType.MultipleValue);
                var wd=cmdStartApp.Option("-f|--folder <folder_where_execute_the_app>", "folder where to execute the StartApp - default folder of the StartApp ", CommandOptionType.SingleOrNoValue);
                var maxSeconds = app.Option("-mS|--maxSeconds", "max seconds for the StartApp to run", CommandOptionType.SingleOrNoValue);
                var lineInterceptorsNames= cmdStartApp.Option("-aLi|--addLineInterceptor", "Add Line Interceptor to execute", CommandOptionType.MultipleValue);
                var timerInterceptorsNames=cmdStartApp.Option("-aTi|--addTimerInterceptor", "Add Timer Interceptor to execute", CommandOptionType.MultipleValue);
                var finishInterceptorsNames = cmdStartApp.Option("-aFi|--addFinishInterceptor", "Add Finish Interceptor to execute", CommandOptionType.MultipleValue);
                var waitForTimersToFinish = cmdStartApp.Option("-wFTitF|--waitForTimerInterceptorsToFinish", "wait for timer interceptors to finish 0 =false ", CommandOptionType.SingleOrNoValue);
                var optionRegex = cmdStartApp.Option("-rx|--regex <string>", "regex to parse original line and pass the matches to the interceptors", CommandOptionType.SingleOrNoValue);
                var justDisplayInfo = cmdStartApp.Option("-t|--test <1>", "just display summary and exit", CommandOptionType.SingleOrNoValue);
                cmdStartApp.OnExecuteAsync(async (ct) => 
                {
                    var data = new SideCarData();
                    data.ParseSeconds(maxSeconds);
                    
                    
                    data.ParseCommandName(nameExe);
                    data.ParseArguments(argExe);
                    data.ParseWorkingDirectory(wd);
                    data.ParseInterceptors(lineInterceptorsNames, timerInterceptorsNames, finishInterceptorsNames);
                    data.ParseWaitForTimersToFinish(waitForTimersToFinish);
                    data.SetRegex(optionRegex.HasValue() ? optionRegex.Value() : null);
                    data.SetTest(justDisplayInfo.HasValue() && (justDisplayInfo.Value() == "1"));
                    var res= data.ExecuteApp();
                    while (data.ExistRunningProcess)
                    {
                        await Task.Delay(5000);
                    }
                    return res;

                });

                //cmdStartApp.Command("plugins", cmd =>
                //{
                //    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;

                //    cmd.FullName = " Load dynamically plugins ";
                //    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                //    cmd.Option("-f|--folder", "folder with plugins", CommandOptionType.SingleValue);
                //    cmd.Option("-l|--list", "List plugins", CommandOptionType.NoValue);
                //    cmd.Option("-a|--add", "Add interceptor to execute", CommandOptionType.MultipleValue);

                //});

            });
            app.Command("interceptors", cmdInterceptor =>
            {
                cmdInterceptor.Option("-lLi|--ListLineInterceptor", "List line interceptor", CommandOptionType.SingleOrNoValue);
                cmdInterceptor.Option("-lLi|--ListTimerInterceptor", "List timer interceptor", CommandOptionType.SingleOrNoValue);
                cmdInterceptor.Option("-lFi|--ListFinishInterceptor", "List timer interceptor", CommandOptionType.SingleOrNoValue);


            });


            app.Command("listAllCommands", cmd =>
            {

                cmd.FullName = " List all commands for the app";

                cmd.OnExecute(() =>
                {
                    WriteAllCommands(app);
                });
            });
          
            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a command");
                app.ShowHelp();
                return 1;
            });
            return app.Execute(args);

        }
        static void WriteAllCommands(CommandLineApplication cmd)
        {
            Console.WriteLine($"--------------");
            //cmd.ShowRootCommandFullNameAndVersion();
            var p = cmd;
            var all = new List<string>();
            all.Add(p.Name);
            while(p.Parent != null)
            {
                all.Add(p.Parent.Name);
                p = p.Parent;
                
            }
            var text = string.Join("-->", all.ToArray().Reverse());
            //string tabs = "";
            //var nr = all.Count() - 2;
            //if(nr>0)
            //{
            //    tabs = string.Join("", Enumerable.Repeat("   ", nr));
            //}
            Console.WriteLine("command path:"+text);
            Console.WriteLine($"--------------");
            cmd.ShowHelp();
            foreach (var item in cmd.Commands)
            {
                WriteAllCommands(item);
            }
        }
    }
}
