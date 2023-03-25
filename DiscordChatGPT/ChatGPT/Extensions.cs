namespace ChatGPTCLI;

/// <summary>
/// アプリケーションで使用する拡張メソッドを定義します。
/// </summary>
public static class Extensions
{
	public static IEnumerable<T> WithAppend<T>(this IEnumerable<T> t, T value)
	{
		foreach (var obj in t)
		{
			yield return obj;
		}
		yield return value;
	}
}
