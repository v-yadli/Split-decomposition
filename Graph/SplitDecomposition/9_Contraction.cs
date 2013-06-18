using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {
        #region Algorithm 9
        private PrimeNode Contraction(SplitTree ST, SplitTree tPrime, int xId)
        {
            //during the contraction, the active flags won't be used any more. thus available as temporary delete flags
            //when a deleted node has non-empty parentLink and empty unionFind_parent, it is a degenerate to prime conversion and the parentLink points to the converted prime node.
            //when a deleted node has non-empty unionFind_parent, it is a fake node (only playing a role of child representative)
            ST.ResetActiveFlags();
            List<DegenerateNode> Phase1List = new List<DegenerateNode>();
            List<Node> Phase2List = new List<Node>();
            List<Node> nonLeafChildren = new List<Node>();
            Node phase3 = null;//since phase 3 is recursive, we need only a start point.
            #region Phase 0 Initial sweep
            foreach (var v in tPrime.vertices)
            {
                var d = v as DegenerateNode;
                var n = v as Node;
                if (d != null && d.isStar && d.rootMarkerVertex == d.center)
                    Phase1List.Add(d);
                if (n != null && n.Degree(n.rootMarkerVertex) == 1)
                    Phase2List.Add(n);
                if (n != null)
                    phase3 = n;//in case Phase1List and Phase2List are both empty, at least we have a start point for phase 3
            }
            #endregion
            #region Phase 1 node-joins
            foreach (var star in Phase1List)
            {
                if (star.active)//deleted
                    continue;
                phase3 = star;
                nonLeafChildren.Clear();
                phase3.ForEachChild((v) =>
                    {
                        if (v is Node)
                        {
                            nonLeafChildren.Add(v as Node);
                        }
                        return IterationFlag.Continue;
                    }, subtree: true);
                foreach (var c in nonLeafChildren)
                {
                    phase3 = NodeJoin(ST, phase3, c);
                    //c.parentLink = dummy;
                }
                phase3.visited = true;//add the joint node back into T'
            }
            #endregion
            #region Phase 2 node-joins
            foreach (var node in Phase2List)
            {
                if (node.active)//actually I mean "deleted"
                    continue;
                phase3 = node;
                var p = phase3.parent;
                if (p.visited && p is Node)
                {
                    phase3 = NodeJoin(ST, p as Node, phase3);//make sure phase3 now points to the new parent(which is prime)
                }
            }
            #endregion
            #region Phase 3 node-joins
            Debug.Assert(phase3 != null, "Phase 3 node is null, which means that there's nothing left in the fully-mixed subtree T'");
            while (true)
            {
                while (true)
                {
                    nonLeafChildren.Clear();
                    phase3.ForEachChild((v) =>
                        {
                            if (v is Node)
                            {
                                nonLeafChildren.Add(v as Node);
                            }
                            return IterationFlag.Continue;
                        }, subtree: true);
                    if (nonLeafChildren.Count == 0)
                        break;
                    foreach (var c in nonLeafChildren)
                    {
                        phase3 = NodeJoin(ST, phase3, c);
                    }
                }
                var p = phase3.parent;
                if (p == null || p is Leaf || p.visited == false)
                {
                    break;
                }
                else
                    phase3 = p as Node;
            }
            #endregion
            //rebuild ST from dummy flags. Note that this step is very important before using Find() and parent accessor again, because there might be a node with unionFind_parent == dummyFake, and dummyFake has unionFind_parent == null
            //List<GLTVertex> newSTList = new List<GLTVertex>();
            //foreach (var vertex in ST.vertices)
            //{
            //    if (!vertex.active)
            //        newSTList.Add(vertex);//a normal vertex
            //    else if (vertex.parentLink != null && vertex.unionFind_parent == null)
            //        newSTList.Add(vertex.parentLink);//a converted vertex
            //    //otherwise, either a fake node, or a truely-removed one, we don't add them back to ST any more.
            //    //if (vertex.parentLink != deleteDummy && vertex.unionFind_parent != fakeDummy)//neither deleted nor fake
            //    //    newSTList.Add(vertex);
            //    //else if (vertex.parentLink == deleteDummy && vertex.unionFind_parent != null)//replaced
            //    //    newSTList.Add(vertex.unionFind_parent);
            //    //else if (vertex.unionFind_parent == fakeDummy)
            //    //    vertex.unionFind_parent = vertex;//point the unionFind_parent back
            //}
            //ST.vertices = newSTList;

            HashSet<MarkerVertex> Pset = new HashSet<MarkerVertex>();
            phase3.ForEachMarkerVertex((v) =>
                {
                    if (v.perfect)
                        Pset.Add(v);
                    return IterationFlag.Continue;
                });
            Leaf newLeaf = new Leaf()
            {
                id = xId,
                parent = phase3,
            };
            MarkerVertex newMarker = new MarkerVertex()
            {
                opposite = newLeaf,
            };
            newLeaf.opposite = newMarker;
            (phase3 as PrimeNode).AddMarkerVertex(newMarker, Pset);
            (phase3 as PrimeNode).lastMarkerVertex = newMarker;
            ST.AddLeaf(newLeaf);
            return phase3 as PrimeNode;
        }
        #endregion
    }
}
