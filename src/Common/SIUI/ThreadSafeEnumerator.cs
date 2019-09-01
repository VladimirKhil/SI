using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace SIGame.UI
{
    internal sealed class ThreadSafeEnumerator<T>: IEnumerator<T>
    {
        private IEnumerator<T> inner = null;
        private object sync = null;

        public ThreadSafeEnumerator(IEnumerator<T> inner, object sync)
        {
            this.inner = inner;
            this.sync = sync;

            Monitor.Enter(this.sync);
        }

        public T Current
        {
            get { return this.inner.Current; }
        }

        public void Dispose()
        {
            //this.inner.Dispose();
            Monitor.Exit(this.sync);
        }

        object System.Collections.IEnumerator.Current
        {
            get { return this.inner.Current; }
        }

        public bool MoveNext()
        {
            return this.inner.MoveNext();
        }

        public void Reset()
        {
            this.inner.Reset();
        }
    }
}
