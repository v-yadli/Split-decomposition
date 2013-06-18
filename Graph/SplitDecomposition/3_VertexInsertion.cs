using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {        #region Algorithm 3
        private void VertexInsertion(SplitTree ST, List<int> sigma, int idx)
        {
            #region Bootstrapping
            if (ST.vertices.Count == 0)//initialization
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null,
                };
                ST.AddLeaf(v);
                ST.root = v;
            }
            else if (ST.vertices.Count == 1)//only the root, thus we cache the second vertex
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null,
                };
                ST.AddLeaf(v);
            }
            else if (ST.vertices.Count == 2)//now we're building the first trinity
            {
                Leaf v = new Leaf
                {
                    id = sigma[idx],
                    parent = null
                };
                ST.AddLeaf(v);
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
                var returnType = SplitTree_CaseIdentification(ST, sigma, idx, out e, out u, out tPrime);
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
                        //ST.Debug(false);
                        var contractionNode = Contraction(ST, tPrime, sigma[idx]);
                        break;
                }
            }
        }

        #endregion
    }
}
