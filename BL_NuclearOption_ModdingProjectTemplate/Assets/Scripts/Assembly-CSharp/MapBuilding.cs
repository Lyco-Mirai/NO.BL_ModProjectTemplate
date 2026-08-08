using System;
using System.Collections.Generic;
using NuclearOption.Jobs;
using UnityEngine;

public class MapBuilding : MonoBehaviour, IDamageable
{
	[Serializable]
	public class MoveablePart
	{
		[SerializeField]
		private Transform transform;

		[SerializeField]
		private MoveablePartAnimation animation;

		public void Initialize()
		{
			if (!(animation == null))
			{
				animation.Initialize(transform);
			}
		}

		public void Animate()
		{
			if (!(animation == null))
			{
				animation.Animate(transform);
			}
		}
	}

	[SerializeField]
	private MoveablePart[] movingParts;

	[SerializeField]
	private ArmorProperties armorProperties;

	[SerializeField]
	private GameObject destroyedPrefab;

	[SerializeField]
	private GameObject powerlinePrefab;

	[SerializeField]
	private float radius;

	private List<GameObject> dependentObjects = new List<GameObject>();

	private MapBuildingSet buildingSet;

	private int index;

	private float hitPoints = 100f;

	private bool removeFromSet;

	public event Action onBuildingDestroy;

	public ArmorProperties GetArmorProperties()
	{
		return armorProperties;
	}

	public void RegisterDependentObject(GameObject dependentObject)
	{
		dependentObjects.Add(dependentObject);
	}

	public void TakeDamage(float pierceDamage, float blastDamage, float amountAffected, float fireDamage, float impactDamage, PersistentID dealerID)
	{
		if ((object)buildingSet == null)
		{
			Debug.LogError("MapBuilding.TakeDamage called but buildingSet was not set");
			return;
		}
		float num = Mathf.Max(pierceDamage - armorProperties.pierceArmor, 0f) / Mathf.Max(armorProperties.pierceTolerance, 0.01f);
		float num2 = Mathf.Max(blastDamage - armorProperties.blastArmor, 0f) * amountAffected / Mathf.Max(armorProperties.blastTolerance, 0.01f);
		float num3 = Mathf.Max(fireDamage - armorProperties.fireArmor, 0f) / Mathf.Max(armorProperties.fireTolerance, 0.01f);
		float num4 = num + num2 + num3 + impactDamage;
		hitPoints -= num4;
		if (hitPoints < 0f)
		{
			buildingSet.DestroyBuilding(index);
		}
	}

	public void ApplyDamage(float pierceDamage, float blastDamage, float fireDamage, float impactDamage)
	{
	}

	public Unit GetUnit()
	{
		return null;
	}

	public Transform GetTransform()
	{
		return base.transform;
	}

	public void TakeShockwave(Vector3 origin, float blastEffectScale, float blastPower)
	{
	}

	public void Detach(Vector3 velocity, Vector3 relativePos)
	{
	}

	public float GetMass()
	{
		return 0f;
	}

	public void Destruct()
	{
		if (destroyedPrefab != null && Time.timeSinceLevelLoad > 10f)
		{
			SpawnDestroyedPrefab();
		}
		UnityEngine.Object.Destroy(base.gameObject);
	}

	private void SpawnDestroyedPrefab()
	{
		GameObject gameObject = UnityEngine.Object.Instantiate(destroyedPrefab, base.transform);
		gameObject.transform.SetParent(null);
		foreach (GameObject dependentObject in dependentObjects)
		{
			if (dependentObject != null)
			{
				UnityEngine.Object.Destroy(dependentObject);
			}
		}
		if (BattlefieldGrid.TryGetGridSquare(base.transform.GlobalPosition(), out var gridSquare))
		{
			Obstacle obstacle = new Obstacle(gameObject.transform, radius, float.MaxValue);
			int num = gridSquare.obstacles.FindIndex((Obstacle obstacle2) => obstacle2.Transform == base.transform);
			if (num != -1)
			{
				gridSquare.obstacles[num] = obstacle;
			}
			else
			{
				gridSquare.obstacles.Add(obstacle);
			}
		}
	}

	public void SpawnPowerline(MapBuilding adjacentBuilding)
	{
		float num = Vector3.Distance(adjacentBuilding.transform.position, base.transform.position);
		if (!(num > 1000f) && !(num < 200f))
		{
			GameObject gameObject = UnityEngine.Object.Instantiate(powerlinePrefab, base.transform);
			gameObject.transform.localPosition = Vector3.zero;
			gameObject.transform.LookAt(adjacentBuilding.transform);
			gameObject.transform.localScale = new Vector3(1f, 1f, num);
			adjacentBuilding.RegisterDependentObject(gameObject);
		}
	}

	public void AssignBuildingSet(MapBuildingSet buildingSet, int index)
	{
		this.buildingSet = buildingSet;
		MoveablePart[] array = movingParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Initialize();
		}
		this.index = index;
		if (movingParts.Length != 0)
		{
			buildingSet.animatedBuildings.Add(this);
		}
	}

	public void OnDestroy()
	{
		removeFromSet = true;
	}

	public PartResult MapBuilding_OnUpdate()
	{
		if (removeFromSet)
		{
			return PartResult.Remove;
		}
		MoveablePart[] array = movingParts;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Animate();
		}
		return PartResult.None;
	}
}
