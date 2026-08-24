using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using Mirage.Serialization;
using UnityEngine;

namespace NuclearOption.NetworkTransforms
{
	public class ClientAuthStream : IDisposable
	{
		private struct LogEntry
		{
			public ulong steamId;

			public uint netId;

			public RejectMask acceptedMask;

			public double clientLocalTime;

			public double clientServerTime;

			public NetworkTransformBase.NetworkSnapshot snapshot;
		}

		public static string OpenPath;

		private BinaryWriter Writer;

		private Thread thread;

		private CancellationTokenSource cancellation;

		private readonly Queue<LogEntry> logEntries = new Queue<LogEntry>(1000);

		private readonly AutoResetEvent updateFinished = new AutoResetEvent(initialState: false);

		private readonly NetworkWriter netWriter = new NetworkWriter(1200, allowResize: true);

		public void Open(string path)
		{
			if (Writer != null)
			{
				throw new InvalidOperationException("Already open");
			}
			Writer = new BinaryWriter(new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read));
			cancellation = new CancellationTokenSource();
			thread = new Thread(ThreadLoop);
			thread.Priority = System.Threading.ThreadPriority.BelowNormal;
			thread.Start();
		}

		public void Dispose()
		{
			cancellation?.Cancel();
			updateFinished.Set();
			if (thread != null && !thread.Join(TimeSpan.FromMilliseconds(100.0)))
			{
				Debug.Log("ClientAuthStream did not finish within 100ms");
			}
			thread = null;
			Writer?.Close();
			Writer?.Dispose();
			Writer = null;
			cancellation?.Dispose();
			cancellation = null;
			updateFinished.Dispose();
		}

		public void ThreadLoop()
		{
			CancellationToken token = cancellation.Token;
			List<LogEntry> list = new List<LogEntry>(100);
			while (!token.IsCancellationRequested)
			{
				try
				{
					updateFinished.WaitOne();
					list.Clear();
					lock (logEntries)
					{
						list.AddRange(logEntries);
						logEntries.Clear();
					}
					foreach (LogEntry item in list)
					{
						Write(item);
					}
				}
				catch (Exception arg)
				{
					Debug.LogError($"ClientAuthStream {arg}");
				}
			}
		}

		public void MarkUpdatedFinished()
		{
			bool flag;
			lock (logEntries)
			{
				flag = logEntries.Count > 0;
			}
			if (flag)
			{
				updateFinished.Set();
			}
		}

		public void LogBlank()
		{
			lock (logEntries)
			{
				logEntries.Enqueue(default(LogEntry));
			}
		}

		public void Log(AircraftNetworkTransform nt, RejectMask acceptedMask, NetworkTransformBase.NetworkSnapshot snapshot, double clientLocalTime, double clientServerTime)
		{
			ulong valueOrDefault = (nt?.Aircraft?.Player?.GetAuthData()?.SteamID.m_SteamID).GetValueOrDefault();
			uint netId = nt.NetId;
			LogEntry item = new LogEntry
			{
				steamId = valueOrDefault,
				netId = netId,
				acceptedMask = acceptedMask,
				clientLocalTime = clientLocalTime,
				clientServerTime = clientServerTime,
				snapshot = snapshot
			};
			lock (logEntries)
			{
				logEntries.Enqueue(item);
			}
		}

		private void Write(LogEntry entry)
		{
			netWriter.Reset();
			netWriter.WriteUInt64(entry.steamId);
			netWriter.WriteUInt32(entry.netId);
			netWriter.WriteDouble(entry.clientLocalTime);
			netWriter.WriteDouble(entry.clientServerTime);
			netWriter.WriteUInt32((uint)entry.acceptedMask);
			netWriter.Write(entry.snapshot);
			ArraySegment<byte> arraySegment = netWriter.ToArraySegment();
			int count = arraySegment.Count;
			Writer.Write(count);
			Writer.Write(arraySegment);
		}

		public static void ToCSV(string input, string output)
		{
			using (BinaryReader binaryReader = new BinaryReader(new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.Read)))
			{
				using StreamWriter streamWriter = new StreamWriter(output);
				using NetworkReader networkReader = new NetworkReader();
				streamWriter.WriteLine("SteamID,NetId,AcceptedMask,ClientLocalTime,ClientServerTime,Timestamp,ExtraExtrapolation,HasInputs,GlobalPos_X,GlobalPos_Y,GlobalPos_Z,Velocity_X,Velocity_Y,Velocity_Z,Rotation_X,Rotation_Y,Rotation_Z,Rotation_W");
				while (true)
				{
					try
					{
						int count = binaryReader.ReadInt32();
						byte[] array = binaryReader.ReadBytes(count);
						networkReader.Reset(array);
					}
					catch (EndOfStreamException)
					{
						break;
					}
					catch (Exception ex2)
					{
						Console.WriteLine("Error reading log file: " + ex2.Message + ". Stopping conversion.");
						break;
					}
					try
					{
						ulong num = networkReader.ReadUInt64();
						uint num2 = networkReader.ReadUInt32();
						double num3 = networkReader.ReadDouble();
						double num4 = networkReader.ReadDouble();
						RejectMask rejectMask = (RejectMask)networkReader.ReadUInt32();
						NetworkTransformBase.NetworkSnapshot networkSnapshot = networkReader.Read<NetworkTransformBase.NetworkSnapshot>();
						Vector3 vector = networkSnapshot.velocity.Decompress();
						Quaternion rotation = networkSnapshot.rotation;
						streamWriter.WriteLine($"{num}," + $"{num2}," + $"{rejectMask}," + num3.ToString("F6", CultureInfo.InvariantCulture) + "," + num4.ToString("F6", CultureInfo.InvariantCulture) + "," + NullableValue(networkSnapshot.timestamp) + "," + NullableValue(networkSnapshot.extraExtrapolation) + "," + $"{networkSnapshot.ClientInputs.HasValue}," + networkSnapshot.globalPos.x.ToString("F6", CultureInfo.InvariantCulture) + "," + networkSnapshot.globalPos.y.ToString("F6", CultureInfo.InvariantCulture) + "," + networkSnapshot.globalPos.z.ToString("F6", CultureInfo.InvariantCulture) + "," + vector.x.ToString("F4", CultureInfo.InvariantCulture) + "," + vector.y.ToString("F4", CultureInfo.InvariantCulture) + "," + vector.z.ToString("F4", CultureInfo.InvariantCulture) + "," + rotation.x.ToString("F4", CultureInfo.InvariantCulture) + "," + rotation.y.ToString("F4", CultureInfo.InvariantCulture) + "," + rotation.z.ToString("F4", CultureInfo.InvariantCulture) + "," + rotation.w.ToString("F4", CultureInfo.InvariantCulture));
					}
					catch (Exception ex3)
					{
						Console.WriteLine("Error parsing line: " + ex3.Message + ".");
					}
				}
			}
			static string NullableValue(double? val)
			{
				if (!val.HasValue)
				{
					return string.Empty;
				}
				return val.Value.ToString(CultureInfo.InvariantCulture);
			}
		}
	}
}
