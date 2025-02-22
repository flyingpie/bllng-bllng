using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;

namespace BllngBllng;

public static class Program
{
	public static void Main(string[] args)
	{
		Console.WriteLine("""
			Usage:
				dotnet run <process name> <show|hide>
			E.g.:
				dotnet run WindowsTerminal show
				dotnet run WindowsTerminal hide
			""");

		if (args.Length < 2)
		{
			return;
		}

		var procName = args[0];
		var isVisible = args[1].Equals("show", StringComparison.OrdinalIgnoreCase);

		var processes = Process
			.GetProcesses()
			.Where(p => p.ProcessName.Equals(procName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (processes.Count == 0)
		{
			Console.WriteLine($"Did not find any processes with name '{procName}', are you sure it's running?");
			return;
		}

		var taskbar = (ITaskbarList)new CTaskbarList();
		taskbar.HrInit();

		foreach (var process in processes)
		{
			var windowInfos = GetWindowInfos(process);

			foreach (var windowInfo in windowInfos)
			{
				if (isVisible)
				{
					taskbar.AddTab(windowInfo.Handle);
				}
				else
				{
					taskbar.DeleteTab(windowInfo.Handle);
				}
			}
		}
	}

	public static IEnumerable<WindowInfo> GetWindowInfos(Process process)
	{
		foreach (var thread in process.Threads.OfType<ProcessThread>())
		{
			var windowHandles = User32.EnumThreadWindowsGet(thread.Id, null).ToList();

			if (windowHandles.Count == 0)
			{
				continue;
			}

			foreach (var windowHandle in windowHandles)
			{
				var windowFlags_style = User32.GetWindowLong(windowHandle, User32.GWL_STYLE);
				var windowText = User32.GetWindowText(windowHandle);

				var isVisible = (windowFlags_style & User32.WS_VISIBLE) == User32.WS_VISIBLE;

				// TODO: Maybe also exclude child windows?
				var isChild = (windowFlags_style & User32.WS_CHILDWINDOW);

				if (!isVisible)
				{
					continue;
				}

				yield return new WindowInfo()
				{
					Handle = windowHandle,
					Caption = windowText,
				};
			}
		}
	}

	public class WindowInfo
	{
		public nint Handle { get; set; }

		public string? Caption { get; set; }
	}
}

public static class User32
{
	public const int GWL_STYLE = -16;

	public const long WS_CHILDWINDOW = 0x40000000L;
	public const long WS_VISIBLE = 0x10000000L;

	public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

	[DllImport("user32.dll")]
	public static extern bool EnumThreadWindows(IntPtr dwThreadId, EnumWindowsProc enumProc, IntPtr lParam);

	public static IEnumerable<IntPtr> EnumThreadWindowsGet(IntPtr dwThreadId, EnumWindowsProc filter)
	{
		var found = IntPtr.Zero;
		var windows = new List<IntPtr>();

		EnumThreadWindows(dwThreadId, delegate (IntPtr wnd, IntPtr param) { windows.Add(wnd); return true; }, IntPtr.Zero);

		return windows;
	}

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	private static extern int GetWindowTextLength(IntPtr hWnd);

	public static string GetWindowText(IntPtr hWnd)
	{
		int size = GetWindowTextLength(hWnd);
		if (size > 0)
		{
			var builder = new StringBuilder(size + 1);
			GetWindowText(hWnd, builder, builder.Capacity);
			return builder.ToString();
		}

		return string.Empty;
	}
}

//
// Summary:
//     Class interface for ITaskbarList and derivatives.
[ComImport]
[Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
[ClassInterface(ClassInterfaceType.None)]
public class CTaskbarList
{
}

[ComImport]
[SuppressUnmanagedCodeSecurity]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
[Guid("56FDF342-FD6D-11d0-958A-006097C9A090")]
[CoClass(typeof(CTaskbarList))]
public interface ITaskbarList
{
	//
	// Summary:
	//     Initializes the taskbar list object. This method must be called before any other
	//     ITaskbarList methods can be called.
	void HrInit();

	//
	// Summary:
	//     Adds an item to the taskbar.
	//
	// Parameters:
	//   hwnd:
	//     A handle to the window to be added to the taskbar.
	void AddTab(IntPtr hwnd);

	//
	// Summary:
	//     Deletes an item from the taskbar.
	//
	// Parameters:
	//   hwnd:
	//     A handle to the window to be deleted from the taskbar.
	void DeleteTab(IntPtr hwnd);
}