using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {        #region Algorithm 4
        //XXX this algorithm may terminate when there's still an active vertex.
        private SplitTree SmallestSpanningTree(SplitTree ST, int x)
        {
            SplitTree ret = new SplitTree();//Here we don't need the associated graph, just a tree.
            if (ST.vertices.Count == 0)
                return ret;
            //Compute N(x). Note that the initial set L is also Nx
            //PerfectFlags are reset in Algorithm 5, which calls this one.
            Queue<GLTVertex> L = new Queue<GLTVertex>();
            ST.ResetActiveFlags();
            ST.ResetNeighborFlags();
            var originalNx = storage[x];
            GLTVertex p = null;
            bool rootVisited = false;
            foreach (int n in originalNx)
            {
                Leaf v = null;
                if (ST.LeafMapper.TryGetValue(n, out v))
                {
                    v.active = true;//marking a vertex with active means that it is currently enqueued
                    (v as Leaf).neighbor = true;
                    //set the non-root marker vertices opposite leaves of N(x) to perfect
                    var opposite = (v as Leaf).opposite as MarkerVertex;
                    //if (opposite.node == null)//non-root
                    {
                        opposite.perfect = true;
                    }
                    L.Enqueue(v);
                }
            }
            ST.ResetVisitFlags();
            while (L.Count > 0
                //(!rootVisited && L.Count >= 2) || (rootVisited && L.Count >= 1)
                )
            {
                var u = L.Dequeue();
                u.active = false;
                u.visited = true;
                ret.vertices.Add(u);
                p = u.parent;
                if (p == null)
                    rootVisited = true;
                else if (!p.visited && !p.active)
                {
                    p.active = true;
                    L.Enqueue(p);
                }
            }
            if (ret.vertices.Count == 0)
                return ret;
            //adjust the root
            GLTVertex root = ret.vertices[0];
            while ((p = root.parent) != null && p.visited)
                root = p;
            //do not use numberOfChildren, since not all of the children are included in the subtree
            while (true)
            {
                if (root is Leaf)//if root is a leaf, then definitely it has less than 2 children. proceed to the child if it has one.
                {
                    var opposite = (root as Leaf).opposite;
                    bool rootInNx = (root as Leaf).neighbor;
                    if (opposite == null)
                    {
                        //The current leaf is the only vertex in the GLT.
                        //If it is not in Nx, drop it
                        if (!rootInNx)
                        {
                            root.visited = false;
                            ret.vertices.Remove(root);
                            root = null;
                        }
                        break;
                    }

                    if (rootInNx)
                        break;
                    var marker = opposite as MarkerVertex;
                    Debug.Assert(marker != null, "The opposite of a leaf is something other than MarkerVertex!");
                    //Since we are traversing from parent to children, the opposite marker vertex must be the root marker vertex
                    ret.vertices.Remove(root);
                    root.visited = false;
                    root = marker.node;
                }
                else//root is a node. then we check each of its marker vertex except the root marker, find its opposite and see whether it's included or not
                {
                    int count = 0;
                    GLTVertex child = null;
                    (root as Node).ForEachChild((v) =>
                        {
                            ++count;
                            child = v;
                            return IterationFlag.Continue;
                        }, true);
                    if (count != 1)
                        break;
                    //remember to remove root from ret and uncheck its visited flag before iterating to the child
                    ret.vertices.Remove(root);
                    root.visited = false;
                    root = child;
                }
                //ret.vertices.Remove(root);
                //root = c;
            }
            ret.root = root;

            return ret;
        }
        #endregion
    }
}