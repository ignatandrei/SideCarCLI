using System;
using System.Collections.Generic;
using System.Text;

namespace SideCarCLI
{
    static class ExecCommands
    {
        public static object ExecuteCommand(object command)
        {
            Console.WriteLine(command.ToString());
            return 1;
        }
    }
}
