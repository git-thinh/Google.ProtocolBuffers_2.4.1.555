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
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace Google.ProtocolBuffers.Rpc
{
    /// <summary>
    ///   Exception class: RpcCommunicationException : ApplicationException
    ///   Unspecified communications error.
    /// </summary>
    [Serializable]
    [DebuggerStepThrough]
    [DebuggerNonUserCode]
    [CompilerGenerated]
    [GeneratedCode("CSharpTest.Net.Generators", "1.10.1102.349")]
    public sealed class RpcCommunicationException : ApplicationException
    {
        private static string DefaultMessage = "Unspecified communications error.";

        /// <summary>
        ///   Serialization constructor
        /// </summary>
        private RpcCommunicationException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        /// <summary>
        ///   Unspecified communications error.
        /// </summary>
        public RpcCommunicationException()
            : base(DefaultMessage)
        {
        }

        /// <summary>
        ///   Unspecified communications error.
        /// </summary>
        public RpcCommunicationException(Exception innerException)
            : base(DefaultMessage, innerException)
        {
        }

        /// <summary>
        ///   Unspecified communications error.
        /// </summary>
        public RpcCommunicationException(string message)
            : base(message ?? DefaultMessage)
        {
        }

        /// <summary>
        ///   Unspecified communications error.
        /// </summary>
        public RpcCommunicationException(string message, Exception innerException)
            : base(message ?? DefaultMessage, innerException)
        {
        }

        /// <summary>
        ///   if(condition == false) throws Unspecified communications error.
        /// </summary>
        public static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new RpcCommunicationException();
            }
        }
    }
}