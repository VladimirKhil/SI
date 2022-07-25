using SICore.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Utils;

namespace SICore
{
    /// <summary>
    /// Represents agent logic.
    /// </summary>
    /// <typeparam name="D">Agent data type.</typeparam>
    public abstract class Logic<D> : ILogic
        where D : Data
    {
        /// <summary>
        /// Agent data.
        /// </summary>
        protected D _data; // TODO: field must be private. Implement a property for protected access.

        /// <summary>
        /// Typed agent data.
        /// </summary>
        public D ClientData => _data;

        /// <summary>
        /// Common-typed agent data.
        /// </summary>
        public Data Data => _data;

        private Timer _taskTimer = null;
        private int _taskArgument = -1;
        private readonly Stack<Tuple<int, int, int>> _oldTasks = new();
        
        private readonly Lock _taskTimerLock = new Lock(nameof(_taskTimerLock));
        
        /// <summary>
        /// Estimated time for current task to fire.
        /// </summary>
        private DateTime _finishingTime;

        internal int CurrentTask { get; private set; } = -1;

        internal int NextTask => _oldTasks.Any() ? _oldTasks.Peek().Item1 : -1;

        protected IEnumerable<Tuple<int, int, int>> OldTasks => _oldTasks;

        internal Logic(D data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _taskTimer = new Timer(TaskTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        // TODO: PERF
        private void TaskTimer_Elapsed(object state)
        {
            if (CurrentTask == -1)
            {
                return;
            }

            try
            {
                ExecuteTask(CurrentTask, _taskArgument);
            }
            catch (Exception exc)
            {
                _data.BackLink.SendError(exc);
            }
        }

        protected virtual internal void ExecuteImmediate() =>
            _taskTimerLock.WithLock(() =>
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Change(10, Timeout.Infinite);
                }
            });

        protected StopReason _stopReason = StopReason.None;

        internal StopReason StopReason => _stopReason;

        internal virtual bool Stop(StopReason reason)
        {
            if (_stopReason == StopReason.None)
            {
                _stopReason = reason;
                ExecuteImmediate();

                return true;
            }

            return false;
        }

        protected void PauseExecution(int task, int taskArgument)
        {
            var now = DateTime.UtcNow;

            // Saving running task, its argument and left time
            _oldTasks.Push(Tuple.Create(task, taskArgument, (int)((_finishingTime - now).TotalMilliseconds / 100)));

            CurrentTask = -1;
        }

        protected internal void ResumeExecution(int resumeTime = 0)
        {
            if (!_oldTasks.Any())
            {
                throw new Exception("Cannot resume execution: no saved task!");
            }

            var oldTask = _oldTasks.Pop();
            ScheduleExecution(oldTask.Item1, oldTask.Item2, resumeTime > 0 ? resumeTime : Math.Max(1, oldTask.Item3));
        }

        protected internal void UpdatePausedTask(int task, int taskArgument, int taskTime)
        {
            if (_oldTasks.Any())
            {
                _oldTasks.Pop();
            }

            _oldTasks.Push(Tuple.Create(task, taskArgument, taskTime));
        }

        protected void ClearOldTasks() => _oldTasks.Clear();

        /// <summary>
        /// Выполнить задачу
        /// </summary>
        /// <param name="task">Выполняемая задача</param>
        /// <param name="arg">Параметр задачи</param>
        protected virtual void ExecuteTask(int task, int arg) { }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="s"></param>
        public void AddLog(string s)
        {
            _data.OnAddString(null, s, LogMode.Log);
        }

        #region IDisposable Members

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            await _taskTimerLock.TryLockAsync(
                () =>
                {
                    if (_taskTimer != null)
                    {
                        _taskTimer.Dispose();
                        _taskTimer = null;
                    }
                },
                5000);

            _taskTimerLock.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        protected void SetTask(int task, int taskArgument)
        {
            CurrentTask = task;

            _taskArgument = taskArgument;
        }

        /// <summary>
        /// Выполнить действие через заданный промежуток времени
        /// </summary>
        /// <param name="task">Действие</param>
        /// <param name="taskArgument">Параметр действия</param>
        /// <param name="taskTime">Промежуток времени</param>
        protected void ScheduleExecution(int task, int taskArgument, double taskTime)
        {
            SetTask(task, taskArgument);
            RunTaskTimer(taskTime);
        }

        protected void RunTaskTimer(double taskTime) =>
            _taskTimerLock.WithLock(() =>
            {
                if (_taskTimer != null && taskTime > 0 && taskTime < 10 * 60 * 10) // 10 min
                {
                    _taskTimer.Change((int)taskTime * 100, Timeout.Infinite);
                    _finishingTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(taskTime * 100);
                }
            });

        protected int SelectRandom<T>(IEnumerable<T> list, Predicate<T> condition) =>
            list.SelectRandom(condition, _data.Rand);

        protected int SelectRandomOnIndex<T>(IEnumerable<T> list, Predicate<int> condition) =>
            list.SelectRandomOnIndex(condition, _data.Rand);

        public string GetRandomString(string resource) => _data.Rand.GetRandomString(resource);
    }
}
