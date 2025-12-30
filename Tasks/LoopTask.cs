using System;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 循环任务：重复执行任务
    /// </summary>
    public class LoopTask : BaseTask
    {
        private readonly ITask _childTask;
        private readonly int _loopCount; // -1表示无限循环
        private readonly Func<bool> _breakCondition; // 循环终止条件
        private int _currentLoop;

        public override float Progress
        {
            get
            {
                if (_loopCount <= 0) // 无限循环无法计算进度
                    return 0f;

                float completedLoops = _currentLoop;
                if (_childTask != null)
                {
                    completedLoops += _childTask.Progress;
                }

                return completedLoops / _loopCount;
            }
        }

        /// <summary>
        /// 创建指定次数的循环任务
        /// </summary>
        /// <param name="childTask">要循环执行的任务</param>
        /// <param name="loopCount">循环次数（-1表示无限循环）</param>
        /// <param name="priority">优先级</param>
        public LoopTask(ITask childTask, int loopCount, int priority = 0) : base(priority)
        {
            _childTask = childTask;
            _loopCount = loopCount;
            _breakCondition = null;
            _currentLoop = 0;
        }

        /// <summary>
        /// 创建带终止条件的循环任务
        /// </summary>
        /// <param name="childTask">要循环执行的任务</param>
        /// <param name="breakCondition">终止条件（返回true时终止循环）</param>
        /// <param name="priority">优先级</param>
        public LoopTask(ITask childTask, Func<bool> breakCondition, int priority = 0) : base(priority)
        {
            _childTask = childTask;
            _loopCount = -1;
            _breakCondition = breakCondition;
            _currentLoop = 0;
        }

        protected override bool OnExecute(float deltaTime)
        {
            if (_childTask == null)
                return true;

            // 检查终止条件
            if (_breakCondition != null && _breakCondition.Invoke())
            {
                return true;
            }

            // 检查循环次数
            if (_loopCount > 0 && _currentLoop >= _loopCount)
            {
                return true;
            }

            // 执行子任务
            bool isComplete = _childTask.Execute(deltaTime);

            if (isComplete)
            {
                // 子任务完成，准备下一次循环
                _currentLoop++;
                _childTask.Reset();

                // 检查是否已达到循环次数
                if (_loopCount > 0 && _currentLoop >= _loopCount)
                {
                    return true;
                }

                // 检查终止条件
                if (_breakCondition != null && _breakCondition.Invoke())
                {
                    return true;
                }
            }

            // 继续循环
            return false;
        }

        protected override void OnReset()
        {
            _currentLoop = 0;
            _childTask?.Reset();
        }

        public override void Clear()
        {
            base.Clear();
            _currentLoop = 0;
        }
    }
}

