using System;
using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务运行器：统一管理所有调度器和任务
    /// </summary>
    public class TaskRunner
    {
        private readonly Dictionary<string, IScheduler> _schedulers;
        private readonly Dictionary<int, ITask> _allTasks;
        private readonly Dictionary<int, List<int>> _dependencyMap; // 任务ID -> 依赖它的任务ID列表

        /// <summary>默认调度器名称</summary>
        public const string DEFAULT_SCHEDULER = "Default";

        /// <summary>高优先级调度器名称</summary>
        public const string PRIORITY_SCHEDULER_NAME = "Priority";

        /// <summary>是否正在运行</summary>
        public bool IsRunning { get; private set; }

        /// <summary>总任务数</summary>
        public int TotalTaskCount => _allTasks.Count;

        public TaskRunner()
        {
            _schedulers = new Dictionary<string, IScheduler>();
            _allTasks = new Dictionary<int, ITask>();
            _dependencyMap = new Dictionary<int, List<int>>();
            IsRunning = true;

            // 创建默认调度器
            RegisterScheduler(DEFAULT_SCHEDULER, new FrameScheduler(5));
            RegisterScheduler(PRIORITY_SCHEDULER_NAME, new PriorityScheduler(5));
        }

        /// <summary>
        /// 注册调度器
        /// </summary>
        public void RegisterScheduler(string name, IScheduler scheduler)
        {
            if (string.IsNullOrEmpty(name) || scheduler == null)
                return;

            if (_schedulers.ContainsKey(name))
            {
                UnityEngine.Debug.LogWarning($"调度器 '{name}' 已存在，将被覆盖");
            }

            _schedulers[name] = scheduler;
        }

        /// <summary>
        /// 获取调度器
        /// </summary>
        public IScheduler GetScheduler(string name)
        {
            _schedulers.TryGetValue(name, out var scheduler);
            return scheduler;
        }

        /// <summary>
        /// 提交任务到默认调度器
        /// </summary>
        public void Submit(ITask task)
        {
            Submit(task, DEFAULT_SCHEDULER);
        }

        /// <summary>
        /// 提交任务到指定调度器
        /// </summary>
        public void Submit(ITask task, string schedulerName)
        {
            if (task == null)
                return;

            if (!_schedulers.TryGetValue(schedulerName, out var scheduler))
            {
                UnityEngine.Debug.LogError($"调度器 '{schedulerName}' 不存在");
                return;
            }

            // 检查依赖关系
            if (task.Dependencies != null && task.Dependencies.Count > 0)
            {
                foreach (var dependency in task.Dependencies)
                {
                    // 确保依赖任务已提交
                    if (!_allTasks.ContainsKey(dependency.Id))
                    {
                        UnityEngine.Debug.LogWarning($"任务 [ID:{task.Id}] 依赖的任务 [ID:{dependency.Id}] 未找到");
                    }
                    else
                    {
                        // 记录依赖关系
                        if (!_dependencyMap.ContainsKey(dependency.Id))
                        {
                            _dependencyMap[dependency.Id] = new List<int>();
                        }
                        _dependencyMap[dependency.Id].Add(task.Id);
                    }
                }
            }

            // 记录任务
            _allTasks[task.Id] = task;

            // 监听任务完成事件
            if (task is ITaskCallback callback)
            {
                callback.OnCompleted += OnTaskCompleted;
                callback.OnFailed += OnTaskFailed;
                callback.OnCancelled += OnTaskCancelled;
            }

            // 提交到调度器
            scheduler.Schedule(task);
        }

        /// <summary>
        /// 批量提交任务
        /// </summary>
        public void SubmitRange(IEnumerable<ITask> tasks, string schedulerName = null)
        {
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    Submit(task, schedulerName ?? DEFAULT_SCHEDULER);
                }
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool CancelTask(int taskId)
        {
            if (_allTasks.TryGetValue(taskId, out var task))
            {
                task.Cancel();
                RemoveTask(taskId);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 查询任务
        /// </summary>
        public ITask GetTask(int taskId)
        {
            _allTasks.TryGetValue(taskId, out var task);
            return task;
        }

        /// <summary>
        /// 获取所有任务
        /// </summary>
        public IEnumerable<ITask> GetAllTasks()
        {
            return _allTasks.Values;
        }

        /// <summary>
        /// 获取指定状态的任务
        /// </summary>
        public List<ITask> GetTasksByStatus(ETaskStatus status)
        {
            var result = new List<ITask>();
            foreach (var task in _allTasks.Values)
            {
                if (task.Status == status)
                {
                    result.Add(task);
                }
            }
            return result;
        }

        /// <summary>
        /// 更新所有调度器（每帧调用）
        /// </summary>
        public void Update(float deltaTime)
        {
            if (!IsRunning) return;

            foreach (var scheduler in _schedulers.Values)
            {
                scheduler.Update(deltaTime);
            }
        }

        /// <summary>
        /// 暂停所有任务
        /// </summary>
        public void Pause()
        {
            IsRunning = false;
            foreach (var scheduler in _schedulers.Values)
            {
                scheduler.Pause();
            }
        }

        /// <summary>
        /// 恢复所有任务
        /// </summary>
        public void Resume()
        {
            IsRunning = true;
            foreach (var scheduler in _schedulers.Values)
            {
                scheduler.Resume();
            }
        }

        /// <summary>
        /// 清空所有任务
        /// </summary>
        public void Clear()
        {
            foreach (var scheduler in _schedulers.Values)
            {
                scheduler.Clear();
            }

            _allTasks.Clear();
            _dependencyMap.Clear();
        }

        /// <summary>
        /// 任务完成时的处理
        /// </summary>
        private void OnTaskCompleted(ITask task)
        {
            RemoveTask(task.Id);

            // 检查是否有任务依赖它
            if (_dependencyMap.TryGetValue(task.Id, out var dependentTaskIds))
            {
                // 可以通知依赖任务，依赖已完成
                _dependencyMap.Remove(task.Id);
            }
        }

        /// <summary>
        /// 任务失败时的处理
        /// </summary>
        private void OnTaskFailed(ITask task, Exception exception)
        {
            RemoveTask(task.Id);
        }

        /// <summary>
        /// 任务取消时的处理
        /// </summary>
        private void OnTaskCancelled(ITask task)
        {
            RemoveTask(task.Id);
        }

        /// <summary>
        /// 从记录中移除任务
        /// </summary>
        private void RemoveTask(int taskId)
        {
            if (_allTasks.TryGetValue(taskId, out var task))
            {
                // 取消事件监听
                if (task is ITaskCallback callback)
                {
                    callback.OnCompleted -= OnTaskCompleted;
                    callback.OnFailed -= OnTaskFailed;
                    callback.OnCancelled -= OnTaskCancelled;
                }

                _allTasks.Remove(taskId);
            }
        }

        /// <summary>
        /// 获取运行统计信息
        /// </summary>
        public string GetStatistics()
        {
            var stats = $"TaskRunner Statistics:\n";
            stats += $"Total Tasks: {TotalTaskCount}\n";
            stats += $"Running: {GetTasksByStatus(ETaskStatus.Running).Count}\n";
            stats += $"Pending: {GetTasksByStatus(ETaskStatus.Pending).Count}\n";
            stats += $"Paused: {GetTasksByStatus(ETaskStatus.Paused).Count}\n";

            foreach (var kvp in _schedulers)
            {
                stats += $"Scheduler '{kvp.Key}': {kvp.Value.TaskCount} tasks\n";
            }

            return stats;
        }
    }
}

