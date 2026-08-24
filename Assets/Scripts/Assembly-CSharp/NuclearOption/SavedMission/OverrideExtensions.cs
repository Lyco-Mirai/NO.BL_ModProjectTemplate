using System;
using Mirage.Serialization;

namespace NuclearOption.SavedMission
{
	public static class OverrideExtensions
	{
		public static T? AsNullable<T>(this Override<T> @override) where T : struct, IEquatable<T>
		{
			if (!@override.IsOverride)
			{
				return null;
			}
			return @override.Value;
		}

		public static void IfOverride<T>(this ValueWrapperOverride<T> wrapper, Action<T> callback) where T : IEquatable<T>
		{
			wrapper.Value.IfOverride(callback);
		}

		public static void IfOverride<T>(this Override<T> @override, Action<T> callback) where T : IEquatable<T>
		{
			if (@override.IsOverride)
			{
				callback(@override.Value);
			}
		}

		[WeaverSerializeCollection]
		public static void WriteOverride<T>(this NetworkWriter writer, Override<T> data) where T : IEquatable<T>
		{
			writer.WriteBoolean(data.IsOverride);
			if (data.IsOverride)
			{
				writer.Write(data.Value);
			}
		}

		[WeaverSerializeCollection]
		public static Override<T> ReadOverride<T>(this NetworkReader reader) where T : IEquatable<T>
		{
			bool num = reader.ReadBoolean();
			T value = default(T);
			if (num)
			{
				value = reader.Read<T>();
			}
			return new Override<T>(num, value);
		}
	}
}
