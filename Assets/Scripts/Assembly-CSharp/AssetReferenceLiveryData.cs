using System;
using UnityEngine.AddressableAssets;

[Serializable]
public class AssetReferenceLiveryData : AssetReferenceT<LiveryData>
{
	public AssetReferenceLiveryData(string guid)
		: base(guid)
	{
	}
}
