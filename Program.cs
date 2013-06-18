using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    class Program
    {
        static void Test(string record)
        {
            Graph g = new Graph();
            g.Load(record + ".txt");
            Console.WriteLine("Splitting to connected components...");
            var list = g.ConnectedComponents();
            Console.WriteLine(list.Count);
            Console.WriteLine(list[0].NodeCount);
            var ST = list[0].SplitDecomposition();
            //ST.Debug(false);
            var gPrime = ST.GenerateAccessabilityGraph();
            Console.WriteLine(list[0].SameAs(gPrime));
        }
        static void LoadTest()
        {
            //g.Load("TestGraph2.txt");
            //Test("TestGraph");
            //Test("citation");
            //Test("2");
            //Test("Email_Enron");
            //g.Load("80682.txt");
            //Test("2169");
            //Test("80682_sorted");
            //Test("72898");
            //Test("5771");
            //var g1 = new Graph();
            //var g2 = new Graph();
            //g1.Load("80682.txt");
            //g2.Load("80682_sorted.txt");
            //Console.WriteLine(g1.SameAs(g2));
        }
        static void GenerateTest()
        {
            for (int i = 0; i < /*300000*/1000; ++i)
            {
                //Graph g = Graph.GenerateErdosRenyi(9, 30);
                Graph g = Graph.GenerateErdosRenyi(10000, 160000);
                var list = g.ConnectedComponents();
                SplitTree ST = null;
                bool crashed = false;
                try
                {
                     ST = list[0].SplitDecomposition();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Test #{0} crashed", i);
                    list[0].Save(i.ToString() + ".txt");
                    crashed = true;
                }
                finally
                {
                    if (!crashed)
                    {
                        var gPrime = ST.GenerateAccessabilityGraph();
                        if (!list[0].SameAs(gPrime))
                        {
                            Console.WriteLine("Test #{0} failed", i);
                            list[0].Save(i.ToString() + ".txt");
                        }
                        else
                        {
                            Console.WriteLine("Test #{0} pass", i);
                        }
                    }
                }
            }
        }
        static void Main(string[] args)
        {
            GenerateTest();
            //LoadTest();
        }
    }
}
