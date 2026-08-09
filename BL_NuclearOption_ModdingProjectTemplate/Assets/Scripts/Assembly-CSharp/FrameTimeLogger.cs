using System.IO;
using UnityEngine;

public class FrameTimeLogger : MonoBehaviour
{
	public string OutFile = "./frames.csv";

	private const int FRAME_TIMING_COUNT = 100;

	private FrameTiming[] frameTimings = new FrameTiming[100];

	private StreamWriter writer;

	private void Start()
	{
		writer = new StreamWriter(OutFile)
		{
			AutoFlush = true
		};
		writer.WriteLine("count,frame,average");
	}

	private void OnDestroy()
	{
		writer.Close();
		writer.Dispose();
		writer = null;
	}

	private void Update()
	{
		uint latestTimings = FrameTimingManager.GetLatestTimings((uint)frameTimings.Length, frameTimings);
		double num = 0.0;
		for (int i = 0; i < latestTimings; i++)
		{
			num += frameTimings[i].cpuMainThreadFrameTime;
		}
		double num2 = num / (double)latestTimings;
		writer.WriteLine($"{latestTimings},{frameTimings[0].cpuMainThreadFrameTime},{num2}");
	}
}
