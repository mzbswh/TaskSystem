using System;
using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务对象池：减少GC压力
    /// </summary>
    public class TaskPool
    {
        private static TaskPool _instance;
        public static TaskPool Instance => _instance ??= new TaskPool();

        private readonly Dictionary<Type, Queue<ITask>> _pools;
        private readonly Dictionary<Type, int> _poolSizes;
        private const int DEFAULT_POOL_SIZE = 10;
        private const int MAX_POOL_SIZE = 100;

        private TaskPool()
        {
            _pools = new Dictionary<Type, Queue<ITask>>();
            _poolSizes = new Dictionary<Type, int>();
        }

        /// <summary>
        /// 从对象池获取任务
        /// </summary>
        public T Acquire<T>() where T : class, ITask, new()
        {
            var type = typeof(T);

            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<ITask>();
                _pools[type] = pool;
                _poolSizes[type] = 0;
            }

            ITask task;
            if (pool.Count > 0)
            {
                task = pool.Dequeue();
                task.Reset();
            }
            else
            {
                task = new T();
            }

            return task as T;
        }

        /// <summary>
        /// 创建Job任务
        /// </summary>
        public Job CreateJob(Action action, int priority = 0)
        {
            var job = Acquire<Job>();
            job.SetAction(action).SetPriority(priority);
            return job;
        }

        /// <summary>
        /// 创建Job任务（Func版本）
        /// </summary>
        public Job CreateJob(Func<bool> func, int priority = 0)
        {
            var job = Acquire<Job>();
            job.SetFunc(func).SetPriority(priority);
            return job;
        }

        /// <summary>
        /// 创建Operation任务
        /// </summary>
        public Operation CreateOperation(Func<float, bool> executeFunc, int priority = 0)
        {
            return new Operation(executeFunc, priority);
        }

        /// <summary>
        /// 创建Sequence任务
        /// </summary>
        public SequenceTask CreateSequence(int priority = 0)
        {
            var task = Acquire<SequenceTask>();
            task.SetPriority(priority);
            return task;
        }

        /// <summary>
        /// 创建Parallel任务
        /// </summary>
        public ParallelTask CreateParallel(EParallelWaitMode waitMode = EParallelWaitMode.WaitAll, int priority = 0)
        {
            var task = Acquire<ParallelTask>();
            task.SetWaitMode(waitMode).SetPriority(priority);
            return task;
        }

        /// <summary>
        /// 创建Delay任务
        /// </summary>
        public DelayTask CreateDelay(float delayTime, int priority = 0)
        {
            return new DelayTask(delayTime, priority);
        }

        /// <summary>
        /// 创建Delay任务（带子任务）
        /// </summary>
        public DelayTask CreateDelay(float delayTime, ITask childTask, int priority = 0)
        {
            return new DelayTask(delayTime, childTask, priority);
        }

        /// <summary>
        /// 释放任务到对象池
        /// </summary>
        public void Release(ITask task)
        {
            if (task == null)
                return;

            var type = task.GetType();

            if (!_pools.TryGetValue(type, out var pool))
            {
                pool = new Queue<ITask>();
                _pools[type] = pool;
                _poolSizes[type] = 0;
            }

            // 检查池大小限制
            if (pool.Count >= MAX_POOL_SIZE)
            {
                task.Clear();
                return;
            }

            // 清理任务并放入池中
            task.Clear();
            pool.Enqueue(task);
        }

        /// <summary>
        /// 自动释放已完成的任务
        /// </summary>
        public void AutoRelease(ITask task)
        {
            if (task == null)
                return;

            // 监听任务完成事件
            if (task is ITaskCallback callback)
            {
                callback.OnCompleted += (t) => Release(t);
                callback.OnFailed += (t, e) => Release(t);
                callback.OnCancelled += (t) => Release(t);
            }
        }

        /// <summary>
        /// 清空指定类型的对象池
        /// </summary>
        public void ClearPool<T>() where T : ITask
        {
            var type = typeof(T);
            if (_pools.TryGetValue(type, out var pool))
            {
                while (pool.Count > 0)
                {
                    var task = pool.Dequeue();
                    task.Clear();
                }
            }
        }

        /// <summary>
        /// 清空所有对象池
        /// </summary>
        public void ClearAll()
        {
            foreach (var pool in _pools.Values)
            {
                while (pool.Count > 0)
                {
                    var task = pool.Dequeue();
                    task.Clear();
                }
            }

            _pools.Clear();
            _poolSizes.Clear();
        }

        /// <summary>
        /// 预热对象池
        /// </summary>
        public void Prewarm<T>(int count) where T : class, ITask, new()
        {
            count = Math.Min(count, MAX_POOL_SIZE);

            for (int i = 0; i < count; i++)
            {
                var task = new T();
                Release(task);
            }
        }

        /// <summary>
        /// 获取对象池统计信息
        /// </summary>
        public string GetStatistics()
        {
            var stats = "TaskPool Statistics:\n";
            foreach (var kvp in _pools)
            {
                stats += $"{kvp.Key.Name}: {kvp.Value.Count} cached\n";
            }
            return stats;
        }
    }
}

