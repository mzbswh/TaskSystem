using System;
using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务基类，提供通用功能实现
    /// </summary>
    public abstract class BaseTask : ITask, ITaskCallback
    {
        private static int _nextId = 1;

        /// <summary>任务ID</summary>
        public int Id { get; private set; }

        /// <summary>任务优先级</summary>
        public int Priority { get; set; }

        /// <summary>任务状态</summary>
        public ETaskStatus Status { get; protected set; }

        /// <summary>任务进度（子类可重写）</summary>
        public virtual float Progress => Status == ETaskStatus.Completed ? 1f : 0f;

        /// <summary>依赖任务列表</summary>
        public List<ITask> Dependencies { get; private set; }

        /// <summary>最大重试次数</summary>
        public int MaxRetryCount { get; set; }

        /// <summary>当前重试次数</summary>
        public int CurrentRetry { get; private set; }

        /// <summary>任务开始事件</summary>
        public event Action<ITask> OnStarted;

        /// <summary>任务完成事件</summary>
        public event Action<ITask> OnCompleted;

        /// <summary>任务失败事件</summary>
        public event Action<ITask, Exception> OnFailed;

        /// <summary>任务取消事件</summary>
        public event Action<ITask> OnCancelled;

        /// <summary>进度更新事件</summary>
        public event Action<ITask, float> OnProgressChanged;

        /// <summary>上一次报告的进度</summary>
        private float _lastReportedProgress = -1f;

        protected BaseTask(int priority = 0)
        {
            Id = _nextId++;
            Priority = priority;
            Status = ETaskStatus.Pending;
            Dependencies = new List<ITask>();
            MaxRetryCount = 0;
            CurrentRetry = 0;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public bool Execute(float deltaTime = 0f)
        {
            // 检查是否已完成、取消或失败
            if (Status == ETaskStatus.Completed ||
                Status == ETaskStatus.Cancelled ||
                (Status == ETaskStatus.Failed && CurrentRetry >= MaxRetryCount))
            {
                return true;
            }

            // 检查是否暂停
            if (Status == ETaskStatus.Paused)
            {
                return false;
            }

            // 检查依赖任务
            if (!CheckDependencies())
            {
                return false;
            }

            // 首次执行，触发开始事件
            if (Status == ETaskStatus.Pending)
            {
                Status = ETaskStatus.Running;
                OnStarted?.Invoke(this);
            }

            try
            {
                // 执行具体逻辑
                bool isComplete = OnExecute(deltaTime);

                // 更新进度
                UpdateProgress();

                if (isComplete)
                {
                    Status = ETaskStatus.Completed;
                    OnCompleted?.Invoke(this);
                }

                return isComplete;
            }
            catch (Exception e)
            {
                HandleException(e);
                return Status == ETaskStatus.Failed; // 如果失败且不重试，返回true移除任务
            }
        }

        /// <summary>
        /// 检查依赖任务是否全部完成
        /// </summary>
        private bool CheckDependencies()
        {
            if (Dependencies == null || Dependencies.Count == 0)
                return true;

            foreach (var dependency in Dependencies)
            {
                if (dependency.Status != ETaskStatus.Completed)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        private void HandleException(Exception e)
        {
            CurrentRetry++;

            if (CurrentRetry <= MaxRetryCount)
            {
                // 可以重试，重置为待执行状态
                UnityEngine.Debug.LogWarning($"任务执行失败 [ID:{Id}]，正在重试 ({CurrentRetry}/{MaxRetryCount}): {e.Message}");
                Status = ETaskStatus.Pending;
                OnReset(); // 重置子类状态
            }
            else
            {
                // 超过重试次数，标记为失败
                Status = ETaskStatus.Failed;
                OnFailed?.Invoke(this, e);
                UnityEngine.Debug.LogError($"任务执行失败 [ID:{Id}]，已达最大重试次数: {e.Message}\n{e.StackTrace}");
            }
        }

        /// <summary>
        /// 更新进度并触发事件
        /// </summary>
        private void UpdateProgress()
        {
            float currentProgress = Progress;
            if (Math.Abs(currentProgress - _lastReportedProgress) > 0.001f)
            {
                _lastReportedProgress = currentProgress;
                OnProgressChanged?.Invoke(this, currentProgress);
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public virtual void Cancel()
        {
            if (Status != ETaskStatus.Completed && Status != ETaskStatus.Failed)
            {
                Status = ETaskStatus.Cancelled;
                OnCancelled?.Invoke(this);
            }
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        public virtual void Pause()
        {
            if (Status == ETaskStatus.Running)
            {
                Status = ETaskStatus.Paused;
            }
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        public virtual void Resume()
        {
            if (Status == ETaskStatus.Paused)
            {
                Status = ETaskStatus.Running;
            }
        }

        /// <summary>
        /// 重置任务
        /// </summary>
        public virtual void Reset()
        {
            Status = ETaskStatus.Pending;
            CurrentRetry = 0;
            _lastReportedProgress = -1f;
            OnReset();
        }

        /// <summary>
        /// 清理任务（对象池回收时调用）
        /// </summary>
        public virtual void Clear()
        {
            Status = ETaskStatus.Pending;
            Priority = 0;
            CurrentRetry = 0;
            MaxRetryCount = 0;
            _lastReportedProgress = -1f;

            // 清理依赖
            if (Dependencies != null)
            {
                Dependencies.Clear();
            }

            // 清理事件
            OnStarted = null;
            OnCompleted = null;
            OnFailed = null;
            OnCancelled = null;
            OnProgressChanged = null;

            OnReset();
        }

        /// <summary>
        /// 添加依赖任务
        /// </summary>
        public void AddDependency(ITask task)
        {
            if (task != null && !Dependencies.Contains(task))
            {
                Dependencies.Add(task);
            }
        }

        /// <summary>
        /// 移除依赖任务
        /// </summary>
        public void RemoveDependency(ITask task)
        {
            if (task != null)
            {
                Dependencies.Remove(task);
            }
        }

        /// <summary>
        /// 子类实现具体的执行逻辑
        /// </summary>
        /// <param name="deltaTime">距离上次执行的时间间隔（秒）</param>
        /// <returns>是否完成</returns>
        protected abstract bool OnExecute(float deltaTime);

        /// <summary>
        /// 子类实现重置逻辑（可选）
        /// </summary>
        protected virtual void OnReset() { }
    }
}

