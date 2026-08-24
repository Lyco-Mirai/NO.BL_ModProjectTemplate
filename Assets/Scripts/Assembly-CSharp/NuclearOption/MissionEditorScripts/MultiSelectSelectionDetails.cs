using System;
using System.Collections.Generic;
using System.Linq;
using NuclearOption.SavedMission;
using UnityEngine;

namespace NuclearOption.MissionEditorScripts
{
	public class MultiSelectSelectionDetails : SelectionDetails, IDisposable
	{
		public readonly List<SingleSelectionDetails> Items = new List<SingleSelectionDetails>();

		private readonly List<Vector3> positionOffsets = new List<Vector3>();

		private readonly List<Transform> rotationProxy = new List<Transform>();

		private Transform proxyParent;

		public static GroupRotationMode RotationMode;

		private bool lockList;

		public override bool PositionHandleAllowed => Items.Any((SingleSelectionDetails x) => x.PositionHandleAllowed);

		public override bool RotationHandleAllowed => Items.Any((SingleSelectionDetails x) => x.RotationHandleAllowed);

		public Type SelectionType => Items[0].GetType();

		public override string DisplayName => $"{Items.Count} Objects";

		public override bool IsDestroyed => false;

		public override bool TryGetFaction(out Faction faction)
		{
			bool flag = false;
			faction = null;
			foreach (SingleSelectionDetails item in Items)
			{
				if (item.TryGetFaction(out var faction2))
				{
					if (!flag)
					{
						faction = faction2;
						flag = true;
					}
					else if (faction != faction2)
					{
						faction = null;
						return false;
					}
				}
			}
			return flag;
		}

		public MultiSelectSelectionDetails()
		{
			base.PositionWrapper = new ValueWrapperGlobalPosition();
			base.PositionWrapper.RegisterOnChange(this, OnPositionChanged);
			base.RotationWrapper = new ValueWrapperQuaternion();
			base.RotationWrapper.RegisterOnChange(this, OnRotationChanged);
		}

		public override bool Delete()
		{
			lockList = true;
			try
			{
				for (int num = Items.Count - 1; num >= 0; num--)
				{
					if (Items[num].Delete())
					{
						Items.RemoveAt(num);
					}
				}
			}
			finally
			{
				lockList = false;
			}
			AfterRemove();
			return false;
		}

		public override void Focus()
		{
			Vector3 v = default(Vector3);
			foreach (SingleSelectionDetails item in Items)
			{
				v += item.PositionWrapper.Value.AsVector3() / Items.Count;
			}
			GlobalPosition globalPosition = new GlobalPosition(v);
			float num = 0f;
			foreach (SingleSelectionDetails item2 in Items)
			{
				float num2 = FastMath.Distance(item2.PositionWrapper.Value, globalPosition);
				if (num < num2)
				{
					num = num2;
				}
			}
			num = Mathf.Clamp(num, 50f, 2000f);
			SceneSingleton<CameraStateManager>.i.FocusPosition(globalPosition.ToLocalPosition(), null, num);
		}

		public void Add(SingleSelectionDetails single)
		{
			foreach (SingleSelectionDetails item in Items)
			{
				if (item.Source == single.Source)
				{
					Debug.LogError($"{single.Source} is already in multiselect");
					return;
				}
			}
			Items.Add(single);
			AfterAdd();
		}

		public void ClearIfSelected(IEditorSelectable value)
		{
			Remove(value, errorIfNotSelected: false);
		}

