// ============================================================================
// 任务系统使用示例
// ============================================================================

using TankSlg.TaskSystem;
using UnityEngine;

namespace TankSlg.Examples
{
    /// <summary>
    /// 任务系统使用示例
    /// </summary>
    public class TaskSystemExample : MonoBehaviour
    {
        private TaskRunner _taskRunner;

        void Start()
        {
            // 创建TaskRunner
            _taskRunner = new TaskRunner();

            // 示例1: 简单的Job任务
            SimpleJobExample();

            // 示例2: 跨帧Operation任务
            OperationExample();

            // 示例3: 顺序任务（Sequence）
            SequenceExample();

            // 示例4: 并行任务（Parallel）
            ParallelExample();

            // 示例5: 使用TaskBuilder构建复杂任务
            BuilderExample();

            // 示例6: 使用扩展方法
            ExtensionExample();

            // 示例7: 依赖任务
            DependencyExample();

            // 示例8: 对象池使用
            PoolExample();
        }

        void Update()
        {
            // 每帧更新TaskRunner
            _taskRunner?.Update(UnityEngine.Time.deltaTime);
        }

        // 示例1: 简单的Job任务
        void SimpleJobExample()
        {
            var job = new Job(() =>
            {
                Debug.Log("Job任务执行!");
            });

            _taskRunner.Submit(job);
        }

        // 示例2: 跨帧Operation任务
        void OperationExample()
        {
            var operation = new Operation((progress) =>
            {
                // 模拟耗时操作
                Debug.Log($"Operation进度: {progress}");

                // 返回true表示完成
                return progress >= 0.9f;
            });

            // 手动更新进度
            operation.OnProgressChanged += (task, p) =>
            {
                if (task is Operation op)
                {
                    op.SetProgress(p + 0.1f);
                }
            };

            _taskRunner.Submit(operation);
        }

        // 示例3: 顺序任务
        void SequenceExample()
        {
            var sequence = new SequenceTask()
                .AddTask(new Job(() => Debug.Log("步骤1")))
                .AddTask(new DelayTask(1f))
                .AddTask(new Job(() => Debug.Log("步骤2")))
                .AddTask(new DelayTask(1f))
                .AddTask(new Job(() => Debug.Log("步骤3")));

            sequence.OnCompleted += (task) => Debug.Log("Sequence完成!");

            _taskRunner.Submit(sequence);
        }

        // 示例4: 并行任务
        void ParallelExample()
        {
            var parallel = new ParallelTask(EParallelWaitMode.WaitAll)
                .AddTask(new Job(() => Debug.Log("并行任务A")))
                .AddTask(new Job(() => Debug.Log("并行任务B")))
                .AddTask(new Job(() => Debug.Log("并行任务C")));

            parallel.OnCompleted += (task) => Debug.Log("所有并行任务完成!");

            _taskRunner.Submit(parallel);
        }

        // 示例5: 使用TaskBuilder
        void BuilderExample()
        {
            TaskBuilder.Create()
                .Do(() => Debug.Log("开始"))
                .Delay(1f)
                .Do(() => Debug.Log("延迟1秒后"))
                .If(
                    () => Random.value > 0.5f,
                    builder => builder.Do(() => Debug.Log("条件为真")),
                    builder => builder.Do(() => Debug.Log("条件为假"))
                )
                .Loop(3, builder => builder.Do(() => Debug.Log("循环执行")))
                .OnComplete(task => Debug.Log("任务完成!"))
                .BuildAndSubmit(_taskRunner);
        }

        // 示例6: 使用扩展方法
        void ExtensionExample()
        {
            new Job(() => Debug.Log("第一步"))
                .Then(() => Debug.Log("第二步"))
                .ThenDelay(1f)
                .Then(() => Debug.Log("延迟后执行"))
                .WithPriority(10)
                .OnComplete(task => Debug.Log("链式任务完成"))
                .Submit(_taskRunner);
        }

        // 示例7: 依赖任务
        void DependencyExample()
        {
            // 创建依赖任务
            var task1 = new Job(() => Debug.Log("任务1"));
            var task2 = new Job(() => Debug.Log("任务2"));
            var task3 = new Job(() => Debug.Log("任务3 - 依赖任务1和2"));

            // 设置依赖关系
            task3.AddDependency(task1);
            task3.AddDependency(task2);

            // 提交任务
            _taskRunner.Submit(task1);
            _taskRunner.Submit(task2);
            _taskRunner.Submit(task3); // 会等待task1和task2完成后执行
        }

        // 示例8: 对象池使用
        void PoolExample()
        {
            // 从对象池获取任务
            var sequence = TaskPool.Instance.CreateSequence();
            sequence
                .AddTask(new Job(() => Debug.Log("池化任务1")))
                .AddTask(new Job(() => Debug.Log("池化任务2")));

            // 自动回收到对象池
            TaskPool.Instance.AutoRelease(sequence);

            _taskRunner.Submit(sequence);
        }

        // 示例9: 错误处理和重试
        void ErrorHandlingExample()
        {
            var task = new Job(() =>
            {
                if (Random.value < 0.5f)
                {
                    throw new System.Exception("随机错误");
                }
                Debug.Log("任务成功");
            });

            // 设置重试次数
            task.MaxRetryCount = 3;

            task.OnFailed += (t, ex) =>
            {
                Debug.LogError($"任务失败: {ex.Message}");
            };

            _taskRunner.Submit(task);
        }

        // 示例10: 监控和统计
        void MonitoringExample()
        {
            // 获取运行统计
            Debug.Log(_taskRunner.GetStatistics());

            // 获取对象池统计
            Debug.Log(TaskPool.Instance.GetStatistics());

            // 查询特定状态的任务
            var runningTasks = _taskRunner.GetTasksByStatus(ETaskStatus.Running);
            Debug.Log($"正在运行的任务数: {runningTasks.Count}");
        }

        void OnDestroy()
        {
            // 清理
            _taskRunner?.Clear();
        }
    }
}

