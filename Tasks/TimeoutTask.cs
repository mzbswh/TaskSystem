using UnityEngine;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 超时任务：为子任务添加超时控制
    /// </summary>
    public class TimeoutTask : BaseTask
    {
        private readonly ITask _childTask;
        private readonly float _timeoutDuration;
        private float _elapsedTime;

        public override float Progress => _childTask?.Progress ?? 0f;

        /// <summary>
        /// 创建超时任务
        /// </summary>
        /// <param name="childTask">子任务</param>
        /// <param name="timeoutDuration">超时时间（秒）</param>
        /// <param name="priority">优先级</param>
        public TimeoutTask(ITask childTask, float timeoutDuration, int priority = 0) : base(priority)
        {
            _childTask = childTask;
            _timeoutDuration = timeoutDuration;
            _elapsedTime = 0f;
        }

        protected override bool OnExecute(float deltaTime)
        {
            if (_childTask == null)
                return true;

            // 更新计时
            _elapsedTime += deltaTime;

            // 检查超时
            if (_elapsedTime >= _timeoutDuration)
            {
                // 超时，取消子任务
                _childTask.Cancel();

                // 抛出超时异常（会被BaseTask捕获并处理）
                throw new System.TimeoutException($"任务超时 [ID:{Id}]，超时时间：{_timeoutDuration}秒");
            }

            // 执行子任务
            return _childTask.Execute(deltaTime);
        }

        protected override void OnReset()
        {
            _elapsedTime = 0f;
            _childTask?.Reset();
        }

        public override void Clear()
        {
            base.Clear();
            _elapsedTime = 0f;
        }
    }
}

