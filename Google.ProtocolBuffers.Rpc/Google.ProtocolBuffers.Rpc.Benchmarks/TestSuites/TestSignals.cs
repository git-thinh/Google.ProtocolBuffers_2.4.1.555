#region Copyright 2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
/* Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *   http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
#endregion
using System;
using System.Threading;

namespace ProtocolBuffers.Rpc.Benchmarks.TestSuites
{
    class TestSignals : IDisposable
    {
        private readonly string _signal;
        private EventWaitHandle _ready, _start, _exit;

        public TestSignals(string signal)
        {
            _signal = signal;
            _ready = new EventWaitHandle(false, EventResetMode.AutoReset, signal + ".ready");
            _start = new EventWaitHandle(false, EventResetMode.ManualReset, signal + ".start");
            _exit = new EventWaitHandle(false, EventResetMode.ManualReset, signal + ".exit");
        }

        public string Name { get { return _signal; } }
        public override string ToString() { return _signal; }

        public void Reset()
        {
            _ready.Reset();
            _start.Reset();
            _start.Reset();
            for( int i=0; i < 100; i++ )
            {
                using (EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset, _signal + ".ready" + i))
                {
                    if (handle.WaitOne(0, false))
                        handle.Reset();
                    else
                        break;
                }
            }
        }

        public void Dispose()
        {
            _ready.Close();
            _start.Close();
            _exit.Close();
        }

        public void Ready()
        {
            _ready.Set();
        }

        public void Ready(int ixclient)
        {
            using (EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset, _signal + ".ready" + ixclient))
                handle.Set();
        }


        public void ReadyWait()
        {
            if (!_ready.WaitOne(TimeSpan.FromSeconds(30)))
                throw new TimeoutException();
        }

        public void ReadyWait(int ixclient)
        {
            using (EventWaitHandle handle = new EventWaitHandle(false, EventResetMode.ManualReset, _signal + ".ready" + ixclient))
                if (!handle.WaitOne(TimeSpan.FromSeconds(30)))
                    throw new TimeoutException();
        }

        public void Begin()
        {
            _start.Set();
        }

        public bool BeginWait()
        {
            int response = WaitHandle.WaitAny(new[] {_exit, _start});
            if(response == WaitHandle.WaitTimeout)
                throw new TimeoutException();
            return (response > 0);
        }

        public void Exit()
        {
            _exit.Set();
        }

        public void ExitWait()
        {
            if (!_exit.WaitOne(TimeSpan.FromMinutes(5)))
                throw new TimeoutException();
        }

        public IDisposable AcquireSemaphore()
        {
            return new Semaphore(_signal + ".clients");
        }

        class Semaphore : IDisposable
        {
            private System.Threading.Semaphore _lock;
            public Semaphore(string name)
            {
                _lock = new System.Threading.Semaphore(1, 1, name);
                _lock.WaitOne();
            }
            public void Dispose()
            {
                _lock.Release();
                _lock.Close();
            }
        }
    }
}