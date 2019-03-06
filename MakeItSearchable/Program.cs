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
        private static int WM_KEYBOARD_LL = 13;
        private static int WM_KEYUP = 0x0101;
        private static Keys lastKey;
        private static Keys currentKey;
        private static long cKeyPressTime;
        private static long lKeyPressTime;
        // WM_KEYDOWN = 0x0100
        // WM_KEYUP = 0x0101
        private static IntPtr hook = IntPtr.Zero;
        private static LowLevelKeyboardProc llkProc = HookCallback;

        [STAThread]
        static void Main(string[] args)
        {
            cKeyPressTime = lKeyPressTime = 0;
            hook = SetHook(llkProc);
            var a = Control.MousePosition.X;
            var b = Control.MousePosition.Y;
            a = a;
            Application.Run();
            UnhookWindowsHookEx(hook);
            //var a = Cursor.Position;

        }
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wPram, IntPtr IParam);

        private static IntPtr HookCallback(int nCode, IntPtr wPram, IntPtr IParam)
        {
            if (nCode >= 0 && wPram == (IntPtr)WM_KEYUP)
            {
                int vkCode = Marshal.ReadInt32(IParam);
                cKeyPressTime = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                lKeyPressTime = cKeyPressTime;
                lastKey = currentKey;
                currentKey = (Keys)vkCode;
                string cKey = currentKey.ToString();
                string lKey = lastKey.ToString();
                string combination =  lKey + cKey;
                Regex alphanum = new Regex("[a-zA-Z]");
                if (cKey.Length == 1 &&
                    lKey.Length == 1 &&
                    cKeyPressTime - lKeyPressTime < 222 &&
                    alphanum.IsMatch(cKey) &&
                    alphanum.IsMatch(lKey))
                {
                    Cursor.Position = (Point)(new PointConverter()).ConvertFromString("0, 0");
                }
                if (combination.Equals("LControlKeyC") && Clipboard.ContainsText(TextDataFormat.Text))
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
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            Process currentProcess = Process.GetCurrentProcess();
            ProcessModule currentModule = currentProcess.MainModule;
            string moduleName = currentModule.ModuleName;
            IntPtr moduleHandle = GetModuleHandle(moduleName);
            return SetWindowsHookEx(WM_KEYBOARD_LL, llkProc, moduleHandle, 0);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string IpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc ipfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
    }
}