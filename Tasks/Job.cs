using System;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// Job任务：单次执行的简单任务
    /// </summary>
    public class Job : BaseTask
    {
        private Action _action;
        private Func<bool> _func;

        /// <summary>
        /// 无参构造函数（用于对象池）
        /// </summary>
        public Job() : base(0)
        {
            _action = null;
            _func = null;
        }

        /// <summary>
        /// 使用Action创建Job（立即完成）
        /// </summary>
        public Job(Action action, int priority = 0) : base(priority)
        {
            _action = action;
            _func = null;
        }

        /// <summary>
        /// 使用Func创建Job（根据返回值判断是否完成）
        /// </summary>
        public Job(Func<bool> func, int priority = 0) : base(priority)
        {
            _action = null;
            _func = func;
        }

        /// <summary>
        /// 设置Action（用于对象池复用）
        /// </summary>
        public Job SetAction(Action action)
        {
            _action = action;
            _func = null;
            return this;
        }

        /// <summary>
        /// 设置Func（用于对象池复用）
        /// </summary>
        public Job SetFunc(Func<bool> func)
        {
            _func = func;
            _action = null;
            return this;
        }

        /// <summary>
        /// 设置优先级（链式调用）
        /// </summary>
        public Job SetPriority(int priority)
        {
            Priority = priority;
            return this;
        }

        protected override bool OnExecute(float deltaTime)
        {
            if (_action != null)
            {
                _action.Invoke();
                return true; // Action执行完即完成
            }

            if (_func != null)
            {
                return _func.Invoke(); // 根据Func返回值判断
            }

            return true; // 空任务直接完成
        }

        public override void Clear()
        {
            base.Clear();
            _action = null;
            _func = null;
        }
    }
}

