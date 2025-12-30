using System;
using System.Collections;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 任务扩展方法
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// 链式添加后续任务
        /// </summary>
        public static SequenceTask Then(this ITask task, ITask nextTask)
        {
            var sequence = new SequenceTask();
            sequence.AddTask(task);
            sequence.AddTask(nextTask);
            return sequence;
        }
        
        /// <summary>
        /// 链式添加后续Action
        /// </summary>
        public static SequenceTask Then(this ITask task, Action action)
        {
            return task.Then(new Job(action));
        }
        
        /// <summary>
        /// 添加延迟
        /// </summary>
        public static SequenceTask ThenDelay(this ITask task, float seconds)
        {
            return task.Then(new DelayTask(seconds));
        }
        
        /// <summary>
        /// 添加完成回调
        /// </summary>
        public static T OnComplete<T>(this T task, Action<ITask> callback) where T : ITask
        {
            if (task is ITaskCallback taskCallback)
            {
                taskCallback.OnCompleted += callback;
            }
            return task;
        }
        
        /// <summary>
        /// 添加失败回调
        /// </summary>
        public static T OnFail<T>(this T task, Action<ITask, Exception> callback) where T : ITask
        {
            if (task is ITaskCallback taskCallback)
            {
                taskCallback.OnFailed += callback;
            }
            return task;
        }
        
        /// <summary>
        /// 添加取消回调
        /// </summary>
        public static T OnCancel<T>(this T task, Action<ITask> callback) where T : ITask
        {
            if (task is ITaskCallback taskCallback)
            {
                taskCallback.OnCancelled += callback;
            }
            return task;
        }
        
        /// <summary>
        /// 添加进度回调
        /// </summary>
        public static T OnProgress<T>(this T task, Action<ITask, float> callback) where T : ITask
        {
            if (task is ITaskCallback taskCallback)
            {
                taskCallback.OnProgressChanged += callback;
            }
            return task;
        }
        
        /// <summary>
        /// 设置优先级
        /// </summary>
        public static T WithPriority<T>(this T task, int priority) where T : ITask
        {
            task.Priority = priority;
            return task;
        }
        
        /// <summary>
        /// 设置超时
        /// </summary>
        public static TimeoutTask WithTimeout(this ITask task, float timeoutSeconds)
        {
            return new TimeoutTask(task, timeoutSeconds);
        }
        
        /// <summary>
        /// 添加依赖
        /// </summary>
        public static T DependsOn<T>(this T task, ITask dependency) where T : BaseTask
        {
            task.AddDependency(dependency);
            return task;
        }
        
        /// <summary>
        /// 添加多个依赖
        /// </summary>
        public static T DependsOn<T>(this T task, params ITask[] dependencies) where T : BaseTask
        {
            if (dependencies != null)
            {
                foreach (var dependency in dependencies)
                {
                    task.AddDependency(dependency);
                }
            }
            return task;
        }
        
        /// <summary>
        /// 设置重试次数
        /// </summary>
        public static T WithRetry<T>(this T task, int maxRetryCount) where T : BaseTask
        {
            task.MaxRetryCount = maxRetryCount;
            return task;
        }
        
        /// <summary>
        /// 转换为循环任务
        /// </summary>
        public static LoopTask Repeat(this ITask task, int count)
        {
            return new LoopTask(task, count);
        }
        
        /// <summary>
        /// 转换为条件循环任务
        /// </summary>
        public static LoopTask RepeatUntil(this ITask task, Func<bool> breakCondition)
        {
            return new LoopTask(task, breakCondition);
        }
        
        /// <summary>
        /// 自动释放到对象池
        /// </summary>
        public static T AutoRelease<T>(this T task) where T : ITask
        {
            TaskPool.Instance.AutoRelease(task);
            return task;
        }
        
        /// <summary>
        /// 提交到TaskRunner
        /// </summary>
        public static T Submit<T>(this T task, TaskRunner runner, string schedulerName = null) where T : ITask
        {
            runner.Submit(task, schedulerName ?? TaskRunner.DEFAULT_SCHEDULER);
            return task;
        }
        
        /// <summary>
        /// 提交到默认TaskRunner（假设有全局实例）
        /// </summary>
        public static T Submit<T>(this T task) where T : ITask
        {
            // 这里假设有全局TaskRunner，实际使用时需要根据项目情况调整
            // TaskRunner.Global.Submit(task);
            UnityEngine.Debug.LogWarning("请使用 Submit(TaskRunner runner) 方法");
            return task;
        }
    }
}

