using System;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 条件任务：根据条件选择执行不同的任务
    /// </summary>
    public class ConditionalTask : BaseTask
    {
        private readonly Func<bool> _condition;
        private readonly ITask _trueTask;
        private readonly ITask _falseTask;
        private ITask _selectedTask;
        
        public override float Progress => _selectedTask?.Progress ?? 0f;
        
        /// <summary>
        /// 创建条件任务
        /// </summary>
        /// <param name="condition">条件判断函数</param>
        /// <param name="trueTask">条件为true时执行的任务</param>
        /// <param name="falseTask">条件为false时执行的任务（可选）</param>
        /// <param name="priority">优先级</param>
        public ConditionalTask(Func<bool> condition, ITask trueTask, ITask falseTask = null, int priority = 0) 
            : base(priority)
        {
            _condition = condition;
            _trueTask = trueTask;
            _falseTask = falseTask;
            _selectedTask = null;
        }
        
        protected override bool OnExecute(float deltaTime)
        {
            // 首次执行时选择任务
            if (_selectedTask == null)
            {
                bool conditionResult = _condition?.Invoke() ?? true;
                _selectedTask = conditionResult ? _trueTask : _falseTask;

                // 如果没有可执行的任务，直接完成
                if (_selectedTask == null)
                    return true;
            }

            // 执行选中的任务
            return _selectedTask.Execute(deltaTime);
        }
        
        protected override void OnReset()
        {
            _selectedTask = null;
            _trueTask?.Reset();
            _falseTask?.Reset();
        }
        
        public override void Clear()
        {
            base.Clear();
            _selectedTask = null;
        }
    }
}

