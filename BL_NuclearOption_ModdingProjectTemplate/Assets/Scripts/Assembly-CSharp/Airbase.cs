using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Mirage;
using Mirage.RemoteCalls;
using Mirage.Serialization;
using NuclearOption.MissionEditorScripts;
using NuclearOption.Networking;
using NuclearOption.SavedMission;
using NuclearOption.SceneLoading;
using RoadPathfinding;
using UnityEngine;

[RequireComponent(typeof(Capture))]
public sealed class Airbase : NetworkBehaviour, IEditorSelectable, ICapturable
{
	[Serializable]
	public class VerticalLandingPoint
	{
		public Transform point;

		public float approachAngleRange;

		public float size = 40f;

		[Tooltip("Parent Unit part (optional)")]
		public UnitPart unitPart;

		private readonly Queue<Aircraft> landingQueue = new Queue<Aircraft>();

		[NonSerialized]
		private List<Runway> crossingRunways = new List<Runway>();

		public void FindCrossingRunways(Airbase airbase)
		{
			crossingRunways = new List<Runway>();
			Runway[] runways = airbase.runways;
			foreach (Runway runway in runways)
			{
				if (FastMath.InRange(runway.GetNearestPoint(point, extend: false), point.position, size))
				{
					crossingRunways.Add(runway);
				}
			}
		}

		public bool IsOccupied(Aircraft querier)
		{
			foreach (Runway crossingRunway in crossingRunways)
			{
				if (crossingRunway.OtherAircraftUsingRunway(querier))
				{
					return true;
				}
			}
			Aircraft result;
			while (landingQueue.TryPeek(out result))
			{
				if (result == null)
				{
					landingQueue.Dequeue();
					continue;
				}
				return result != querier;
			}
			return false;
		}

		public Queue<Aircraft> GetLandingQueue()
		{
			return landingQueue;
		}

		public void RegisterLanding(Aircraft aircraft)
		{
			landingQueue.Enqueue(aircraft);
		}

		public bool IsAvailable()
		{
			if (unitPart == null)
			{
				return true;
			}
			if (Vector3.Dot(point.transform.up, Vector3.up) < 0.9f)
			{
				return false;
			}
			return unitPart.parentUnit.enabled;
		}

		public float GetAngle(Transform fromTransform)
		{
			return Vector3.Angle(fromTransform.position - point.position, point.forward);
		}

		public GlobalPosition GetApproachPoint(Aircraft landingAircraft)
		{
			Vector3 target = landingAircraft.transform.position - point.position;
			target.y = 0f;
			Vector3 forward = point.transform.forward;
			forward.y = 0f;
			Vector3 vector = Vector3.RotateTowards(forward, target, approachAngleRange * (MathF.PI / 180f), 0f) * 500f;
			return (point.position + vector).ToGlobalPosition();
		}

		public Vector3 GetVelocity()
		{
			if (unitPart == null || unitPart.rb == null)
			{
				return Vector3.zero;
			}
			return unitPart.rb.GetPointVelocity(point.position);
		}

		public bool IsSuitable(RunwayQuery query)
		{
			return size > query.MinSize;
		}

		public static VerticalLandingPoint FromSaved(Airbase airbase, GlobalPosition globalPosition)
		{
			Transform transform = new GameObject("verticalLandingPoints").transform;
			transform.parent = airbase.transform;
			transform.transform.position = globalPosition.ToLocalPosition();
			return new VerticalLandingPoint
			{
				point = transform,
				approachAngleRange = 180f,
				size = 40f,
				unitPart = null
			};
		}
	}

	[Serializable]
	public class Runway
	{
		public readonly struct RunwayAngleResult
		{
			public readonly float Angle;

			public readonly bool Reverse;

			public RunwayAngleResult(float angle, bool reverse)
			{
				Angle = angle;
				Reverse = reverse;
			}
		}

		public readonly struct RunwayDistanceResult
		{
			public readonly float Distance;

			public readonly bool Reverse;

			public RunwayDistanceResult(float distance, bool reverse)
			{
				Distance = distance;
				Reverse = reverse;
			}
		}

		public readonly struct RunwayUsage
		{
			public readonly Runway Runway;

			public readonly bool Reverse;

			public RunwayUsage(Runway runway, bool reverse)
			{
				Runway = runway;
				Reverse = reverse;
			}

			public string GetName()
			{
				return Runway.GetName(Reverse);
			}

			public Vector3 GetDirection()
			{
				return Runway.GetDirection(Reverse);
			}

			public Transform GetEnd()
			{
				if (!Reverse)
				{
					return Runway.End;
				}
				return Runway.Start;
			}

			public Transform GetStart()
			{
				if (!Reverse)
				{
					return Runway.Start;
				}
				return Runway.End;
			}

			public GlobalPosition GetTouchdownPoint()
			{
				float num = ((Runway.Length > 500f) ? Mathf.Lerp(20f, 300f, (Runway.Length - 500f) * 0.0005f) : 0f);
				Transform transform = (Reverse ? Runway.End : Runway.Start);
				Vector3 normalized = GetDirection().normalized;
				return transform.GlobalPosition() + num * normalized;
			}

			public Vector3 GetGlideslopeAimpoint(Aircraft aircraft, float distance, float timeToTouchdown)
			{
				Vector3 vector = GetTouchdownPoint().ToLocalPosition();
				Vector3 normalized = (-GetDirection() + Vector3.up * Runway.Length * 0.06f).normalized;
				Vector3 vector2 = ((Runway.rb == null || timeToTouchdown <= 0f) ? Vector3.zero : Runway.GetVelocity());
				return vector + Vector3.up * aircraft.definition.spawnOffset.y + normalized * distance + vector2;
			}

			public GlobalPosition GetNearestGlideslopePoint(GlobalPosition fromPosition, float heightOffset, float timeToTouchdown)
			{
				GlobalPosition touchdownPoint = GetTouchdownPoint();
				Vector3 vector = ((Runway.rb == null || timeToTouchdown <= 0f) ? Vector3.zero : (Runway.GetVelocity() * timeToTouchdown));
				Vector3 direction = GetDirection();
				Vector3 vector2 = fromPosition - touchdownPoint;
				Vector3 normalized = (-direction + Runway.Length * 0.06f * Vector3.up).normalized;
				return touchdownPoint + vector + heightOffset * Vector3.up + Vector3.Project(vector2, normalized);
			}

			public float GetGlideslopeError(Aircraft aircraft, float timeToTouchdown)
			{
				Vector3 vector = GetTouchdownPoint().ToLocalPosition();
				if (!(Runway.rb == null) && !(timeToTouchdown <= 0f))
				{
					_ = Runway.GetVelocity() * timeToTouchdown;
				}
				else
				{
					_ = Vector3.zero;
				}
				Vector3 vector2 = new Vector3(aircraft.transform.position.x, 0f, aircraft.transform.position.z) - new Vector3(vector.x, 0f, vector.z);
				float num = vector.y + aircraft.definition.spawnOffset.y + 0.06f * vector2.magnitude;
				return aircraft.transform.position.y - num;
			}
		}

		[SerializeField]
		private string name;

		public Transform Start;

