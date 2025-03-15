using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Leonardo.Scripts.ClientRelated
{
    /// <summary>
    /// Provides functionality to execute code on the Unity main thread, which is necessary for operations that can only be performed on the main thread such as modifying the transform components.
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private readonly Queue<Action> _executionQueue = new Queue<Action>();
        private readonly object _lock = new object();

        #region Singleton Instance

        /// <summary>
        /// Singleton of this class.
        /// </summary>
        /// <returns>Returns the singleton instance.</returns>
        public static UnityMainThreadDispatcher Instance()
        {
            if (_instance == null)
            {
                var instanceGameObject = new GameObject("UnityMainThreadDispatcher");
                _instance = instanceGameObject.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(instanceGameObject);
            }
            return _instance;
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(this);
            }
        }
        #endregion

        #region Script Specific PUBLIC Methods

        /// <summary>
        /// Adds an action to be queue'd on the main thread.
        /// </summary>
        /// <param name="action">The action to add to the queue and execute.</param>
        public void Enqueue(Action action)
        {
            lock (_lock)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        #endregion

        #region Script Specific PRIVATE Methods

        private void Update()
        {
            ExecuteMethodsInQueue();
        }

        /// <summary>
        /// Executes all queued actions on the main thread.
        /// </summary>
        private void ExecuteMethodsInQueue()
        {
            lock (_lock)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }
        
        #endregion
        
        
    }
}