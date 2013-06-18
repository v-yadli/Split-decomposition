using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    class DegenerateNode : Node
    {
        public List<MarkerVertex> Vu;
        public MarkerVertex center;//will be null if this is a clique node.
        public bool isClique { get { return center == null; } }
        public bool isStar { get { return center != null; } }
        public override void ForEachNeighbor(MarkerVertex source, Func<MarkerVertex, IterationFlag> action)
        {
            if (center == null || source == center)
            {
                foreach (var v in Vu)
                {
                    if (v == source)
                        continue;
                    if (action(v) == IterationFlag.Break)
                        break;
                }
            }
            else
            {
                action(center);
            }
        }
        internal override GLTVertex Clone()
        {
            DegenerateNode newDegNode = new DegenerateNode()
            {
                activeTimestamp = activeTimestamp,
                center = center,
                parentLink = parentLink,
                rootMarkerVertex = rootMarkerVertex,
                unionFind_parent = unionFind_parent,
                visitedTimestamp = visitedTimestamp,
                Vu = Vu,
            };
            rootMarkerVertex.node = newDegNode;
            ForEachChild((v) =>
                {
                    v.parentLink = newDegNode;
                    v.unionFind_parent = null;
                    return IterationFlag.Continue;
                }, false);
            return newDegNode;
        }
        public override int Degree(MarkerVertex v)
        {
            if (!Vu.Contains(v))
                return 0;
            if (isStar)
            {
                return (v == center) ? Vu.Count - 1 : 1;
            }
            return Vu.Count - 1;
        }
        public void RemoveMarkerVertices(HashSet<MarkerVertex> set)
        {
            List<MarkerVertex> newVu = new List<MarkerVertex>();
            foreach (var v in Vu)
                if (!set.Contains(v))
                    newVu.Add(v);
            Vu = newVu;
        }
        public override void ForEachChild(Func<GLTVertex, IterationFlag> action, bool subtree)
        {
            foreach (var v in Vu)
            {
                if (v == rootMarkerVertex)
                    continue;
                GLTVertex child = v.GetOppositeGLTVertex();
                if (!subtree || child.visited)
                    if (action(child) == IterationFlag.Break)
                        break;
            }
        }
        public override void ForEachMarkerVertex(Func<MarkerVertex, IterationFlag> action)
        {
            foreach (var v in Vu)
                if (action(v) == IterationFlag.Break) break;
        }

        internal PrimeNode ConvertToPrime()
        {
            PrimeNode ret = new PrimeNode()
            {
                rootMarkerVertex = rootMarkerVertex,
                visited = visited,
                parentLink = parentLink,
                unionFind_parent = unionFind_parent,
                universalMarkerVetex = null,//really not determined right now..
                Gu = new Dictionary<MarkerVertex,HashSet<MarkerVertex>>()
            };
            rootMarkerVertex.node = ret;
            GLTVertex rep = null;
            foreach (var v in Vu)
            {
                if (v == rootMarkerVertex)
                    continue;
                if (rep == null)
                {
                    rep = v.GetOppositeGLTVertex();
                    rep.parentLink = ret;
                    rep.unionFind_parent = rep;
                }
                else
                {
                    var o = v.GetOppositeGLTVertex();
                    o.parentLink = null;
                    o.unionFind_parent = rep;
                }
            }
            if (isClique)
            {
                foreach (var v in Vu)
                {
                    HashSet<MarkerVertex> n = new HashSet<MarkerVertex>();
                    foreach (var w in Vu)
                    {
                        if (w == v)
                            continue;
                        n.Add(w);
                    }
                    ret.Gu.Add(v, n);
                }
            }
            else
            {
                foreach (var v in Vu)
                {
                    if (v == center)
                    {
                        HashSet<MarkerVertex> n = new HashSet<MarkerVertex>();
                        foreach (var w in Vu)
                        {
                            if (w == v)
                                continue;
                            n.Add(w);
                        }
                        ret.Gu.Add(v, n);
                    }
                    else
                    {
                        ret.Gu.Add(v, new HashSet<MarkerVertex> { center });
                    }
                }
            }
            return ret;
        }
    }
}
