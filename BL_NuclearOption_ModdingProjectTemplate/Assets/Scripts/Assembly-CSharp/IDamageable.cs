using UnityEngine;

public interface IDamageable
{
	void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float ImpactDamage, PersistentID dealerID);

	void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage);

	void TakeShockwave(Vector3 origin, float overpressure, float blastPower);

	void Detach(Vector3 velocity, Vector3 relativePos);

	ArmorProperties GetArmorProperties();

	Unit GetUnit();

	float GetMass();

	Transform GetTransform();
}
