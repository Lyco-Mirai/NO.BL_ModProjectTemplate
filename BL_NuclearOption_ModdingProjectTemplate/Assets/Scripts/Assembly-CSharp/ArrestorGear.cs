using UnityEngine;

public class ArrestorGear : MonoBehaviour
{
	public int wireNumber;

	[SerializeField]
	private Transform leftSide;

	[SerializeField]
	private Transform rightSide;

	[SerializeField]
	private Transform cableCenter;

	[SerializeField]
	private Transform restCenter;

	[SerializeField]
	private Transform cableLeftSide;

	[SerializeField]
	private Transform cableRightSide;

	[SerializeField]
	private float damping;

	[SerializeField]
	private float spring;

	[SerializeField]
	private float maxExtension;

	private float relaxedLength;

	private float extensionPrev;

	private float adjustedDamping;

	private float adjustedSpring;

	private TailHook attachedHook;

	private float hookTime;

	private void Awake()
	{
		relaxedLength = Vector3.Distance(leftSide.position, rightSide.position);
		restCenter.position = (leftSide.position + rightSide.position) * 0.5f;
		cableCenter.position = restCenter.position;
		cableLeftSide.transform.LookAt(cableCenter.position);
		cableRightSide.transform.LookAt(cableCenter.position);
		cableLeftSide.localScale = new Vector3(1f, 1f, relaxedLength * 0.5f);
		cableRightSide.localScale = new Vector3(1f, 1f, relaxedLength * 0.5f);
		extensionPrev = 0f;
	}

	public bool Hook(TailHook tailHook)
	{
		if (attachedHook != null || Mathf.Abs(Vector3.Dot(base.transform.forward, tailHook.unitPart.rb.velocity.normalized)) < 0.5f)
		{
			return false;
		}
		float mass = tailHook.unitPart.parentUnit.GetMass();
		adjustedDamping = mass / 20000f * damping;
		adjustedSpring = mass / 20000f * spring;
		attachedHook = tailHook;
		base.enabled = true;
		return true;
	}

	public void Unhook()
	{
		hookTime = 0f;
		attachedHook.Unhook();
		attachedHook = null;
	}

	private void FixedUpdate()
	{
		float num = 0f;
		float num2 = 0f;
		if (attachedHook != null)
		{
			cableCenter.transform.position = attachedHook.hookEnd.position;
			num = Vector3.Distance(attachedHook.hookEnd.position, leftSide.position);
			num2 = Vector3.Distance(attachedHook.hookEnd.position, rightSide.position);
			float num3 = num + num2 - relaxedLength;
			float num4 = (num3 - extensionPrev) / Time.fixedDeltaTime;
			float num5 = ((num4 > 0f) ? (num4 * adjustedDamping) : 0f);
			float num6 = num3 * adjustedSpring;
			num6 *= Mathf.Clamp01(num4 * 0.1f);
			num6 *= Mathf.Clamp01(num3 * 0.03f);
			num5 *= Mathf.Clamp01(num3 * 0.05f);
			Vector3 vector = leftSide.position - attachedHook.hookEnd.position + (rightSide.position - attachedHook.hookEnd.position);
			attachedHook.ApplyForce(vector.normalized * (num6 + num5));
			extensionPrev = num3;
			hookTime += Time.fixedDeltaTime;
			if ((num4 < 1f && hookTime > 1f) || num3 > maxExtension || hookTime > 5f)
			{
				Unhook();
			}
		}
		else
		{
			cableCenter.position += Vector3.ClampMagnitude(restCenter.position - cableCenter.position, 4f * Time.deltaTime);
			if (Vector3.Distance(cableCenter.position, restCenter.position) < 0.01f)
			{
				cableCenter.position = restCenter.position;
				base.enabled = false;
			}
		}
		num = Vector3.Distance(cableCenter.position, leftSide.position);
		num2 = Vector3.Distance(cableCenter.position, rightSide.position);
		cableLeftSide.LookAt(cableCenter.transform.position);
		cableRightSide.LookAt(cableCenter.transform.position);
		cableLeftSide.localScale = new Vector3(1f, 1f, num);
		cableRightSide.localScale = new Vector3(1f, 1f, num2);
	}
}
