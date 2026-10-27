using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using CommunityToolkit.Diagnostics;
using Primary.Pooling;
using Primary.Scenes;

namespace Primary.Rendering.Tree
{
    public sealed class RegionEntityList
    {
        private readonly int _entitiesPerNode;
        private readonly int _halfFullThreshold;

        private readonly Stack<Node> _nodes;

        private readonly Node _firstNode;
        private Node _lastNode;

        private int _count;

        private int _idCounter;

        internal RegionEntityList(int entitiesPerNode)
        {
            _entitiesPerNode = entitiesPerNode;
            _halfFullThreshold = entitiesPerNode / 2;

            _nodes = new Stack<Node>();

            _firstNode = new Node(0, entitiesPerNode);
            _lastNode = _firstNode;

            _count = 0;

            _idCounter = 1;
        }

        internal void Clear()
        {
            Node node = _lastNode;
            while (true)
            {
                node.Clear();
                if (node == _firstNode)
                    break;

                _nodes.Push(node);

                Node oldNode = node;
                node = node.Previous!;

                oldNode.Next = null;
                oldNode.Previous = null;
            }

            _firstNode.Next = null;
            _lastNode = _firstNode;

            _count = 0;
        }

        internal int Add(SceneEntity entity)
        {
            ++_count;

            Node? node = _firstNode;

            do
            {
                if (!node.IsFull)
                {
                    node.Add(entity);
                    return node.Id;
                }
            } while ((node = node.Next) != null);

            if (!_nodes.TryPop(out Node? nextNode))
                nextNode = new Node(_idCounter++, _entitiesPerNode);

            _lastNode.BalanceTo(nextNode, _halfFullThreshold);
            _lastNode.Add(entity);

            _lastNode.Next = nextNode;
            nextNode.Previous = _lastNode;

            _lastNode = nextNode;
            return nextNode.Previous.Id;
        }

        internal bool Remove(SceneEntity entity, int hintId = -1)
        {
            Node? node = _firstNode;

            do
            {
                if (hintId != -1 && node.Id != hintId)
                    continue;

                if (node.Remove(entity))
                {
                    if (node.Count < _halfFullThreshold && node.Next != null)
                    {
                        Node nextNode = node.Next;
                        int requiredElements = _halfFullThreshold - node.Count;

                        // If the next node would be half empty after the copy absorb it
                        if (nextNode.Count - requiredElements < _halfFullThreshold)
                        {
                            nextNode.BalanceTo(node, nextNode.Count);

                            node.Next = nextNode.Next;

                            if (nextNode == _lastNode)
                            {
                                _lastNode = node;
                            }
                            else
                            {
                                Debug.Assert(node.Next != null);
                                node.Next.Previous = node;
                            }

                            _nodes.Push(nextNode);
                        }
                        else
                        {
                            nextNode.BalanceTo(node, requiredElements);
                        }
                    }

                    --_count;
                    return true;
                }
            } while ((node = node.Next) != null);

            return false;
        }

        internal void CopyTo(Span<SceneEntity> entities)
        {
            Guard.IsGreaterThanOrEqualTo(entities.Length, _count);

            int i = 0;

            Node? node = _firstNode;
            do
            {
                node.CopyTo(entities[i..]);
                i += node.Count;
            } while ((node = node.Next) != null);
        }

        public int Count => _count;

        public Node FirstNode => _firstNode;
        public Node LastNode => _lastNode;

        public sealed class Node
        {
            private readonly int _id;

            private Node? _next;
            private Node? _prev;

            private int _numElements;
            private SceneEntity[] _entities;

            internal Node(int id, int elementCount)
            {
                _id = id;

                _next = null;
                _prev = null;

                _numElements = 0;
                _entities = new SceneEntity[elementCount];
            }

            internal void Clear()
            {
                _numElements = 0;
            }

            internal void Add(SceneEntity entity)
            {
                Guard.IsLessThan(_numElements, _entities.Length, "Out of space in entity list node");
                _entities[_numElements++] = entity;
            }

            internal bool Remove(SceneEntity entity)
            {
                int indexOf = Array.IndexOf(_entities, entity);
                if (indexOf != -1)
                {
                    --_numElements;
                    if (indexOf < _numElements)
                    {
                        Array.Copy(_entities, indexOf + 1, _entities, indexOf, _numElements - indexOf);
                    }

                    return true;
                }

                return false;
            }

            internal void BalanceTo(Node node, int count)
            {
                if (count == 0)
                    return;

                int nextCount = node._numElements + count;

                Guard.IsLessThanOrEqualTo(count, _numElements, "Trying to balance more elements than there are in this node");
                Guard.IsLessThanOrEqualTo(nextCount, node._entities.Length, "Not enough space left in node to balance elements");

                Array.Copy(_entities, _numElements - count, node._entities, node._numElements, count);
                
                node._numElements = nextCount;
                _numElements -= count;
            }

            internal void CopyTo(Span<SceneEntity> entities)
            {
                Debug.Assert(entities.Length >= _numElements);
                _entities.AsSpan(0, _numElements).CopyTo(entities);
            }

            public int Id => _id;

            public Node? Next { get => _next; internal set => _next = value; }
            public Node? Previous { get => _prev; internal set => _prev = value; }

            public int Count => _numElements;
            public ReadOnlySpan<SceneEntity> Entities => _entities.AsSpan(0, _numElements);

            public bool IsFull => _numElements == _entities.Length;
        }
    }
}
