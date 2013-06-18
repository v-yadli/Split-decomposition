using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    class PrimeNode : Node
    {
        public Dictionary<MarkerVertex, HashSet<MarkerVertex>> Gu;
        //public MarkerVertex lastMarkerVertex;// the pointer to the last marker vertex in \sigma(G(u))
        public MarkerVertex universalMarkerVetex;// there will be at most 1 universal marker vertex, as otherwise there will be a non-trivial split
        public MarkerVertex lastMarkerVertex;//will be updated by Algorithm 9, or AttachToPrimeNode
        internal override GLTVertex Clone()
        {
            PrimeNode newPrimeNode = new PrimeNode()
            {
                activeTimestamp = activeTimestamp,
                Gu = Gu,
                parentLink = parentLink,
                unionFind_parent = unionFind_parent,
                universalMarkerVetex = universalMarkerVetex,
                visitedTimestamp = visitedTimestamp,
                rootMarkerVertex = rootMarkerVertex,
            };
            rootMarkerVertex.node = newPrimeNode;
            var representative = childSetRepresentative;
            representative.parentLink = newPrimeNode;//Thankfully we can make it in O(1)
            return newPrimeNode;
        }
        public GLTVertex childSetRepresentative
        {
            get
            {
                return Gu.First((v) => v.Key != rootMarkerVertex).Key.GetOppositeGLTVertex().Find();
            }
        }
        public override int Degree(MarkerVertex v)
        {
            return Gu[v].Count;
        }
        public override void ForEachNeighbor(MarkerVertex source, Func<MarkerVertex, IterationFlag> action)
        {
            //iterate
            foreach (var v in Gu[source])
            {
                if (action(v) == IterationFlag.Break)
                    break;
            }
        }
        public override void ForEachChild(Func<GLTVertex, IterationFlag> action, bool subtree)
        {
            foreach (var v in Gu)
            {
                if (v.Key == rootMarkerVertex)
                    continue;
                GLTVertex child = v.Key.GetOppositeGLTVertex();
                if (!subtree || child.visited)
                    if (action(child) == IterationFlag.Break)
                        break;
            }
        }
        public override void ForEachMarkerVertex(Func<MarkerVertex, IterationFlag> action)
        {
            foreach (var t in Gu)
                if (action(t.Key) == IterationFlag.Break)
                    break;
        }

        //the parameter: the new marker vertex, and a list of its neighbors
        internal void AddMarkerVertex(MarkerVertex u, HashSet<MarkerVertex> neighbors)
        {
            int newUniversalCount = Gu.Count;
            universalMarkerVetex = null;//re-calculate
            foreach (var v in neighbors)
            {
                HashSet<MarkerVertex> set = null;
                if (Gu.TryGetValue(v, out set))
                {
                    set.Add(u);
                    if (set.Count == newUniversalCount)
                        universalMarkerVetex = v;
                }
            }
            Gu[u] = neighbors;

            if (neighbors.Count == newUniversalCount)
                universalMarkerVetex = u;
        }

        internal void RemoveMarkerVertex(MarkerVertex q)
        {
            if (q == universalMarkerVetex)
                universalMarkerVetex = null;
            int universalCount = Gu.Count - 2;
            var neighbors = Gu[q];
            foreach (var v in neighbors)
            {
                var set = Gu[v];
                set.Remove(q);
                if (set.Count == universalCount)
                    universalMarkerVetex = v;
            }
            Gu.Remove(q);
        }
    }
}
