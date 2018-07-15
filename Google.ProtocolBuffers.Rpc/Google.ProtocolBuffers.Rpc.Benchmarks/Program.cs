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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using ProtocolBuffers.Rpc.Benchmarks.TestSuites;

namespace ProtocolBuffers.Rpc.Benchmarks
{
    static class Program
    {
        static int DoHelp()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage:");
            Console.WriteLine("    ProtocolBuffers.Rpc.Benchmarks.exe [/nologo] [/wait]");
            Console.WriteLine("");
            return 0;
        }

        [STAThread]
        static int Main(string[] raw)
        {
            List<string> args = new List<string>(raw);
            bool noLogo = args.Remove("/nologo") || args.Remove("-nologo");
            bool bWait = args.Remove("/wait") || args.Remove("-wait");

            if (!noLogo)
            {
                Console.WriteLine(typeof(Program).Assembly.FullName);
                foreach (System.Reflection.AssemblyCopyrightAttribute a in typeof(Program).Assembly.GetCustomAttributes(typeof(System.Reflection.AssemblyCopyrightAttribute), false))
                    Console.WriteLine(a.Copyright);
                Console.WriteLine();
            }

            if (args.Contains("/?") || args.Contains("-?") || args.Contains("/help") || args.Contains("-help"))
                return DoHelp();

            try
            {
                string cmd;
                if(args.Count > 0)
                {
                    cmd = args[0];
                    args.RemoveAt(0);
                }
                else
                    cmd = "run";

                switch(cmd)
                {
                    case "run": Environment.ExitCode = RunAll(args); break;
                    case "client": return RunClient(args);
                    case "server": return RunServer(args);
                    default:
                        return DoHelp();
                }
            }
            catch (ApplicationException ae)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(ae.Message);
                Environment.ExitCode = -1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(e.ToString());
                Environment.ExitCode = -1;
            }

            if (bWait || Debugger.IsAttached)
            {
                Console.WriteLine();
                Console.WriteLine("Press [Enter] to continue...");
                Console.ReadLine();
            }

            return Environment.ExitCode;
        }

        private static readonly List<Process> Running = new List<Process>();

        private static readonly Dictionary<string, Func<BaseTestSuite>> Tests =
            new Dictionary<string, Func<BaseTestSuite>>(StringComparer.OrdinalIgnoreCase)
                {
                    {"ProtoBuf_LRPC", () => new ProtoBuf_LRPC()},
                    {"ProtoBuf_LRPC_Auth", () => new ProtoBuf_LRPC_Auth()},
                    {"ProtoBuf_Pipes", () => new ProtoBuf_Pipes()},
                    {"ProtoBuf_Pipes_Auth", () => new ProtoBuf_Pipes_Auth()},
                    {"ProtoBuf_Tcp_Auth", () => new ProtoBuf_Tcp_Auth()},
                    {"ProtoBuf_Wcf", () => new ProtoBuf_Wcf()},
                    {"Wcf_Pipes", () => new Wcf_Pipes()},
                    {"Wcf_Pipes_Auth", () => new Wcf_Pipes_Auth()},
                    {"Wcf_Tcp", () => new Wcf_Tcp()},
                    {"Wcf_Tcp_Auth", () => new Wcf_Tcp_Auth()},
                    {"Wcf_Http", () => new Wcf_Http()},
                };

        private static int RunAll(ICollection<string> args)
        {
            if(args.Count == 0)
                args = new List<string>(Tests.Keys);

            int failures = 0;
            foreach(string key in args)
            {
                try
                {
                    Tests[key]();
                    int clientRuns = 3;
                    foreach (int numclients in new[] { 5 })
                    foreach (int numthreads in new[] { 3 })
                    foreach (int repeated in new[] { -50000 })
                    foreach (int recordSize in new[] { 1000 })
                    {
                        TestSignals signal = new TestSignals(Guid.NewGuid().ToString("N"));
                        try
                        {
                            using (Semaphore clients = new Semaphore(numclients, numclients, signal.Name + ".clients"))
                            {
                                Start("server", key, signal, recordSize);
                                signal.ReadyWait();

                                for (int ixclient = 0; ixclient < numclients; ixclient++)
                                {
                                    Start("client", key, signal, ixclient, numthreads, clientRuns, repeated, recordSize);
                                    signal.ReadyWait(ixclient);
                                }
                                signal.Begin();

                                int lockCount = 0;
                                try
                                {
                                    for (int ixclient = 0; ixclient < numclients; ixclient++)
                                    {
                                        clients.WaitOne();
                                        lockCount++;
                                    }
                                }
                                finally
                                {
                                    for (int ixclient = 0; ixclient < lockCount; ixclient++)
                                        clients.Release();
                                }
                            }
                        }
                        finally
                        {
                            Stop(signal);
                            signal.Dispose();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine("test '{0}' failed: {1}", key, e.Message);
                    failures++;
                }
            }
            return failures;
        }

        private static void Start(params object[] oargs)
        {
            string exe = typeof(Program).Assembly.Location;
            string[] sargs = new string[oargs.Length + 1];
            for(int i=0; i < oargs.Length; i++)
                sargs[i + 1] = oargs[i].ToString();
            sargs[0] = "/nologo";

            if (Debugger.IsAttached)
            {
                new Thread(() => Program.Main(sargs)).Start();
            }
            else
            {
                ProcessStartInfo psi = new ProcessStartInfo(exe, String.Join(" ", sargs));
                psi.UseShellExecute = false;
                Running.Add(Process.Start(psi));
            }
        }

        private static void Stop(TestSignals signal)
        {
            signal.Exit();
            foreach (Process p in Running)
                p.WaitForExit();
            Running.Clear();
        }

        private static int RunServer(IList<string> args)
        {
            string name = args[0];
            args.RemoveAt(0);
            return Tests[name]().RunServer(args);
        }

        private static int RunClient(IList<string> args)
        {
            string name = args[0];
            args.RemoveAt(0);
            return Tests[name]().RunClient(args);
        }
    }
}
