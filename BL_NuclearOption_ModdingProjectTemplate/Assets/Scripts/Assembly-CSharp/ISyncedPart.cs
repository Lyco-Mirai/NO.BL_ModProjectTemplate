using UnityEngine;

public interface ISyncedPart
{
	Transform transform { get; }

	void SendData();
}
