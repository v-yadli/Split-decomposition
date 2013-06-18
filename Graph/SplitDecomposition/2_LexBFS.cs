using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C5;

namespace GraphCompression
{    class LexTupleComparor : IComparer<Tuple<List<int>, int>>
    {
        public int Compare(Tuple<List<int>, int> x, Tuple<List<int>, int> y)
        {
            int nx = x.Item1.Count;
            int ny = y.Item1.Count;
            int n, ret;
            ret = nx - ny;//ret is used when comparison reaches the end but order is still not determined (just return the longer one)
            n = nx > ny ? ny : nx;
            for (int i = 0; i < n; ++i)
            {
                if (x.Item1[i] < y.Item1[i])
                    return -1;
                else if (x.Item1[i] > y.Item1[i])
                    return 1;
            }
            return ret;
        }
    }
    public partial class Graph
    {        #region Algorithm 2
        private List<int> LexBFS()
        {
            int n = NodeCount;
            List<int> ret = new List<int>(n);
            List<List<int>> labels = new List<List<int>>(n);
            List<IPriorityQueueHandle<Tuple<List<int>, int>>> handles = new List<IPriorityQueueHandle<Tuple<List<int>, int>>>(n);
            Dictionary<int, int> mapper = new Dictionary<int, int>(n);
            C5.IntervalHeap<Tuple<List<int>, int>> lexHeap = new IntervalHeap<Tuple<List<int>, int>>(n, new LexTupleComparor());
            int idx = 0;
            ForEachVertex((neighbor) =>
            {
                labels.Add(new List<int>());
                IPriorityQueueHandle<Tuple<List<int>, int>> handle = null;
                lexHeap.Add(ref handle, Tuple.Create(labels[idx], neighbor));
                handles.Add(handle);
                mapper[neighbor] = idx;
                idx++;
            });
            for (int i = 1; i <= n; ++i)
            {
                var node = lexHeap.DeleteMax();
                handles[mapper[node.Item2]] = null;
                ret.Add(node.Item2);
                ForEachNeighbor(node.Item2, (neighbor) =>
                    {
                        idx = mapper[neighbor];
                        if (handles[idx] == null) return;
                        labels[idx].Add(n - i + 1);
                        var newTuple = Tuple.Create(labels[idx], neighbor);
                        lexHeap.Replace(handles[idx], newTuple);
                    });
            }
            //lexHeap.Replace
            return ret;
        }
        #endregion // Algorithm 2
    }
}
