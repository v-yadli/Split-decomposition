using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GraphCompression
{
    public class SplitTree
    {
        #region Decomposition algorithm related
        public SplitTree()
        {
            vertices = new List<GLTVertex>();
            root = null;
            LeafMapper = new Dictionary<int, Leaf>();
        }
        public void Debug(bool subtree)
        {
            Console.Clear();
            for (int i = 0; i < vertices.Count; ++i)
            {
                vertices[i].Print(0, subtree);
            }
        }
        public List<GLTVertex> vertices;//this list is the storage for the tree vertices, while the tree topology is encoded in the vertices themselves.
        public Dictionary<int, Leaf> LeafMapper;
        public GLTVertex root;
        internal Leaf lastVertex;
        public void ResetVisitFlags()
        {
            GLTVertex.ResetVisitedFlags();
        }
        public void ResetNeighborFlags()
        {
            Leaf.resetNeighborFlags();
        }
        /// <summary>
        /// Remove a subtree from the current subtree, rooted at node
        /// </summary>
        public void RemoveSubTree(GLTVertex node, GLTVertex exclude = null)
        {
            node.visited = false;
            vertices.Remove(node);
            if (node == root)//the entire subtree is destroyed..
            {
                root = exclude;
            }
            if (node is Node)
            {
                (node as Node).ForEachChild((u) =>
                    {
                        if (u == exclude)
                            return IterationFlag.Continue;
                        RemoveSubTree(u);
                        return IterationFlag.Continue;
                    }, subtree: true);
            }
        }

        internal void ResetActiveFlags()
        {
            GLTVertex.ResetActiveFlags();
        }

        internal void AddLeaf(Leaf x)
        {
            vertices.Add(x);
            LeafMapper[x.id] = x;
            lastVertex = x;
        }

        //Proposition 4.17
        internal void SplitEdgeToClique(TreeEdge e, int xId)
        {
            //Assume e is P-P
            GLTVertex oParent = e.u.GetGLTVertex();
            GLTVertex oChild = e.v.GetGLTVertex();
            bool uIsParent = true;

            //test and swap
            if (oChild.parent != oParent)
            {
                var tmp = oParent;
                oParent = oChild;
                oChild = tmp;
                uIsParent = false;
            }

            ////Fixing multiple-clique forking problem
            //if (oChild is DegenerateNode && (oChild as DegenerateNode).isClique)
            //{
            //    //voila, we can merge the two!
            //    AttachToDegenerateNode(oChild as DegenerateNode, xId);
            //    return;
            //}

            MarkerVertex v1 = new MarkerVertex();
            MarkerVertex v2 = new MarkerVertex();
            MarkerVertex v3 = new MarkerVertex();


            var deg = new DegenerateNode()
            {
                active = false,
                center = null,
                parent = oParent,
                rootMarkerVertex = uIsParent ? v1 : v2,
                visited = false,
                Vu = new List<MarkerVertex> { v1, v2, v3 }
            };
            if (uIsParent)
                v1.node = deg;
            else v2.node = deg;
            deg.parent = oParent;//no matter what case (oParent is Prime or not), parentLink & unionFind_parent are hooked up correctly.
            if (oParent is PrimeNode == false)
            {
                oChild.parent = deg;

            }
            else
            {
                //(oParent as PrimeNode).ForEachChild(c =>
                //    {
                //        if (c.unionFind_parent == oChild)
                //            Console.WriteLine("!!!");
                //        return IterationFlag.Continue;
                //    }, false);
                //oChild.parent = deg;
                var newChild = oChild.Clone();//since oChild is potentially pointed by others' unionFind_parent, we have to clone.
                if (newChild is Leaf)
                {
                    LeafMapper[(newChild as Leaf).id] = newChild as Leaf;
                    //a new leaf is cloned. thus e.u / e.v is cloned. check and update.
                    if (e.u is Leaf)
                        e.u = newChild;
                    else
                        e.v = newChild;
                }
                newChild.parent = deg;
            }
            //linking e.u & e.v to v1 & v2
            if (e.u is MarkerVertex)
                (e.u as MarkerVertex).opposite = v1;
            else
                (e.u as Leaf).opposite = v1;
            if (e.v is MarkerVertex)
                (e.v as MarkerVertex).opposite = v2;
            else
                (e.v as Leaf).opposite = v2;

            v1.opposite = e.u;
            v2.opposite = e.v;
            Leaf x = new Leaf
            {
                id = xId,
                opposite = v3,
            };
            x.parent = deg;
            v3.opposite = x;
            //vertices.Add(deg);
            AddLeaf(x);
        }

        //Proposition 4.17
        internal void SplitEdgeToStar(TreeEdge e, int xId)
        {
            //Assume e is P-E
            GLTVertex oParent = e.u.GetGLTVertex();
            GLTVertex oChild = e.v.GetGLTVertex();
            bool uIsParent = true;

            //test and swap
            if (oChild.parent != oParent)
            {
                var tmp = oParent;
                oParent = oChild;
                oChild = tmp;
                uIsParent = false;
            }

            ////Fixing multiple-star forking problem
            //var oChildDeg = oChild as DegenerateNode;
            //if (oChildDeg != null && oChildDeg.isStar && oChildDeg.center == oChildDeg.rootMarkerVertex)
            //{
            //    //note that if child's center is not root, we cannot merge.
            //    AttachToDegenerateNode(oChildDeg, xId);
            //    return;
            //}


            MarkerVertex v1 = new MarkerVertex();
            MarkerVertex v2 = new MarkerVertex();
            MarkerVertex v3 = new MarkerVertex();


            //center is E (e.v) => v2
            var deg = new DegenerateNode()
            {
                active = false,
                center = v2,
                parent = oParent,
                rootMarkerVertex = uIsParent ? v1 : v2,
                visited = false,
                Vu = new List<MarkerVertex> { v1, v2, v3 }
            };
            if (uIsParent)
                v1.node = deg;
            else v2.node = deg;

            deg.parent = oParent;
            if (oParent is PrimeNode == false)
            {
                oChild.parent = deg;
            }
            else
            {
                //(oParent as PrimeNode).ForEachChild(c =>
                //    {
                //        if (c.unionFind_parent == oChild)
                //            Console.WriteLine("!!!");
                //        return IterationFlag.Continue;
                //    }, false);
                //oChild.parent = deg;
                var newChild = oChild.Clone();//since oChild is potentially pointed by others' unionFind_parent, we have to clone.
                if (newChild is Leaf)
                {
                    LeafMapper[(newChild as Leaf).id] = newChild as Leaf;
                    //a new leaf is cloned. thus e.u / e.v is cloned. check and update.
                    if (e.u is Leaf)
                        e.u = newChild;
                    else
                        e.v = newChild;
                }
                newChild.parent = deg;
            }
            //linking e.u & e.v to v1 & v2
            if (e.u is MarkerVertex)
                (e.u as MarkerVertex).opposite = v1;
            else
                (e.u as Leaf).opposite = v1;
            if (e.v is MarkerVertex)
                (e.v as MarkerVertex).opposite = v2;
            else
                (e.v as Leaf).opposite = v2;
            v1.opposite = e.u;
            v2.opposite = e.v;
            Leaf x = new Leaf
            {
                id = xId,
                opposite = v3,
            };
            v3.opposite = x;
            x.parent = deg;
            //vertices.Add(deg);
            AddLeaf(x);
        }

        //Proposition 4.15
        internal void AttachToDegenerateNode(DegenerateNode deg, int xId)
        {
            MarkerVertex v = new MarkerVertex();
            Leaf x = new Leaf()
            {
                id = xId,
                opposite = v,
                parent = deg,
            };
            v.opposite = x;
            deg.Vu.Add(v);//and done! if clique, all vertices are P, thus fully connecting v.
            //and if star, v is connected to center, the only perfect marker.
            AddLeaf(x);
        }

        //Proposition 4.15
        internal void AttachToPrimeNode(PrimeNode primeNode, int xId)
        {
            MarkerVertex v = new MarkerVertex();
            Leaf x = new Leaf()
            {
                id = xId,
                opposite = v,
                parent = primeNode,
            };
            v.opposite = x;
            HashSet<MarkerVertex> list = new HashSet<MarkerVertex>();
            primeNode.ForEachMarkerVertex((u) =>
                {
                    if (u.perfect)
                        list.Add(u);
                    return IterationFlag.Continue;
                });
            primeNode.AddMarkerVertex(v, list);
            primeNode.lastMarkerVertex = v;
            AddLeaf(x);
        }
        #endregion
        //==========================================================================================================

        public Graph GenerateAccessabilityGraph()
        {
            var G = new Graph();
            foreach (var kvp in LeafMapper)
            {
                var leaf = kvp.Value;
                var list = leaf.GetAccessableLeaves();
                foreach (var l in list)
                {
                    G.AddEdge(leaf.id, l.id);
                }
            }
            return G;
        }
    }
}
