using UnityEngine;

public class StringOptionsAttribute : PropertyAttribute
{
	public string[] Options { get; private set; }

	public StringOptionsAttribute(params string[] options)
	{
		Options = options;
	}
}
