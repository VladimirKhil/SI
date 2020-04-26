using SICore.Network.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SICore
{
    /// <summary>
    /// Логика клиента
    /// </summary>
    /// <typeparam name="A">Тип клиента</typeparam>
    /// <typeparam name="D">Тип данных клиента</typeparam>
    public abstract class Logic<A, D> : IDisposable, ILogic
        where A : IActor
        where D : Data
    {
        /// <summary>
        /// Сам клиент
        /// </summary>
        protected A _actor;

        /// <summary>
        /// Данные
        /// </summary>
        protected D _data;

        public D ClientData => _data;

        public Data Data => _data;

        protected Timer _taskTimer = null;
        protected Action _task = null;
        private readonly Stack<Tuple<Action, int>> _oldTasks = new Stack<Tuple<Action, int>>();
        protected object _taskTimerLock = new object();
        protected DateTime _finishingTime;

        internal Logic(A actor, D data)
        {
            _actor = actor;
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _taskTimer = new Timer(TaskTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        // TODO: PERF
        private void TaskTimer_Elapsed(object state)
        {
            if (_task != null)
            {
                try
                {
                    _task();
                }
                catch (Exception exc)
                {
                    _data.BackLink.SendError(exc);
                }
            }
        }

        protected internal void ExecuteImmediate()
        {
            lock (_taskTimerLock)
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Change(10, Timeout.Infinite);
                }
            }
        }

        protected StopReason _stopReason = StopReason.None;

        internal StopReason StopReason => _stopReason;

        internal virtual void Stop(StopReason reason)
        {
            if (_stopReason == StopReason.None)
            {
                _stopReason = reason;
                ExecuteImmediate();
            }
        }

        protected void PauseExecution(Action task)
        {
            var now = DateTime.Now;
            _oldTasks.Push(Tuple.Create(task, (int)((_finishingTime - now).TotalMilliseconds / 100)));
            _task = null;
        }

        protected internal void ResumeExecution(int resumeTime = 0)
        {
            if (_oldTasks.Any())
            {
                var oldTask = _oldTasks.Pop();
                Execute(oldTask.Item1, resumeTime > 0 ? resumeTime : Math.Max(1, oldTask.Item2));
            }
        }

        /// <summary>
        /// Выполнить задачу
        /// </summary>
        /// <param name="task">Выполняемая задача</param>
        /// <param name="arg">Параметр задачи</param>
        protected virtual void ExecuteTask(Tasks task, int arg) { }

        /// <summary>
        /// Запись сообщения в лог
        /// </summary>
        /// <param name="s"></param>
        public void AddLog(string s)
        {
            _data.OnAddString(null, s, LogMode.Log);
        }

        #region IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            lock (_taskTimerLock)
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Dispose();
                    _taskTimer = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        /// <summary>
        /// Выполнить действие через заданный промежуток времени
        /// </summary>
        /// <param name="task">Действие</param>
        /// <param name="taskTime">Промежуток времени</param>
        /// <param name="taskParam">Параметр действия</param>
        protected void Execute(Action task, double taskTime)
        {
            _task = task;
            lock (_taskTimerLock)
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Change((int)taskTime * 100, Timeout.Infinite);
                    _finishingTime = DateTime.Now + TimeSpan.FromMilliseconds(taskTime * 100);
                }
            }
        }

        /// <summary>
        /// Выполнить действие через заданный промежуток времени
        /// </summary>
        /// <param name="task">Действие</param>
        /// <param name="taskTime">Промежуток времени</param>
        /// <param name="arg">Параметр действия</param>
        protected void Execute(Action<int> task, double taskTime, int arg)
        {
            _task = () => task(arg);
            lock (_taskTimerLock)
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Change((int)taskTime * 100, Timeout.Infinite);
                    _finishingTime = DateTime.Now + TimeSpan.FromMilliseconds(taskTime * 100);
                }
            }
        }

        protected static int SelectRandom<T>(IEnumerable<T> list, Predicate<T> condition)
        {
            var goodItems = list
                .Select((item, index) => new { Item = item, Index = index })
                .Where(item => condition(item.Item)).ToArray();

            if (goodItems.Length == 0)
            {
                throw new Exception("goodItems.Length == 0");
            }

            var ind = Data.Rand.Next(goodItems.Length);
            return goodItems[ind].Index;
        }

        protected static int SelectRandomOnIndex<T>(IEnumerable<T> list, Predicate<int> condition)
        {
            var goodItems = list
                .Select((item, index) => new { Item = item, Index = index })
                .Where(item => condition(item.Index)).ToArray();

            var ind = Data.Rand.Next(goodItems.Length);
            return goodItems[ind].Index;
        }

        public virtual void SetInfo(IAccountInfo accountInfo)
        {
            
        }
    }
}
