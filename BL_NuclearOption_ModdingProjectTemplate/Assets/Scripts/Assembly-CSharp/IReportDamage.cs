using System;

public interface IReportDamage
{
	event Action<OnReportDamage> onReportDamage;
}