		public Transform End;

		public Transform[] entryPoints;

		public Transform[] exitPoints;

		public bool Reversable;

		public bool Takeoff;

		public bool Landing;

		public bool Arrestor;

		public bool SkiJump;

		public bool AllowSimultaneousTakeoff = true;

		[SerializeField]
		private float width;

		[SerializeField]
		private Rigidbody rb;

		[SerializeField]
		private Renderer entryLightsAvailable;

		[SerializeField]
		private Renderer entryLightsOccupied;

		private List<Aircraft> landingList = new List<Aircraft>();

		private Vector3 direction;

		private Queue<Aircraft> takeoffQueue = new Queue<Aircraft>();

		private bool CurrentlyOperatingReversed;

		[NonSerialized]
		private List<Runway> crossingRunways = new List<Runway>();

		[NonSerialized]
		public float LastUsed;

		[NonSerialized]
		public byte index;

		[NonSerialized]
		public Airbase airbase;

		[NonSerialized]
		public bool occupied;

		public float Length { get; private set; }

		private RunwayType RunwayType => (RunwayType)((Landing ? 1 : 0) | (Takeoff ? 2 : 0));

		public event Action<Aircraft> OnRegisterLanding;

		public static Runway FromSaved(Airbase airbase, SavedRunway saved)
		{
			Runway runway = new Runway();
			runway.name = saved.Name;
			runway.Reversable = saved.Reversable;
			runway.Takeoff = saved.Takeoff;
			runway.Landing = saved.Landing;
			runway.Arrestor = saved.Arrestor;
			runway.SkiJump = saved.SkiJump;
			runway.width = saved.Width;
			runway.Start = CreateTransform(airbase.transform, saved.Start);
			runway.End = CreateTransform(airbase.transform, saved.End);
			runway.exitPoints = CreateTransforms(airbase.transform, saved.exitPoints);
			Transform[] array = runway.exitPoints;
			if (array == null || array.Length == 0)
			{
				runway.exitPoints = new Transform[2];
				runway.exitPoints[0] = runway.Start;
				runway.exitPoints[1] = runway.End;
			}
			return runway;
			static Transform CreateTransform(Transform parent, GlobalPosition position)
			{
				Transform transform = new GameObject("runway_transform").transform;
				transform.parent = parent;
				transform.position = position.ToLocalPosition();
				return transform;
			}
			static Transform[] CreateTransforms(Transform parent, GlobalPosition[] position)
			{
				Transform[] array2 = new Transform[position.Length];
				for (int i = 0; i < position.Length; i++)
				{
					array2[i] = CreateTransform(parent, position[i]);
				}
				return array2;
			}
		}

		public void Setup(Airbase airbase, int index)
		{
			this.airbase = airbase;
			this.index = (byte)index;
			LastUsed = -100f;
			Length = Vector3.Distance(Start.position, End.position);
		}

		public List<Aircraft> GetLandingList()
		{
			return landingList;
		}

		public void SetUsageDirection(bool reversed)
		{
			CurrentlyOperatingReversed = reversed;
			LastUsed = Time.timeSinceLevelLoad;
		}

		private bool CrossesRunway(Runway otherRunway)
		{
			if (TargetCalc.ClosestPointsOnTwoLines(out var closestPointLine, out var closestPointLine2, Start.position, GetDirection(reverse: false), otherRunway.Start.position, otherRunway.GetDirection(reverse: false)) && Vector3.Distance(closestPointLine, closestPointLine2) < GetWidth())
			{
				return true;
			}
			return false;
		}

		public void FindCrossingRunways()
		{
			crossingRunways = new List<Runway>();
			Runway[] runways = airbase.runways;
			foreach (Runway runway in runways)
			{
				if (runway.CrossesRunway(this))
				{
					crossingRunways.Add(runway);
				}
			}
		}

		public bool IsAvailableForTakeoff(Aircraft querier)
		{
			if (landingList.Count > 0)
			{
				return false;
			}
			if (takeoffQueue.TryPeek(out var result))
			{
				if (result == null || result.disabled)
				{
					takeoffQueue.Dequeue();
				}
				return result == querier;
			}
			if (OtherAircraftUsingRunway(querier))
			{
				return false;
			}
			foreach (Runway crossingRunway in crossingRunways)
			{
				if (crossingRunway.OtherAircraftUsingRunway(querier))
				{
					return false;
				}
			}
			return true;
		}

		public void QueueTakeoff(Aircraft plane)
		{
			if (takeoffQueue == null)
			{
				takeoffQueue = new Queue<Aircraft>();
			}
			takeoffQueue.Enqueue(plane);
		}

		public void DequeueTakeoff(Aircraft plane)
		{
			if (takeoffQueue == null)
			{
				takeoffQueue = new Queue<Aircraft>();
			}
			if (takeoffQueue.TryPeek(out var result) && (result == plane || result == null || result.disabled))
			{
				takeoffQueue.Dequeue();
			}
		}

		public void RegisterStartTakeoff(Aircraft plane)
		{
			if (AllowSimultaneousTakeoff && takeoffQueue.TryPeek(out var result) && (result == plane || result == null || result.disabled))
			{
				takeoffQueue.Dequeue();
			}
		}

		public void RegisterTakeoffLeftRunway(Aircraft plane)
		{
			if (takeoffQueue.TryPeek(out var result) && (result == plane || result == null || result.disabled))
			{
				takeoffQueue.Dequeue();
			}
		}

		public void RegisterLanding(Aircraft plane)
		{
			if (!landingList.Contains(plane))
			{
				landingList.Add(plane);
				this.OnRegisterLanding?.Invoke(plane);
			}
		}

		public void DeregisterLanding(Aircraft plane)
		{
			landingList.Remove(plane);
		}

		public void MonitorLandings(Airbase airbase)
		{
			for (int num = landingList.Count - 1; num >= 0; num--)
			{
				if (landingList[num] == null)
				{
					landingList.RemoveAt(num);
				}
				LastUsed = Time.timeSinceLevelLoad;
			}
			if (landingList.Count == 0 && Landing && airbase.attachedUnit is Ship ship)
			{
				ship.RestoreSteeringRate();
			}
		}

		public bool LandingsInProgress()
		{
			return landingList.Count > 0;
		}

		public bool IsSuitable(RunwayQuery query)
		{
			if (!RunwayType.Allowed(query.RunwayType))
			{
				return false;
			}
			if (!IsLevel())
			{
				return false;
			}
			float num = query.MinSize;
			if (Arrestor)
			{
				if (query.TailHook)
				{
					return true;
				}
				float num2 = ((airbase.attachedUnit != null) ? airbase.attachedUnit.speed : 0f);
				num = (query.LandingSpeed - num2) * (query.LandingSpeed - num2) / 12f;
			}
			return num < Length;
		}

		public bool OtherAircraftUsingRunway(Aircraft checker)
		{
			foreach (Aircraft landing in landingList)
			{
				if (landing != checker)
				{
					return true;
				}
			}
			if (takeoffQueue.TryPeek(out var result) && result != null && result != checker)
			{
				return true;
			}
			return false;
		}

