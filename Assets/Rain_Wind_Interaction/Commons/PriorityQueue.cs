using System;
using System.Collections.Generic;

namespace Rain_Wind_Interaction.Commons
{
    public class PriorityQueue<T>
    {

        private List<Tuple<T, double>> _elements = new();


        /// <summary>
        /// Return the total number of elements currently in the Queue.
        /// </summary>
        /// <returns>Total number of elements currently in Queue</returns>
        public int Count => _elements.Count;


        /// <summary>
        /// Add given item to Queue and assign item the given priority value.
        /// </summary>
        /// <param name="item">Item to be added.</param>
        /// <param name="priorityValue">Item priority value as Double.</param>
        public void Enqueue(T item, double priorityValue)
        {
            _elements.Add(Tuple.Create(item, priorityValue));
        }


        /// <summary>
        /// Return lowest priority value item and remove item from Queue.
        /// </summary>
        /// <returns>Queue item with lowest priority value.</returns>
        public T Dequeue()
        {
            int bestPriorityIndex = 0;

            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].Item2 < _elements[bestPriorityIndex].Item2)
                {
                    bestPriorityIndex = i;
                }
            }

            T bestItem = _elements[bestPriorityIndex].Item1;
            _elements.RemoveAt(bestPriorityIndex);
            return bestItem;
        }


        /// <summary>
        /// Return lowest priority value item without removing item from Queue.
        /// </summary>
        /// <returns>Queue item with lowest priority value.</returns>
        public T Peek()
        {
            int bestPriorityIndex = 0;

            for (int i = 0; i < _elements.Count; i++)
            {
                if (_elements[i].Item2 < _elements[bestPriorityIndex].Item2)
                {
                    bestPriorityIndex = i;
                }
            }

            T bestItem = _elements[bestPriorityIndex].Item1;
            return bestItem;
        }
    }
}
