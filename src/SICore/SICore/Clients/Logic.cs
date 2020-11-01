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
    /// <typeparam name="D">Тип данных клиента</typeparam>
    public abstract class Logic<D> : IDisposable, ILogic
        where D : Data
    {
        /// <summary>
        /// Данные
        /// </summary>
        protected D _data;

        public D ClientData => _data;

        public Data Data => _data;

        private Timer _taskTimer = null;
        private int _taskArgument = -1;
        private readonly Stack<Tuple<int, int, int>> _oldTasks = new Stack<Tuple<int, int, int>>();
        private readonly object _taskTimerLock = new object();
        private DateTime _finishingTime;

        internal int CurrentTask { get; private set; } = -1;

        internal int NextTask => _oldTasks.Any() ? _oldTasks.Peek().Item1 : -1;

        internal IEnumerable<Tuple<int, int, int>> OldTasks => _oldTasks;

        internal Logic(D data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _taskTimer = new Timer(TaskTimer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
        }

        // TODO: PERF
        private void TaskTimer_Elapsed(object state)
        {
            if (CurrentTask != -1)
            {
                try
                {
                    ExecuteTask(CurrentTask, _taskArgument);
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

        protected void PauseExecution(int task, int taskArgument)
        {
            var now = DateTime.UtcNow;
            _oldTasks.Push(Tuple.Create(task, taskArgument, (int)((_finishingTime - now).TotalMilliseconds / 100)));
            CurrentTask = -1;
        }

        protected internal void ResumeExecution(int resumeTime = 0)
        {
            if (_oldTasks.Any())
            {
                var oldTask = _oldTasks.Pop();
                ScheduleExecution(oldTask.Item1, oldTask.Item2, resumeTime > 0 ? resumeTime : Math.Max(1, oldTask.Item3));
            }
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

        protected virtual void Dispose(bool disposing)
        {
            var locked = Monitor.TryEnter(_taskTimerLock, TimeSpan.FromSeconds(5.0));
            if (!locked)
            {
                ClientData.BackLink.OnError(new Exception($"Cannot lock {nameof(_taskTimerLock)}!"));
            }

            try
            {
                if (_taskTimer != null)
                {
                    _taskTimer.Dispose();
                    _taskTimer = null;
                }
            }
            finally
            {
                if (locked)
                {
                    Monitor.Exit(_taskTimerLock);
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
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
        /// <param name="taskTime">Промежуток времени</param>
        /// <param name="taskParam">Параметр действия</param>
        protected void ScheduleExecution(int task, int taskArgument, double taskTime)
        {
            SetTask(task, taskArgument);
            RunTaskTimer(taskTime);
        }

        protected void RunTaskTimer(double taskTime)
        {
            lock (_taskTimerLock)
            {
                if (_taskTimer != null && taskTime > 0 && taskTime < 10 * 60 * 10) // 10 min
                {
                    _taskTimer.Change((int)taskTime * 100, Timeout.Infinite);
                    _finishingTime = DateTime.UtcNow + TimeSpan.FromMilliseconds(taskTime * 100);
                }
            }
        }

        protected int SelectRandom<T>(IEnumerable<T> list, Predicate<T> condition)
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

        protected int SelectRandomOnIndex<T>(IEnumerable<T> list, Predicate<int> condition)
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

        /// <summary>
        /// Получить случайную строку ресурса
        /// </summary>
        /// <param name="resource">Строки ресурса, разделённые точкой с запятой</param>
        /// <returns>Одна из строк ресурса (случайная)</returns>
        public string GetRandomString(string resource)
        {
            var resources = resource.Split(';');
            var index = Data.Rand.Next(resources.Length);

            return resources[index];
        }
    }
}
