using UnityEngine;

public class BayDoor : MonoBehaviour
{
	[SerializeField]
	protected float hingeAngle;

	[SerializeField]
	protected float openSpeed;

	[SerializeField]
	protected float closeSpeed;

	[SerializeField]
	protected AudioSource doorAudioSource;

	[SerializeField]
	protected AudioClip openStartSound;

	[SerializeField]
	protected AudioClip closeStartSound;

	private Vector3 baseAngle;

	protected float openTimer;

	protected float openAmount;

	protected float openAmountPrev;

	protected virtual void Awake()
	{
		baseAngle = base.transform.localEulerAngles;
		base.enabled = false;
	}

	public virtual void OpenDoor(float duration)
	{
		openTimer = duration;
		base.enabled = true;
	}

	protected virtual void Update()
	{
		openTimer -= Time.deltaTime;
		if (openTimer > 0f)
		{
			openAmount += openSpeed * Time.deltaTime;
			if (doorAudioSource != null && doorAudioSource.clip != openStartSound)
			{
				doorAudioSource.Stop();
				doorAudioSource.clip = openStartSound;
				doorAudioSource.Play();
			}
		}
		else
		{
			openAmount += (0f - closeSpeed) * Time.deltaTime;
			if (doorAudioSource != null && doorAudioSource.clip != closeStartSound)
			{
				doorAudioSource.Stop();
				doorAudioSource.clip = closeStartSound;
				doorAudioSource.Play();
			}
		}
		openAmount = Mathf.Clamp01(openAmount);
		if (openAmount != openAmountPrev)
		{
			base.transform.localEulerAngles = new Vector3(baseAngle.x, baseAngle.y, openAmount * hingeAngle);
			openAmountPrev = openAmount;
			if (openAmount <= 0f)
			{
				base.enabled = false;
			}
		}
	}
}
