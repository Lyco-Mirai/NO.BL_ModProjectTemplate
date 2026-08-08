using System;
using JamesFrowen.ScriptableVariables;
using Mirage.SocketLayer;
using UnityEngine;

public class BandwidthText : MonoBehaviour
{
	[SerializeField]
	protected float _updateInterval = 0.2f;

	public Metrics Metrics;

	private float[] deltaTime = new float[0];

	[Header("References")]
	[SerializeField]
	private NonAllocGui.Wrapper _connectionText;

	[SerializeField]
	private NonAllocGui.Wrapper _sendCountText;

	[SerializeField]
	private NonAllocGui.Wrapper _sendBytesText;

	[SerializeField]
	private NonAllocGui.Wrapper _receiveCountText;

	[SerializeField]
	private NonAllocGui.Wrapper _receiveBytesText;

	private float _updateTimer;

	public void Update()
	{
		if (Metrics == null)
		{
			return;
		}
		_updateTimer += Time.unscaledDeltaTime;
		Metrics.Frame[] buffer = Metrics.buffer;
		if (deltaTime.Length != buffer.Length)
		{
			Array.Resize(ref deltaTime, buffer.Length);
		}
		deltaTime[Metrics.tick] = Time.unscaledDeltaTime;
		if (_updateTimer <= _updateInterval)
		{
			return;
		}
		_updateTimer = 0f;
		int num = 0;
		float num2 = 0f;
		int num3 = 0;
		float num4 = 0f;
		float num5 = 0f;
		for (int i = 0; i < buffer.Length; i++)
		{
			if (buffer[i].init)
			{
				num5 += deltaTime[i];
				num += buffer[i].sendCount;
				num2 += (float)buffer[i].sendBytes;
				num3 += buffer[i].receiveCount;
				num4 += (float)buffer[i].receiveBytes;
			}
		}
		int connectionCount = buffer[Metrics.tick].connectionCount;
		_connectionText.SetValue(connectionCount);
		_sendCountText.SetValue((int)((float)num / num5));
		_sendBytesText.SetValue(num2 / num5 / 1024f);
		_receiveCountText.SetValue((int)((float)num3 / num5));
		_receiveBytesText.SetValue(num4 / num5 / 1024f);
	}
}
