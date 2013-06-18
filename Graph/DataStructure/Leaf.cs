using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public class Leaf : GLTVertex
    {
        public int id;//id establishes the association between the leaf and a node in the accessibliity graph
        public MarkerVertex opposite;
        private int neighborTimestamp = 0;
        private int perfectTimestamp = 0;
        private static int neighborTime = 0;
        public static void resetNeighborFlags()
        {
            ++neighborTime;
        }
        public bool neighbor //will be true if current leaf \in N(x)
        {
            get { return neighborTime == neighborTimestamp; }
            set { if (value)neighborTimestamp = neighborTime; else neighborTimestamp = -1; }
        }
        public bool perfect
        {
            get
            {
                return perfectTimestamp == MarkerVertex.perfectTime;
            }
            set
            {
                if (value)
                {
                    perfectTimestamp = MarkerVertex.perfectTime;
                }
                else
                {
                    perfectTimestamp = -1;//manually mark to -1 to avoid collision.
                }
            }
        }
        //public override int numberOfChildren
        //{
        //    get
        //    {
        //        if (this.parent == null)//I'm the root
        //            return 1;//That's for sure.
        //        else return 0;
        //    }
        //}
        internal override GLTVertex Clone()
        {
            Leaf newLeaf = new Leaf
            {
                id = id,
                neighborTimestamp = neighborTimestamp,
                opposite = opposite,
                parentLink = parentLink,
                perfectTimestamp = perfectTimestamp,
                unionFind_parent = unionFind_parent,
                visitedTimestamp = visitedTimestamp,
                activeTimestamp = activeTimestamp,
            };
            newLeaf.opposite.opposite = newLeaf;
            
            return newLeaf;
        }
        internal List<Leaf> GetAccessableLeaves()
        {
            List<Leaf> accessableLeaves = new List<Leaf>();
            Queue<MarkerVertex> q = new Queue<MarkerVertex>();
            //q only contains marker vertices of internal nodes
            q.Enqueue(this.opposite as MarkerVertex);
            while (q.Count != 0)
            {
                var m = q.Dequeue();
                var n = m.GetGLTVertex() as Node;
                n.ForEachNeighbor(m, (v) =>
                    {
                        if (v.opposite is Leaf)
                            accessableLeaves.Add(v.opposite as Leaf);
                        else
                            q.Enqueue(v.opposite as MarkerVertex);
                        return IterationFlag.Continue;
                    });
            }
            return accessableLeaves;
        }
    }
}
