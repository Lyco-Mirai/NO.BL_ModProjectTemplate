using System;
using UnityEngine;

[Serializable]
public class ArmorProperties
{
	public float pierceArmor;

	public float blastArmor;

	public float fireArmor;

	public float pierceTolerance = 1f;

	public float blastTolerance = 1f;

	public float fireTolerance = 1f;

	public float overpressureLimit = 5f;

	public float CalcNetDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		float num = Mathf.Max(pierceDamage - pierceArmor, 0f) / pierceTolerance;
		float num2 = Mathf.Max(blastDamage - blastArmor, 0f) / blastTolerance;
		float num3 = Mathf.Max(fireDamage - fireArmor, 0f) / fireTolerance;
		return num + num2 + num3 + impactDamage;
	}
}
