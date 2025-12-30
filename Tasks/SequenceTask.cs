using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 顺序执行任务：按顺序执行多个子任务，任一失败则整体失败
    /// </summary>
    public class SequenceTask : BaseTask
    {
        private readonly List<ITask> _childTasks;
        private int _currentIndex;

        public override float Progress
        {
            get
            {
                if (_childTasks == null || _childTasks.Count == 0)
                    return 1f;

                float completedTasks = _currentIndex;

                // 加上当前任务的进度
                if (_currentIndex < _childTasks.Count)
                {
                    completedTasks += _childTasks[_currentIndex].Progress;
                }

                return completedTasks / _childTasks.Count;
            }
        }

        /// <summary>
        /// 无参构造函数（用于对象池）
        /// </summary>
        public SequenceTask() : base(0)
        {
            _childTasks = new List<ITask>();
            _currentIndex = 0;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public SequenceTask(int priority) : base(priority)
        {
            _childTasks = new List<ITask>();
            _currentIndex = 0;
        }

        /// <summary>
        /// 设置优先级（链式调用）
        /// </summary>
        public SequenceTask SetPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        /// <summary>
        /// 添加子任务
        /// </summary>
        public SequenceTask AddTask(ITask task)
        {
            if (task != null)
            {
                _childTasks.Add(task);
            }
            return this;
        }

        /// <summary>
        /// 批量添加子任务
        /// </summary>
        public SequenceTask AddTasks(params ITask[] tasks)
        {
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    if (task != null)
                    {
                        _childTasks.Add(task);
                    }
                }
            }
            return this;
        }

        protected override bool OnExecute(float deltaTime)
        {
            if (_childTasks == null || _childTasks.Count == 0)
                return true;

            // 执行当前任务
            while (_currentIndex < _childTasks.Count)
            {
                var currentTask = _childTasks[_currentIndex];

                // 检查任务是否失败或取消
                if (currentTask.Status == ETaskStatus.Failed ||
                    currentTask.Status == ETaskStatus.Cancelled)
                {
                    // 子任务失败，整个序列失败
                    Cancel();
                    return true;
                }

                // 执行任务
                bool isComplete = currentTask.Execute(deltaTime);

                if (!isComplete)
                {
                    // 当前任务未完成，等待下一帧
                    return false;
                }

                // 当前任务完成，检查状态
                if (currentTask.Status == ETaskStatus.Failed ||
                    currentTask.Status == ETaskStatus.Cancelled)
                {
                    Cancel();
                    return true;
                }

                // 进入下一个任务
                _currentIndex++;
            }

            // 所有任务完成
            return true;
        }

        protected override void OnReset()
        {
            _currentIndex = 0;

            // 重置所有子任务
            if (_childTasks != null)
            {
                foreach (var task in _childTasks)
                {
                    task?.Reset();
                }
            }
        }

        public override void Clear()
        {
            base.Clear();
            _currentIndex = 0;

            if (_childTasks != null)
            {
                _childTasks.Clear();
            }
        }
    }
}

