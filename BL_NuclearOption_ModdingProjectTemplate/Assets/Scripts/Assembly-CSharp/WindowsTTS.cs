using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using SpeechLib;

public class WindowsTTS
{
	private readonly SpVoice voice = new SpVoiceClass();

	public void Speak(int speed, int volume, string text, bool asSsml)
	{
		voice.Rate = speed;
		voice.Volume = volume;
		SpeechVoiceSpeakFlags speechVoiceSpeakFlags = (SpeechVoiceSpeakFlags)3;
		if (asSsml)
		{
			text = "<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xml:lang=\"en-US\">" + text + "</speak>";
			speechVoiceSpeakFlags |= SpeechVoiceSpeakFlags.SVSFParseSsml;
		}
		voice.Speak(text, speechVoiceSpeakFlags);
	}

	public bool IsPlaying()
	{
		return !voice.WaitUntilDone(0);
	}

	public static UniTask SpeakAsync(int speed, int volume, string text, bool asSsml)
	{
		return UniTask.RunOnThreadPool(async delegate
		{
			WindowsTTS tts = new WindowsTTS();
			tts.Speak(speed, volume, text, asSsml);
			while (tts.IsPlaying())
			{
				await Task.Delay(100);
			}
		});
	}
}
