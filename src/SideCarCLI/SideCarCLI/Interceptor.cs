using System;
using System.Collections.Generic;
using System.Text;

namespace SideCarCLI
{
    public class Interceptor
    {
        public string Name { get; set; }
        public string FullPath { get; set; }
        public string FolderToExecute { get; set; }

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
