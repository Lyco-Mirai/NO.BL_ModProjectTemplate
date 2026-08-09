public struct OpportunityThreat
{
	public readonly float opportunity;

	public readonly float threat;

	public OpportunityThreat(float opportunity, float threat)
	{
		this.opportunity = opportunity;
		this.threat = threat;
	}

	public float GetCombinedScore()
	{
		return opportunity * (threat + 1f);
	}
}
