using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace NuclearOption.SavedMission
{
	public class NewtonsoftHelper
	{
		public class ShortNameSerializationBinder : DefaultSerializationBinder
		{
			private static readonly ConcurrentDictionary<string, Type> TypeCache = new ConcurrentDictionary<string, Type>();

			private static readonly Type[] AllowedBaseTypes = new Type[2]
			{
				typeof(SavedObjective),
				typeof(SavedOutcome)
			};

			public override void BindToName(Type serializedType, out string assemblyName, out string typeName)
			{
				assemblyName = null;
				typeName = serializedType.FullName;
			}

			public override Type BindToType(string assemblyName, string typeName)
			{
				if (!typeName.StartsWith("NuclearOption.SavedMission."))
				{
					throw new JsonSerializationException("Blocked deserialization of type outside allowed namespace: " + typeName);
				}
				if (TypeCache.TryGetValue(typeName, out var value))
				{
					return value;
				}
				Type type = typeof(Mission).Assembly.GetType(typeName);
				if (type == null)
				{
					throw new JsonSerializationException("Type not found: " + typeName);
				}
				bool flag = false;
				Type[] allowedBaseTypes = AllowedBaseTypes;
				for (int i = 0; i < allowedBaseTypes.Length; i++)
				{
					if (allowedBaseTypes[i].IsAssignableFrom(type))
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					throw new JsonSerializationException("Blocked deserialization of type not in allowed hierarchy: " + typeName);
				}
				TypeCache.TryAdd(typeName, type);
				return type;
			}
		}

		public class FieldsOnlyContractResolver : DefaultContractResolver
		{
			protected override List<MemberInfo> GetSerializableMembers(Type objectType)
			{
				List<MemberInfo> serializableMembers = base.GetSerializableMembers(objectType);
				serializableMembers.RemoveAll((MemberInfo m) => m.MemberType == MemberTypes.Property);
				Type type = objectType;
				while (type != null && type != typeof(object))
				{
					FieldInfo[] fields = type.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.NonPublic);
					foreach (FieldInfo fieldInfo in fields)
					{
						if (fieldInfo.IsDefined(typeof(SerializeField), inherit: true) && !serializableMembers.Contains(fieldInfo))
						{
							serializableMembers.Add(fieldInfo);
						}
					}
					type = type.BaseType;
				}
				return serializableMembers;
			}

			protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
			{
				return (from p in base.CreateProperties(type, memberSerialization)
					orderby GetInheritanceDepth(p.DeclaringType), p.Order.GetValueOrDefault()
					select p).ToList();
			}

			private int GetInheritanceDepth(Type type)
			{
				int num = 0;
				while (type != null && type != typeof(object))
				{
					num++;
					type = type.BaseType;
				}
				return num;
			}

			protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
			{
				JsonProperty jsonProperty = base.CreateProperty(member, memberSerialization);
				if (member is FieldInfo { IsPublic: false })
				{
					jsonProperty.Readable = true;
					jsonProperty.Writable = true;
				}
				return jsonProperty;
			}
		}

		public class SavedObjectiveJsonConverter : JsonConverter<SavedObjective>
		{
			[ThreadStatic]
			private static bool isWriting;

			public override bool CanWrite => !isWriting;

			public override SavedObjective ReadJson(JsonReader reader, Type objectType, SavedObjective existingValue, bool hasExisting, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
				{
					return null;
				}
				JObject jObject = JObject.Load(reader);
				SavedObjective savedObjective = SavedObjective.CreateSavedObjective((jObject["Type"] ?? throw new JsonSerializationException("Missing required 'Type' field on SavedObjective JSON object.")).ToObject<ObjectiveType>(serializer), "");
				using JsonReader reader2 = jObject.CreateReader();
				serializer.Populate(reader2, savedObjective);
				return savedObjective;
			}

			public override void WriteJson(JsonWriter writer, SavedObjective value, JsonSerializer serializer)
			{
				if (value == null)
				{
					writer.WriteNull();
					return;
				}
				isWriting = true;
				try
				{
					JObject jObject = JObject.FromObject(value, serializer);
					jObject.AddFirst(new JProperty("Type", JToken.FromObject(value.ObjectiveTypeEnum, serializer)));
					jObject.WriteTo(writer);
				}
				finally
				{
					isWriting = false;
				}
			}
		}

		public class SavedOutcomeJsonConverter : JsonConverter<SavedOutcome>
		{
			[ThreadStatic]
			private static bool isWriting;

			public override bool CanWrite => !isWriting;

			public override SavedOutcome ReadJson(JsonReader reader, Type objectType, SavedOutcome existingValue, bool hasExisting, JsonSerializer serializer)
			{
				if (reader.TokenType == JsonToken.Null)
				{
					return null;
				}
				JObject jObject = JObject.Load(reader);
				SavedOutcome savedOutcome = SavedOutcome.CreateSaved((jObject["Type"] ?? throw new JsonSerializationException("Missing required 'Type' field on SavedOutcome JSON object.")).ToObject<OutcomeType>(serializer), "");
				using JsonReader reader2 = jObject.CreateReader();
				serializer.Populate(reader2, savedOutcome);
				return savedOutcome;
			}

			public override void WriteJson(JsonWriter writer, SavedOutcome value, JsonSerializer serializer)
			{
				if (value == null)
				{
					writer.WriteNull();
					return;
				}
				isWriting = true;
				try
				{
					JObject jObject = JObject.FromObject(value, serializer);
					jObject.AddFirst(new JProperty("Type", JToken.FromObject(value.OutcomeTypeEnum, serializer)));
					jObject.WriteTo(writer);
				}
				finally
				{
					isWriting = false;
				}
			}
		}

		public static readonly JsonSerializerSettings Default = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			TypeNameHandling = TypeNameHandling.Auto,
			Converters = new List<JsonConverter>
			{
				new StringEnumConverter(),
				new SavedObjectiveJsonConverter(),
				new SavedOutcomeJsonConverter()
			},
			ContractResolver = new FieldsOnlyContractResolver(),
			SerializationBinder = new ShortNameSerializationBinder()
		};

		private NewtonsoftHelper()
		{
		}

		public static string ToJson(object obj, bool prettyPrint = false)
		{
			return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None, Default);
		}

		public static T FromJson<T>(string json)
		{
			long timestamp = BenchmarkScope.GetTimestamp();
			T result = JsonConvert.DeserializeObject<T>(json, Default);
			double num = BenchmarkScope.MillisecondsSince(timestamp);
			ColorLog<NewtonsoftHelper>.Info($"FromJson<{typeof(T).Name}> took {num:0.000}ms");
			return result;
		}
	}
}
