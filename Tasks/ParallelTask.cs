using System.Collections.Generic;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 并行任务等待模式
    /// </summary>
    public enum EParallelWaitMode
    {
        /// <summary>等待所有任务完成</summary>
        WaitAll,
        /// <summary>任一任务完成即可</summary>
        WaitAny
    }

    /// <summary>
    /// 并行执行任务：同时执行多个子任务
    /// </summary>
    public class ParallelTask : BaseTask
    {
        private readonly List<ITask> _childTasks;
        private EParallelWaitMode _waitMode;
        private readonly HashSet<int> _completedTaskIds;

        public override float Progress
        {
            get
            {
                if (_childTasks == null || _childTasks.Count == 0)
                    return 1f;

                float totalProgress = 0f;
                foreach (var task in _childTasks)
                {
                    totalProgress += task.Progress;
                }

                return totalProgress / _childTasks.Count;
            }
        }

        /// <summary>
        /// 无参构造函数（用于对象池）
        /// </summary>
        public ParallelTask() : base(0)
        {
            _childTasks = new List<ITask>();
            _waitMode = EParallelWaitMode.WaitAll;
            _completedTaskIds = new HashSet<int>();
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public ParallelTask(EParallelWaitMode waitMode = EParallelWaitMode.WaitAll, int priority = 0) : base(priority)
        {
            _childTasks = new List<ITask>();
            _waitMode = waitMode;
            _completedTaskIds = new HashSet<int>();
        }

        /// <summary>
        /// 设置等待模式（用于对象池复用）
        /// </summary>
        public ParallelTask SetWaitMode(EParallelWaitMode waitMode)
        {
            _waitMode = waitMode;
            return this;
        }

        /// <summary>
        /// 设置优先级（链式调用）
        /// </summary>
        public ParallelTask SetPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        /// <summary>
        /// 添加子任务
        /// </summary>
        public ParallelTask AddTask(ITask task)
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
        public ParallelTask AddTasks(params ITask[] tasks)
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

            bool hasAnyCompleted = false;
            bool allCompleted = true;

            // 并行执行所有任务
            foreach (var task in _childTasks)
            {
                // 跳过已完成的任务
                if (_completedTaskIds.Contains(task.Id))
                {
                    hasAnyCompleted = true;
                    continue;
                }

                // 执行任务
                bool isComplete = task.Execute(deltaTime);

                if (isComplete)
                {
                    _completedTaskIds.Add(task.Id);
                    hasAnyCompleted = true;
                }
                else
                {
                    allCompleted = false;
                }
            }

            // 根据等待模式判断是否完成
            if (_waitMode == EParallelWaitMode.WaitAny)
            {
                return hasAnyCompleted;
            }
            else // WaitAll
            {
                return allCompleted;
            }
        }

        protected override void OnReset()
        {
            _completedTaskIds.Clear();

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
            _completedTaskIds.Clear();
            _waitMode = EParallelWaitMode.WaitAll;

            if (_childTasks != null)
            {
                _childTasks.Clear();
            }
        }
    }
}

