using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

namespace MakeItSearchable
{

  public class Program
  {
    private const string LeftControlAnd_C_Key = "LControlKeyC";
    private static readonly int WM_KEYBOARD_LL = 13;
    private static readonly int WM_KEYUP = 0x0101;
    private static Keys LastKey;
    private static Keys CurrentKey;
    private static long C_KeyPressTime;
    private static long L_KeyPressTime;
    // WM_KEYDOWN = 0x0100
    // WM_KEYUP = 0x0101
    private static IntPtr _hook = IntPtr.Zero;
    private static readonly LowLevelKeyboardProc _lowLevelKeyboardProc = HookCallback;


    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wPram, IntPtr IParam);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetModuleHandle(string IpModuleName);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc ipfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll")]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [STAThread]
    static void Main(string[] args)
    {
      C_KeyPressTime = L_KeyPressTime = 0;
      _hook = SetHook();
      var a = Control.MousePosition.X;
      var b = Control.MousePosition.Y;
      Application.Run();
      UnhookWindowsHookEx(_hook);
      //var a = Cursor.Position;
    }

    private static IntPtr HookCallback(int nCode, IntPtr wPram, IntPtr IParam)
    {
      if (nCode >= 0 && wPram == (IntPtr)WM_KEYUP)
      {
        int vkCode = Marshal.ReadInt32(IParam);
        C_KeyPressTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
        L_KeyPressTime = C_KeyPressTime;
        LastKey = CurrentKey;
        CurrentKey = (Keys)vkCode;
        string currentKey = CurrentKey.ToString();
        string lastKey = LastKey.ToString();
        string combination = lastKey + currentKey;
        Regex alphanum = new Regex("[a-zA-Z]");
        if (currentKey.Length == 1 &&
            lastKey.Length == 1 &&
            C_KeyPressTime - L_KeyPressTime < 222 &&
            alphanum.IsMatch(currentKey) &&
            alphanum.IsMatch(lastKey))
        {
          Cursor.Position = (Point)new PointConverter().ConvertFromString("0, 0");
        }
        if (combination.Equals(LeftControlAnd_C_Key) && Clipboard.ContainsText(TextDataFormat.Text))
        {
          string clipboardText = Clipboard.GetText(TextDataFormat.Text);
          if (clipboardText.IndexOf("mis ") > -1)
          {
            string searchable = new Regex("[^a-zA-Z0-9]+").Replace(clipboardText, " ");
            searchable = searchable.Replace("mis ", "");
            Clipboard.SetText(searchable);
            Console.Out.WriteLine(searchable);
          }
        }
      }
      return CallNextHookEx(IntPtr.Zero, nCode, wPram, IParam);
    }
    private static IntPtr SetHook()
    {
      string moduleName = Process.GetCurrentProcess().MainModule.ModuleName;
      IntPtr moduleHandle = GetModuleHandle(moduleName);
      return SetWindowsHookEx(WM_KEYBOARD_LL, _lowLevelKeyboardProc, moduleHandle, 0);
    }
  }
}