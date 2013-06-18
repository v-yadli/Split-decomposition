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
        //effective for a leaf, or a marker vertex, or a node.
        public GLTVertex GetGLTVertex()
        {
            if (this is Leaf)
                return this as Leaf;
            if (this is Node)
                return this as Node;
            var m = (this as MarkerVertex);
            if (m.node != null)
                return m.node;
            //this is a non-root marker vertex.
            return m.opposite.GetGLTVertex().parent;
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
                        t.Key.Print(indent + 1, subtree);
                        string adjacency = t.Key.VertexSequence.ToString() + " -> [ " + t.Value.Aggregate("", (str, v) =>
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
        internal int activeTimestamp = 0;
        internal int visitedTimestamp = 0;
        //spawn an identical GLTVertex, and overtake all parent/child relationships, so that the original one can safely become a fake node.
        internal abstract GLTVertex Clone();
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
        public abstract void ForEachChild(Func<GLTVertex, IterationFlag> action, bool subtree);
        private int statesUpdateTimestamp = -1;
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
        //Remark 5.5
        HashSet<MarkerVertex> _neighbors = new HashSet<MarkerVertex>();
        private State ComputeState(MarkerVertex v)
        {
            if (v.perfect)
                return State.Perfect;//v.perfect means we will meet a perfect descendant subtree in the opposite.
            var oppositeGLTVertex = GetOppositeGLTVertex(v);
            if (oppositeGLTVertex.visited == false)
                return State.Empty;
            //v is empty iff opposite is not visited
            if (oppositeGLTVertex is Leaf)
            {
                if ((oppositeGLTVertex as Leaf).neighbor)
                    return State.Perfect;
                else return State.Empty;
            }
            //from now on we assert v is NE
            State state = State.Perfect;
            _neighbors.Clear();
            var oppositeNode = (oppositeGLTVertex as Node);
            (oppositeNode).ForEachNeighbor(v.opposite as MarkerVertex, (u) =>
                {
                    _neighbors.Add(u);
                    return IterationFlag.Continue;
                });
            (oppositeNode).ForEachMarkerVertex((u) =>
                {
                    if (u == v.opposite)
                        return IterationFlag.Continue;
                    if (_neighbors.Contains(u))
                    {
                        if (oppositeNode.ComputeState(u) != State.Perfect)
                        {
                            state = State.Mixed;
                            return IterationFlag.Break;
                        }
                    }
                    else
                    {
                        if (oppositeNode.ComputeState(u) != State.Empty)
                        {
                            state = State.Mixed;
                            return IterationFlag.Break;
                        }
                    }
                    return IterationFlag.Continue;
                });
            //since the graph is connected, we can assert state != null here
            if (state == State.Perfect)
                v.perfect = true;
            return state;
        }
        private List<MarkerVertex> _P;
        private List<MarkerVertex> _M;
        private List<MarkerVertex> _E;
        private MarkerVertex _historyExclude = null;
        private bool _historyExcludeRootMarkerVertex = false;
        public void ComputeStates(out List<MarkerVertex> P, out List<MarkerVertex> M, out List<MarkerVertex> E, MarkerVertex exclude = null, bool excludeRootMarkerVertex = false)
        {
            if (statesUpdateTimestamp != MarkerVertex.perfectTime || exclude != _historyExclude || _historyExcludeRootMarkerVertex != excludeRootMarkerVertex)
            {
                _P = new List<MarkerVertex>();
                _M = new List<MarkerVertex>();
                _E = new List<MarkerVertex>();
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
                statesUpdateTimestamp = MarkerVertex.perfectTime;
                _historyExclude = exclude;
                _historyExcludeRootMarkerVertex = excludeRootMarkerVertex;
            }
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
                //if (ret == null)
                //    ret = v.GetGLTVertex().parent;
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
                //if (ret == null)
                //    ret = u.GetGLTVertex().parent;
                return ret;
            }
        }
    }

}
