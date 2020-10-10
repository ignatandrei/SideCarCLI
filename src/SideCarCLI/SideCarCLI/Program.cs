using McMaster.Extensions.CommandLineUtils;
using System;

namespace SideCarCLI
{
    class Program
    {
        static int Main(string[] args)
        {
            var app = new CommandLineApplication();

            
            app.HelpOption("-h|--help", inherited: true);

            app.Command("_about", cmd =>
            {
                cmd.OnExecute(() =>
                {
                    app.ShowVersion();
                    Console.WriteLine("made by Andrei Ignat, http://msprogrammer.serviciipeweb.ro/category/sidecar/");
                });
             });



            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowVersion();
                app.ShowHelp();
                return 1;
            });
            return app.Execute(args);

        }
    }
}
