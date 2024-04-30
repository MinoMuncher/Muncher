using TetrEnvironment.Constants;

namespace Minomuncher;

public static class Extension
{
	public static int[] ToIntArray(this Tetromino.MinoType[] array)
	{
		var result = new int[array.Length];
		for (int i = 0; i < result.Length; i++)
			result[i] = (int)array[i];

		return result;
	}
}