		public void Remove(IEditorSelectable obj, bool errorIfNotSelected = true)
		{
			if (lockList)
			{
				return;
			}
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Source == obj)
				{
					Items.RemoveAt(i);
					AfterRemove();
					return;
				}
			}
			if (errorIfNotSelected)
			{
				Debug.LogError($"Count not find {obj} in multiselect");
			}
		}

		public void RemoveAll<T>(List<T> toRemove, bool errorIfNotSelected) where T : IEditorSelectable
		{
			if (lockList)
			{
				return;
			}
			foreach (T item in toRemove)
			{
				bool flag = false;
				for (int i = 0; i < Items.Count; i++)
				{
					if (Items[i].Source == (object)item)
					{
						Items.RemoveAt(i);
						flag = true;
						break;
					}
				}
				if (!flag && errorIfNotSelected)
				{
					Debug.LogError($"Count not find {item} in multiselect");
				}
			}
			AfterRemove();
		}

		private void AfterAdd()
		{
			RecalculatePositionsAndRotation();
		}

		private void AfterRemove()
		{
			if (Items.Count == 0)
			{
				SceneSingleton<UnitSelection>.i.ClearMultiSelection(this);
			}
			else if (Items.Count == 1)
			{
				SceneSingleton<UnitSelection>.i.ReplaceMultiSelection(this, Items[0]);
			}
			else
			{
				RecalculatePositionsAndRotation();
			}
		}

		public void Dispose()
		{
			if (proxyParent != null)
			{
				UnityEngine.Object.Destroy(proxyParent.gameObject);
			}
			foreach (Transform item in rotationProxy)
			{
				if (item != null)
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
			}
		}

		public bool SetPivot(IEditorSelectable source)
		{
			if (source == null)
			{
				return false;
			}
			for (int i = 0; i < Items.Count; i++)
			{
				if (Items[i].Source == source)
				{
					return SetPivot(Items[i]);
				}
			}
			return false;
		}

		public bool SetPivot(SingleSelectionDetails target)
		{
			if (target == null || !target.PositionHandleAllowed)
			{
				return false;
			}
			int num = Items.IndexOf(target);
			if (num < 0)
			{
				return false;
			}
			if (num == 0)
			{
				return true;
			}
			Items.RemoveAt(num);
			Items.Insert(0, target);
			RecalculatePositionsAndRotation();
			return true;
		}

		public void RecalculatePositionsAndRotation()
		{
			RecalculatePositions();
			RecalculateRotations();
		}

		private void RecalculatePositions()
		{
			while (positionOffsets.Count < Items.Count)
			{
				positionOffsets.Add(default(Vector3));
			}
			while (positionOffsets.Count > Items.Count)
			{
				positionOffsets.RemoveAt(positionOffsets.Count - 1);
			}
			GlobalPosition globalPosition = default(GlobalPosition);
			foreach (SingleSelectionDetails item in Items)
			{
				if (item.PositionHandleAllowed)
				{
					globalPosition = item.PositionWrapper.Value;
					break;
				}
			}
			if (globalPosition == default(GlobalPosition))
			{
				return;
			}
			for (int i = 0; i < Items.Count; i++)
			{
				SingleSelectionDetails singleSelectionDetails = Items[i];
				if (singleSelectionDetails.PositionHandleAllowed)
				{
					positionOffsets[i] = singleSelectionDetails.PositionWrapper.Value - globalPosition;
				}
			}
			base.PositionWrapper.SetValue(globalPosition, this);
		}

		private void RecalculateRotations()
		{
			int num = 0;
			foreach (SingleSelectionDetails item in Items)
			{
				if (item.RotationHandleAllowed)
				{
					num++;
				}
			}
			if (num == 0)
			{
				return;
			}
			if (proxyParent == null)
			{
				GameObject gameObject = new GameObject($"Multiselect_Group_proxy {positionOffsets.Count}");
				gameObject.name = $"Multiselect_Group_proxy {positionOffsets.Count}";
				proxyParent = gameObject.transform;
			}
			while (rotationProxy.Count < Items.Count)
			{
				GameObject gameObject2 = new GameObject($"Multiselect_proxy {positionOffsets.Count}");
				gameObject2.name = $"Multiselect_proxy {positionOffsets.Count}";
				gameObject2.transform.SetParent(proxyParent);
				rotationProxy.Add(gameObject2.transform);
			}
			while (rotationProxy.Count > Items.Count)
			{
				int index = rotationProxy.Count - 1;
				UnityEngine.Object.Destroy(rotationProxy[index].gameObject);
				rotationProxy.RemoveAt(index);
			}
			Quaternion? quaternion = null;
			foreach (SingleSelectionDetails item2 in Items)
			{
				if (item2.RotationHandleAllowed)
				{
					quaternion = item2.RotationWrapper.Value;
					break;
				}
			}
			Quaternion quaternion2 = quaternion ?? Quaternion.identity;
			proxyParent.SetPositionAndRotation(base.PositionWrapper.Value.ToLocalPosition(), quaternion2);
			for (int i = 0; i < Items.Count; i++)
			{
				SingleSelectionDetails singleSelectionDetails = Items[i];
				if (singleSelectionDetails.PositionHandleAllowed || singleSelectionDetails.RotationHandleAllowed)
				{
					rotationProxy[i].SetPositionAndRotation(singleSelectionDetails.PositionWrapper.Value.ToLocalPosition(), singleSelectionDetails.RotationWrapper.Value);
				}
			}
			base.RotationWrapper.SetValue(quaternion2, this);
		}

		private void OnPositionChanged()
		{
			GlobalPosition value = base.PositionWrapper.Value;
			for (int i = 0; i < Items.Count; i++)
			{
				SingleSelectionDetails singleSelectionDetails = Items[i];
				if (singleSelectionDetails.PositionHandleAllowed)
				{
					GlobalPosition globalPosition = value + positionOffsets[i];
					if (singleSelectionDetails is UnitSelectionDetails unitSelectionDetails)
					{
						globalPosition = unitSelectionDetails.ClampPosition(globalPosition);
					}
					singleSelectionDetails.PositionWrapper.SetValue(globalPosition, this);
				}
			}
			if (proxyParent != null)
			{
				proxyParent.position = base.PositionWrapper.Value.ToLocalPosition();
			}
		}

		private void OnRotationChanged()
		{
			Quaternion value = base.RotationWrapper.Value;
			if (RotationMode == GroupRotationMode.Local)
			{
				Quaternion quaternion = value * Quaternion.Inverse(proxyParent.rotation);
				proxyParent.rotation = value;
				for (int i = 0; i < Items.Count; i++)
				{
					SingleSelectionDetails singleSelectionDetails = Items[i];
					if (singleSelectionDetails.RotationHandleAllowed)
					{
						singleSelectionDetails.RotationWrapper.SetValue(quaternion * singleSelectionDetails.RotationWrapper.Value, this);
					}
					if (singleSelectionDetails.PositionHandleAllowed || singleSelectionDetails.RotationHandleAllowed)
					{
						rotationProxy[i].SetPositionAndRotation(singleSelectionDetails.PositionWrapper.Value.ToLocalPosition(), singleSelectionDetails.RotationWrapper.Value);
					}
				}
				return;
			}
			proxyParent.rotation = value;
			for (int j = 0; j < Items.Count; j++)
			{
				SingleSelectionDetails singleSelectionDetails2 = Items[j];
				if (!singleSelectionDetails2.PositionHandleAllowed && !singleSelectionDetails2.RotationHandleAllowed)
				{
					continue;
				}
				rotationProxy[j].GetPositionAndRotation(out var position, out var rotation);
				GlobalPosition globalPosition = position.ToGlobalPosition();
				if (singleSelectionDetails2.RotationHandleAllowed)
				{
					singleSelectionDetails2.RotationWrapper.SetValue(rotation, this);
				}
				if (singleSelectionDetails2.PositionHandleAllowed)
				{
					if (singleSelectionDetails2 is UnitSelectionDetails unitSelectionDetails)
					{
						globalPosition = unitSelectionDetails.ClampPosition(globalPosition);
					}
					singleSelectionDetails2.PositionWrapper.SetValue(globalPosition, this);
				}
			}
		}
	}
}
