using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace NuclearOption.DedicatedServer.Commands
{
	public class ServerRemoteCommands : IDisposable
	{
		public readonly struct MainThreadAction
		{
			public readonly string Name;

			public readonly Action Action;

			public MainThreadAction(string name, Action action)
			{
				Name = name;
				Action = action;
			}
		}

		public readonly Dictionary<string, ServerCommand> Commands = new Dictionary<string, ServerCommand>(StringComparer.OrdinalIgnoreCase);

		private readonly byte[] messageBuffer;

		private readonly byte[] sendBuffer;

		private readonly ConcurrentQueue<MainThreadAction> mainThreadQueue = new ConcurrentQueue<MainThreadAction>();

		private readonly Thread mainThread;

		private TcpListener tcpListener;

		private Thread runThread;

		private CancellationTokenSource cancellation;

		private string runningCommand;

		public static ServerRemoteCommands Instance { get; private set; }

		public static ServerRemoteCommands GetOrCreate(int receiveBufferSize = 2048, int sendBufferSize = 131027)
		{
			if (Instance == null)
			{
				Instance = new ServerRemoteCommands(receiveBufferSize, sendBufferSize);
			}
			return Instance;
		}

		public ServerRemoteCommands(int receiveBufferSize = 2048, int sendBufferSize = 131027)
		{
			messageBuffer = new byte[receiveBufferSize];
			sendBuffer = new byte[sendBufferSize];
			_ = Time.frameCount;
			mainThread = Thread.CurrentThread;
		}

		public void AddCommands(List<ServerCommand> commands)
		{
			foreach (ServerCommand command in commands)
			{
				if (Commands.TryAdd(command.Name, command))
				{
					ColorLog<ServerRemoteCommands>.Info("Adding command " + command.Name);
				}
				else
				{
					ColorLog<ServerRemoteCommands>.Info("Failed to add command " + command.Name + " because one with the same name already exists");
				}
			}
		}

		public void RunOnMainThread(Action action)
		{
			mainThreadQueue.Enqueue(new MainThreadAction(runningCommand, action));
		}

		public (bool ok, T result) RunOnMainThreadBlocking<T>(Func<(bool ok, T result)> action)
		{
			if (Thread.CurrentThread == mainThread)
			{
				ColorLog<ServerRemoteCommands>.Info("Already running on main thread, returning result right away");
				return action();
			}
			TaskCompletionSource<(bool, T)> source = new TaskCompletionSource<(bool, T)>();
			RunOnMainThread(delegate
			{
				Stopwatch stopwatch2 = Stopwatch.StartNew();
				try
				{
					(bool, T) result = action();
					ColorLog<ServerRemoteCommands>.Info($"Action {runningCommand} took {stopwatch2.Elapsed.TotalMilliseconds:F3}ms to run on MainThread");
					source.SetResult(result);
				}
				catch (Exception exception)
				{
					source.SetResult((false, default(T)));
					UnityEngine.Debug.LogException(exception);
				}
			});
			Task<(bool, T)> task = source.Task;
			ColorLog<ServerRemoteCommands>.Info("Blocking side thread until MainThread Action is done");
			Stopwatch stopwatch = Stopwatch.StartNew();
			task.Wait();
			ColorLog<ServerRemoteCommands>.Info($"MainThread Action is done, blocked side thread for {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
			return task.Result;
		}

		public void Dispose()
		{
			cancellation?.Cancel();
			cancellation?.Dispose();
			tcpListener?.Stop();
			tcpListener = null;
			runThread = null;
			cancellation = null;
		}

		public void Start(ushort port)
		{
			try
			{
				tcpListener = new TcpListener(IPAddress.Loopback, port);
				tcpListener.Start();
				ColorLog<ServerRemoteCommands>.Info($"ServerRemoteCommands started on localhost:{port}, LittleEndian={BitConverter.IsLittleEndian}");
			}
			catch (SocketException ex)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn("Error starting TCP listener: " + ex.Message);
			}
			try
			{
				cancellation = new CancellationTokenSource();
				runThread = new Thread(SideThread_Loop);
				runThread.Start();
			}
			catch (Exception arg)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Failed to start side thread {arg}");
			}
		}

		private void SideThread_Loop()
		{
			CancellationToken token = cancellation.Token;
			TcpListener tcpListener = this.tcpListener;
			while (!token.IsCancellationRequested)
			{
				try
				{
					Stopwatch stopwatch;
					using (TcpClient tcpClient = tcpListener.AcceptTcpClient())
					{
						stopwatch = Stopwatch.StartNew();
						tcpClient.ReceiveTimeout = 1000;
						tcpClient.NoDelay = true;
						tcpClient.LingerState = new LingerOption(enable: true, 10);
						using NetworkStream stream = tcpClient.GetStream();
						SideThread_ReadNext(stream);
						tcpClient.Client.Shutdown(SocketShutdown.Send);
					}
					ColorLog<ServerRemoteCommands>.Info($"TcpClient closed, process time: {stopwatch.Elapsed}");
					stopwatch.Stop();
					stopwatch = null;
				}
				catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
				{
					ColorLog<ServerRemoteCommands>.InfoWarn("TcpListener Interrupted");
					break;
				}
				catch (ThreadAbortException)
				{
					ColorLog<ServerRemoteCommands>.Info("Thread Aborted");
				}
				catch (Exception arg)
				{
					ColorLog<ServerRemoteCommands>.InfoWarn($"Error from SideThread_ReadNext: {arg}");
				}
			}
		}

		private void SideThread_ReadNext(NetworkStream stream)
		{
			ColorLog<ServerRemoteCommands>.Info("New command connection received.");
			try
			{
				CommandResponse commandResponse = SideThread_ReadInner(stream);
				ColorLog<ServerRemoteCommands>.Info($"response: {commandResponse.StatusCode}");
				int num = 0;
				Span<byte> destination = sendBuffer.AsSpan();
				BitConverter.TryWriteBytes(destination, (int)commandResponse.StatusCode);
				num += 4;
				byte[] body = commandResponse.Body;
				int value = ((body != null) ? body.Length : 0);
				BitConverter.TryWriteBytes(destination.Slice(num), value);
				num += 4;
				if (commandResponse.Body != null)
				{
					commandResponse.Body.CopyTo(destination.Slice(num));
					num += commandResponse.Body.Length;
				}
				stream.Write(destination.Slice(0, num));
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Read timeout: {ex}");
			}
			catch (Exception arg)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Error processing command: {arg}");
			}
		}

		private CommandResponse SideThread_ReadInner(NetworkStream stream)
		{
			Span<byte> span = stackalloc byte[4];
			int num = stream.Read(span);
			if (num < 4)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn("Incomplete message length received. Discarding.");
				return CommandResponse.Create(StatusCode.BadHeader);
			}
			int num2 = BitConverter.ToInt32(span);
			if (num2 <= 0)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Message Length {num2} is 0 or negative. Discarding.");
				return CommandResponse.Create(StatusCode.BadLength);
			}
			if (num2 > messageBuffer.Length)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Message Length {num2} but max size is set to {messageBuffer.Length}. Discarding.");
				return CommandResponse.Create(StatusCode.BadLength);
			}
			for (num = 0; num < num2; num += stream.Read(messageBuffer, num, num2 - num))
			{
			}
			string json = Encoding.UTF8.GetString(messageBuffer, 0, num2);
			CommandMessage message;
			try
			{
				message = JsonUtility.FromJson<CommandMessage>(json);
			}
			catch (Exception arg)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Failed to deserialize JSON: {arg}");
				return CommandResponse.Create(StatusCode.JsonError);
			}
			return FindAndRunCommand(message);
		}

		public CommandResponse FindAndRunCommand(CommandMessage message)
		{
			ColorLog<ServerRemoteCommands>.Info("Received Command " + (message.name ?? "NULL") + " [" + ((message.arguments != null) ? string.Join(",", message.arguments) : "") + "]");
			if (string.IsNullOrEmpty(message.name) || !Commands.TryGetValue(message.name, out var value))
			{
				ColorLog<ServerRemoteCommands>.InfoWarn("Unknown command " + message.name);
				return CommandResponse.Create(StatusCode.UnknownCommand);
			}
			try
			{
				runningCommand = message.name;
				return value.Run(this, message.arguments);
			}
			catch (Exception arg)
			{
				ColorLog<ServerRemoteCommands>.InfoWarn($"Error running command: {arg}");
				return CommandResponse.Create(StatusCode.CommandError);
			}
		}

		public void PollAll(int maxMessage)
		{
			if (mainThreadQueue.IsEmpty)
			{
				return;
			}
			Stopwatch stopwatch = Stopwatch.StartNew();
			int num = 0;
			while (PollOne())
			{
				num++;
				if (num >= maxMessage)
				{
					break;
				}
			}
			ColorLog<ServerRemoteCommands>.Info($"PollAll processed {num} commands in {stopwatch.Elapsed.TotalMilliseconds:F3}ms");
		}

		public bool PollOne()
		{
			if (mainThreadQueue.TryDequeue(out var result))
			{
				ColorLog<ServerRemoteCommands>.InfoWarn("Running " + result.Name + " on main thread");
				try
				{
					result.Action();
				}
				catch (Exception arg)
				{
					ColorLog<ServerRemoteCommands>.InfoWarn($"Error running {result.Name} on main thread: {arg}");
				}
				return true;
			}
			return false;
		}

		public static void DEBUG_Send()
		{
			using TcpClient tcpClient = new TcpClient();
			tcpClient.SendTimeout = 1000;
			tcpClient.ReceiveTimeout = 1000;
			tcpClient.Connect("localhost", 7779);
			using NetworkStream networkStream = tcpClient.GetStream();
			Span<byte> span = stackalloc byte[200];
			int bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CommandMessage
			{
				name = "update-ready"
			}), span.Slice(4));
			BitConverter.TryWriteBytes(span.Slice(0, 4), bytes);
			networkStream.Write(span);
			UnityEngine.Debug.Log("Sent");
			int num = 0;
			Span<byte> buffer = stackalloc byte[8];
			int num2 = 0;
			do
			{
				num2 += networkStream.Read(buffer);
			}
			while (num2 != 8);
			StatusCode statusCode = (StatusCode)BitConverter.ToInt32(buffer.Slice(0, 4));
			num = BitConverter.ToInt32(buffer.Slice(4, 4));
			UnityEngine.Debug.Log($"Status:{statusCode}");
			UnityEngine.Debug.Log($"JsonLength:{num}");
			if (num > 0)
			{
				Span<byte> span2 = stackalloc byte[num];
				int num3 = 0;
				do
				{
					num3 += networkStream.Read(span2);
				}
				while (num3 != num);
				string text = Encoding.UTF8.GetString(span2);
				UnityEngine.Debug.Log("Json:" + text);
			}
		}
	}
}
