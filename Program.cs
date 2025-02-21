using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BllngBllng;

public static class Program
{
	public static void Main(string[] args)
	{
		const string WtProcName = "WindowsTerminal";

		var isOn = args.Contains("show", StringComparer.OrdinalIgnoreCase);

		var processes = Process
			.GetProcesses()
			.Where(p => p.ProcessName.Equals(WtProcName, StringComparison.OrdinalIgnoreCase))
			.ToList();

		if (processes.Count == 0)
		{
			Console.WriteLine($"Did not find any processes with name '{WtProcName}', are you sure it's running?");
			return;
		}

		foreach (var process in processes)
		{
			SetIconVisible(process, isOn);
		}
	}

	public static void SetIconVisible(Process process, bool isVisible)
	{
		var handle = process.MainWindowHandle;

		Console.WriteLine($"Setting taskbar visibility icon {(isVisible ? "VISIBLE" : "HIDDEN")} for process with pid '{process.Id}' and window handle '{process.MainWindowHandle}'");

		// Get current window properties
		var props = User32.GetWindowLong(handle, User32.GWLEXSTYLE);

		Console.WriteLine($"Current window props for window with pid '{process.Id}': {props:x8}");

		var newProps = isVisible
			// Show
			? (props | User32.WSEXTOOLWINDOW) & User32.WSEXAPPWINDOW
			// Hide
			: (props | User32.WSEXTOOLWINDOW) & ~User32.WSEXAPPWINDOW;

		Console.WriteLine($"SetWindowLong(hWnd:{handle:x8}, nIndex{User32.GWLEXSTYLE:x8}, dwNewLong{newProps:x8})");

		User32.SetWindowLong(handle, User32.GWLEXSTYLE, newProps);
	}
}

public static class User32
{
	public const int GWLEXSTYLE = -20;
	public const int WSEXAPPWINDOW = 0x00040000;
	public const int WSEXTOOLWINDOW = 0x00000080;

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int GetWindowLong(nint hWnd, int nIndex);

	[DllImport("user32.dll", SetLastError = true)]
	public static extern int SetWindowLong(nint hWnd, int nIndex, int dwNewLong);
}