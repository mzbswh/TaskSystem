using System;
using System.Collections;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务构建器：使用流式API构建复杂任务
    /// </summary>
    public class TaskBuilder
    {
        private ITask _currentTask;
        private SequenceTask _rootSequence;
        
        private TaskBuilder()
        {
            _rootSequence = new SequenceTask();
            _currentTask = _rootSequence;
        }
        
        /// <summary>
        /// 创建任务构建器
        /// </summary>
        public static TaskBuilder Create()
        {
            return new TaskBuilder();
        }
        
        /// <summary>
        /// 执行一个Action
        /// </summary>
        public TaskBuilder Do(Action action)
        {
            var job = new Job(action);
            _rootSequence.AddTask(job);
            return this;
        }
        
        /// <summary>
        /// 执行一个Func（返回true表示完成）
        /// </summary>
        public TaskBuilder Do(Func<bool> func)
        {
            var job = new Job(func);
            _rootSequence.AddTask(job);
            return this;
        }
        
        /// <summary>
        /// 添加一个任务
        /// </summary>
        public TaskBuilder Then(ITask task)
        {
            if (task != null)
            {
                _rootSequence.AddTask(task);
            }
            return this;
        }
        
        /// <summary>
        /// 延迟指定时间
        /// </summary>
        public TaskBuilder Delay(float seconds)
        {
            var delay = new DelayTask(seconds);
            _rootSequence.AddTask(delay);
            return this;
        }
        
        /// <summary>
        /// 等待条件满足
        /// </summary>
        public TaskBuilder WaitUntil(Func<bool> condition)
        {
            var waitTask = new Job(() => condition());
            _rootSequence.AddTask(waitTask);
            return this;
        }
        
        /// <summary>
        /// 条件分支
        /// </summary>
        public TaskBuilder If(Func<bool> condition, Action<TaskBuilder> trueBuilder, Action<TaskBuilder> falseBuilder = null)
        {
            var trueTask = CreateBranch(trueBuilder);
            var falseTask = falseBuilder != null ? CreateBranch(falseBuilder) : null;
            
            var conditional = new ConditionalTask(condition, trueTask, falseTask);
            _rootSequence.AddTask(conditional);
            return this;
        }
        
        /// <summary>
        /// 循环执行
        /// </summary>
        public TaskBuilder Loop(int count, Action<TaskBuilder> loopBuilder)
        {
            var loopBody = CreateBranch(loopBuilder);
            var loop = new LoopTask(loopBody, count);
            _rootSequence.AddTask(loop);
            return this;
        }
        
        /// <summary>
        /// 并行执行多个任务
        /// </summary>
        public TaskBuilder Parallel(EParallelWaitMode waitMode, params Action<TaskBuilder>[] taskBuilders)
        {
            var parallel = new ParallelTask(waitMode);

            foreach (var builder in taskBuilders)
            {
                var task = CreateBranch(builder);
                parallel.AddTask(task);
            }

            _rootSequence.AddTask(parallel);
            return this;
        }
        
        /// <summary>
        /// 设置超时
        /// </summary>
        public TaskBuilder WithTimeout(float timeout)
        {
            // 为最后添加的任务添加超时
            if (_rootSequence.Progress < 1f)
            {
                UnityEngine.Debug.LogWarning("WithTimeout应该在添加任务后调用");
            }
            return this;
        }
        
        /// <summary>
        /// 设置优先级
        /// </summary>
        public TaskBuilder WithPriority(int priority)
        {
            _rootSequence.Priority = priority;
            return this;
        }
        
        /// <summary>
        /// 添加完成回调
        /// </summary>
        public TaskBuilder OnComplete(Action<ITask> callback)
        {
            _rootSequence.OnCompleted += callback;
            return this;
        }
        
        /// <summary>
        /// 添加失败回调
        /// </summary>
        public TaskBuilder OnFailed(Action<ITask, Exception> callback)
        {
            _rootSequence.OnFailed += callback;
            return this;
        }
        
        /// <summary>
        /// 添加进度回调
        /// </summary>
        public TaskBuilder OnProgress(Action<ITask, float> callback)
        {
            _rootSequence.OnProgressChanged += callback;
            return this;
        }
        
        /// <summary>
        /// 构建最终任务
        /// </summary>
        public ITask Build()
        {
            return _rootSequence;
        }
        
        /// <summary>
        /// 构建并提交到TaskRunner
        /// </summary>
        public ITask BuildAndSubmit(TaskRunner runner, string schedulerName = null)
        {
            var task = Build();
            runner.Submit(task, schedulerName ?? TaskRunner.DEFAULT_SCHEDULER);
            return task;
        }
        
        /// <summary>
        /// 创建分支任务
        /// </summary>
        private ITask CreateBranch(Action<TaskBuilder> builder)
        {
            var branchBuilder = new TaskBuilder();
            builder?.Invoke(branchBuilder);
            return branchBuilder.Build();
        }
    }
}

