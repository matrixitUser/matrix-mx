using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Matrix.Domain.Entities
{
    public static class GraphHelpers
    {

        /// <summary>
        /// обход в глубину, алгоритм DFS
        /// <param name="start">начальная вершина</param>
        /// <param name="getNeighbors">получение соседей</param>
        /// <param name="visitAndContinue">посещение вершины, если false - останов обхода</param>
        /// </summary>
        public static void DFS<TNode>(TNode start, Func<TNode, IEnumerable<TNode>> getNeighbors, Func<TNode, TNode, bool> visitAndContinue, Func<TNode, object> key) where TNode : class
        {
            if (getNeighbors == null) getNeighbors = (n) => new TNode[] { };
            if (visitAndContinue == null) visitAndContinue = (n1, n2) => true;
            if (key == null) key = (n) => n;

            var visited = new HashSet<object>();
            var stack = new Stack<TNode>();
            stack.Push(start);
            while (stack.Any())
            {
                var current = stack.Pop();
                var children = getNeighbors(current);
                if (children == null || !children.Any())
                {
                    if (!visitAndContinue(current, null)) return;
                    continue;
                }

                foreach (var child in children)
                {
                    if (visited.Contains(key(child)))
                    {
                        if (!visitAndContinue(current, child)) return;
                        continue;
                    }

                    visited.Add(key(child));
                    stack.Push(child);
                    if (!visitAndContinue(current, child)) return;
                }
            }
        }
    }
}
