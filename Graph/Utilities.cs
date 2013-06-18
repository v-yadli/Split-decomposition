using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    class Debug
    {
        static public void Assert(bool eval, string failureMessage)
        {
            if (!eval)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(failureMessage);
                throw new Exception();
            }
        }
        //static public void AssertNot(bool eval, string failureMessage)
        //{
        //    if (eval)
        //    {
        //        Console.ForegroundColor = ConsoleColor.Red;
        //        Console.WriteLine(failureMessage);
        //        Environment.Exit(-1);
        //    }
        //}
    }
}
