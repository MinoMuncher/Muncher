using System.Runtime.InteropServices;

namespace Minomuncher.NativeMethod;

public class NativeMethod
{
	private static INativeMethod? _instance = null;

	public static string Analyze(IntPtr[] arr, int size)
	{
		if (_instance == null)
		{
			_instance = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? new WindowsNativeMethod()
				: new LinuxNativeMethod();
		}

		return _instance.analyze(arr, size);
	}
}