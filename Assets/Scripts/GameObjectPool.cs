using System.Collections.Generic;
using UnityEngine;

namespace SnowballSmash.Gameplay
{
    /// <summary>
    /// Pools GameObject instances by prefab while preserving each instance's owning prefab.
    /// </summary>
    public class GameObjectPool
    {
        private readonly Dictionary<GameObject, Queue<GameObject>> availableByPrefab = new();
        private readonly Dictionary<GameObject, GameObject> prefabByInstance = new();

        /// <summary>
        /// Clears all tracked prefab queues and instance ownership.
        /// </summary>
        public void Clear()
        {
            availableByPrefab.Clear();
            prefabByInstance.Clear();
        }

        /// <summary>
        /// Creates inactive instances for a prefab if it does not already have a queue.
        /// </summary>
        public void Preload(GameObject prefab, Transform parent, int count)
        {
            if (availableByPrefab.ContainsKey(prefab))
            {
                return;
            }

            availableByPrefab[prefab] = new Queue<GameObject>();

            for (int i = 0; i < count; i++)
            {
                GameObject instance = CreateInstance(prefab, parent);
                Return(instance, parent);
            }
        }

        /// <summary>
        /// Gets an inactive instance for the prefab or creates one when the queue is empty.
        /// </summary>
        public GameObject Get(GameObject prefab, Transform parent)
        {
            if (!availableByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue = new Queue<GameObject>();
                availableByPrefab[prefab] = queue;
            }

            return queue.Count > 0 ? queue.Dequeue() : CreateInstance(prefab, parent);
        }

        /// <summary>
        /// Hides an instance and returns it to the queue for its owning prefab.
        /// </summary>
        public void Return(GameObject instance, Transform parent)
        {
            instance.SetActive(false);
            instance.transform.SetParent(parent, false);

            if (prefabByInstance.TryGetValue(instance, out GameObject prefab)
                && availableByPrefab.TryGetValue(prefab, out Queue<GameObject> queue))
            {
                queue.Enqueue(instance);
            }
        }

        private GameObject CreateInstance(GameObject prefab, Transform parent)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.SetActive(false);
            prefabByInstance[instance] = prefab;
            return instance;
        }
    }
}
