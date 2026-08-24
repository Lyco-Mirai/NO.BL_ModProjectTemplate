using System;
using System.Collections.Generic;
using UnityEngine;

namespace NuclearOption.Jobs
{
	public class JobPart<TPart, TField> : IHasIndexInJob, IDisposable where TPart : MonoBehaviour where TField : unmanaged
	{
		public readonly TPart Part;

		public readonly Ptr<TField> Field;

		public readonly List<PtrRefCounter<IndexLink>> links = new List<PtrRefCounter<IndexLink>>();

		public NullableIndex IndexInJob { get; set; }

		public JobPart(TPart part, Ptr<TField> field)
		{
			Part = part;
			Field = field;
		}

		public void Dispose()
		{
			foreach (PtrRefCounter<IndexLink> link in links)
			{
				link.RemoveRef();
			}
			links.Clear();
		}

		public override string ToString()
		{
			if (!(Part != null))
			{
				return "null";
			}
			return $"{Part.GetInstanceID()} {Part}";
		}
	}
}
