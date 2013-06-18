using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GraphCompression
{

    public partial class Graph
    {
        #region Algorithm 1
        public SplitTree SplitDecomposition()
        {
            var ST = new SplitTree();
            var sigma = LexBFS();
            for (int i = 0, n = NodeCount; i < n; ++i)
            {
                VertexInsertion(ST, sigma, i);
                //if (i % 100 == 0 && n > 100)
                //    Console.WriteLine(i);
                //if (i >= 2)
                //    ST.Debug(false);
            }
            return ST;
        }
        #endregion
    }
}