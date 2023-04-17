using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiBeyer
{
    internal class DebugTools
    {
        internal static string GetCallingMethod()
        {
            StackFrame caller1 = (new System.Diagnostics.StackTrace()).GetFrame(2);
            StackFrame caller2 = (new System.Diagnostics.StackTrace()).GetFrame(3);
            return caller2?.GetMethod()?.Name+"->"+caller1.GetMethod().Name;
        }

        internal static void Log(string text)
        {
            Console.WriteLine(text);
        }
    }
}
