using System.Collections.Generic;
using System.Text;
using TMPro;

public class MessageFeed
{
	private readonly struct Message
	{
		public readonly string Line;

		public readonly float RemoveTime;

		public Message(string line, float removeTime)
		{
			Line = line;
			RemoveTime = removeTime;
		}

		public override string ToString()
		{
			return Line;
		}
	}

	private readonly Queue<Message> _queue = new Queue<Message>();

	private readonly TMP_Text _display;

	private readonly StringBuilder _sb = new StringBuilder();

	private readonly int _maxLines;

	private bool _isDirty;

	private bool _hasContent;

	public bool NoText => !_hasContent;

	public int NbLines => _queue.Count;

	public MessageFeed(TMP_Text display, int maxLines)
	{
		_display = display;
		_maxLines = maxLines;
		_display.SetText(string.Empty);
	}

	public void Enqueue(string text, float removeTime)
	{
		_queue.Enqueue(new Message(text, removeTime));
		_isDirty = true;
	}

	public void Dequeue()
	{
		_queue.Dequeue();
		_isDirty = true;
	}

	public void Update(float now)
	{
		while (_queue.Count > _maxLines)
		{
			_queue.Dequeue();
			_isDirty = true;
		}
		while (_queue.Count > 0 && _queue.Peek().RemoveTime < now)
		{
			_queue.Dequeue();
			_isDirty = true;
		}
		if (_isDirty)
		{
			RefreshUI();
			_isDirty = false;
		}
	}

	private void RefreshUI()
	{
		if (_queue.Count > 0)
		{
			_sb.Clear();
			bool flag = true;
			foreach (Message item in _queue)
			{
				if (!flag)
				{
					_sb.Append('\n');
				}
				_sb.Append(item.Line);
				flag = false;
			}
			_display.SetText(_sb);
			_hasContent = true;
		}
		else if (_hasContent)
		{
			_display.SetText(string.Empty);
			_hasContent = false;
		}
	}
}
