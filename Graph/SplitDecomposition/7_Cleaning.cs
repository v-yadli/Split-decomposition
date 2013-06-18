using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {
        #region Algorithm 7
        private void Cleaning(SplitTree ST, SplitTree tPrime)
        {
            //different from original algorithm 7, since the structure of T' is marked with visited flags, here we don't reset the flags
            //scan T' twice
            System.Collections.Generic.HashSet<MarkerVertex> A = new System.Collections.Generic.HashSet<MarkerVertex>();
            System.Collections.Generic.HashSet<MarkerVertex> B = new System.Collections.Generic.HashSet<MarkerVertex>();
            for (int i = 0, n = tPrime.vertices.Count; i < n; ++i)
            {
                if (tPrime.vertices[i] is DegenerateNode)
                {
                    var deg = tPrime.vertices[i] as DegenerateNode;
                    A.Clear();//Here A is P* and B is V \ P*
                    B.Clear();
                    deg.ForEachMarkerVertex((v) =>
                        {
                            if (v.perfect && v != deg.center)
                                A.Add(v);
                            else
                                B.Add(v);
                            return IterationFlag.Continue;
                        });
                    if (A.Count > 1 && B.Count > 1)
                    {
                        var newNode = SplitNode(deg, B, A);
                        if (newNode.parent == deg)
                            newNode.rootMarkerVertex.opposite.MarkAsPerfect();
                        else
                            deg.rootMarkerVertex.MarkAsPerfect();
                        ST.vertices.Add(newNode);//In this way, the structure of T' is unchanged as the newly forked node contains P*(u)
                    }
                }
            }
            //again we don't need to reset flags
            for (int i = 0, n = tPrime.vertices.Count; i < n; ++i)
            {
                if (tPrime.vertices[i] is DegenerateNode)
                {
                    var deg = tPrime.vertices[i] as DegenerateNode;
                    A.Clear();//Here A is V \ E* and B is E
                    B.Clear();
                    deg.ForEachMarkerVertex((v) =>
                        {
                            if ((v.perfect && v == deg.center) || (!v.perfect && !deg.GetOppositeGLTVertex(v).visited))//if v is not P and is not incident to a tree edge in T', it is E*
                                B.Add(v);
                            else
                                A.Add(v);
                            return IterationFlag.Continue;
                        });
                    if (A.Count > 1 & B.Count > 1)
                    {
                        ST.vertices.Add(SplitNode(deg, A, B));//let V \ E* be preserved in T'
                    }
                }
            }
            //and we're done with cl(ST(G)) and T_c.
        }
        #endregion
    }
}
