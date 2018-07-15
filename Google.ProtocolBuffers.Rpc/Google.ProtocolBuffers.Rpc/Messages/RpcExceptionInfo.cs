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
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace Google.ProtocolBuffers.Rpc.Messages
{
    /// <summary>
    ///   Controls the amount of details about excpetions that will be returned to the client.
    /// </summary>
    public enum RpcErrorDetailBehavior
    {
        /// <summary>
        ///   do not include any details about the exception
        /// </summary>
        NoDetails,

        /// <summary>
        ///   include only the type of the exception
        /// </summary>
        TypeOnly,

        /// <summary>
        ///   return the contents of the Message field for excpetions
        /// </summary>
        MessageOnly,

        /// <summary>
        ///   include all details of exceptions
        /// </summary>
        FullDetails,
    }

    /// <summary>
    ///   Controls how (and if) clients resolve exception types returned by the server
    /// </summary>
    public enum RpcErrorTypeBehavior
    {
        /// <summary>
        ///   The client will treat any excpetion returned by the server as a System.ApplicationException
        /// </summary>
        NoTypeResolution,

        /// <summary>
        ///   The client will use any exception type that is defined in mscorlib.dll, all others become ApplicationException
        /// </summary>
        OnlyUseMsCorLibTypes,

        /// <summary>
        ///   The client will use excpetion types defined in any assembly already loaded into the current domain
        /// </summary>
        OnlyUseLoadedAssemblies,

        /// <summary>
        ///   Like OnlyUseLoadedAssemblies but will also resolve any assembly that provides a strong name
        /// </summary>
        OnlyLoadStrongNamed,

        /// <summary>
        ///   The client will attempt to load any assembly specifed by the exception details returned from the server
        /// </summary>
        LoadAnyAssembly,
    }

    partial class RpcExceptionInfo
    {
        private static readonly FormatterConverter FormatterConverter = new FormatterConverter();
        private static readonly StreamingContext StreamingContext = new StreamingContext(StreamingContextStates.Remoting);

        /// <summary>
        ///   Constructs the exception details for the server to return
        /// </summary>
        public static RpcExceptionInfo Create(Exception error, RpcErrorDetailBehavior details)
        {
            if (details == RpcErrorDetailBehavior.NoDetails)
            {
                return DefaultInstance;
            }

            Builder builder = CreateBuilder();
            SerializationInfo si = new SerializationInfo(error.GetType(), FormatterConverter);

            if (details == RpcErrorDetailBehavior.MessageOnly)
            {
                si.AddValue("Message", error.Message);
            }
            else if (details == RpcErrorDetailBehavior.FullDetails)
            {
                try
                {
                    error.GetObjectData(si, StreamingContext);
                    builder.SetHasFullDetails(true);
                }
                catch
                {
                }
            }

            builder.AssemblyName = si.AssemblyName;
            builder.FullTypeName = si.FullTypeName;

            foreach (SerializationEntry se in si)
            {
                switch (se.Name)
                {
                    case "ClassName":
                        if (se.Value is string)
                        {
                            builder.SetClassName((string) se.Value);
                        }
                        break;
                    case "Message":
                        if (se.Value is string)
                        {
                            builder.SetMessage((string) se.Value);
                        }
                        break;
                        //case "Data": if (se.Value is string) builder.SetData((string)se.Value); break;
                    case "InnerException":
                        if (se.Value is Exception)
                        {
                            builder.SetInnerException(Create((Exception) se.Value, details));
                        }
                        break;
                    case "HelpURL":
                        if (se.Value is string)
                        {
                            builder.SetHelpUrl((string) se.Value);
                        }
                        break;
                    case "StackTraceString":
                        if (se.Value is string)
                        {
                            builder.SetStackTraceString((string) se.Value);
                        }
                        break;
                    case "RemoteStackTraceString":
                        if (se.Value is string)
                        {
                            builder.SetRemoteStackTraceString((string) se.Value);
                        }
                        break;
                    case "RemoteStackIndex":
                        if (se.Value is int)
                        {
                            builder.SetRemoteStackIndex((int) se.Value);
                        }
                        break;
                    case "ExceptionMethod":
                        if (se.Value is string)
                        {
                            builder.SetExceptionMethod((string) se.Value);
                        }
                        break;
                    case "HResult":
                        if (se.Value is int)
                        {
                            builder.SetHResult((int) se.Value);
                        }
                        break;
                    case "Source":
                        if (se.Value is string)
                        {
                            builder.SetSource((string) se.Value);
                        }
                        break;
                    default:
                        {
                            if (se.ObjectType == typeof (String) || //se.ObjectType == typeof(byte[]) || 
                                (se.ObjectType.IsPrimitive && se.ObjectType.Assembly == typeof (String).Assembly))
                            {
                                Types.RpcExceptionData.Builder data = Types.RpcExceptionData.CreateBuilder()
                                    .SetMember(se.Name)
                                    .SetType(se.ObjectType.FullName)
                                    ;
                                if (se.Value != null)
                                {
                                    data.SetValue(se.Value is byte[]
                                                      ? Convert.ToBase64String((byte[]) se.Value)
                                                      : Convert.ToString(se.Value));
                                }
                                builder.AddExceptionData(data.Build());
                            }
                            else
                            {
                                builder.AddExceptionData(
                                    Types.RpcExceptionData.CreateBuilder().SetMember(se.Name).SetType(
                                        typeof (Object).FullName));
                            }
                            break;
                        }
                }
            }

            return builder.Build();
        }

        /// <summary>
        ///   Reconstruct the exception and raise
        /// </summary>
        [DebuggerNonUserCode, DebuggerStepThrough]
        public void ReThrow(RpcErrorTypeBehavior typeResolution)
        {
            throw CreateException(typeResolution, true);
        }

        private static readonly Regex ValidTypeName = new Regex(
            @"^[a-zA-Z_][a-zA-Z0-9_]*(?:\.[a-zA-Z_][a-zA-Z0-9_]*)*$", RegexOptions.IgnoreCase);

        #region LoadedAssemblies resolver

        private static class LoadedAssemblies
        {
            private static readonly IDictionary<string, Assembly> _loaded;

            static LoadedAssemblies()
            {
                _loaded = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);
                AppDomain.CurrentDomain.AssemblyLoad += AssemblyLoaded;
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    _loaded[a.GetName().Name] = a;
                }
            }

            private static void AssemblyLoaded(object sender, AssemblyLoadEventArgs args)
            {
                _loaded[args.LoadedAssembly.GetName().Name] = args.LoadedAssembly;
            }

            public static Assembly Lookup(AssemblyName name)
            {
                Assembly a;
                if (_loaded.TryGetValue(name.Name, out a))
                {
                    return a;
                }
                return null;
            }
        }

        #endregion

        private Type GetExceptionType(RpcErrorTypeBehavior typeResolution)
        {
            Type tException = typeof (ApplicationException);
            Assembly found = tException.Assembly;

            if (typeResolution == RpcErrorTypeBehavior.NoTypeResolution)
            {
                return tException;
            }

            if (!HasFullTypeName || !HasAssemblyName || String.IsNullOrEmpty(FullTypeName) ||
                String.IsNullOrEmpty(AssemblyName))
            {
                return tException;
            }

            if (!ValidTypeName.IsMatch(FullTypeName))
            {
                return tException;
            }

            if (typeResolution == RpcErrorTypeBehavior.OnlyUseMsCorLibTypes)
            {
                return found.GetType(FullTypeName) ?? tException;
            }

            AssemblyName name = new AssemblyName(AssemblyName);
            if (name.CodeBase != null)
            {
                return tException;
            }

            if (null != (found = LoadedAssemblies.Lookup(name)))
            {
                return found.GetType(FullTypeName) ?? tException;
            }

            if (typeResolution == RpcErrorTypeBehavior.OnlyUseLoadedAssemblies)
            {
                return tException;
            }

            if (typeResolution == RpcErrorTypeBehavior.OnlyLoadStrongNamed && name.GetPublicKeyToken() == null)
            {
                return tException;
            }

            Type test = Type.GetType(String.Format("{0}, {1}", FullTypeName, name));
            if (test != null)
            {
                return test;
            }

            if (typeResolution == RpcErrorTypeBehavior.LoadAnyAssembly)
            {
                return Type.GetType(String.Format("{0}, {1}", FullTypeName, new AssemblyName(name.Name))) ?? tException;
            }

            return tException;
        }

        /// <summary>
        ///   Constructs the exception for the client to raise
        /// </summary>
        private Exception CreateException(RpcErrorTypeBehavior typeResolution, bool top)
        {
            Exception baseEx;
            try
            {
                Type tException = GetExceptionType(typeResolution);
                if (tException == null || !typeof (Exception).IsAssignableFrom(tException))
                {
                    tException = typeof (ApplicationException);
                }

                if (!HasMessage)
                {
                    return (Exception) Activator.CreateInstance(tException);
                }

                ConstructorInfo ci;
                SerializationInfo si = new SerializationInfo(tException, FormatterConverter);
                si.AddValue("ClassName", !HasClassName ? null : ClassName);
                si.AddValue("Message", !HasMessage ? null : Message);
                si.AddValue("InnerException",
                            !HasInnerException ? null : InnerException.CreateException(typeResolution, false));
                si.AddValue("HelpURL", !HasHelpUrl ? null : HelpUrl);
                si.AddValue("StackTraceString", !HasStackTraceString ? null : StackTraceString);
                si.AddValue("RemoteStackTraceString",
                            top
                                ? (!HasStackTraceString ? null : StackTraceString)
                                : (!HasRemoteStackTraceString ? null : RemoteStackTraceString));
                si.AddValue("RemoteStackIndex", !HasRemoteStackIndex ? 0 : RemoteStackIndex);
                si.AddValue("ExceptionMethod", !HasExceptionMethod ? null : ExceptionMethod);
                si.AddValue("HResult", !HasHResult ? 0 : HResult);
                si.AddValue("Source", !HasSource ? null : Source);

                Exception ex = (Exception) FormatterServices.GetUninitializedObject(tException);
                try
                {
                    foreach (Types.RpcExceptionData data in ExceptionDataList)
                    {
                        Type t = Type.GetType(data.Type.Split(',')[0], true, false);
                        object value = !data.HasValue
                                           ? null
                                           : t == typeof (byte[])
                                                 ? Convert.FromBase64String(data.Value)
                                                 : Convert.ChangeType(data.Value, t);
                        si.AddValue(data.Member, value, t);
                    }

                    if (HasFullDetails)
                    {
                        ci =
                            tException.GetConstructor(
                                BindingFlags.CreateInstance | BindingFlags.Public | BindingFlags.NonPublic |
                                BindingFlags.Instance, null,
                                CallingConventions.Any, new[] {typeof (SerializationInfo), typeof (StreamingContext)},
                                new ParameterModifier[2]);
                        ci.Invoke(ex, new object[] {si, StreamingContext});
                        return ex;
                    }
                }
                catch
                {
                    ex = (Exception) FormatterServices.GetUninitializedObject(tException);
                }

                ci = tException.GetConstructor(new[] {typeof (string)});
                ci.Invoke(ex, new object[] {Message});
                return ex;
            }
            catch
            {
                baseEx = HasMessage ? new ApplicationException(Message) : new ApplicationException();
            }
            return baseEx;
        }
    }
}