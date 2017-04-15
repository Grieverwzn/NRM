using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CplexMultiThreadTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Solver s = new Solver();s.NumOfThreads = 4;
            s.Init();
            s.DoCal();

            Console.WriteLine("-------- Press <Enter> to Exit --------");
            Console.ReadLine();
        }
    }
}