		public bool CrossingRunwaysInUse()
		{
			foreach (Runway crossingRunway in crossingRunways)
			{
				if (crossingRunway.LandingsInProgress())
				{
					return true;
				}
			}
			return false;
		}

		public bool ClearForTakeoff(Aircraft aircraft, bool checkCrossing)
		{
			foreach (Runway crossingRunway in crossingRunways)
			{
				if (!crossingRunway.ClearForTakeoff(aircraft, checkCrossing: false))
				{
					return false;
				}
			}
			if (takeoffQueue.Count != 0 && takeoffQueue.TryPeek(out var result))
			{
				return result == aircraft;
			}
			return true;
		}

		public bool AircraftOnApproach(Aircraft aircraft, float range, bool excludeBetweenEndpoints)
		{
			if (excludeBetweenEndpoints && Vector3.Dot(aircraft.transform.position - Start.position, GetDirection(reverse: false)) > 0f && Vector3.Dot(aircraft.transform.position - End.position, GetDirection(reverse: true)) > 0f)
			{
				return false;
			}
			if (!FastMath.InRange(Start.position, aircraft.transform.position, range) && !FastMath.InRange(End.position, aircraft.transform.position, range))
			{
				return false;
			}
			return Vector3.Dot(aircraft.transform.forward, ((Start.position + End.position) * 0.5f - aircraft.transform.position).normalized) > 0.8f;
		}

		public bool AircraftOnRunway(Aircraft aircraft)
		{
			return Vector3.Distance(aircraft.transform.position, GetNearestPoint(aircraft.transform, extend: false)) < GetWidth() * 0.5f + aircraft.maxRadius;
		}

		public float AircraftDistanceFromRunway(Aircraft aircraft)
		{
			if (Vector3.Dot(aircraft.transform.position - Start.position, GetDirection(reverse: false)) < 0f || Vector3.Dot(aircraft.transform.position - End.position, GetDirection(reverse: true)) < 0f)
			{
				return float.MaxValue;
			}
			return Vector3.Distance(aircraft.transform.position, GetNearestPoint(aircraft.transform, extend: false)) - GetWidth() + aircraft.maxRadius;
		}

		public bool AircraftApproachingRunway(Aircraft aircraft, float distance)
		{
			Vector3 nearestPoint = GetNearestPoint(aircraft.transform, extend: true);
			if (Vector3.Dot(nearestPoint - aircraft.transform.position, aircraft.transform.forward) < 0f)
			{
				return false;
			}
			return (nearestPoint - aircraft.transform.position).sqrMagnitude < distance * distance;
		}

		public bool TryGetExitTaxiPoint(Transform fromTransform, float speed, out Transform exitPoint)
		{
			exitPoint = null;
			float num = float.MaxValue;
			for (int i = 0; i < exitPoints.Length; i++)
			{
				Vector3 lhs = exitPoints[i].position - fromTransform.position;
				if (Vector3.Dot(exitPoints[i].forward, fromTransform.forward) > 0f && Vector3.Dot(lhs, fromTransform.forward) > 0f)
				{
					float magnitude = lhs.magnitude;
					if (magnitude < num && 225f > speed * speed - 12f * magnitude)
					{
						num = magnitude;
						exitPoint = exitPoints[i];
					}
				}
			}
			return exitPoint != null;
		}

		public Vector3 GetVelocity()
		{
			if (rb == null)
			{
				return Vector3.zero;
			}
			return rb.GetPointVelocity(Start.position);
		}

		public bool IsLevel()
		{
			return Mathf.Abs(Start.position.y - End.position.y) < Length * 0.03f;
		}

		public float GetWidth()
		{
			return width;
		}

		public Vector3 GetNearestPoint(Transform fromTransform, bool extend)
		{
			return GetNearestPoint(fromTransform.position, extend);
		}

		public Vector3 GetNearestPoint(Vector3 fromPosition, bool extend)
		{
			if (!extend)
			{
				Vector3 lhs = fromPosition - Start.position;
				Vector3 lhs2 = fromPosition - End.position;
				if (Vector3.Dot(lhs, GetDirection(reverse: false)) < 0f || Vector3.Dot(lhs2, GetDirection(reverse: true)) < 0f)
				{
					if (!(lhs.sqrMagnitude < lhs2.sqrMagnitude))
					{
						return End.position;
					}
					return Start.position;
				}
			}
			return Start.position + Vector3.Project(fromPosition - Start.position, GetDirection(reverse: false));
		}

		public Vector3 GetDirection(bool reverse)
		{
			if (!reverse)
			{
				return End.position - Start.position;
			}
			return Start.position - End.position;
		}

		public string GetName(bool reverse)
		{
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}
			int num = Mathf.RoundToInt(Quaternion.LookRotation(reverse ? (Start.position - End.position) : (End.position - Start.position), Vector3.up).eulerAngles.y * 0.1f);
			if (num == 0)
			{
				num = 36;
			}
			if (num >= 10)
			{
				return $"Runway {num}";
			}
			return $"Runway 0{num}";
		}

		public RunwayDistanceResult GetDistance(Transform fromTransform)
		{
			if (Time.timeSinceLevelLoad - LastUsed < 30f)
			{
				return new RunwayDistanceResult(Vector3.Distance((CurrentlyOperatingReversed ? End : Start).position, fromTransform.position), CurrentlyOperatingReversed);
			}
			float num = Vector3.Distance(Start.position, fromTransform.position);
			if (!Reversable)
			{
				return new RunwayDistanceResult(num, reverse: false);
			}
			float num2 = Vector3.Distance(End.position, fromTransform.position);
			if (num2 < num)
			{
				return new RunwayDistanceResult(num2, reverse: true);
			}
			return new RunwayDistanceResult(num, reverse: false);
		}

