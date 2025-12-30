using System;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务状态
    /// </summary>
    public enum ETaskStatus
    {
        /// <summary>等待执行</summary>
        Pending,
        /// <summary>正在执行</summary>
        Running,
        /// <summary>已暂停</summary>
        Paused,
        /// <summary>已完成</summary>
        Completed,
        /// <summary>已取消</summary>
        Cancelled,
        /// <summary>执行失败</summary>
        Failed
    }

    /// <summary>
    /// 任务接口
    /// </summary>
    public interface ITask
    {
        /// <summary>任务ID</summary>
        int Id { get; }

        /// <summary>任务优先级（数值越大优先级越高）</summary>
        int Priority { get; set; }

        /// <summary>任务状态</summary>
        ETaskStatus Status { get; }

        /// <summary>任务进度（0-1）</summary>
        float Progress { get; }

        /// <summary>依赖任务列表</summary>
        System.Collections.Generic.List<ITask> Dependencies { get; }

        /// <summary>
        /// 执行任务（每帧或每次调度时调用）
        /// </summary>
        /// <param name="deltaTime">距离上次执行的时间间隔（秒）</param>
        /// <returns>是否已完成（true表示完成或失败，false表示需要继续执行）</returns>
        bool Execute(float deltaTime = 0f);

        /// <summary>
        /// 取消任务
        /// </summary>
        void Cancel();

        /// <summary>
        /// 暂停任务
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复任务
        /// </summary>
        void Resume();

        /// <summary>
        /// 重置任务（重新执行前调用）
        /// </summary>
        void Reset();

        /// <summary>
        /// 清理任务（对象池回收时调用）
        /// </summary>
        void Clear();
    }

    /// <summary>
    /// 任务事件回调接口
    /// </summary>
    public interface ITaskCallback
    {
        /// <summary>任务开始时触发</summary>
        event Action<ITask> OnStarted;

        /// <summary>任务完成时触发</summary>
        event Action<ITask> OnCompleted;

        /// <summary>任务失败时触发</summary>
        event Action<ITask, Exception> OnFailed;

        /// <summary>任务取消时触发</summary>
        event Action<ITask> OnCancelled;

        /// <summary>任务进度更新时触发</summary>
        event Action<ITask, float> OnProgressChanged;
    }
}

