using System;
using System.Collections.Generic;

namespace NuclearOption.MissionEditorScripts
{
	public static class UndoRedo
	{
		public class RingBuffer<T>
		{
			public T[] Buffer;

			public int Head;

			public int Tail;

			public RingBuffer(int capcity)
			{
				Buffer = new T[capcity];
			}

			public void Push(T item)
			{
				Buffer[Head] = item;
				Head = (Head + 1) % Buffer.Length;
				if (Head == Tail)
				{
					Tail = (Tail + 1) % Buffer.Length;
				}
			}

			public bool TryPop(out T item)
			{
				if (Head == Tail)
				{
					item = default(T);
					return false;
				}
				item = Buffer[Tail];
				Tail = (Tail + 1) % Buffer.Length;
				return true;
			}

			public void Clear()
			{
				for (int i = 0; i < Buffer.Length; i++)
				{
					Buffer[i] = default(T);
				}
				Head = 0;
				Tail = 0;
			}
		}

		public readonly struct Command
		{
			public readonly string Description;

			public readonly Action Do;

			public readonly Action Undo;
		}

		private static RingBuffer<Command> UndoStack;

		private static RingBuffer<Command> RedoStack;

		public static void Init(int stackSize)
		{
			UndoStack = new RingBuffer<Command>(stackSize);
			RedoStack = new RingBuffer<Command>(stackSize);
		}

		public static void Do(Command command)
		{
			UndoStack.Push(command);
			RedoStack.Clear();
			command.Do();
		}

		public static void Undo(int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!UndoStack.TryPop(out var item))
				{
					break;
				}
				item.Undo();
			}
		}

		public static void Redo(int count)
		{
			for (int i = 0; i < count; i++)
			{
				if (!RedoStack.TryPop(out var item))
				{
					break;
				}
				item.Do();
			}
		}

		public static IEnumerable<string> GetUndoDescriptions()
		{
			return GetDescriptions(UndoStack);
		}

		public static IEnumerable<string> GetRedoDescriptions()
		{
			return GetDescriptions(RedoStack);
		}

		private static IEnumerable<string> GetDescriptions(RingBuffer<Command> buffer)
		{
			for (int index = (buffer.Head - 1 + buffer.Buffer.Length) % buffer.Buffer.Length; index != buffer.Tail; index = (index - 1 + buffer.Buffer.Length) % buffer.Buffer.Length)
			{
				yield return buffer.Buffer[index].Description;
			}
		}
	}
}
