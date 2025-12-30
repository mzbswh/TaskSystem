using System;
using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 优先级调度器：按优先级执行任务
    /// </summary>
    public class PriorityScheduler : IScheduler
    {
        /// <summary>任务比较器（按优先级降序）</summary>
        private class TaskComparer : IComparer<ITask>
        {
            public int Compare(ITask x, ITask y)
            {
                if (x == null || y == null)
                    return 0;

                // 优先级高的排前面（降序）
                int priorityCompare = y.Priority.CompareTo(x.Priority);
                if (priorityCompare != 0)
                    return priorityCompare;

                // 优先级相同，按ID排序保证稳定性
                return x.Id.CompareTo(y.Id);
            }
        }

        private readonly SortedSet<ITask> _taskQueue;
        private readonly Dictionary<int, ITask> _taskDict;
        private readonly int _maxTasksPerFrame;
        private bool _isRunning;

        public int TaskCount => _taskQueue.Count;
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="maxTasksPerFrame">每帧最多执行的任务数量</param>
        public PriorityScheduler(int maxTasksPerFrame = 5)
        {
            _taskQueue = new SortedSet<ITask>(new TaskComparer());
            _taskDict = new Dictionary<int, ITask>();
            _maxTasksPerFrame = Math.Max(1, maxTasksPerFrame);
            _isRunning = true;
        }

        public void Schedule(ITask task)
        {
            if (task != null && !_taskDict.ContainsKey(task.Id))
            {
                _taskQueue.Add(task);
                _taskDict[task.Id] = task;
            }
        }

        public void ScheduleRange(IEnumerable<ITask> tasks)
        {
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    Schedule(task);
                }
            }
        }

        public bool Remove(int taskId)
        {
            if (_taskDict.TryGetValue(taskId, out var task))
            {
                _taskQueue.Remove(task);
                _taskDict.Remove(taskId);
                task.Cancel();
                return true;
            }
            return false;
        }

        public bool Remove(ITask task)
        {
            if (task != null)
            {
                return Remove(task.Id);
            }
            return false;
        }

        public ITask GetTask(int taskId)
        {
            _taskDict.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// 更新调度器（带时间参数）
        /// </summary>
        /// <param name="deltaTime">距离上次更新的时间间隔</param>
        public void Update(float deltaTime)
        {
            if (!_isRunning || _taskQueue.Count == 0)
                return;

            // 收集本帧要执行的任务
            var tasksToExecute = new List<ITask>();
            int count = 0;

            foreach (var task in _taskQueue)
            {
                if (count >= _maxTasksPerFrame)
                    break;

                tasksToExecute.Add(task);
                count++;
            }

            // 执行任务
            var completedTasks = new List<ITask>();

            foreach (var task in tasksToExecute)
            {
                try
                {
                    bool isComplete = task.Execute(deltaTime);

                    if (isComplete)
                    {
                        completedTasks.Add(task);
                    }
                    else
                    {
                        // 任务未完成，可能优先级已改变，需要重新排序
                        // 从SortedSet中移除再重新添加
                        _taskQueue.Remove(task);
                        _taskQueue.Add(task);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"PriorityScheduler执行任务时出错 [ID:{task.Id}]: {e.Message}\n{e.StackTrace}");
                    completedTasks.Add(task);
                }
            }

            // 移除已完成的任务
            foreach (var task in completedTasks)
            {
                _taskQueue.Remove(task);
                _taskDict.Remove(task.Id);
            }
        }

        public void Clear()
        {
            _taskQueue.Clear();
            _taskDict.Clear();
        }

        public void Pause()
        {
            _isRunning = false;
        }

        public void Resume()
        {
            _isRunning = true;
        }

        /// <summary>
        /// 更新任务优先级（需要手动调用）
        /// </summary>
        public void UpdateTaskPriority(int taskId, int newPriority)
        {
            if (_taskDict.TryGetValue(taskId, out var task))
            {
                _taskQueue.Remove(task);
                task.Priority = newPriority;
                _taskQueue.Add(task);
            }
        }
    }
}

