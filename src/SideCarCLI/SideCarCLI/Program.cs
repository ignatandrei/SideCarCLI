using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace SideCarCLI
{
    class Program
    {
        
        static int Main(string[] args)
        {
            string fileInterceptors = Path.Combine("cmdInterceptors", "interceptors.json");
            var interceptors = JsonSerializer.Deserialize<Interceptors>(File.ReadAllText(fileInterceptors));

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


                cmdStartApp.OnExecute(() =>
                {
                    var data = new SideCarData(interceptors);
                    data.ParseSeconds(maxSeconds);
                    
                    
                    data.ParseCommandName(nameExe);
                    data.ParseArguments(argExe);
                    data.ParseWorkingDirectory(wd);
                    
                    data.ExecuteApp();  
                  

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
