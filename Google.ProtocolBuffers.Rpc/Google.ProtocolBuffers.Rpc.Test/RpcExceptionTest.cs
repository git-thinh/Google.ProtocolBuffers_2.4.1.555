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
using System.Diagnostics;
using System.Reflection;
using Google.ProtocolBuffers.Rpc.Messages;
using NUnit.Framework;

namespace Google.ProtocolBuffers
{
    /// <summary>
    ///   Tests to cover the serialization and deserialization of exceptions given the various global policies.
    /// </summary>
    [TestFixture]
    public class RpcExceptionTest
    {
        private readonly Exception ThrownException;

        public RpcExceptionTest()
        {
            ThrownException = AsThrown(new ArgumentNullException());
        }

        [DebuggerNonUserCode, DebuggerStepThrough]
        private Exception AsThrown(Exception e)
        {
            try
            {
                throw e;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        [Test]
        public void TestBehaviorNoDetails()
        {
            RpcExceptionInfo msg = RpcExceptionInfo.Create(ThrownException, RpcErrorDetailBehavior.NoDetails);
            Assert.IsFalse(msg.HasAssemblyName);
            Assert.IsFalse(msg.HasFullTypeName);
            Assert.IsFalse(msg.HasMessage);
            //basically it should be empty:
            Assert.AreEqual(new byte[0], msg.ToByteArray());

            try
            {
                msg.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof (ApplicationException), e.GetType());
            }
        }

        [Test]
        public void TestBehaviorTypeOnly()
        {
            RpcExceptionInfo msg = RpcExceptionInfo.Create(ThrownException, RpcErrorDetailBehavior.TypeOnly);
            Assert.IsTrue(msg.HasAssemblyName);
            Assert.IsTrue(msg.HasFullTypeName);
            Assert.IsFalse(msg.HasMessage);
            Assert.IsFalse(msg.HasSource);
            Assert.IsFalse(msg.HasStackTraceString);
            Assert.IsFalse(msg.HasRemoteStackTraceString);

            Assert.AreEqual(typeof (string).Assembly.FullName, msg.AssemblyName);
            Assert.AreEqual(typeof (ArgumentNullException).FullName, msg.FullTypeName);

            try
            {
                msg.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception e)
            {
                Assert.AreEqual(typeof (ArgumentNullException), e.GetType());
            }
        }

        [Test]
        public void TestBehaviorMessageOnly()
        {
            RpcExceptionInfo msg = RpcExceptionInfo.Create(AsThrown(new InvalidOperationException("message")),
                                                           RpcErrorDetailBehavior.MessageOnly);
            Assert.IsTrue(msg.HasAssemblyName);
            Assert.IsTrue(msg.HasFullTypeName);
            Assert.IsTrue(msg.HasMessage);
            Assert.IsFalse(msg.HasSource);
            Assert.IsFalse(msg.HasStackTraceString);
            Assert.IsFalse(msg.HasRemoteStackTraceString);

            Assert.AreEqual(typeof (string).Assembly.FullName, msg.AssemblyName);
            Assert.AreEqual(typeof (InvalidOperationException).FullName, msg.FullTypeName);
            Assert.AreEqual("message", msg.Message);

            try
            {
                msg.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception e)
            {
                Assert.AreEqual("message", e.Message);
                Assert.AreEqual(typeof (InvalidOperationException), e.GetType());
            }
        }

        [Test]
        public void TestBehaviorFullDetails()
        {
            RpcExceptionInfo msg = RpcExceptionInfo.Create(AsThrown(new InvalidOperationException("message")),
                                                           RpcErrorDetailBehavior.FullDetails);
            Assert.IsTrue(msg.HasAssemblyName);
            Assert.IsTrue(msg.HasFullTypeName);
            Assert.IsTrue(msg.HasMessage);
            Assert.IsTrue(msg.HasSource);
            Assert.IsTrue(msg.HasStackTraceString);
            Assert.IsTrue(msg.StackTraceString.Contains(GetType().FullName + ".AsThrown(Exception e)"));
            Assert.IsFalse(msg.HasRemoteStackTraceString);

            Assert.AreEqual(typeof (string).Assembly.FullName, msg.AssemblyName);
            Assert.AreEqual(typeof (InvalidOperationException).FullName, msg.FullTypeName);
            Assert.AreEqual("message", msg.Message);

            try
            {
                msg.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception e)
            {
                Assert.AreEqual("message", e.Message);
                Assert.AreEqual(typeof (InvalidOperationException), e.GetType());
                //make sure the original stack is still there as well?
                Assert.IsTrue(e.StackTrace.Contains(GetType().FullName + ".AsThrown(Exception e)"));
                Assert.IsTrue(e.StackTrace.Contains(GetType().FullName + ".TestBehaviorFullDetails()"));
            }
        }

        [Test]
        public void TestClientNoTypeResolution()
        {
            Exception e = null;
            try
            {
                RpcExceptionInfo.Create(ThrownException, RpcErrorDetailBehavior.FullDetails).ReThrow(
                    RpcErrorTypeBehavior.NoTypeResolution);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual(typeof (ApplicationException), e.GetType());
            Assert.AreEqual(ThrownException.Message, e.Message);
        }

        [Test]
        public void TestClientOnlyUseMsCorLibTypes()
        {
            Exception e = new Exception();
            try
            {
                RpcExceptionInfo.Create(ThrownException, RpcErrorDetailBehavior.FullDetails)
                    .ReThrow(RpcErrorTypeBehavior.OnlyUseMsCorLibTypes);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual(typeof (ArgumentNullException), e.GetType());
            Assert.AreEqual(ThrownException.Message, e.Message);

            //again with a different exception type that is not in mscore:
            try
            {
                RpcExceptionInfo.Create(AsThrown(new UninitializedMessageException(RpcVoid.DefaultInstance)),
                                        RpcErrorDetailBehavior.FullDetails)
                    .ReThrow(RpcErrorTypeBehavior.OnlyUseMsCorLibTypes);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual(typeof (ApplicationException), e.GetType());
        }

        [Test]
        public void TestClientOnlyUseLoadedAssemblies()
        {
            //this test expects that System.Web.dll is not already loaded.
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Assert.AreNotEqual(a.GetName().Name, "System.Web");
            }

            RpcExceptionInfo exInfo = RpcExceptionInfo.CreateBuilder()
                .SetAssemblyName("System.Web, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")
                .SetFullTypeName("System.Web.HttpCompileException")
                .Build();

            Exception e = new Exception();
            try
            {
                exInfo.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual(typeof (ApplicationException), e.GetType());
            Assembly.Load(exInfo.AssemblyName);

            try
            {
                exInfo.ReThrow(RpcErrorTypeBehavior.OnlyUseLoadedAssemblies);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual("System.Web.HttpCompileException", e.GetType().FullName);
            Assert.IsNotEmpty(e.Message);
        }

        [Test]
        public void TestClientOnlyLoadStrongNamed()
        {
            //this test expects that System.Web.dll is not already loaded.
            foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
            {
                Assert.AreNotEqual(a.GetName().Name, "System.Design");
            }

            RpcExceptionInfo exInfo = RpcExceptionInfo.CreateBuilder()
                .SetAssemblyName("System.Design, Version=2.0.0.0, Culture=neutral")
                .SetFullTypeName("System.Data.Design.TypedDataSetGeneratorException")
                .Build();

            Exception e = new Exception();
            try
            {
                exInfo.ReThrow(RpcErrorTypeBehavior.OnlyLoadStrongNamed);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual(typeof (ApplicationException), e.GetType());

            //now provide a key'd assembly name:
            exInfo =
                exInfo.ToBuilder().SetAssemblyName(
                    "System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Build();

            try
            {
                exInfo.ReThrow(RpcErrorTypeBehavior.OnlyLoadStrongNamed);
            }
            catch (Exception ex)
            {
                e = ex;
            }

            Assert.AreEqual("System.Data.Design.TypedDataSetGeneratorException", e.GetType().FullName);
            Assert.IsNotEmpty(e.Message);
        }

        [Test, ExpectedException(typeof (ApplicationException))]
        public void TestClientLoadAnyAssemblyInvalid()
        {
            RpcExceptionInfo exInfo = RpcExceptionInfo.CreateBuilder()
                .SetAssemblyName(@"Me.Oh.My")
                .SetFullTypeName("System.ASDF.Abc123Exception")
                .Build();
            exInfo.ReThrow(RpcErrorTypeBehavior.LoadAnyAssembly);
        }
    }
}