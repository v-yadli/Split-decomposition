using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace GraphCompression
{
    /// <summary>
    /// Twin-represented undirected graph
    /// </summary>
    partial class Graph
    {
        Dictionary<int, HashSet<int>> storage = new Dictionary<int, HashSet<int>>();
        public void Load(string dir)
        {
            var lines = File.ReadAllLines(dir);
            var splitArray = "\t ".ToCharArray();
            foreach (var s in lines)
            {
                if (s.StartsWith("#"))
                    continue;
                var sp = s.Split(splitArray, StringSplitOptions.RemoveEmptyEntries);
                AddEdge(int.Parse(sp[0]), int.Parse(sp[1]));
            }
        }
        public void AddEdge(int from, int to)
        {
            if (!storage.ContainsKey(from))
            {
                var newList = new HashSet<int>();
                newList.Add(to);
                storage[from] = newList;
            }
            else
                storage[from].Add(to);
            if (!storage.ContainsKey(to))
            {
                var newList = new HashSet<int>();
                newList.Add(from);
                storage[to] = newList;
            }
            else
                storage[to].Add(from);
        }
        public void ForEachNeighbor(int node, Action<int> action)
        {
            foreach (var neighbor in storage[node])
                action(neighbor);
        }
        public int NodeCount
        {
            get { return storage.Count; }
        }
    }
}
