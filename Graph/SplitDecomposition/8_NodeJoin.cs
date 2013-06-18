using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public partial class Graph
    {        #region Algorithm 8
        //uPrime is the child of u
        //the result of the join is returned.
        private PrimeNode NodeJoin(SplitTree ST, Node u, Node uPrime)
        {
            MarkerVertex qPrime = uPrime.rootMarkerVertex;
            MarkerVertex q = qPrime.opposite as MarkerVertex;
            PrimeNode ret = null;
            List<GLTVertex> uPrimeChildren = new List<GLTVertex>();

            if (u is DegenerateNode)
            {
                ret = (u as DegenerateNode).ConvertToPrime();
                //u.parentLink = ret;
                //u.unionFind_parent = null;
                u.active = true;
            }
            else
                ret = u as PrimeNode;
            var representative = ret.childSetRepresentative;
            if (uPrime is DegenerateNode && (uPrime as DegenerateNode).isStar && qPrime != (uPrime as DegenerateNode).center)//qPrime has degree 1, not center.
            {
                var center = (uPrime as DegenerateNode).center;
                var centerOpposite = center.GetOppositeGLTVertex();
                //Algorithm 8 says q now represents the center of u', so we have to update the center's opposite..
                if (centerOpposite is Node)
                {
                    (centerOpposite as Node).rootMarkerVertex.opposite = q;
                }
                else
                    (centerOpposite as Leaf).opposite = q;
                uPrimeChildren.Add(centerOpposite);
                //And also copy the center's perfect state, and opposite.
                q.perfect = center.perfect;
                q.opposite = center.opposite;
                (uPrime as DegenerateNode).ForEachMarkerVertex((v) =>
                    {
                        if (v != center && v != qPrime)
                        {
                            ret.AddMarkerVertex(v, new HashSet<MarkerVertex> { q });
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
                                HashSet<MarkerVertex> nSet = new HashSet<MarkerVertex>(qNeighbor);
                                uPrime.ForEachNeighbor(v, (w) =>
                                    {
                                        if (w != qPrime)
                                            nSet.Add(w);
                                        return IterationFlag.Continue;
                                    });
                                ret.AddMarkerVertex(v, nSet);
                            }
                            else
                            {
                                HashSet<MarkerVertex> nSet = new HashSet<MarkerVertex>();
                                uPrime.ForEachNeighbor(v, (w) =>
                                    {
                                        if (w != qPrime)
                                            nSet.Add(w);
                                        return IterationFlag.Continue;
                                    });
                                ret.AddMarkerVertex(v, nSet);
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
            uPrime.active = true;
            if (uPrime == representative)
            {
                uPrime.parentLink = ret;//update the parentLink to the new prime node
                //don't touch the unoinFind_parent field. leave it pointing to uPrime itself.
            }
            else//uPrime is not a fake node.
            {
                uPrime.parentLink = ret;
                //uPrime.unionFind_parent = null;
            }
            return ret;
        }
        #endregion
    }
}