namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 调度器接口：统一不同调度策略
    /// </summary>
    public interface IScheduler
    {
        /// <summary>调度器中的任务数量</summary>
        int TaskCount { get; }

        /// <summary>是否正在运行</summary>
        bool IsRunning { get; }

        /// <summary>
        /// 调度任务
        /// </summary>
        /// <param name="task">要调度的任务</param>
        void Schedule(ITask task);

        /// <summary>
        /// 批量调度任务
        /// </summary>
        void ScheduleRange(System.Collections.Generic.IEnumerable<ITask> tasks);

        /// <summary>
        /// 移除任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>是否成功移除</returns>
        bool Remove(int taskId);

        /// <summary>
        /// 移除任务
        /// </summary>
        /// <param name="task">任务对象</param>
        /// <returns>是否成功移除</returns>
        bool Remove(ITask task);

        /// <summary>
        /// 更新调度器（每帧调用）
        /// </summary>
        void Update(float deltaTime);

        /// <summary>
        /// 清空所有任务
        /// </summary>
        void Clear();

        /// <summary>
        /// 暂停调度器
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复调度器
        /// </summary>
        void Resume();

        /// <summary>
        /// 查询任务
        /// </summary>
        /// <param name="taskId">任务ID</param>
        /// <returns>任务对象，未找到返回null</returns>
        ITask GetTask(int taskId);
    }
}

