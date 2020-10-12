using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SideCarCLI
{
    class Program
    {
        static SideCarData data;
        static int Main(string[] args)
        {
            data = new SideCarData();
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
            var maxSeconds=app.Option("-max|--maxSeconds", "max seconds for the StartApp to run", CommandOptionType.SingleOrNoValue); ;
            app.Command("_about", cmd =>
            {
                cmd.OnExecute(() =>
                {
                    app.ShowVersion();
                    Console.WriteLine("made by Andrei Ignat, http://msprogrammer.serviciipeweb.ro/category/sidecar/");
                });
            });

            app.Command("startApp", cmdStartApp =>
            {
                cmdStartApp.FullName = "start the CLI application that you need to intercept";
                cmdStartApp.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                cmdStartApp.Option("-n|--name <fullPathToApplication>", "Path to the StartApp", CommandOptionType.SingleValue);
                cmdStartApp.Option("-a|--arguments <arguments_to_the_app>", "StartApp arguments", CommandOptionType.MultipleValue);
                cmdStartApp.Option("-f|--folder <folder_where_execute_the_app>", "folder where to execute the StartApp - default folder of the StartApp ", CommandOptionType.SingleOrNoValue);

                cmdStartApp.OnParsingComplete(pr =>
                {
                    //Console.WriteLine(pr.SelectedCommand.GetOptions().First().Value());
                    pr.SelectedCommand.GetOptions().ToList().ForEach(c => Console.WriteLine(c.Value()));
                    //

                    var commandName = pr.SelectedCommand.GetOptions().ToArray()[0].Value(); // ping
                    var commandArgumets = pr.SelectedCommand.GetOptions().ToArray()[1].Value(); // arguments for command the Sidecar will execute. i.e. "-t www.yahoo.com"
                    Console.WriteLine(commandName + " " + commandArgumets);
                    Process.Start(commandName);
                });


                cmdStartApp.Command("lineInterceptors", cmd =>
                {
                    cmd.FullName = "Specify application for start when StartApp has a new line output";
                    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                    cmd.Option("-l|--list", "List interceptors for lines", CommandOptionType.NoValue);
                    cmd.Option("-a|--add", "Add interceptor to execute", CommandOptionType.MultipleValue);
                    cmd.Option("-f|--folder", "folder where to start the interceptor", CommandOptionType.SingleOrNoValue);
                });

                cmdStartApp.Command("timer", cmd =>
                {
                    cmd.FullName = "Specify timer to start an application at repeating interval ";
                    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                    cmd.Option("-i|--intervalRepeatSeconds", "Repeat interval in seconds", CommandOptionType.SingleValue);
                    cmd.Option("-l|--list", "List interceptors to execute periodically", CommandOptionType.NoValue);
                    cmd.Option("-a|--add", "Add interceptor to execute", CommandOptionType.MultipleValue);
                    cmd.Option("-f|--folder", "folder where to start the interceptor", CommandOptionType.SingleOrNoValue);

                });
                cmdStartApp.Command("finishInterceptors", cmd =>
                {
                    cmd.FullName = "Specify interceptors for start when finish the app";
                    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                    cmd.Option("-l|--list", "List interceptors for finish application", CommandOptionType.NoValue);
                    cmd.Option("-a|--add", "Add interceptor to execute", CommandOptionType.MultipleValue);
                    cmd.Option("-f|--folder", "folder where to start the interceptor", CommandOptionType.SingleOrNoValue);

                });
                cmdStartApp.Command("plugins", cmd =>
                {
                    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;

                    cmd.FullName = " Load dynamically plugins ";
                    cmd.ResponseFileHandling = ResponseFileHandling.ParseArgsAsLineSeparated;
                    cmd.Option("-f|--folder", "folder with plugins", CommandOptionType.SingleValue);
                    cmd.Option("-l|--list", "List plugins", CommandOptionType.NoValue);
                    cmd.Option("-a|--add", "Add interceptor to execute", CommandOptionType.MultipleValue);

                });
            });

            app.Command("_listAllCommands", cmd =>
            {

                cmd.FullName = " List all commands for the app";

                cmd.OnExecute(() =>
                {
                    WriteAllCommands(app);
                });
            });
            app.OnParsingComplete(pr =>
            {
                data.ParseSeconds(maxSeconds);
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
