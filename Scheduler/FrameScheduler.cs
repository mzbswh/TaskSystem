using System;
using System.Collections.Generic;
using TankSlg.TaskSystem;

namespace TankSlg
{
    /// <summary>
    /// 分帧调度器：将多个操作分散到多个帧中执行，避免单帧卡顿
    /// 支持ITask和Action两种方式
    /// </summary>
    public class FrameScheduler : IScheduler
    {
        /// <summary> 是否正在运行 </summary>
        public bool IsRunning => _isRunning;

        /// <summary> 任务队列 </summary>
        private Queue<ITask> _taskQueue = new Queue<ITask>();

        /// <summary> 任务字典（用于快速查找） </summary>
        private Dictionary<int, ITask> _taskDict = new Dictionary<int, ITask>();

        /// <summary> 每帧最多执行的任务数量 </summary>
        private int _maxTasksPerFrame;

        /// <summary> 是否正在执行中 </summary>
        private bool _isRunning = true;

        /// <summary> 所有任务完成时的回调 </summary>
        private Action _onCompleteCallback;

        /// <summary> 当前进度（已完成任务数） </summary>
        private int _completedCount = 0;

        /// <summary> 总任务数 </summary>
        private int _totalCount = 0;

        /// <summary> 任务数量 </summary>
        public int TaskCount => _taskQueue.Count;

        /// <param name="maxTasksPerFrame">每帧最多执行的任务数量，默认为5</param>
        public FrameScheduler(int maxTasksPerFrame = 5)
        {
            _maxTasksPerFrame = maxTasksPerFrame;
        }

        /// <summary>
        /// 调度ITask任务
        /// </summary>
        public void Schedule(ITask task)
        {
            if (task != null)
            {
                _taskQueue.Enqueue(task);
                _taskDict[task.Id] = task;
                _totalCount++;
            }
        }

        /// <summary>
        /// 批量调度任务
        /// </summary>
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

        /// <summary>
        /// 添加单个Action任务（兼容旧API）
        /// </summary>
        public void AddTask(Action action)
        {
            if (action != null)
            {
                var job = new Job(action);
                Schedule(job);
            }
        }

        /// <summary>
        /// 批量添加Action任务（兼容旧API）
        /// </summary>
        public void AddTasks(IEnumerable<Action> actions)
        {
            if (actions != null)
            {
                foreach (var action in actions)
                {
                    AddTask(action);
                }
            }
        }

        /// <summary>
        /// 添加任务（泛型版本，兼容旧API）
        /// </summary>
        public void AddTasks<T>(IEnumerable<T> collection, Action<T> action)
        {
            if (collection != null && action != null)
            {
                foreach (var item in collection)
                {
                    AddTask(() => action(item));
                }
            }
        }

        /// <summary>
        /// 设置完成回调
        /// </summary>
        /// <param name="onComplete">完成时的回调</param>
        public void SetCompleteCallback(Action onComplete)
        {
            _onCompleteCallback = onComplete;
        }

        /// <summary>
        /// 开始执行（兼容旧API）
        /// </summary>
        public void Start()
        {
            _isRunning = true;
            _completedCount = 0;
        }

        /// <summary>
        /// 每帧更新（带时间参数）
        /// </summary>
        /// <param name="deltaTime">距离上次更新的时间间隔</param>
        public void Update(float deltaTime)
        {
            if (!_isRunning || _taskQueue.Count == 0)
            {
                if (_isRunning && _taskQueue.Count == 0)
                {
                    _isRunning = true; // 保持运行状态，等待新任务
                    _onCompleteCallback?.Invoke();
                    _onCompleteCallback = null;
                }
                return;
            }

            // 执行本帧的任务
            int tasksThisFrame = Math.Min(_maxTasksPerFrame, _taskQueue.Count);
            for (int i = 0; i < tasksThisFrame; i++)
            {
                if (_taskQueue.Count == 0)
                    break;

                var task = _taskQueue.Dequeue();

                try
                {
                    bool isComplete = task.Execute(deltaTime);

                    if (isComplete)
                    {
                        _completedCount++;
                        _taskDict.Remove(task.Id);
                    }
                    else
                    {
                        // 任务未完成，重新入队
                        _taskQueue.Enqueue(task);
                    }
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"FrameScheduler执行任务时出错 [ID:{task.Id}]: {e.Message}\n{e.StackTrace}");
                    _taskDict.Remove(task.Id);
                }
            }

            // 检查是否全部完成
            if (_taskQueue.Count == 0)
            {
                _onCompleteCallback?.Invoke();
                _onCompleteCallback = null;
            }
        }

        /// <summary>
        /// 获取当前进度（0-1）
        /// </summary>
        public float GetProgress()
        {
            if (_totalCount == 0) return 1f;
            return (float)_completedCount / _totalCount;
        }

        /// <summary>
        /// 获取剩余任务数量
        /// </summary>
        public int GetRemainingTaskCount()
        {
            return _taskQueue.Count;
        }

        /// <summary>
        /// 获取总任务数量
        /// </summary>
        public int GetTotalTaskCount()
        {
            return _totalCount;
        }

        /// <summary>
        /// 获取已完成任务数量
        /// </summary>
        public int GetCompletedTaskCount()
        {
            return _completedCount;
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public bool Remove(int taskId)
        {
            if (_taskDict.TryGetValue(taskId, out var task))
            {
                _taskDict.Remove(taskId);
                task.Cancel();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public bool Remove(ITask task)
        {
            if (task != null)
            {
                return Remove(task.Id);
            }
            return false;
        }

        /// <summary>
        /// 查询任务
        /// </summary>
        public ITask GetTask(int taskId)
        {
            _taskDict.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// 暂停调度器
        /// </summary>
        public void Pause()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 恢复调度器
        /// </summary>
        public void Resume()
        {
            _isRunning = true;
        }

        /// <summary>
        /// 是否正在处理中（兼容旧API）
        /// </summary>
        public bool IsProcessing()
        {
            return _isRunning && _taskQueue.Count > 0;
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        public void Clear()
        {
            _taskQueue.Clear();
            _taskDict.Clear();
            _onCompleteCallback = null;
            _completedCount = 0;
            _totalCount = 0;
        }

        /// <summary>
        /// 停止执行（不清空任务，兼容旧API）
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
        }

        /// <summary>
        /// 设置每帧最多执行的任务数量
        /// </summary>
        public void SetMaxTasksPerFrame(int count)
        {
            _maxTasksPerFrame = Math.Max(1, count);
        }
    }
}
