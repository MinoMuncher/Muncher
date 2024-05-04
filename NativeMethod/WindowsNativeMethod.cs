using System.Runtime.InteropServices;

namespace Minomuncher.NativeMethod;

public class WindowsNativeMethod : INativeMethod
{
	[DllImport("evaluator.dll")]
	private static extern string analyze(IntPtr[] arr, int size);

	string INativeMethod.analyze(IntPtr[] arr, int size)
		=> analyze(arr, size);
}