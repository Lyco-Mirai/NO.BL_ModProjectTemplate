public struct DamageInfo
{
	public CompressedFloat pierceDamage;

	public CompressedFloat blastDamage;

	public CompressedFloat fireDamage;

	public CompressedFloat impactDamage;

	public DamageInfo(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
		this.pierceDamage = pierceDamage.Compress();
		this.blastDamage = blastDamage.Compress();
		this.fireDamage = fireDamage.Compress();
		this.impactDamage = impactDamage.Compress();
	}

	public bool IsValid()
	{
		if (float.IsFinite(pierceDamage.Decompress()) && float.IsFinite(blastDamage.Decompress()) && float.IsFinite(fireDamage.Decompress()))
		{
			return float.IsFinite(impactDamage.Decompress());
		}
		return false;
	}
}
