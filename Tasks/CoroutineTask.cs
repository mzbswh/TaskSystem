using System.Collections;

namespace TankSlg.TaskSystem
{
    /// <summary>
    /// 协程任务：将Unity协程包装为任务
    /// </summary>
    public class CoroutineTask : BaseTask
    {
        private readonly IEnumerator _coroutine;
        private IEnumerator _currentCoroutine;
        private bool _started;
        
        public CoroutineTask(IEnumerator coroutine, int priority = 0) : base(priority)
        {
            _coroutine = coroutine;
            _currentCoroutine = null;
            _started = false;
        }
        
        protected override bool OnExecute(float deltaTime)
        {
            // 首次执行时初始化协程
            if (!_started)
            {
                _currentCoroutine = _coroutine;
                _started = true;
            }

            if (_currentCoroutine == null)
                return true;

            // 执行协程的下一步
            try
            {
                bool hasNext = _currentCoroutine.MoveNext();
                return !hasNext; // 没有下一步则完成
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"协程任务执行出错 [ID:{Id}]: {e.Message}");
                throw;
            }
        }
        
        protected override void OnReset()
        {
            _currentCoroutine = null;
            _started = false;
        }
        
        public override void Clear()
        {
            base.Clear();
            _currentCoroutine = null;
            _started = false;
        }
    }
}

