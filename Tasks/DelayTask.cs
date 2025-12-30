using UnityEngine;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 延迟任务：延迟指定时间后执行
    /// </summary>
    public class DelayTask : BaseTask
    {
        private readonly float _delayTime;
        private readonly ITask _childTask;
        private float _elapsedTime;
        private bool _delayComplete;
        
        public override float Progress
        {
            get
            {
                if (_delayTime <= 0f)
                    return _childTask?.Progress ?? 1f;
                
                if (!_delayComplete)
                {
                    return _elapsedTime / _delayTime * 0.5f; // 延迟占50%进度
                }
                
                return 0.5f + (_childTask?.Progress ?? 1f) * 0.5f; // 任务执行占50%进度
            }
        }
        
        /// <summary>
        /// 创建延迟任务（仅延迟，无后续任务）
        /// </summary>
        public DelayTask(float delayTime, int priority = 0) : base(priority)
        {
            _delayTime = delayTime;
            _childTask = null;
            _elapsedTime = 0f;
            _delayComplete = false;
        }
        
        /// <summary>
        /// 创建延迟任务（延迟后执行子任务）
        /// </summary>
        public DelayTask(float delayTime, ITask childTask, int priority = 0) : base(priority)
        {
            _delayTime = delayTime;
            _childTask = childTask;
            _elapsedTime = 0f;
            _delayComplete = false;
        }
        
        protected override bool OnExecute(float deltaTime)
        {
            // 延迟阶段
            if (!_delayComplete)
            {
                _elapsedTime += deltaTime;

                if (_elapsedTime >= _delayTime)
                {
                    _delayComplete = true;

                    // 如果没有子任务，延迟完成即任务完成
                    if (_childTask == null)
                        return true;
                }
                else
                {
                    return false;
                }
            }

            // 执行子任务
            if (_childTask != null)
            {
                return _childTask.Execute(deltaTime);
            }

            return true;
        }
        
        protected override void OnReset()
        {
            _elapsedTime = 0f;
            _delayComplete = false;
            _childTask?.Reset();
        }
        
        public override void Clear()
        {
            base.Clear();
            _elapsedTime = 0f;
            _delayComplete = false;
        }
    }
}

