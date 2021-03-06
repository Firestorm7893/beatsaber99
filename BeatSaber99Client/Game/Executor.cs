﻿using System;
using System.Collections.Concurrent;
using UnityEngine;

namespace BeatSaber99Client.Game
{
    /// <summary>
    /// Dispatches events from other threads onto the main game thread.
    /// </summary>
    public class Executor : MonoBehaviour
    {
        public static Executor instance;
        public static void Init()
        {
            new GameObject("beatsaber99_executor").AddComponent<Executor>();
        }

        private static ConcurrentQueue<Action> _actions = new ConcurrentQueue<Action>();

        public static void Enqueue(Action action)
        {
            _actions.Enqueue(action);
        }

        public void Update()
        {
            while (_actions.Count > 0)
            {
                if (_actions.TryDequeue(out var result))
                    result();
                    
            }
        }

        public void Start()
        {
            DontDestroyOnLoad(this.gameObject);
            instance = this;
        }
    }
}