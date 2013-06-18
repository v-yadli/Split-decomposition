using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {        #region Algorithm 5
        public enum CaseIdentification_ResultType
        {
            TreeEdge,
            HybridNode,
            FullyMixedSubTree,
            SingleLeaf
        }

        public CaseIdentification_ResultType SplitTree_CaseIdentification
            (SplitTree ST, List<int> sigma, int idx, out TreeEdge e, out Vertex u, out SplitTree tree)
        {
            //nodeOrder is the inverse permutation of sigma
            //Here idx is the index of x.
            e = new TreeEdge();
            u = null;
            #region Line 1
            MarkerVertex.ResetPerfectFlags();//new N(x) will be generated. All old perfect flags outdated.
            tree = SmallestSpanningTree(ST, sigma[idx]);//N(x) is implied in neighbor flag of each vertex
            #endregion
            //remember tree is rooted in tree.root,and it perhaps has a valid parent!
            //Also, check visited flag when traversing, as only those with visited flag true are included in subtree.
            #region Line 2
            int nodeCount = 0;
            Queue<Node> processingQueue = new Queue<Node>();
            Action<Node> testAllLeave = (n) =>
                {
                    bool? allLeave = null;
                    (n as Node).ForEachChild((v) =>
                        {
                            if (!(v is Leaf))
                            {
                                allLeave = false;
                                return IterationFlag.Break;
                            }
                            allLeave = true;
                            return IterationFlag.Continue;
                        }, subtree: false);//TODO XXX here subtree should be true, according to Algorithm 5
                    if (!(allLeave == null || !allLeave.Value))//has all leave
                        processingQueue.Enqueue(n);
                };
            foreach (var n in tree.vertices)
                if (n is Node)
                {
                    ++nodeCount;
                    testAllLeave(n as Node);
                }

            if (nodeCount > 1)//since there're not only 1 node, a node with all leaf children will certainly not be root
            {
                while (processingQueue.Count != 0)
                {
                    var node = processingQueue.Dequeue();
                    //cast the spell of Remark 5.5
                    List<MarkerVertex> P, M, E;
                    node.ComputeStates(out P, out M, out E, exclude: node.rootMarkerVertex);
                    //since node has all leaf children, when we exclude the rootMarkerVertex, the time complexity is bound to |Children|
                    //also, M should be empty. Thus case 2 automatically satisfied. <- XXX not really, cannot assert here
                    //Debug.Assert(M.Count == 0, "A node with all leaf children has a mixed marker vertex!!");
                    var Pset = new System.Collections.Generic.HashSet<MarkerVertex>(P);
                    var inCount = 0;
                    var neighborCount = 0;
                    node.ForEachNeighbor(node.rootMarkerVertex, (q) =>
                        {
                            ++neighborCount;
                            if (Pset.Contains(q))
                                ++inCount;
                            return IterationFlag.Continue;
                        });
                    //P == N_Gu(q) iff P.Count == inCount == neighborCount
                    if (P.Count == inCount && inCount == neighborCount && M.Count == 0)//now we say that rootMarkerVertex'es opposite vertex is perfect
                    {
                        //(node.rootMarkerVertex.opposite as GLTVertex).
                        node.rootMarkerVertex.opposite.MarkAsPerfect();
                        tree.RemoveSubTree(node);
                        if (node.parent is Node)
                            testAllLeave(node.parent as Node);
                    }
                }
            }
            #endregion
            #region Line 3
            u = tree.root;
            bool unique = true;
            Debug.Assert(u != null, "the root of subtree is null, after removing pendant perfect subtrees");
            while (true)
            {
                Node child = null;
                if (u is Leaf)
                {
                    child = (u as Leaf).opposite.node;
                    if (child == null || !child.visited)//now the leaf root u is the unique vertex in T'
                    {
                        //this case is not discussed in paper... Thanks Emeric, problem solved.
                        return CaseIdentification_ResultType.SingleLeaf;
                    }
                }
                else
                {
                    (u as Node).ForEachChild((v) =>
                        {
                            if (v is Node)
                            {
                                if (child == null)
                                {
                                    unique = true;
                                    child = v as Node;
                                }
                                else
                                {
                                    unique = false;
                                    return IterationFlag.Break;
                                }
                            }
                            return IterationFlag.Continue;
                        }, subtree: true);
                }
                //here unique indicates whether u has a unique node child
                if (unique == false)
                    break;//there are at least two non-leaf children of the current root; break out and return the subtree, now.
                else
                    if (child == null)//u contains no non-leaf child
                    {
                        //now we can say u is the unique node in T'
                        break;
                    }
                    else// the unique child node is located
                    {
                        if (u is Leaf)
                        {
                            //the root is a leaf, which indicates that root \in N(x), thus perfect.
                            (u as Leaf).visited = false;
                            tree.vertices.Remove(u as Leaf);
                            u = child;//prune it directly.
                        }
                        else
                        {
                            List<MarkerVertex> P, M, E;
                            (u as Node).ComputeStates(out P, out M, out E, exclude: child.rootMarkerVertex.opposite as MarkerVertex, excludeRootMarkerVertex: false);
                            bool perfect = false;
                            if (M.Count == 0)//condition 2 is satisfied
                            {
                                var Pset = new System.Collections.Generic.HashSet<MarkerVertex>(P);
                                var inCount = 0;
                                var neighborCount = 0;
                                (u as Node).ForEachNeighbor(child.rootMarkerVertex.opposite as MarkerVertex, (q) =>
                                    {
                                        ++neighborCount;
                                        if (Pset.Contains(q))
                                            ++inCount;
                                        return IterationFlag.Continue;
                                    });
                                //P == N_Gu(q) iff P.Count == inCount == neighborCount
                                if (P.Count == inCount && inCount == neighborCount)//now we say that child's rootMarkerVertex is perfect
                                {
                                    child.rootMarkerVertex.MarkAsPerfect();
                                    perfect = true;
                                }
                            }
                            if (perfect)
                            {
                                tree.RemoveSubTree(u as Node, exclude: child);
                                u = child;
                            }
                            else
                            {
                                //the tree edge uv is fully mixed. thus the subtree T' is fully mixed.
                                unique = false;
                                break;
                            }
                        }
                    }
                tree.root = u as GLTVertex;//let the tree root follow u after each iteration of the processing loop
            }
            #endregion
            #region Line 4
            if (!unique)
                return CaseIdentification_ResultType.FullyMixedSubTree;
            else
            {

                #region if (u is DegenerateNode)
                if (u is DegenerateNode)
                {
                    #region Lemma 5.6
                    var uDeg = u as DegenerateNode;
                    MarkerVertex q = null;
                    List<MarkerVertex> P, M, E;
                    //Note, we computed P(u), thus if q exists, its perfect flag is available
                    (uDeg).ComputeStates(out P, out M, out E);
                    //Debug.Assert(M.Count == 0, "Algorithm 5 Line 4, lemma 5.6 requires P(u) == NE(u)!");
                    if (M.Count == 0)
                    {
                        if (P.Count == 1 && uDeg.isStar && uDeg.center == P[0])//case 3
                        {
                            uDeg.ForEachMarkerVertex((v) =>
                                {
                                    if (v != uDeg.center)
                                    {
                                        q = v;
                                        return IterationFlag.Break;
                                    }
                                    return IterationFlag.Continue;
                                });
                        }
                        else if (P.Count == 2 && uDeg.isStar && P.Contains(uDeg.center))//case 4
                        {
                            if (uDeg.center == P[0])
                                q = P[1];
                            else q = P[0];
                        }
                        else
                        {
                            int count = P.Count + E.Count;//faster than the commented line below
                            //uDeg.ForEachMarkerVertex((v) => { ++count; return IterationFlag.Continue; });
                            if (count == P.Count)//case 1
                            {
                                if (uDeg.isClique)
                                    q = P[0];
                                else q = uDeg.center;
                            }
                            else if (P.Count == count - 1 && (uDeg.isClique || E[0] == uDeg.center))//case 2
                            {
                                //E[0]: V(u) \ P(u)
                                q = E[0];
                            }
                        }
                    }
                    #endregion
                    if (q != null)
                    {
                        e.u = q;
                        e.v = q.opposite;
                        q.opposite.MarkAsPerfect();
                        return CaseIdentification_ResultType.TreeEdge;
                    }
                    else
                    {
                        return CaseIdentification_ResultType.HybridNode;
                    }
                }
                #endregion
                #region else: prime node
                else//prime node
                {
                    var uPrime = u as PrimeNode;
                    MarkerVertex q = uPrime.lastMarkerVertex;//last leaf vertex in \sigma[G(u)]
                    MarkerVertex qPrime = (u as PrimeNode).universalMarkerVetex;//the universal marker (if any)

                    //Again cast the spell of Remark 5.5
                    List<MarkerVertex> P, M, E;
                    //Note, here we didn't exclude anything. Which means that, if a PP/PE edge is returned, perfect flags will be available.
                    uPrime.ComputeStates(out P, out M, out E);
                    //first, for q
                    if (M.Count == 0 || (M.Count == 1 && M[0] == q))//condition 2
                    {
                        var Pset = new System.Collections.Generic.HashSet<MarkerVertex>(P);
                        var inCount = 0;
                        var neighborCount = 0;
                        uPrime.ForEachNeighbor(q, (r) =>
                            {
                                ++neighborCount;
                                if (Pset.Contains(r))
                                    ++inCount;
                                return IterationFlag.Continue;
                            });
                        //P == N_Gu(q) iff P.Count == inCount == neighborCount, or P.Count == inCount+1 == neighborCount+1 and q in P
                        if ((P.Count == inCount && inCount == neighborCount)
                            ||
                            (P.Count == inCount + 1 && P.Count == neighborCount + 1 && Pset.Contains(q)))
                        {
                            e.u = q;
                            e.v = q.opposite;
                            q.opposite.MarkAsPerfect();
                            return CaseIdentification_ResultType.TreeEdge;
                        }
                    }
                    //then for q'
                    if (qPrime != null)
                    {
                        if (M.Count == 0 || (M.Count == 1 && M[0] == qPrime))//condition 2
                        {
                            var Pset = new System.Collections.Generic.HashSet<MarkerVertex>(P);
                            var inCount = 0;
                            var neighborCount = 0;
                            uPrime.ForEachNeighbor(qPrime, (r) =>
                                {
                                    ++neighborCount;
                                    if (Pset.Contains(r))
                                        ++inCount;
                                    return IterationFlag.Continue;
                                });
                            //P == N_Gu(q) iff P.Count == inCount == neighborCount, or P.Count == inCount+1 == neighborCount+1 and q in P
                            if ((P.Count == inCount && inCount == neighborCount)
                                ||
                                (P.Count == inCount + 1 && P.Count == neighborCount + 1 && Pset.Contains(qPrime)))
                            {
                                e.u = qPrime;
                                e.v = qPrime.opposite;
                                qPrime.opposite.MarkAsPerfect();
                                return CaseIdentification_ResultType.TreeEdge;
                            }
                        }
                    }
                    //else, just return u
                    return CaseIdentification_ResultType.HybridNode;
                }
                #endregion
            }
            #endregion
        }
        #endregion
    }
}