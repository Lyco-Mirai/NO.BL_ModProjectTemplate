namespace NuclearOption.MissionEditorScripts.MultiSelect
{
	public static class MultiSelectHelper
	{
		public static MultiSelect<TObject>.GetField<T> Cast<TObject, T>(this MultiSelect<TObject>.GetFieldRef<T> getFieldRef)
		{
			return (TObject x) => getFieldRef(x);
		}
	}
}
