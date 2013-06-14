using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    class Program
    {
        static void Main(string[] args)
        {
            Graph g = new Graph();
            g.Load("TestGraph.txt");
            var ST = g.SplitDecomposition();
        }
    }
}
