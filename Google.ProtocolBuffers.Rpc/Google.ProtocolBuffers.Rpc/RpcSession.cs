#region Copyright 2010-2011 by Roger Knapp, Licensed under the Apache License, Version 2.0
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Google.ProtocolBuffers.Rpc
{
    public sealed class RpcSession : IDisposable
    {
        private static TimeSpan _expiresAfter = TimeSpan.FromMinutes(3);

        public static TimeSpan SessionTimeout
        {
            get { return _expiresAfter; }
            set { _expiresAfter = value; }
        }

        private static readonly Dictionary<Guid, RpcSession> _sessions;
        private static readonly Timer _idleTimer;
        public static bool EnableSessions = true;

        static RpcSession()
        {
            _sessions = new Dictionary<Guid, RpcSession>();
            _idleTimer = new Timer(OnIdle, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        private static void OnIdle(object o)
        {
            lock (_sessions)
            {
                foreach (RpcSession session in new List<RpcSession>(_sessions.Values))
                {
                    try
                    {
                        if ((DateTime.Now - session._lastUsed) > _expiresAfter)
                        {
                            KillSession(session);
                        }
                    }
                    catch (Exception e)
                    {
                        Trace.TraceError(e.ToString());
                    }
                }
            }
        }

        public static void KillSession(RpcSession session)
        {
            if (session == null)
            {
                return;
            }
            lock (_sessions)
                _sessions.Remove(session._key);
            session.Dispose();
        }

        public static void KillSession(Guid sessionId)
        {
            RpcSession session;
            if (_sessions.TryGetValue(sessionId, out session))
            {
                KillSession(session);
            }
        }

        public static RpcSession GetSession(Guid sessionId)
        {
            if (!EnableSessions)
            {
                throw new InvalidOperationException("Session state disabled.");
            }

            RpcSession session;

            lock (_sessions)
            {
                if (!_sessions.TryGetValue(sessionId, out session))
                {
                    _sessions.Add(sessionId, session = new RpcSession(sessionId));
                }
                session._lastUsed = DateTime.Now;
            }
            return session;
        }

        private DateTime _lastUsed;
        private readonly Guid _key;
        private readonly Dictionary<string, object> _contents;

        private RpcSession(Guid key)
        {
            _key = key;
            _lastUsed = DateTime.Now;
            _contents = new Dictionary<string, object>(StringComparer.Ordinal);
        }

        public void Dispose()
        {
            List<object> values = new List<object>(_contents.Values);
            _contents.Clear();
            foreach (object value in values)
            {
                try
                {
                    if (value is IDisposable)
                    {
                        ((IDisposable) value).Dispose();
                    }
                }
                catch (Exception e)
                {
                    Trace.TraceWarning("Exception disposing of session object of type {0}, error = {1}", value, e);
                }
            }
        }

        public void Clear()
        {
            _contents.Clear();
        }

        public int Count
        {
            get { return _contents.Count; }
        }

        public bool ContainsKey(string key)
        {
            return _contents.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return new List<string>(_contents.Keys); }
        }

        public bool Remove(string key)
        {
            return _contents.Remove(key);
        }

        public void Add<T>(string key, T value)
        {
            _contents.Add(key, value);
        }

        public T Get<T>(string key)
        {
            T result = default(T);
            if (!TryGetValue(key, out result))
            {
                throw new KeyNotFoundException();
            }
            return result;
        }

        public bool TryGetValue<T>(string key, out T value)
        {
            object obj;
            if (!_contents.TryGetValue(key, out obj) || !(obj is T))
            {
                value = default(T);
                return false;
            }
            value = (T) obj;
            return true;
        }
    }
}