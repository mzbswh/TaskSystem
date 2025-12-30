using System;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// Operation任务：支持跨帧执行和进度报告的任务
    /// </summary>
    public class Operation : BaseTask
    {
        private readonly Func<float, bool> _executeFunc;
        private float _currentProgress;

        public override float Progress => _currentProgress;

        /// <summary>
        /// 创建Operation任务
        /// </summary>
        /// <param name="executeFunc">执行函数，参数为当前进度(0-1)，返回是否完成</param>
        /// <param name="priority">优先级</param>
        public Operation(Func<float, bool> executeFunc, int priority = 0) : base(priority)
        {
            _executeFunc = executeFunc;
            _currentProgress = 0f;
        }

        /// <summary>
        /// 创建简单的Operation任务（无进度参数）
        /// </summary>
        public Operation(Func<bool> executeFunc, int priority = 0) : base(priority)
        {
            _executeFunc = (progress) => executeFunc();
            _currentProgress = 0f;
        }

        protected override bool OnExecute(float deltaTime)
        {
            if (_executeFunc == null)
                return true;

            bool isComplete = _executeFunc.Invoke(_currentProgress);

            if (isComplete)
            {
                _currentProgress = 1f;
            }

            return isComplete;
        }

        /// <summary>
        /// 手动设置进度（供外部调用）
        /// </summary>
        public void SetProgress(float progress)
        {
            _currentProgress = UnityEngine.Mathf.Clamp01(progress);
        }

        protected override void OnReset()
        {
            _currentProgress = 0f;
        }

        public override void Clear()
        {
            base.Clear();
            _currentProgress = 0f;
        }
    }
}