		public RunwayAngleResult GetAngle(Transform fromTransform)
		{
			if (direction == Vector3.zero)
			{
				direction = (End.position - Start.position).normalized;
			}
			Vector3 vector = Start.position - fromTransform.position;
			vector.y = 0f;
			float num = Vector3.Angle(vector, direction);
			if (!Reversable)
			{
				return new RunwayAngleResult(num, reverse: false);
			}
			Vector3 vector2 = End.position - fromTransform.position;
			vector.y = 0f;
			float num2 = Vector3.Angle(vector2, -direction);
			if (num < num2)
			{
				return new RunwayAngleResult(num, reverse: false);
			}
			return new RunwayAngleResult(num2, reverse: true);
		}
	}

	public readonly struct TrySpawnResult
	{
		public readonly bool Allowed;

		public readonly Hangar Hangar;

		public readonly bool DelayedSpawn;

		public TrySpawnResult(bool allowed, Hangar hangar, bool delayedSpawn)
		{
			Allowed = allowed;
			Hangar = hangar;
			DelayedSpawn = delayedSpawn;
		}
	}

	public const string ATTACHED_AIRBASE_PREFIX = "<UNIT_AIRBASE>++";

	[SerializeField]
	private SavedAirbase airbaseSettings = new SavedAirbase();

	public Transform aircraftSelectionTransform;

	public Transform fixedCameraTransform;

	public Transform center;

	[SerializeField]
	private Transform[] servicePoints;

	public Runway[] runways;

	public VerticalLandingPoint[] verticalLandingPoints;

	[SerializeField]
	public Building MapTower;

	[NonSerialized]
	private Building tower;

	[SerializeField]
	private Renderer[] runwayLights;

	[SerializeField]
	private GameObject[] selectionObjects;

	[SerializeField]
	private RoadNetwork taxiNetwork;

	[SerializeField]
	private Unit attachedUnit;

	public bool AllowTaxiDuringLandings;

	public bool AllowTaxiDuringTakeoffs;

	private float initializeTimer;

	private List<GridSquare> gridSquares;

	private List<AircraftDefinition> availableAircraft = new List<AircraftDefinition>();

	private readonly List<Aircraft> cache = new List<Aircraft>();

	private bool airbaseRegistered;

	[SyncVar(initialOnly = true)]
	private string networkUniqueName;

	[CompilerGenerated]
	[SyncVar]
	private bool _003Cdisabled_003Ek__BackingField;

	private ObjectType objectType;

	[NonSerialized]
	private const int SYNC_VAR_COUNT = 2;

	[NonSerialized]
	private const int RPC_COUNT = 2;

	public Capture capture { get; private set; }

	public List<Aircraft> ControlledAircraft { get; private set; }

	public FactionHQ CurrentHQ { get; private set; }

	public List<Building> buildings { get; private set; } = new List<Building>();

	public List<Hangar> hangars { get; private set; } = new List<Hangar>();

	public List<WarheadStorage> stores { get; private set; } = new List<WarheadStorage>();

	public bool disabled
	{
		[CompilerGenerated]
		get
		{
			return _003Cdisabled_003Ek__BackingField;
		}
		[CompilerGenerated]
		private set
		{
			Network_003Cdisabled_003Ek__BackingField = value;
		}
	}

	public SavedAirbase SavedAirbase { get; private set; }

	public bool BuiltIn
	{
		get
		{
			if (ObjectType != ObjectType.MapObject)
			{
				return objectType == ObjectType.SceneObject;
			}
			return true;
		}
	}

	public bool AttachedAirbase => (object)attachedUnit != null;

	public bool IsCustom
	{
		get
		{
			if (!BuiltIn)
			{
				return !AttachedAirbase;
			}
			return false;
		}
	}

	public bool SavedAirbaseOverride => SavedAirbase != airbaseSettings;

	public ObjectType ObjectType
	{
		get
		{
			if (objectType == ObjectType.NotSet)
			{
				objectType = MapLoader.GetObjectType(base.Identity);
			}
			return objectType;
		}
	}

	Capture ICapturable.Capture => capture;

	bool ICapturable.disabled => disabled;

	Transform ICapturable.center => center;

	float ICapturable.CaptureRange => SavedAirbase.CaptureRange;

	float ICapturable.CaptureDefense => SavedAirbase.CaptureDefense;

	FactionHQ ICapturable.CurrentHQ => CurrentHQ;

	List<GridSquare> ICapturable.gridSquares => gridSquares;

	public string NetworknetworkUniqueName
	{
		get
		{
			return networkUniqueName;
		}
		set
		{
			networkUniqueName = value;
		}
	}

	public bool Network_003Cdisabled_003Ek__BackingField
	{
		get
		{
			return disabled;
		}
		set
		{
			if (!SyncVarEqual(value, disabled))
			{
				bool flag = disabled;
				disabled = value;
				SetDirtyBit(2uL);
			}
		}
	}

	public event Action onLostControl;

	public event Action onTakeControl;

	public bool TryGetAttachedUnit(out Unit attachedUnit)
	{
		attachedUnit = this.attachedUnit;
		return AttachedAirbase;
	}

	private void OnValidate()
	{
		if (airbaseSettings == null)
		{
			airbaseSettings = new SavedAirbase();
		}
		if (attachedUnit != null)
		{
			airbaseSettings.Capturable = false;
		}
		airbaseSettings.Tower = ((MapTower != null) ? MapTower.MapUniqueName : "");
		if (MapTower != null)
		{
			MapTower.MapAirbase = this;
		}
		OnValidateSetCurrentHQ();
	}

	private void OnValidateSetCurrentHQ()
	{
		if (!FactionHelper.EmptyOrNoFactionOrNeutral(airbaseSettings.faction))
		{
			if (!(CurrentHQ != null) || !(CurrentHQ.faction.factionName == airbaseSettings.faction))
			{
				FactionHQ factionHQ = (from x in Resources.FindObjectsOfTypeAll<FactionHQ>()
					where x.gameObject.scene == base.gameObject.scene
					select x).FirstOrDefault((FactionHQ x) => x.faction.factionName == airbaseSettings.faction);
				if (factionHQ == null)
				{
					Debug.LogError("Failed to find Hq in scene with name " + airbaseSettings.faction);
				}
				CurrentHQ = factionHQ;
			}
		}
		else
		{
			CurrentHQ = null;
		}
	}

	public void LinkSavedAirbase(SavedAirbase savedAirbase, bool customAirbase)
	{
		if (BuiltIn)
		{
			savedAirbase.CaptureRange = airbaseSettings.CaptureRange;
		}
		SavedAirbase = savedAirbase;
		savedAirbase.Airbase = this;
		savedAirbase.CenterWrapper.RegisterOnChange(this, delegate(GlobalPosition newPos)
		{
			center.position = newPos.ToLocalPosition();
		});
		AfterLink(savedAirbase);
	}

	private void AfterLink(SavedAirbase savedAirbase)
	{
		if (base.IsClientOnly)
		{
			return;
		}
		if (!AttachedAirbase)
		{
			if (GameManager.gameState == GameState.Editor)
			{
				EditorSetFaction(savedAirbase.faction, setAttachedAirbaseFaction: false);
			}
			else
			{
				CaptureFaction(savedAirbase.FindHQ());
			}
		}
		Network_003Cdisabled_003Ek__BackingField = savedAirbase.Disabled;
		capture.SetCapturable(savedAirbase.Capturable);
	}

	public void UnlinkSavedAirbase()
	{
		if (IsCustom || AttachedAirbase)
		{
			airbaseSettings.UniqueName = SavedAirbase.UniqueName;
		}
		SavedAirbase.CenterWrapper.UnregisterOnChange(this);
		SavedAirbase.Airbase = null;
		SavedAirbase = airbaseSettings;
		AfterLink(airbaseSettings);
	}

	public bool UnitDestroyed()
	{
		if (attachedUnit != null)
		{
			return attachedUnit.disabled;
		}
		return false;
	}

	public void SetDisabled(bool disabled)
	{
		Network_003Cdisabled_003Ek__BackingField = disabled;
	}

	private void OnDestroy()
	{
		if (airbaseRegistered)
		{
			FactionRegistry.UnregisterAirbase(this);
			airbaseRegistered = false;
		}
		if (SavedAirbase != airbaseSettings)
		{
			SavedAirbase.CenterWrapper.UnregisterOnChange(this);
		}
	}

	public RoadNetwork GetTaxiNetwork()
	{
		return taxiNetwork;
	}

	public void ShowSlectionObjects(bool show)
	{
		GameObject[] array = selectionObjects;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(show);
		}
	}

	public float GetRadius()
	{
		if (SavedAirbase != null)
		{
			return SavedAirbase.CaptureRange;
		}
		return 0f;
	}

	public Runway.RunwayUsage? GetTakeoffRunway(Aircraft aircraft, float takeoffDist)
	{
		Runway runway = null;
		bool flag = false;
		float num = float.MaxValue;
		Runway[] array = runways;
		foreach (Runway runway2 in array)
		{
			if (runway2.Takeoff && !(runway2.Length < takeoffDist))
			{
				Runway.RunwayDistanceResult distance = runway2.GetDistance(aircraft.transform);
				if (distance.Distance < num)
				{
					num = distance.Distance;
					runway = runway2;
					flag = distance.Reverse;
				}
			}
		}
		if (runway != null)
		{
			runway.SetUsageDirection(flag);
			return new Runway.RunwayUsage(runway, flag);
		}
		return null;
	}

	public Runway.RunwayUsage? RequestLanding(Aircraft aircraft, RunwayQuery runwayQuery)
	{
		Runway runway = null;
		bool reverse = false;
		float num = 180f;
		Runway[] array = runways;
		foreach (Runway runway2 in array)
		{
			Runway.RunwayAngleResult angle = runway2.GetAngle(aircraft.transform);
			if (runway2.Landing && runway2.IsSuitable(runwayQuery) && angle.Angle < num)
			{
				num = angle.Angle;
				runway = runway2;
				reverse = angle.Reverse;
			}
		}
		if (runway != null)
		{
			return new Runway.RunwayUsage(runway, reverse);
		}
		return null;
	}

	public bool RequestTaxi(Aircraft querier)
	{
		bool flag = AircraftApproachingRunwayInUse(querier);
		if (!AircraftOnRunwayInUse(querier))
		{
			return !flag;
		}
		return true;
	}

	public bool TryRequestVerticalLanding(Aircraft aircraft, RunwayQuery runwayQuery, out VerticalLandingPoint landingPoint)
	{
		landingPoint = null;
		if (verticalLandingPoints.Length == 0)
		{
			return false;
		}
		float num = float.MaxValue;
		VerticalLandingPoint[] array = verticalLandingPoints;
		foreach (VerticalLandingPoint verticalLandingPoint in array)
		{
			if (verticalLandingPoint.IsSuitable(runwayQuery))
			{
				float angle = verticalLandingPoint.GetAngle(aircraft.transform);
				angle *= (float)((!verticalLandingPoint.IsOccupied(aircraft)) ? 1 : 100);
				if (angle < num)
				{
					num = angle;
					landingPoint = verticalLandingPoint;
				}
			}
		}
		return landingPoint != null;
	}

	[RateLimit(Refill = 5, MaxTokens = 30, Penalty = 2)]
	[ServerRpc(requireAuthority = false)]
	public void CmdRegisterUsage(Aircraft aircraft, bool isUsing, byte? landingRunway)
	{
		if (ServerRpcSender.ShouldInvokeLocally(this, requireAuthority: false, allowServerToCall: false))
		{
			UserCode_CmdRegisterUsage_1828364621(aircraft, isUsing, landingRunway);
			return;
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Aircraft(writer, aircraft);
		writer.WriteBooleanExtension(isUsing);
		GeneratedNetworkCode._Write_System_002ENullable_00601_003CSystem_002EByte_003E(writer, landingRunway);
		ServerRpcSender.Send(this, 0, writer, Mirage.Channel.Reliable, requireAuthority: false);
		writer.Release();
	}

	[ClientRpc]
	public void RpcRegisterUsage(Aircraft aircraft, bool isUsing, byte? landingRunway)
	{
		if (ClientRpcSender.ShouldInvokeLocally(this, RpcTarget.Observers, null, excludeOwner: false))
		{
			UserCode_RpcRegisterUsage_2147349378(aircraft, isUsing, landingRunway);
		}
		PooledNetworkWriter writer = NetworkWriterPool.GetWriter();
		GeneratedNetworkCode._Write_Aircraft(writer, aircraft);
		writer.WriteBooleanExtension(isUsing);
		GeneratedNetworkCode._Write_System_002ENullable_00601_003CSystem_002EByte_003E(writer, landingRunway);
		ClientRpcSender.Send(this, 1, writer, Mirage.Channel.Reliable, excludeOwner: false);
		writer.Release();
	}

	public void AddControlledAircraft(Aircraft aircraft)
	{
		if (!ControlledAircraft.Contains(aircraft))
		{
			ControlledAircraft.Add(aircraft);
		}
	}

	public void RemoveControlledAircraft(Aircraft aircraft)
	{
		ControlledAircraft.Remove(aircraft);
	}

	public Runway GetLandingRunway()
	{
		Runway[] array = runways;
		foreach (Runway runway in array)
		{
			if (runway.Landing)
			{
				return runway;
			}
		}
		return null;
	}

	public bool TryGetNearestServicePoint(Vector3 fromPosition, out Transform nearestServicePoint)
	{
		if (servicePoints.Length == 0)
		{
			nearestServicePoint = null;
			return false;
		}
		nearestServicePoint = servicePoints[0];
		if (servicePoints.Length == 1)
		{
			return true;
		}
		float num = float.MaxValue;
		Transform[] array = servicePoints;
		foreach (Transform transform in array)
		{
			float num2 = FastMath.SquareDistance(transform.position, fromPosition);
			if (num2 < num)
			{
				num = num2;
				nearestServicePoint = transform;
			}
		}
		return true;
	}

	public bool AircraftIsOnRunway(Aircraft aircraft, bool landingRunwaysOnly, out Runway outRunway)
	{
		Runway runway = null;
		float num = float.MaxValue;
		Runway[] array = runways;
		foreach (Runway runway2 in array)
		{
			if (!landingRunwaysOnly || runway2.Landing)
			{
				float num2 = runway2.AircraftDistanceFromRunway(aircraft);
				if (num2 < runway2.GetWidth() * 0.5f && num2 < num && !(Vector3.Dot(aircraft.transform.position - runway2.Start.position, runway2.GetDirection(reverse: false)) < 0f) && !(Vector3.Dot(aircraft.transform.position - runway2.End.position, runway2.GetDirection(reverse: true)) < 0f))
				{
					runway = runway2;
					num = num2;
				}
			}
		}
		outRunway = runway;
		return outRunway != null;
	}

	public bool AircraftApproachingRunwayInUse(Aircraft aircraft)
	{
		Runway[] array = runways;
		foreach (Runway runway in array)
		{
			if (runway.OtherAircraftUsingRunway(aircraft) && runway.AircraftApproachingRunway(aircraft, 40f + aircraft.maxRadius))
			{
				return true;
			}
		}
		return false;
	}

	public bool AircraftOnRunwayInUse(Aircraft aircraft)
	{
		Runway[] array = runways;
		foreach (Runway runway in array)
		{
			if (runway.OtherAircraftUsingRunway(aircraft) && runway.AircraftOnRunway(aircraft))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsSuitable(RunwayQuery query)
	{
		if (disabled)
		{
			return false;
		}
		if (query.RunwayType.QueryFor(RunwayType.LandingOrTakeoff))
		{
			Runway[] array = runways;
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].IsSuitable(query))
				{
					return true;
				}
			}
		}
		if (query.RunwayType.QueryFor(RunwayType.Vertical))
		{
			VerticalLandingPoint[] array2 = verticalLandingPoints;
			for (int i = 0; i < array2.Length; i++)
			{
				if (array2[i].IsSuitable(query))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void AddBuilding(Building building, bool errorIfAlreadyMember)
	{
		if (buildings.Contains(building))
		{
			if (errorIfAlreadyMember)
			{
				Debug.LogError($"{building} already part of {this}");
			}
			return;
		}
		Hangar component;
		bool flag = building.TryGetComponent<Hangar>(out component);
		WarheadStorage component2;
		bool flag2 = building.TryGetComponent<WarheadStorage>(out component2);
		string text = ((flag && flag2) ? " [hangar, storage]" : (flag ? " [hangar]" : ((!flag2) ? "" : " [storage]")));
		ColorLog<Airbase>.Info($"Adding Building{text} ({building.name}, {building.persistentID}) to {base.name}");
		buildings.Add(building);
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			building.NetworkHQ = CurrentHQ;
		}
		if (flag)
		{
			AddHangar(component);
		}
		if (flag2)
		{
			AddStorage(component2);
		}
		if (building.UniqueName == SavedAirbase.Tower)
		{
			tower = building;
		}
	}

	public void RemoveBuilding(Building building)
	{
		buildings.Remove(building);
	}

	public void AddHangar(Hangar hangar)
	{
		if (hangars.Contains(hangar))
		{
			Debug.LogError($"{hangar} already part of {this}");
			return;
		}
		hangars.Add(hangar);
		hangar.parentAirbase = this;
		SortHangarsByPriority();
	}

	internal void RemoveHangar(Hangar hangar)
	{
		hangars.Remove(hangar);
		hangar.parentAirbase = null;
		if (BuiltIn && hangar.attachedUnit is Building building)
		{
			RemoveBuilding(building);
		}
	}

	public void AddStorage(WarheadStorage storage)
	{
		if (stores.Contains(storage))
		{
			Debug.LogError($"{storage} already part of {this}");
		}
		else
		{
			stores.Add(storage);
		}
	}

	internal void RemoveStorage(WarheadStorage storage)
	{
		stores.Remove(storage);
		if (BuiltIn && storage.attachedUnit is Building building)
		{
			RemoveBuilding(building);
		}
	}

	public bool HasStorage()
	{
		return stores.Count > 0;
	}

	public int GetStorage()
	{
		return stores.Count;
	}

	public int GetWarheads()
	{
		if (!HasStorage())
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < stores.Count; i++)
		{
			if (!stores[i].Disabled)
			{
				num += stores[i].number;
			}
		}
		return num;
	}

	public void AddWarheads(int number)
	{
		if (!HasStorage())
		{
			CurrentHQ.AddWarheadStockpile(number);
			return;
		}
		while (number > 0)
		{
			int num = int.MaxValue;
			int index = 0;
			for (int i = 0; i < stores.Count; i++)
			{
				if (!stores[i].Disabled && stores[i].number < num)
				{
					num = stores[i].number;
					index = i;
				}
			}
			stores[index].AddWarhead(1);
			number--;
			if (number == 0)
			{
				break;
			}
		}
	}

	public void RemoveWarheads(int number)
	{
		if (!HasStorage())
		{
			return;
		}
		while (number > 0)
		{
			int num = 0;
			int index = 0;
			for (int i = 0; i < stores.Count; i++)
			{
				if (!stores[i].Disabled && stores[i].number > num)
				{
					num = stores[i].number;
					index = i;
				}
			}
			stores[index].RemoveWarhead(1);
			number--;
			if (number == 0)
			{
				break;
			}
		}
	}

	public bool HasVehicleDepot()
	{
		for (int i = 0; i < buildings.Count; i++)
		{
			if (buildings[i] is VehicleDepot)
			{
				return true;
			}
		}
		return false;
	}

	public void EditorSetFaction(string factionName, bool setAttachedAirbaseFaction)
	{
		FactionHQ factionHQ = (CurrentHQ = FactionRegistry.HqFromName(factionName));
		foreach (Building building in buildings)
		{
			if (building != null)
			{
				building.NetworkHQ = factionHQ;
			}
		}
		Color colorOrGray = factionHQ.GetColorOrGray();
		AirbaseEditorFlag airbaseEditorFlag = AirbaseEditorFlag.Find(center);
		if (airbaseEditorFlag != null)
		{
			airbaseEditorFlag.SetColor(colorOrGray);
		}
		AirbaseEditorRadius airbaseEditorRadius = AirbaseEditorRadius.Find(center);
		if (airbaseEditorRadius != null)
		{
			airbaseEditorRadius.Setup(colorOrGray, GetRadius());
		}
	}

	public void SetFactionWithoutEvent(FactionHQ capturingHQ, bool runAssets = true)
	{
		if (runAssets)
		{
			_ = base.IsServer;
			_ = AttachedAirbase;
		}
		CurrentHQ = capturingHQ;
	}

	private void CaptureFaction(FactionHQ newHQ)
	{
		if (CurrentHQ == newHQ)
		{
			_ = $"{this} already part of '{CurrentHQ}'";
			_ = GameManager.gameState;
			_ = 3;
		}
		FactionHQ currentHQ = CurrentHQ;
		CurrentHQ = newHQ;
		if (currentHQ != newHQ)
		{
			if (currentHQ != null)
			{
				currentHQ.RemoveAirbase(this);
				this.onLostControl?.Invoke();
			}
			if (newHQ != null)
			{
				newHQ.AddAirbase(this);
				this.onTakeControl?.Invoke();
			}
		}
		if (newHQ == null)
		{
			return;
		}
		foreach (Building building in buildings)
		{
			if (building != null)
			{
				building.NetworkHQ = newHQ;
			}
		}
		WaitRepair().Forget();
	}

	private async UniTask WaitRepair()
	{
		await UniTask.Delay(5000);
		foreach (Building building in buildings)
		{
			if (building != null && building.disabled && building.IsRepairable())
			{
				building.Networkdisabled = false;
			}
		}
		if (tower != null && tower.IsRepairable())
		{
			Renderer[] array = runwayLights;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
		}
	}

	private void Update()
	{
		if (NetworkManagerNuclearOption.i.Server.Active)
		{
			initializeTimer += Time.unscaledDeltaTime;
			if (gridSquares == null && initializeTimer > 1f)
			{
				gridSquares = BattlefieldGrid.GetGridSquaresInRange(center.transform.GlobalPosition(), 1000f);
			}
		}
	}

	private void ControlAircraft()
	{
		cache.Clear();
		cache.AddRange(ControlledAircraft);
		for (int num = cache.Count - 1; num >= 0; num--)
		{
			Aircraft aircraft = cache[num];
			if (aircraft == null || !FastMath.InRange(aircraft.transform.position, center.position, 5000f))
			{
				RpcRegisterUsage(aircraft, isUsing: false, null);
			}
		}
		Runway[] array = runways;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].MonitorLandings(this);
		}
	}

	private void Awake()
	{
		capture = GetComponent<Capture>();
		ControlledAircraft = new List<Aircraft>();
		for (int i = 0; i < runways.Length; i++)
		{
			runways[i].Setup(this, i);
		}
		airbaseSettings.Center = center.GlobalPosition();
		if (aircraftSelectionTransform != null)
		{
			airbaseSettings.SelectionPosition = aircraftSelectionTransform.GlobalPosition();
		}
		SavedAirbase = airbaseSettings;
		base.Identity.OnStartServer.AddListener(OnStartServer);
		base.Identity.OnStartClient.AddListener(OnStartClientOnly);
		base.Identity.OnStopServer.AddListener(OnStopServer);
	}

	public void SetupCustomAirbase(SavedAirbase saved)
	{
		LinkSavedAirbase(saved, customAirbase: true);
		base.transform.position = saved.Center.ToLocalPosition();
		center.localPosition = Vector3.zero;
		if (aircraftSelectionTransform != null)
		{
			aircraftSelectionTransform.position = saved.SelectionPosition.ToLocalPosition();
		}
		verticalLandingPoints = new VerticalLandingPoint[saved.VerticalLandingPoints.Count];
		for (int i = 0; i < saved.VerticalLandingPoints.Count; i++)
		{
			verticalLandingPoints[i] = VerticalLandingPoint.FromSaved(this, saved.VerticalLandingPoints[i]);
		}
		servicePoints = new Transform[saved.ServicePoints.Count];
		for (int j = 0; j < saved.ServicePoints.Count; j++)
		{
			GameObject gameObject = new GameObject($"ServicePoints {j}");
			gameObject.transform.parent = base.transform;
			gameObject.transform.position = saved.ServicePoints[j].ToLocalPosition();
			servicePoints[j] = gameObject.transform;
		}
		int num = saved.runways?.Count ?? 0;
		runways = new Runway[num];
		for (int k = 0; k < num; k++)
		{
			runways[k] = Runway.FromSaved(this, saved.runways[k]);
			runways[k].Setup(this, k);
		}
		taxiNetwork = saved.roads;
	}

	public void SetupAttachedAirbase(Unit unit)
	{
		string uniqueName = "<UNIT_AIRBASE>++" + unit.UniqueName;
		SavedAirbase.UniqueName = uniqueName;
		CurrentHQ = unit.NetworkHQ;
		SavedAirbase savedAirbase = MissionManager.CurrentMission.airbases.FirstOrDefault((SavedAirbase x) => x.UniqueName == uniqueName) ?? SavedAirbase;
		LinkSavedAirbase(savedAirbase, customAirbase: false);
	}

	private void FindAttachedHangars()
	{
		Hangar[] componentsInChildren = attachedUnit.GetComponentsInChildren<Hangar>();
		foreach (Hangar hangar in componentsInChildren)
		{
			AddHangar(hangar);
		}
	}

	private void OnStartClientOnly()
	{
		if (base.IsServer)
		{
			return;
		}
		ColorLog<Airbase>.Info($"Airbase spawned on client NetId={base.NetId} UniqueName={networkUniqueName}");
		FactionRegistry.RegisterAirbase(networkUniqueName, this);
		airbaseRegistered = true;
		FactionHQ factionHQ = null;
		foreach (FactionHQ value in FactionRegistry.HQLookup.Values)
		{
			if (value.ContainsAirbase(this))
			{
				factionHQ = value;
			}
		}
		if (factionHQ != null)
		{
			SetFactionWithoutEvent(factionHQ, runAssets: false);
		}
		if (IsCustom)
		{
			SavedAirbase savedAirbase = MissionManager.CurrentMission.airbases.Find((SavedAirbase x) => x.UniqueName == networkUniqueName);
			if (savedAirbase != null)
			{
				SetupCustomAirbase(savedAirbase);
			}
			else
			{
				Debug.LogError("Could not find SavedAirbase in mission for CustomAirbase, name=" + networkUniqueName);
			}
		}
		if (AttachedAirbase)
		{
			FindAttachedHangars();
			SetupAttachedAirbase(attachedUnit);
		}
	}

	private void OnStartServer()
	{
		FactionRegistry.RegisterAirbase(SavedAirbase.UniqueName, this);
		airbaseRegistered = true;
		NetworknetworkUniqueName = SavedAirbase.UniqueName;
		if (tower != null)
		{
			tower.onDisableUnit += Airbase_OnTowerDisabled;
		}
		if (taxiNetwork == null)
		{
			taxiNetwork = new RoadNetwork();
		}
		taxiNetwork.RegenerateNetwork();
		this.StartSlowUpdate(5f, ControlAircraft);
		if (attachedUnit != null)
		{
			attachedUnit.onDisableUnit += Airbase_OnUnitDisabled;
			FindAttachedHangars();
			if (attachedUnit.NetworkHQ != null)
			{
				attachedUnit.NetworkHQ.AddAirbase(this);
			}
			CaptureFaction(attachedUnit.NetworkHQ);
		}
		Network_003Cdisabled_003Ek__BackingField = SavedAirbase.Disabled;
		capture.SetCapturable(SavedAirbase.Capturable);
		Runway[] array = runways;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].FindCrossingRunways();
		}
		VerticalLandingPoint[] array2 = verticalLandingPoints;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].FindCrossingRunways(this);
		}
	}

	private void OnStopServer()
	{
		if (airbaseRegistered)
		{
			FactionRegistry.UnregisterAirbase(this);
			airbaseRegistered = false;
		}
	}

	private void Airbase_OnUnitDisabled(Unit unit)
	{
		hangars.Clear();
		CaptureFaction(null);
	}

	private void Airbase_OnTowerDisabled(Unit unit)
	{
		Renderer[] array = runwayLights;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
	}

	public bool AnyHangarsAvailable()
	{
		if (disabled)
		{
			return false;
		}
		foreach (Hangar hangar in hangars)
		{
			if (!hangar.Disabled)
			{
				return true;
			}
		}
		return false;
	}

	public List<AircraftDefinition> GetAvailableAircraft()
	{
		availableAircraft.Clear();
		if (disabled)
		{
			return availableAircraft;
		}
		foreach (Hangar hangar in hangars)
		{
			if (hangar.Disabled)
			{
				continue;
			}
			AircraftDefinition[] array = hangar.GetAvailableAircraft();
			foreach (AircraftDefinition item in array)
			{
				if (!availableAircraft.Contains(item))
				{
					availableAircraft.Add(item);
				}
			}
		}
		return availableAircraft;
	}

	public bool CanSpawnAircraft(AircraftDefinition definition)
	{
		if (disabled)
		{
			return false;
		}
		foreach (Hangar hangar in hangars)
		{
			if (!hangar.Disabled && hangar.CanSpawnAircraft(definition))
			{
				return true;
			}
		}
		return false;
	}

	[Server]
	public TrySpawnResult TrySpawnAircraft(Player player, AircraftDefinition definition, LiveryKey livery, Loadout loadout, float fuelLevel)
	{
		if (!base.IsServer)
		{
			throw new MethodInvocationException("[Server] function 'TrySpawnAircraft' called when server not active");
		}
		if (!NetworkManagerNuclearOption.i.Server.Active)
		{
			throw new MethodInvocationException("TrySpawnAircraft called when server is not active");
		}
		for (int i = 0; i < hangars.Count; i++)
		{
			if (!(hangars[i] == null))
			{
				TrySpawnResult result = hangars[i].TrySpawnAircraft(player, definition, livery, loadout, fuelLevel);
				if (result.Allowed)
				{
					return result;
				}
			}
		}
		return default(TrySpawnResult);
	}

	private void SortHangarsByPriority()
	{
		hangars.Sort((Hangar a, Hangar b) => b.GetPriority().CompareTo(a.GetPriority()));
	}

	private void OnDrawGizmos()
	{
		if (verticalLandingPoints == null)
		{
			return;
		}
		DrawSphere(Color.green, center, 30f);
		DrawSphere(Color.blue, aircraftSelectionTransform, 30f);
		VerticalLandingPoint[] array = verticalLandingPoints;
		foreach (VerticalLandingPoint verticalLandingPoint in array)
		{
			DrawSphere(Color.magenta, verticalLandingPoint.point);
		}
		Runway[] array2 = runways;
		foreach (Runway runway in array2)
		{
			DrawSphere(new Color(0f, 0.6f, 1f), runway.Start, 15f);
			DrawSphere(new Color(0f, 0.2f, 1f), runway.End, 15f);
			Transform[] array3 = runway.entryPoints ?? Array.Empty<Transform>();
			foreach (Transform point in array3)
			{
				DrawSphere(new Color(0f, 1f, 0.6f), point, 10f);
			}
			array3 = runway.exitPoints ?? Array.Empty<Transform>();
			foreach (Transform point2 in array3)
			{
				DrawSphere(new Color(0f, 1f, 0.2f), point2, 10f);
			}
		}
		foreach (Road road in taxiNetwork.roads)
		{
			Color green = Color.green;
			Color red = Color.red;
			for (int k = 0; k < road.points.Count; k++)
			{
				DrawSphere(Color.Lerp(green, red, (float)k / (float)Math.Max(road.points.Count - 1, 1)), road.points[k]);
				if (k - 1 >= 0)
				{
					Vector3 a = road.points[k - 1].ToLocalPosition();
					Vector3 vector = road.points[k].ToLocalPosition();
					Gizmos.DrawLine(Vector3.Lerp(a, vector, 0.5f), vector);
				}
				if (k + 1 < road.points.Count)
				{
					Vector3 vector2 = road.points[k].ToLocalPosition();
					Vector3 b = road.points[k + 1].ToLocalPosition();
					Gizmos.DrawLine(vector2, Vector3.Lerp(vector2, b, 0.5f));
				}
			}
		}
	}

	private static void DrawSphere(Color color, GlobalPosition point)
	{
		Gizmos.color = color;
		Gizmos.DrawSphere(point.ToLocalPosition(), 5f);
	}

	private static void DrawSphere(Color color, Transform point, float size = 5f)
	{
		if (!(point == null))
		{
			Gizmos.color = color;
			Gizmos.DrawSphere(point.position, size);
		}
	}

	SingleSelectionDetails IEditorSelectable.CreateSelectionDetails()
	{
		return new AirbaseSelectionDetails(this);
	}

	void ICapturable.OnCapture(FactionHQ value)
	{
		CaptureFaction(value);
	}

	IEnumerable<Unit> ICapturable.GetDefenseUnits()
	{
		return buildings;
	}

	private void MirageProcessed()
	{
	}

	public override bool SerializeSyncVars(NetworkWriter writer, bool initialize)
	{
		ulong syncVarDirtyBits = base.SyncVarDirtyBits;
		bool result = base.SerializeSyncVars(writer, initialize);
		if (initialize)
		{
			writer.WriteString(networkUniqueName);
			writer.WriteBooleanExtension(disabled);
			return true;
		}
		writer.Write(syncVarDirtyBits, 2);
		if ((syncVarDirtyBits & 2L) != 0L)
		{
			writer.WriteBooleanExtension(disabled);
			result = true;
		}
		return result;
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			networkUniqueName = reader.ReadString();
			disabled = reader.ReadBooleanExtension();
			return;
		}
		ulong num = reader.Read(2);
		SetDeserializeMask(num, 0);
		if ((num & 2L) != 0L)
		{
			disabled = reader.ReadBooleanExtension();
		}
	}

	public void UserCode_CmdRegisterUsage_1828364621(Aircraft aircraft, bool isUsing, byte? landingRunway)
	{
		RpcRegisterUsage(aircraft, isUsing, landingRunway);
	}

	protected static void Skeleton_CmdRegisterUsage_1828364621(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Airbase)behaviour).UserCode_CmdRegisterUsage_1828364621(GeneratedNetworkCode._Read_Aircraft(reader), reader.ReadBooleanExtension(), GeneratedNetworkCode._Read_System_002ENullable_00601_003CSystem_002EByte_003E(reader));
	}

	public void UserCode_RpcRegisterUsage_2147349378(Aircraft aircraft, bool isUsing, byte? landingRunway)
	{
		if (aircraft == null)
		{
			ControlledAircraft.RemoveAll((Aircraft s) => s == null);
		}
		else if (isUsing)
		{
			AddControlledAircraft(aircraft);
			if (landingRunway.HasValue)
			{
				byte value = landingRunway.Value;
				if (value < runways.Length)
				{
					runways[value].RegisterLanding(aircraft);
				}
				else
				{
					ColorLog<Airbase>.LogError("runwayIndex out of bounds");
				}
			}
			if (attachedUnit != null && attachedUnit is Ship ship)
			{
				ship.ReduceSteeringRate();
			}
		}
		else
		{
			RemoveControlledAircraft(aircraft);
		}
	}

	protected static void Skeleton_RpcRegisterUsage_2147349378(NetworkBehaviour behaviour, NetworkReader reader, INetworkPlayer senderConnection, int replyId)
	{
		((Airbase)behaviour).UserCode_RpcRegisterUsage_2147349378(GeneratedNetworkCode._Read_Aircraft(reader), reader.ReadBooleanExtension(), GeneratedNetworkCode._Read_System_002ENullable_00601_003CSystem_002EByte_003E(reader));
	}

	protected override int GetRpcCount()
	{
		return 2;
	}

	protected override void RegisterRpc(RemoteCallCollection collection)
	{
		base.RegisterRpc(collection);
		collection.Register(0, "Airbase.CmdRegisterUsage", cmdRequireAuthority: false, RpcInvokeType.ServerRpc, this, Skeleton_CmdRegisterUsage_1828364621, RpcRateLimitConfig.Enabled(1f, 5, 30, 2));
		collection.Register(1, "Airbase.RpcRegisterUsage", cmdRequireAuthority: false, RpcInvokeType.ClientRpc, this, Skeleton_RpcRegisterUsage_2147349378, RpcRateLimitConfig.Disabled());
	}
}
