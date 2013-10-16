using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using MessageBox = System.Windows.MessageBox;

namespace GoogleMusicWrapper
{
    public class GlobalHotkey
    {
        #region fields
        public static int MOD_ALT = 0x1;
        public static int MOD_CONTROL = 0x2;
        public static int MOD_SHIFT = 0x4;
        public static int MOD_WIN = 0x8;
        public const int WM_HOTKEY = 0x312;

        private static readonly Dictionary<int, Action> callbacks = new Dictionary<int, Action>();
        #endregion

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public static void RegisterHotKey(Keys key, Window window, Action _callback)
        {
            var modifiers = 0;

            if ((key & Keys.Alt) == Keys.Alt)
                modifiers = modifiers | MOD_ALT;

            if ((key & Keys.Control) == Keys.Control)
                modifiers = modifiers | MOD_CONTROL;

            if ((key & Keys.Shift) == Keys.Shift)
                modifiers = modifiers | MOD_SHIFT;

            var helper = new WindowInteropHelper(window);
            var source = HwndSource.FromHwnd(helper.Handle);
            Debug.Assert(source != null, "source != null");
            source.AddHook(WndProc);

            var k = key & ~Keys.Control & ~Keys.Shift & ~Keys.Alt;
            var keyId = key.GetHashCode(); // this should be a key unique ID, modify this if you want more than one hotkey
            RegisterHotKey(helper.Handle, keyId, (uint)modifiers, (uint)k);

            callbacks.Add(keyId, _callback);
        }

        private static IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_HOTKEY:

                    var keyId = wParam.ToInt32();
                    if(callbacks.ContainsKey(keyId))
                    {
                        callbacks[keyId]();
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        public static void UnregisterHotKeys(Window window)
        {
            try
            {
                var helper = new WindowInteropHelper(window);
                foreach(var keyId in callbacks.Keys)
                    UnregisterHotKey(helper.Handle, keyId); // modify this if you want more than one hotkey
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
