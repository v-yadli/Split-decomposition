using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using C5;
using System.Diagnostics;

namespace GraphCompression
{
    class LexTupleComparor : IComparer<Tuple<List<int>, int>>
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
    partial class Graph
    {
        #region Algorithm 1
        public SplitTree SplitDecomposition()
        {
            var ST = new SplitTree();
            var sigma = LexBFS();
            int[] tmp = new int[sigma.Count];
            for (int i = 0; i < sigma.Count; ++i)
                tmp[sigma[i]] = i;
            var nodeOrder = tmp.ToList();
            for (int i = 0, n = NodeCount; i < n; ++i)
            {
                VertexInsertion(ST, sigma, nodeOrder, i);
                if (i >= 2)
                    ST.Debug(false);
            }
            return ST;
        }
        #endregion
        #region Algorithm 2
        private List<int> LexBFS()
        {
            int n = NodeCount;
            List<int> ret = new List<int>(n);
            List<List<int>> labels = new List<List<int>>(n);
            List<IPriorityQueueHandle<Tuple<List<int>, int>>> handles = new List<IPriorityQueueHandle<Tuple<List<int>, int>>>(n);
            C5.IntervalHeap<Tuple<List<int>, int>> lexHeap = new IntervalHeap<Tuple<List<int>, int>>(n, new LexTupleComparor());
            for (int i = 0; i < n; ++i)
            {
                labels.Add(new List<int>());
                IPriorityQueueHandle<Tuple<List<int>, int>> handle = null;
                lexHeap.Add(ref handle, Tuple.Create(labels[i], i));
                handles.Add(handle);
            }
            for (int i = 1; i <= n; ++i)
            {
                var node = lexHeap.DeleteMax();
                handles[node.Item2] = null;
                ret.Add(node.Item2);
                ForEachNeighbor(node.Item2, (neighbor) =>
                    {
                        if (handles[neighbor] == null) return;
                        labels[neighbor].Add(n - i + 1);
                        var newTuple = Tuple.Create(labels[neighbor], neighbor);
                        lexHeap.Replace(handles[neighbor], newTuple);
                    });
            }
            //lexHeap.Replace
            return ret;
        }
        #endregion // Algorithm 2
        #region Algorithm 3
        private void VertexInsertion(SplitTree ST, List<int> sigma, List<int> nodeOrder, int idx)
        {
            #region Bootstrapping
            if (ST.vertices.Count == 0)//initialization
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null,
                };
                ST.vertices.Add(v);
                ST.root = v;
            }
            else if (ST.vertices.Count == 1)//only the root, thus we cache the second vertex
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null,
                };
                ST.vertices.Add(v);
            }
            else if (ST.vertices.Count == 2)//now we're building the first trinity
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null
                };
                ST.vertices.Add(v);
                int missingConnection = -1; //test the missing connection between the cached 3 leave
                if (!storage[(ST.vertices[0] as Leaf).id].Contains((ST.vertices[1] as Leaf).id))
                {
                    missingConnection = 0;
                }
                else if (!storage[(ST.vertices[0] as Leaf).id].Contains((ST.vertices[2] as Leaf).id))
                {
                    missingConnection = 1;
                }
                else if (!storage[(ST.vertices[1] as Leaf).id].Contains((ST.vertices[2] as Leaf).id))
                {
                    missingConnection = 2;
                }
                MarkerVertex v1 = new MarkerVertex()
                {
                    opposite = ST.vertices[0]
                };
                MarkerVertex v2 = new MarkerVertex()
                {
                    opposite = ST.vertices[1]
                };
                MarkerVertex v3 = new MarkerVertex()
                {
                    opposite = ST.vertices[2]
                };
                (ST.vertices[0] as Leaf).opposite = v1;
                (ST.vertices[1] as Leaf).opposite = v2;
                (ST.vertices[2] as Leaf).opposite = v3;
                MarkerVertex center = null;
                switch (missingConnection)
                {
                    case 0:
                        center = v3;
                        break;
                    case 1:
                        center = v2;
                        break;
                    case 2:
                        center = v1;
                        break;
                    default: break;
                }
                var deg = new DegenerateNode()
                {
                    parent = ST.vertices[0],
                    Vu = new List<MarkerVertex> { v1, v2, v3 },
                    center = center,
                    rootMarkerVertex = v1
                };
                v1.node = deg;
                ST.vertices[1].parent = ST.vertices[2].parent = deg;
                ST.vertices.Add(deg);
                ST.lastVertex = ST.vertices[2] as Leaf;
            }
            #endregion
            else
            {
                TreeEdge e; Vertex u; SplitTree tPrime;
                var returnType = SplitTree_CaseIdentification(ST, sigma, nodeOrder, idx, out e, out u, out tPrime);
                switch (returnType)
                {
                    case CaseIdentification_ResultType.SingleLeaf:
                        //This case is not discussed in paper. However, if there's only a single leaf in neighbor set,
                        //then a unique PE edge can be found.
                        //Applying proposition 4.17, case 6
                        e.u = (u as Leaf).opposite;
                        e.v = u;
                        ST.SplitEdgeToStar(e, sigma[idx]);
                        break;
                    case CaseIdentification_ResultType.TreeEdge://PP or PE
                        {
                            bool unique = true;
                            bool pp;
                            //testing uniqueness, page 24
                            //check whether pp or pe
                            if (!e.u.Perfect())
                            {
                                var tmp = e.u;
                                e.u = e.v;
                                e.v = tmp;
                            }
                            pp = e.v.Perfect();
                            var u_GLT = e.u_GLT;
                            var v_GLT = e.v_GLT;
                            DegenerateNode degNode = null;
                            if (u_GLT is DegenerateNode)
                                degNode = u_GLT as DegenerateNode;
                            if (v_GLT is DegenerateNode)
                                degNode = v_GLT as DegenerateNode;
                            if (degNode != null)//attached to a clique or a star
                            {
                                if ((pp && degNode.isClique) || (!pp/*pe*/ && degNode.isStar && (e.u == degNode.center || degNode.Degree(e.v as MarkerVertex) == 1)))
                                {
                                    unique = false;
                                }
                            }
                            if (unique)
                            {
                                //Proposition 4.17 case 5 or 6

                                if (pp)//PP
                                {
                                    ST.SplitEdgeToClique(e, sigma[idx]);
                                }
                                else//PE
                                {
                                    ST.SplitEdgeToStar(e, sigma[idx]);
                                }
                            }
                            else
                            {
                                //Proposition 4.15, case 1 or 2
                                var deg = u_GLT;
                                if (v_GLT is DegenerateNode)
                                    deg = v_GLT;
                                ST.AttachToDegenerateNode(deg as DegenerateNode, sigma[idx]);
                            }
                        }
                        break;
                    case CaseIdentification_ResultType.HybridNode:
                        if (u is DegenerateNode)
                        {
                            //Proposition 4.16
                            var uDeg = u as DegenerateNode;
                            System.Collections.Generic.HashSet<MarkerVertex> PStar = new System.Collections.Generic.HashSet<MarkerVertex>();
                            System.Collections.Generic.HashSet<MarkerVertex> EStar = new System.Collections.Generic.HashSet<MarkerVertex>();
                            uDeg.ForEachMarkerVertex((v) =>
                                {
                                    if (v.perfect && v != uDeg.center)
                                        PStar.Add(v);
                                    else
                                        EStar.Add(v);
                                    return IterationFlag.Continue;
                                });
                            //before we split, determine the new perfect states for the two new markers to be generated
                            bool pp = false;
                            if (uDeg.isStar && uDeg.center.perfect)
                            {
                                pp = true;//see figure 7. pp==true iff star and center is perfect.
                            }
                            var newNode = SplitNode(uDeg, PStar, EStar);
                            ST.vertices.Add(newNode);
                            //e.u \in PStar ; e.v \in EStar (thus containing the original center, if star)
                            //PStar in uDeg; EStar in newNode
                            if (newNode.parent == uDeg)
                            {
                                e.u = newNode.rootMarkerVertex.opposite;
                                e.v = newNode.rootMarkerVertex;
                            }
                            else
                            {
                                e.u = uDeg.rootMarkerVertex;
                                e.v = uDeg.rootMarkerVertex.opposite;
                            }
                            //assign perfect state values
                            if (pp)
                            {
                                e.u.MarkAsPerfect();
                                e.v.MarkAsPerfect();
                            }
                            else//PE, and PStar part always has an empty state and EStar part has perfect.
                            {
                                e.u.MarkAsEmpty();
                                e.v.MarkAsPerfect();
                            }
                            //check whether pp or pe
                            if (!e.u.Perfect())
                            {
                                var tmp = e.u;
                                e.u = e.v;
                                e.v = tmp;
                            }
                            if (e.v.Perfect())//PP
                            {
                                ST.SplitEdgeToClique(e, sigma[idx]);
                            }
                            else//PE
                            {
                                ST.SplitEdgeToStar(e, sigma[idx]);
                            }
                        }
                        else
                        {
                            //Proposition 4.15, case 3
                            ST.AttachToPrimeNode(u as PrimeNode, sigma[idx]);
                        }
                        break;
                    case CaseIdentification_ResultType.FullyMixedSubTree:
                        //Proposition 4.20
                        Cleaning(ST, tPrime);
                        ST.Debug(false);
                        var contractionNode = Contraction(ST, tPrime, sigma[idx]);
                        break;
                }
            }
        }

        #endregion
        #region Algorithm 4
        //XXX this algorithm may terminate when there's still an active vertex.
        private SplitTree SmallestSpanningTree(SplitTree ST, int x)
        {
            SplitTree ret = new SplitTree();//Here we don't need the associated graph, just a tree.
            if (ST.vertices.Count == 0)
                return ret;
            //Compute N(x). Note that the initial set L is also Nx
            //PerfectFlags are reset in Algorithm 5, which calls this one.
            //TODO XXX: we haven't set the opposite non-root marker vertices of N(x) to perfect!
            Queue<GLTVertex> L = new Queue<GLTVertex>();
            ST.ResetActiveFlags();
            ST.ResetNeighborFlags();
            var originalNx = storage[x];
            bool rootVisited = false;
            foreach (var v in ST.vertices)
                if (v is Leaf && originalNx.Contains((v as Leaf).id))// v \in N(x)
                {
                    v.active = true;//marking a vertex with active means that it is currently enqueued
                    (v as Leaf).neighbor = true;
                    //set the non-root marker vertices opposite leaves of N(x) to perfect
                    var opposite = (v as Leaf).opposite as MarkerVertex;
                    if (opposite.node == null)//non-root
                    {
                        opposite.perfect = true;
                    }
                    L.Enqueue(v);
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
                if (u.parent == null)
                    rootVisited = true;
                else if (!u.parent.visited && !u.parent.active)
                {
                    u.parent.active = true;
                    L.Enqueue(u.parent);
                }
            }
            if (ret.vertices.Count == 0)
                return ret;
            //adjust the root
            GLTVertex root = ret.vertices[0];
            while (root.parent != null && root.parent.visited)
                root = root.parent;
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
        #region Algorithm 5
        public enum CaseIdentification_ResultType
        {
            TreeEdge,
            HybridNode,
            FullyMixedSubTree,
            SingleLeaf
        }

        public CaseIdentification_ResultType SplitTree_CaseIdentification
            (SplitTree ST, List<int> sigma, List<int> nodeOrder, int idx, out TreeEdge e, out Vertex u, out SplitTree tree)
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
                        }, subtree: true);
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
                    //also, M should be empty. Thus case 2 automatically satisfied.
                    Debug.Assert(M.Count == 0, "A node with all leaf children has a mixed marker vertex!!");
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
                    if (P.Count == inCount && inCount == neighborCount)//now we say that rootMarkerVertex'es opposite vertex is perfect
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
            //XXX how to deal with this?
            //Debug.Assert(!(u is Leaf), "the root of the subtree is a leaf!! not specified in paper!!!");
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
                            //note that the parent of u(if any) gets pruned if and only if u's root marker vertex is perfect(thus the subtree containing its parent is perfect)
                            //which means that we don't have to compute its state again XXX this is problematic. excludeRootMarkerVertex disabled for now
                            List<MarkerVertex> P, M, E;
                            (u as Node).ComputeStates(out P, out M, out E, exclude: child.rootMarkerVertex.opposite as MarkerVertex, excludeRootMarkerVertex: false);
                            //P.Add((u as Node).rootMarkerVertex);//for sure.
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
                    Debug.Assert(M.Count == 0, "Algorithm 5 Line 4, lemma 5.6 requires P(u) == NE(u)!");
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
                    MarkerVertex q = ST.lastVertex.opposite;//the opposite marker of last leaf vertex in \sigma[G(u)]
                    MarkerVertex qPrime = (u as PrimeNode).universalMarkerVetex;

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
        #region Algorithm 6
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
                newNode.rootMarkerVertex.node = newNode;
                newNode.parent = node.parent;
                node.parent = newNode;
                node.rootMarkerVertex = v1;
                v1.node = node;
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
                        ST.vertices.Add(SplitNode(deg, B, A));//In this way, the structure of T' is unchanged as the newly forked node contains P*(u)
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
        #region Algorithm 8
        //uPrime is the child of u
        //the result of the join is returned.
        //a special trick: when a node is removed from ST, its parentlink is set to "dummy", and unionFind_parent is set to the new node if it's a conversion, or null if it's a removed child; also, if fake, unionFind_parent is set to fakeDummy and will be altered back to itself later.
        //The reason that we don't use parentLink for fake node marking is that parentLink will contain the _precious_ parent information
        private PrimeNode NodeJoin(SplitTree ST, Node u, Node uPrime, Node deleteDummy, Node fakeDummy)
        {
            MarkerVertex qPrime = uPrime.rootMarkerVertex;
            MarkerVertex q = qPrime.opposite as MarkerVertex;
            PrimeNode ret = null;
            List<GLTVertex> uPrimeChildren = new List<GLTVertex>();

            if (u is DegenerateNode)
            {
                ret = (u as DegenerateNode).ConvertToPrime();
                u.parentLink = deleteDummy;
                u.unionFind_parent = ret;
            }
            else
                ret = u as PrimeNode;
            var representative = ret.childSetRepresentative;//XXX very important. Must be put before copying center's state into q's state, since q may be the first non-marker vertex in u and our child representative accessing routine grabs q and use its opposite to call Find(), whereas the new opposite (an opposite of the center of the child) is not yet unioned
            if (uPrime is DegenerateNode && (uPrime as DegenerateNode).isStar && qPrime != (uPrime as DegenerateNode).center)//qPrime has degree 1, not center.
            {
                var center = (uPrime as DegenerateNode).center;
                var centerOpposite = center.GetOppositeGLTVertex();
                //Algorithm 8 says q now represents the center of u', so we have to update the center's opposite..
                if (centerOpposite is Node)
                {
                    (centerOpposite as Node).rootMarkerVertex.opposite = q;
                }
                uPrimeChildren.Add(centerOpposite);
                //And also copy the center's perfect state, and opposite.
                q.perfect = center.perfect;
                q.opposite = center.opposite;
                (uPrime as DegenerateNode).ForEachMarkerVertex((v) =>
                    {
                        if (v != center && v != qPrime)
                        {
                            ret.AddMarkerVertex(Tuple.Create(v, new List<MarkerVertex> { q }));
                            uPrimeChildren.Add(v.GetOppositeGLTVertex());
                        }
                        return IterationFlag.Continue;
                    });
            }
            else
            {
                List<MarkerVertex> qNeighbor = new List<MarkerVertex>();
                ret.ForEachNeighbor(q, (v) =>
                    {
                        qNeighbor.Add(v);
                        return IterationFlag.Continue;
                    });
                ret.RemoveMarkerVertex(q);
                var qPrimeNeighbor = new System.Collections.Generic.HashSet<MarkerVertex>();
                uPrime.ForEachNeighbor(qPrime, (v) =>
                    {
                        qPrimeNeighbor.Add(v);
                        return IterationFlag.Continue;
                    });
                uPrime.ForEachMarkerVertex((v) =>
                    {
                        if (v != qPrime)
                        {
                            if (qPrimeNeighbor.Contains(v))
                            {
                                List<MarkerVertex> nSet = new List<MarkerVertex>(qNeighbor);
                                uPrime.ForEachNeighbor(v, (w) =>
                                    {
                                        if (w != qPrime)
                                            nSet.Add(w);
                                        return IterationFlag.Continue;
                                    });
                                ret.AddMarkerVertex(Tuple.Create(v, nSet));
                            }
                            else
                            {
                                List<MarkerVertex> nSet = new List<MarkerVertex>();
                                uPrime.ForEachNeighbor(v, (w) =>
                                    {
                                        if (w != qPrime)
                                            nSet.Add(w);
                                        return IterationFlag.Continue;
                                    });
                                ret.AddMarkerVertex(Tuple.Create(v, nSet));
                            }
                        }
                        return IterationFlag.Continue;
                    });
                if (uPrime is PrimeNode)
                {
                    uPrimeChildren.Add((uPrime as PrimeNode).childSetRepresentative);
                }
                else
                {
                    uPrime.ForEachChild(v =>
                        {
                            uPrimeChildren.Add(v);
                            return IterationFlag.Continue;
                        }, false);
                }
            }

            //union the children of uPrime

            for (int i = 0, n = uPrimeChildren.Count; i < n; ++i)
            {
                uPrimeChildren[i].parentLink = null;
                uPrimeChildren[i].unionFind_parent = representative;
            }

            //set deletion flag for uPrime, or if it's the child representative, make it a fake node
            if (uPrime == representative)
            {
                uPrime.parentLink = ret;//update the parentLink to the new prime node
                uPrime.unionFind_parent = fakeDummy;
            }
            else
            {
                uPrime.parentLink = deleteDummy;
                uPrime.unionFind_parent = null;
            }
            return ret;
        }
        #endregion
        #region Algorithm 9
        private PrimeNode Contraction(SplitTree ST, SplitTree tPrime, int xId)
        {
            DegenerateNode deleteDummy = new DegenerateNode();//when a node gets removed, mark it by setting parentlink to deleteDummy
            DegenerateNode fakeDummy = new DegenerateNode();//when a GLTVertex turns to a 'fake' one (used only as child representative), mark it by setting parentlink to fakeDummy
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
                else if (n != null && n.Degree(n.rootMarkerVertex) == 1)
                    Phase2List.Add(n);
            }
            #endregion
            #region Phase 1 node-joins
            foreach (var star in Phase1List)
            {
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
                    phase3 = NodeJoin(ST, phase3, c, deleteDummy, fakeDummy);
                    //c.parentLink = dummy;
                }
                phase3.visited = true;//add the joint node back into T'
            }
            #endregion
            #region Phase 2 node-joins
            foreach (var node in Phase2List)
            {
                if (node.parentLink == deleteDummy || node.unionFind_parent == fakeDummy)//this node has been joint with the parent
                    continue;
                phase3 = node;
                var p = phase3.parent;
                if (p.visited && p is Node)
                {
                    phase3 = NodeJoin(ST, p as Node, phase3, deleteDummy, fakeDummy);//make sure phase3 now points to the new parent(which is prime)
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
                        phase3 = NodeJoin(ST, phase3, c, deleteDummy, fakeDummy);
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
            List<GLTVertex> newSTList = new List<GLTVertex>();
            foreach (var vertex in ST.vertices)
            {
                if (vertex.parentLink != deleteDummy && vertex.unionFind_parent != fakeDummy)//neither deleted nor fake
                    newSTList.Add(vertex);
                else if (vertex.parentLink == deleteDummy && vertex.unionFind_parent != null)//replaced
                    newSTList.Add(vertex.unionFind_parent);
                else if (vertex.unionFind_parent == fakeDummy)
                    vertex.unionFind_parent = vertex;//point the unionFind_parent back
            }
            ST.vertices = newSTList;

            List<MarkerVertex> Pset = new List<MarkerVertex>();
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
            (phase3 as PrimeNode).AddMarkerVertex(Tuple.Create(newMarker, Pset));
            ST.vertices.Add(newLeaf);//Add the new leaf to the new ST
            ST.lastVertex = newLeaf;
            return phase3 as PrimeNode;
        }
        #endregion
    }
}