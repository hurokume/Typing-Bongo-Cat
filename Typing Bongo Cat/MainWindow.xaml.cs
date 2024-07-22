using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

namespace Typing_Bongo_Cat
{
    public sealed partial class MainWindow : Window
    {
        private int imageToggle = 0;
        private DispatcherTimer timer;
        private DispatcherTimer topmostTimer;
        private const double defaultTimeoutSeconds = 0.01;
        private double timeoutSeconds = defaultTimeoutSeconds;

        // Win32 API declarations
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private static IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr LoadImage(IntPtr hinst, string lpszName, uint uType, int cxDesired, int cyDesired, uint fuLoad);

        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint WM_SETICON = 0x0080;
        const uint IMAGE_ICON = 1;
        const uint LR_LOADFROMFILE = 0x00000010;

        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "Bongo Cat";
            int H = 360, W = 360;
            int X = 1400, Y = 100;
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId myWndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            AppWindow appWindow = AppWindow.GetFromWindowId(myWndId);

            //appWindow.SetPresenter(AppWindowPresenterKind.FullScreen);

            appWindow.Resize(new SizeInt32(W, H));

            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            SetWindowPos(hWnd, HWND_TOPMOST, X, Y, W, H, SWP_SHOWWINDOW | SWP_NOACTIVATE);

            IntPtr hIcon = LoadImage(IntPtr.Zero, "Assets/bongo_icon.ico", IMAGE_ICON, 32, 32, LR_LOADFROMFILE);
            if (hIcon != IntPtr.Zero)
            {
                SendMessage(hWnd, WM_SETICON, (IntPtr)0, hIcon); // Small icon
                SendMessage(hWnd, WM_SETICON, (IntPtr)1, hIcon); // Large icon
                DestroyIcon(hIcon);
            }

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(timeoutSeconds);
            timer.Tick += Timer_Tick;

            topmostTimer = new DispatcherTimer();
            topmostTimer.Interval = TimeSpan.FromSeconds(1);
            topmostTimer.Tick += (sender, e) =>
            {
                SetWindowPos(hWnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOSIZE | SWP_NOMOVE | SWP_SHOWWINDOW);
            };
            topmostTimer.Start();

            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private void MainGrid_Loaded(object sender, RoutedEventArgs e)
        {
            MainGrid.Focus(FocusState.Programmatic);
        }

        private void MainGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Escape)
            {
                HandleKeyPress();
            }
        }

        private void HandleKeyPress()
        {
            timer.Stop();
            timer.Interval = TimeSpan.FromSeconds(timeoutSeconds);
            timer.Start();

            if (imageToggle == 0)
            {
                MainImage1.Visibility = Visibility.Collapsed;
                MainImage2.Visibility = Visibility.Visible;
                MainImage3.Visibility = Visibility.Collapsed;
                imageToggle = 1;
            }
            else if (imageToggle == 1)
            {
                MainImage1.Visibility = Visibility.Collapsed;
                MainImage2.Visibility = Visibility.Collapsed;
                MainImage3.Visibility = Visibility.Visible;
                imageToggle = 0;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            MainImage1.Visibility = Visibility.Visible;
            MainImage2.Visibility = Visibility.Collapsed;
            MainImage3.Visibility = Visibility.Collapsed;

            timer.Stop();
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(13, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)0x100 || wParam == (IntPtr)0x104)) // WM_KEYDOWN and WM_SYSKEYDOWN
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode != 0x1B) // Escape key
                {
                    HandleKeyPress();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        ~MainWindow()
        {
            UnhookWindowsHookEx(_hookID);
        }

        private void MainGrid_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var flyout = new MenuFlyout();

            var item10 = new MenuFlyoutItem { Text = "10 ms" };
            item10.Click += (s, args) => SetTimeoutSeconds(0.01);

            var item20 = new MenuFlyoutItem { Text = "20 ms" };
            item20.Click += (s, args) => SetTimeoutSeconds(0.02);

            var item30 = new MenuFlyoutItem { Text = "30 ms" };
            item30.Click += (s, args) => SetTimeoutSeconds(0.03);

            var item40 = new MenuFlyoutItem { Text = "40 ms" };
            item40.Click += (s, args) => SetTimeoutSeconds(0.04);

            flyout.Items.Add(item10);
            flyout.Items.Add(item20);
            flyout.Items.Add(item30);
            flyout.Items.Add(item40);

            flyout.ShowAt((FrameworkElement)sender, e.GetPosition((UIElement)sender));
        }

        private void SetTimeoutSeconds(double seconds)
        {
            timeoutSeconds = seconds;
        }
    }
}