using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {        #region Algorithm 6
        //returns the new node containing B, plus a marker vertex representing A(while the old node is transformed into the A part)
        private DegenerateNode SplitNode(DegenerateNode node, System.Collections.Generic.HashSet<MarkerVertex> A, System.Collections.Generic.HashSet<MarkerVertex> B)
        {
            //XXX A set here is not necessary...
            //since node must be degenerate(otherwise not splittable due to the definition of prime node), there's an easy way to build frontiers:
            //see paper notes.

            //assume the original node contains A part, now we remove the B part
            node.RemoveMarkerVertices(B);
            var v1 = new MarkerVertex();
            var v2 = new MarkerVertex();
            DegenerateNode newNode = new DegenerateNode()
            {
                Vu = new List<MarkerVertex>(B),
            };
            if (node.isStar)//address the center issue
            {
                if (A.Contains(node.center))
                {
                    //node center doesn't change
                    newNode.center = v2;
                }
                else
                {
                    newNode.center = node.center;
                    node.center = v1;
                }
            }
            node.Vu.Add(v1);
            newNode.Vu.Add(v2);
            v1.opposite = v2;
            v2.opposite = v1;
            if (B.Contains(node.rootMarkerVertex))//B is being splitted out, if B has the root marker vertex, newNode should point to the original parent
            {
                newNode.rootMarkerVertex = node.rootMarkerVertex;
                newNode.parent = node.parent;
                newNode.rootMarkerVertex.node = newNode;
                node.rootMarkerVertex = v1;
                v1.node = node;
                //before assigning parent, check if a clone is necessary
                if (node.parent is PrimeNode)
                {
                    var cloned = node.Clone();
                    node = cloned as DegenerateNode;//thus the old node deprecated
                }
                node.parent = newNode;
            }
            else//A has root marker vertex, thus old parent unchanged.
            {
                newNode.rootMarkerVertex = v2;
                newNode.parent = node;
                v2.node = newNode;
            }
            //update the parent links of the children in the new node
            newNode.ForEachChild((v) =>
                {
                    v.parent = newNode;
                    return IterationFlag.Continue;
                }, subtree: false);
            return newNode;
        }
        #endregion
    }
}
