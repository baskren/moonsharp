﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Execution;
using MoonSharp.RemoteDebugger;
using MoonSharp.RemoteDebugger.Network;

namespace MoonSharp
{
	class Program
	{
		static DynValue Print(ScriptExecutionContext executionContext, CallbackArguments values)
		{
			string prn = string.Join(" ", values.GetArray().Where(v => v.IsNotVoid()).Select(v => v.ToPrintString()).ToArray());
			Console.WriteLine("{0}", prn);
			return DynValue.Nil;
		}

		static DynValue Read(ScriptExecutionContext executionContext, CallbackArguments values)
		{
			double d = double.Parse(Console.ReadLine());
			return DynValue.NewNumber(d);
		}

		static StringBuilder g_TreeDump = new StringBuilder();

		[STAThread]
		static void Main(string[] args)
		{
			Console.WriteLine("Moon# {0}\nCopyright (C) 2014 Marco Mastropaolo\nhttp://www.moonsharp.org",
				Assembly.GetAssembly(typeof(Script)).GetName().Version);

			Console.WriteLine("Based on Lua 5.1 - 5.3, Copyright (C) 1994-2014 Lua.org");

			Console.WriteLine();

			if (args.Length == 1)
			{
				Script script = new Script();

				script.Globals.Set("print", DynValue.NewCallback(new CallbackFunction(Print)));

				script.DoFile(args[0]);

				Console.WriteLine("Done.");
				
				if (System.Diagnostics.Debugger.IsAttached)
					Console.ReadKey();
			}
			else
			{
				Console.WriteLine("Type <enter> twice to execute code.\n");

				Script script = new Script();

				script.Globals.Set("print", DynValue.NewCallback(new CallbackFunction(Print)));

				string cmd = "";

				while (true)
				{
					Console.Write("{0}> ", string.IsNullOrEmpty(cmd) ? "" : ">");
					string s = Console.ReadLine();

					if (s.StartsWith("!"))
					{
						ParseCommand(script, s.Substring(1));
						continue;
					}

					if (s != "")
					{
						cmd += s + "\n";
						continue;
					}

					if (cmd.Length == 0)
						continue;

					//Console.WriteLine("=====");
					//Console.WriteLine("{0}", cmd);
					//Console.WriteLine("=====");

					if (cmd == "exit")
						return;

					try
					{
						DynValue result = null;

						if (cmd.StartsWith("?"))
						{
							var code = cmd.Substring(1);
							var exp = script.CreateDynamicExpression(code);
							result = exp.Evaluate();
						}
						else
						{
							var v = script.LoadString(cmd, null, "stdin");
							result = script.Call(v);
						}

						Console.WriteLine("={0}", result);
					}
					catch (ScriptRuntimeException ex)
					{
						Console.WriteLine("{0}", ex.DecoratedMessage ?? ex.Message);
					}
					catch (Exception ex)
					{
						Console.WriteLine("{0}", ex.Message);
					}

					cmd = "";

				}




			}
		}

		static RemoteDebuggerService m_Debugger;

		private static void ParseCommand(Script S, string p)
		{
			if (p == "debug" && m_Debugger == null)
			{
				m_Debugger = new RemoteDebuggerService();
				m_Debugger.Attach(S, "MoonSharp REPL interpreter");
				Process.Start(m_Debugger.HttpUrlStringLocalHost);
			}
			if (p.StartsWith("run"))
			{
				p = p.Substring(3).Trim();
				S.DoFile(p);
			}
			if (p == "!")
			{
				ParseCommand(S, "debug");
				ParseCommand(S, @"run c:\temp\test.lua");
			}
		}

		static void m_Server_DataReceivedAny(object sender, Utf8TcpPeerEventArgs e)
		{
			Console.WriteLine("RCVD: {0}", e.Message);
			e.Peer.Send(e.Message.ToUpper());
		}


	}
}
