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
    public partial class Graph
    {
        public static Graph GenerateErdosRenyi(int n, int m)
        {
            Random r = new Random();
            Graph g = new Graph();
            for (int i = 0; i < m; ++i)
            {
                g.AddEdge(r.Next(n), r.Next(n));
            }
            return g;
        }
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
            if (from == to)
                return;

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
        public bool SameAs(Graph g)
        {
            if (g.storage.Count != storage.Count)
                return false;
            foreach (var t in storage)
            {
                HashSet<int> correspondingSet = null;
                if (!g.storage.TryGetValue(t.Key, out correspondingSet))
                    return false;
                if (t.Value.Count != correspondingSet.Count)
                    return false;
                foreach (var n in t.Value)
                    if (!correspondingSet.Contains(n))
                        return false;
            }
            return true;
        }
        public void ForEachVertex(Action<int> action)
        {
            foreach (var kvp in storage)
            {
                action(kvp.Key);
            }
        }
        public List<Graph> ConnectedComponents()
        {
            List<Graph> ret = new List<Graph>();
            HashSet<int> visited = new HashSet<int>();
            Queue<int> queue = new Queue<int>();
            ForEachVertex((v) =>
                {
                    if (!visited.Contains(v))
                    {
                        Graph g = new Graph();
                        queue.Enqueue(v);
                        visited.Add(v);
                        while (queue.Count != 0)
                        {
                            int u = queue.Dequeue();
                            g.storage[u] = storage[u];
                            ForEachNeighbor(u, (w) =>
                                {
                                    if (!visited.Contains(w))
                                    {
                                        queue.Enqueue(w);
                                        visited.Add(w);
                                    }
                                });
                        }
                        ret.Add(g);
                    }
                });
            return ret;
        }

        internal void Save(string fileName)
        {
            StreamWriter sw = new StreamWriter(fileName);
            ForEachVertex(i => ForEachNeighbor(i, j => sw.WriteLine("{0} {1}", i, j)));
            sw.Close();
        }
    }
}
