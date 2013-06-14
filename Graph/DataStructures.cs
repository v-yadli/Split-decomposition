using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace GraphCompression
{
    public class Vertex//The base class
    {
        public Vertex()
        {
            VertexSequence = GlobalVertexSequence++;
        }
        //A helper function, effective for markers and leave
        public void MarkAsPerfect()
        {
            if (this is MarkerVertex)
            {
                (this as MarkerVertex).perfect = true;
            }
            else if (this is Leaf)
            {
                (this as Leaf).perfect = true;
            }
        }
        //will throw exception if instance is neither leaf nor marker
        public bool Perfect()
        {
            if (this is MarkerVertex)
                return (this as MarkerVertex).perfect;
            else return (this as Leaf).perfect;
        }
        //effective for a leaf, or a root marker vertex, or a node. for non-root marker vertex, will return null.
        public GLTVertex GetGLTVertex()
        {
            if (this is Leaf)
                return this as Leaf;
            if (this is Node)
                return this as Node;
            return (this as MarkerVertex).node;
        }

        public void MarkAsEmpty()
        {
            if (this is MarkerVertex)
            {
                (this as MarkerVertex).perfect = false;
            }
            else if (this is Leaf)
            {
                (this as Leaf).perfect = false;
            }
        }

        public int VertexSequence;
        public static int GlobalVertexSequence = 0;


        //For debugging
        public void Print(int indent, bool subtree)
        {
            if (this is MarkerVertex)
            {
                WriteLine(indent, "MarkerVertex: SEQ:{0}, rootMarker:{1}, perfect:{2}, opposite:", this.VertexSequence, (this as MarkerVertex).node != null, (this as MarkerVertex).perfect);
                if ((!subtree || (this as MarkerVertex).GetOppositeGLTVertex().visited) && indent < 2)
                {
                    (this as MarkerVertex).opposite.Print(indent + 1, subtree);
                    if ((this as MarkerVertex).GetOppositeGLTVertex() is Leaf == false)
                    {
                        var oppositeVertex = (this as MarkerVertex).GetOppositeGLTVertex();
                        if (oppositeVertex != null)
                            oppositeVertex.Print(indent + 1, subtree);
                        else WriteLine(indent, "[This is root marker vertex]");
                    }
                }
            }
            else if (this is Leaf)
            {
                WriteLine(indent, "Leaf: SEQ:{0}, ID:{1}, neighbor:{2}, perfect:{3}", this.VertexSequence, (this as Leaf).id, (this as Leaf).neighbor, (this as Leaf).perfect);
                if ((this as Leaf).parent == null || (subtree && !(this as Leaf).parent.visited))
                {
                    WriteLine(indent, "[This is root]");
                }
                else
                    if ((!subtree || (this as Leaf).parent.visited) && indent == 0)
                    {

                        WriteLine(indent, "parent:");
                        (this as Leaf).parent.Print(indent + 1, subtree);
                    }
                if (indent < 1)
                {
                    WriteLine(indent, "opposite:");
                    (this as Leaf).opposite.Print(indent + 1, subtree);
                }
            }
            else if (this is PrimeNode)
            {
                var prime = this as PrimeNode;
                WriteLine(indent, "PrimeNode: SEQ:{0}", this.VertexSequence);
                if (prime.parent == null || (subtree && !prime.parent.visited))
                {
                    WriteLine(indent, "[This is root]");
                }
                else
                    if ((!subtree || prime.parent.visited) && indent == 0)
                    {
                        WriteLine(indent, "parent:");
                        prime.parent.Print(indent + 1, subtree);
                    }
                if (indent == 0)
                {
                    foreach (var t in prime.Gu)
                    {
                        t.Item1.Print(indent + 1, subtree);
                        string adjacency = t.Item1.VertexSequence.ToString() + " -> [ " + t.Item2.Aggregate("", (str, v) =>
                        {
                            if (str == "")
                            {
                                return v.VertexSequence.ToString();
                            }
                            else
                            {
                                return str + ", " + v.VertexSequence.ToString();
                            }
                        }) + " ]";
                        WriteLine(indent + 1, adjacency);
                    }
                }
            }
            else if (this is DegenerateNode)
            {
                var deg = this as DegenerateNode;
                if (deg.isStar)
                {
                    WriteLine(indent, "Star: SEQ:{0}", this.VertexSequence);
                }
                else
                {
                    WriteLine(indent, "Clique: SEQ:{0}", this.VertexSequence);
                }
                if (deg.parent == null || (subtree && !deg.parent.visited))
                {
                    WriteLine(indent, "[This is root]");
                }
                else
                    if ((!subtree || deg.parent.visited) && indent == 0)
                    {
                        WriteLine(indent, "parent:");
                        deg.parent.Print(indent + 1, subtree);
                    }
                if (indent == 0)
                {
                    foreach (var v in deg.Vu)
                    {
                        if (v == deg.center)
                        {
                            WriteLine(indent, "[Center]");
                        }
                        v.Print(indent + 1, subtree);
                    }
                }
            }
        }
        private void WriteLine(int indent, string format, params object[] args)
        {
            for (int i = 0; i < indent * 4; ++i)
                Console.Write(" ");
            Console.WriteLine(format, args);
        }
    }
    public enum State
    {
        Perfect,
        Mixed,
        Empty
    }
    public class MarkerVertex : Vertex
    {
        internal static int perfectTime = 0;
        public static void ResetPerfectFlags()
        {
            ++perfectTime;
        }
        public bool perfect
        {
            get
            {
                return perfectTimestamp == perfectTime;
            }
            set
            {
                if (value)
                {
                    perfectTimestamp = perfectTime;
                }
                else
                {
                    perfectTimestamp = -1;//manually mark to -1 to avoid collision.
                }
            }
        }
        private int perfectTimestamp = 0;
        public Vertex opposite;
        public Node node;//only valid when this marker vertex is the root marker vertex
        //public State perfectState;
        public GLTVertex GetOppositeGLTVertex()//only valid if traversing down from parent to a child
        {
            if (opposite is Leaf)
            {
                return opposite as Leaf;
            }
            else//opposite is a marker vertex
            {
                return (opposite as MarkerVertex).node;
            }
        }
    }
    public abstract class GLTVertex : Vertex // the vertices in GLT, node or leaf
    {
        static int activeTime = 0;
        static int visitedTime = 0;
        private int activeTimestamp = 0;
        private int visitedTimestamp = 0;
        internal GLTVertex parentLink;//stores true parent for representative child, and for normal nodes and leaves
        internal GLTVertex unionFind_parent = null;//will be activated when linking to a prime parent. forming the union-find data structure.
        //unionFind_parent will point to this GLTVertex itself when it is a set-representative.
        public GLTVertex Find()
        {
            if (unionFind_parent == this)
                return this;
            return unionFind_parent = unionFind_parent.Find();
        }
        public GLTVertex parent
        {
            get
            {
                if (unionFind_parent != null)//this GLTVertex belongs to a disjoint set
                {
                    return Find().parentLink;//Find first searches for set-representative, then one step up to the real parent
                }
                return parentLink;
            }
            set
            {
                if (value is PrimeNode)
                {
                    parentLink = null;
                    unionFind_parent = (value as PrimeNode).childSetRepresentative;
                }
                else
                {
                    unionFind_parent = null;
                    parentLink = value;
                }
            }
        }
        static public void ResetActiveFlags()
        {
            ++activeTime;
        }
        static public void ResetVisitedFlags()
        {
            ++visitedTime;
        }
        public bool active
        {
            get
            {
                return activeTime == activeTimestamp;
            }
            set
            {
                if (value)
                    activeTimestamp = activeTime;
                else
                    activeTimestamp = -1;
            }
        }
        public bool visited//will be used to induce T' from T
        {
            get
            {
                return visitedTime == visitedTimestamp;
            }
            set
            {
                if (value)
                    visitedTimestamp = visitedTime;
                else
                    visitedTimestamp = -1;
            }
        }
        //abstract public int numberOfChildren { get; }
    }
    internal class Leaf : GLTVertex
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
    }
    public enum IterationFlag
    {
        Continue,
        Break,
    }
    public abstract class Node : GLTVertex
    {
        public MarkerVertex rootMarkerVertex;//The extrimity of the link to the parent
        //private int _numOfChildren;
        /// <summary>
        /// Iterate through all children
        /// </summary>
        /// <param name="subtree">If set to true, GLTVertex.visited will be checked</param>
        public virtual void ForEachChild(Func<GLTVertex, IterationFlag> action, bool subtree)
        {
            throw new Exception();
        }
        public abstract int Degree(MarkerVertex v);
        /// <summary>
        /// Iterate through all neighbors of a marker vertex, in the label graph of the current node
        /// </summary>
        public virtual void ForEachNeighbor(MarkerVertex source, Func<MarkerVertex, IterationFlag> action)
        {
            throw new Exception();
        }
        public virtual void ForEachMarkerVertex(Func<MarkerVertex, IterationFlag> action)
        {
            throw new Exception();
        }
        public GLTVertex GetOppositeGLTVertex(MarkerVertex v)
        {
            if (v == rootMarkerVertex)
                return parent;
            return v.GetOppositeGLTVertex();
        }
        public int AcquireSigmaGuOrder(MarkerVertex v, List<int> nodeOrder)
        {
            var oppositeGLTVertex = GetOppositeGLTVertex(v);
            if (oppositeGLTVertex is Leaf)
            {
                return nodeOrder[(oppositeGLTVertex as Leaf).id];
            }
            int min = nodeOrder.Count + 1;
            (oppositeGLTVertex as Node).ForEachNeighbor(v.opposite as MarkerVertex, (u) =>
                {
                    var o = (oppositeGLTVertex as Node).AcquireSigmaGuOrder(u, nodeOrder);
                    if (o < min)
                        o = min;
                    return IterationFlag.Continue;
                });
            return min;
        }
        public State ComputeState(MarkerVertex v)
        {
            if (v.perfect)
                return State.Perfect;
            var oppositeGLTVertex = GetOppositeGLTVertex(v);
            if (oppositeGLTVertex is Leaf)
            {
                if ((oppositeGLTVertex as Leaf).neighbor)
                    return State.Perfect;
                else return State.Empty;
            }
            State? state = null;
            (oppositeGLTVertex as Node).ForEachNeighbor(v.opposite as MarkerVertex, (u) =>
                {
                    var s = (oppositeGLTVertex as Node).ComputeState(u);
                    if (state == null)
                        state = s;
                    else if (state != s)
                        state = State.Mixed;
                    return IterationFlag.Continue;
                });
            //since the graph is connected, we can assert state != null here
            Debug.Assert(state != null, "state is null!");
            if (state.Value == State.Perfect)
                v.perfect = true;
            return state.Value;
        }
        public void ComputeStates(out List<MarkerVertex> P, out List<MarkerVertex> M, out List<MarkerVertex> E, MarkerVertex exclude = null, bool excludeRootMarkerVertex = false)
        {
            var _P = new List<MarkerVertex>();
            var _M = new List<MarkerVertex>();
            var _E = new List<MarkerVertex>();
            ForEachMarkerVertex((v) =>
                {
                    if (v == exclude)
                        return IterationFlag.Continue;
                    if (v == rootMarkerVertex && excludeRootMarkerVertex)
                        return IterationFlag.Continue;
                    var s = ComputeState(v);
                    switch (s)
                    {
                        case State.Empty:
                            _E.Add(v);
                            break;
                        case State.Mixed:
                            _M.Add(v);
                            break;
                        case State.Perfect:
                            _P.Add(v);
                            break;
                    }
                    return IterationFlag.Continue;
                });
            P = _P;
            M = _M;
            E = _E;
        }
        //public override int numberOfChildren
        //{
        //    get
        //    {
        //        return _numOfChildren;
        //    }
        //}
    }
    class PrimeNode : Node
    {
        public List<Tuple<MarkerVertex, List<MarkerVertex>>> Gu;
        //public MarkerVertex lastMarkerVertex;// the pointer to the last marker vertex in \sigma(G(u))
        //TODO XXX: universalMarkerVertex not handled
        public MarkerVertex universalMarkerVetex;// there will be at most 1 universal marker vertex, as otherwise there will be a non-trivial split
        public GLTVertex childSetRepresentative
        {
            get
            {
                return Gu.First((v) => v.Item1 != rootMarkerVertex).Item1.GetOppositeGLTVertex().Find();
            }
        }
        public override int Degree(MarkerVertex v)
        {
            return Gu.Find((u) => u.Item1 == v).Item2.Count;
        }
        public override void ForEachNeighbor(MarkerVertex source, Func<MarkerVertex, IterationFlag> action)
        {
            //lookup
            for (int i = 0; i < Gu.Count; ++i)
                if (Gu[i].Item1 == source)
                {
                    //iterate
                    foreach (var v in Gu[i].Item2)
                    {
                        if (action(v) == IterationFlag.Break)
                            break;
                    }
                    break;
                }
        }
        public override void ForEachChild(Func<GLTVertex, IterationFlag> action, bool subtree)
        {
            foreach (var v in Gu)
            {
                if (v.Item1 == rootMarkerVertex)
                    continue;
                GLTVertex child = v.Item1.GetOppositeGLTVertex();
                if (!subtree || child.visited)
                    if (action(child) == IterationFlag.Break)
                        break;
            }
        }
        public override void ForEachMarkerVertex(Func<MarkerVertex, IterationFlag> action)
        {
            foreach (var t in Gu)
                if (action(t.Item1) == IterationFlag.Break)
                    break;
        }

        //the parameter: the new marker vertex, and a list of its neighbors
        internal void AddMarkerVertex(Tuple<MarkerVertex, List<MarkerVertex>> tuple)
        {
            int newUniversalCount = Gu.Count;
            universalMarkerVetex = null;//re-calculate
            HashSet<MarkerVertex> set = new HashSet<MarkerVertex>(tuple.Item2);
            for (int i = 0, n = Gu.Count; i < n; ++i)
            {
                if (set.Contains(Gu[i].Item1))
                {
                    Gu[i].Item2.Add(tuple.Item1);
                    if (Gu[i].Item2.Count == newUniversalCount)
                        universalMarkerVetex = Gu[i].Item1;
                }
            }
            Gu.Add(tuple);
            if (tuple.Item2.Count == newUniversalCount)
                universalMarkerVetex = tuple.Item1;
        }

        internal void RemoveMarkerVertex(MarkerVertex q)
        {
            if (q == universalMarkerVetex)
                universalMarkerVetex = null;
            int universalCount = Gu.Count - 2;
            for (int i = 0; ; )
            {
                if (i >= Gu.Count)
                    break;
                if (Gu[i].Item1 == q)
                {
                    Gu.RemoveAt(i);
                    continue;
                }
                Gu[i].Item2.Remove(q);
                if (Gu[i].Item2.Count == universalCount)
                    universalMarkerVetex = Gu[i].Item1;
                ++i;
            }
        }
    }
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
                parentLink = parentLink,
                unionFind_parent = unionFind_parent,
                universalMarkerVetex = null,//really not determined right now..
                Gu = new List<Tuple<MarkerVertex, List<MarkerVertex>>>()
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
                    List<MarkerVertex> n = new List<MarkerVertex>();
                    foreach (var w in Vu)
                    {
                        if (w == v)
                            continue;
                        n.Add(w);
                    }
                    ret.Gu.Add(Tuple.Create(v, n));
                }
            }
            else
            {
                foreach (var v in Vu)
                {
                    if (v == center)
                    {
                        List<MarkerVertex> n = new List<MarkerVertex>();
                        foreach (var w in Vu)
                        {
                            if (w == v)
                                continue;
                            n.Add(w);
                        }
                        ret.Gu.Add(Tuple.Create(v, n));
                    }
                    else
                    {
                        ret.Gu.Add(Tuple.Create(v, new List<MarkerVertex> { center }));
                    }
                }
            }
            return ret;
        }
    }
    public struct TreeEdge
    {
        public Vertex u, v;
        public GLTVertex u_GLT
        {
            get
            {
                if (u is Leaf)
                    return u as Leaf;
                var ret = u.GetGLTVertex();
                if (ret == null)
                    ret = v.GetGLTVertex().parent;
                return ret;
            }
        }
        public GLTVertex v_GLT
        {
            get
            {
                if (v is Leaf)
                    return v as Leaf;
                var ret = v.GetGLTVertex();
                if (ret == null)
                    ret = u.GetGLTVertex().parent;
                return ret;
            }
        }
    }
    public class SplitTree
    {
        public SplitTree()
        {
            vertices = new List<GLTVertex>();
            root = null;
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

        //Proposition 4.17
        internal void SplitEdgeToClique(TreeEdge e, int xId)
        {
            //Assume e is P-P
            GLTVertex oParent = e.u.GetGLTVertex();
            GLTVertex oChild = e.v.GetGLTVertex();
            bool uIsParent = true;
            if (oParent == null)//now we're sure that e.u is a non-root marker vertex, and parent
            {
                //thus we must succeed in getting oChild
                oParent = oChild.parent;
            }
            else
            {
                //GLTVertex for e.u is acquired. Thus either e.u is root marker(and thus child), or leaf(currently unknown)
                if (e.u is MarkerVertex)//e.u is child, swapping later
                {
                    //assign vertex of e.v first
                    oChild = oParent.parent;
                }
                else//now e.u is a leaf. If e.v's vertex is acquired, then e.v is a root marker vertex, and child.
                //otherwise, e.u is child
                {
                    if (oChild == null)
                    {
                        oChild = oParent.parent;
                    }
                }
            }
            //test and swap
            if (oChild.parent != oParent)
            {
                var tmp = oParent;
                oParent = oChild;
                oChild = tmp;
                uIsParent = false;
            }

            MarkerVertex v1 = new MarkerVertex();
            MarkerVertex v2 = new MarkerVertex();
            MarkerVertex v3 = new MarkerVertex();
            v1.opposite = e.u;
            if (e.u is MarkerVertex)
                (e.u as MarkerVertex).opposite = v1;
            v2.opposite = e.v;
            if (e.v is MarkerVertex)
                (e.v as MarkerVertex).opposite = v2;
            Leaf x = new Leaf
            {
                id = xId,
                opposite = v3,
            };
            v3.opposite = x;

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
            x.parent = deg;
            deg.parent = oParent;
            oChild.parent = deg;
            vertices.Add(deg);
            vertices.Add(x);
            lastVertex = x;
        }

        //Proposition 4.17
        internal void SplitEdgeToStar(TreeEdge e, int xId)
        {
            //Assume e is P-E
            GLTVertex oParent = e.u.GetGLTVertex();
            GLTVertex oChild = e.v.GetGLTVertex();
            bool uIsParent = true;
            if (oParent == null)//now we're sure that e.u is a non-root marker vertex, and parent
            {
                //thus we must succeed in getting oChild
                oParent = oChild.parent;
            }
            else
            {
                //GLTVertex for e.u is acquired. Thus either e.u is root marker(and thus child), or leaf(currently unknown)
                if (e.u is MarkerVertex)//e.u is child, swapping later
                {
                    //assign vertex of e.v first
                    oChild = oParent.parent;
                }
                else//now e.u is a leaf. If e.v's vertex is acquired, then e.v is a root marker vertex, and child.
                //otherwise, e.u is child
                {
                    if (oChild == null)
                    {
                        oChild = oParent.parent;
                    }
                }
            }
            //test and swap
            if (oChild.parent != oParent)
            {
                var tmp = oParent;
                oParent = oChild;
                oChild = tmp;
                uIsParent = false;
            }

            MarkerVertex v1 = new MarkerVertex();
            MarkerVertex v2 = new MarkerVertex();
            MarkerVertex v3 = new MarkerVertex();
            v1.opposite = e.u;
            if (e.u is MarkerVertex)
                (e.u as MarkerVertex).opposite = v1;
            else
                (e.u as Leaf).opposite = v1;
            v2.opposite = e.v;
            if (e.v is MarkerVertex)
                (e.v as MarkerVertex).opposite = v2;
            else
                (e.v as Leaf).opposite = v2;
            Leaf x = new Leaf
            {
                id = xId,
                opposite = v3,
            };
            v3.opposite = x;

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
            x.parent = deg;
            deg.parent = oParent;
            oChild.parent = deg;
            vertices.Add(deg);
            vertices.Add(x);
            lastVertex = x;
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
            vertices.Add(x);
            lastVertex = x;
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
            List<MarkerVertex> list = new List<MarkerVertex>();
            primeNode.ForEachMarkerVertex((u) =>
                {
                    if (u.perfect)
                        list.Add(u);
                    return IterationFlag.Continue;
                });
            primeNode.AddMarkerVertex(Tuple.Create(v, list));
            vertices.Add(x);
            lastVertex = x;
        }
    }
}
