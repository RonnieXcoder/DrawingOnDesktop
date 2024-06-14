using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Raylib_cs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Display;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics;
using Windows.System;
using Windows.UI.ViewManagement;


// If you enjoy this project, you can support it by making a donation!
// Donation link: https://buymeacoffee.com/_ronniexcoder

namespace DrawingOnDesktop
{

    public sealed partial class MainWindow : Window
    {

        private int screenWidth = 1920;
        private int screenHeight = 1080;
        private bool isWindowClosed = false;
        private int initialPointerX = 0, initialPointerY = 0, windowStartX = 0, windowStartY = 0;
        private bool isMoving = false;
        private SolidColorBrush currentBrush = new SolidColorBrush(Colors.Black);
        public List<int> BrushThickness { get; } = new List<int>()
        {
            4,
            8,
            18,
            20,
            31,
            42,
            54,
            66,
            78,
            80,
            94,
            108,
            116,
            148,
            162
        };

        public enum CurrentColor
        {
            Black,
            Yellow,
            Red,
            Blue,
            Green,
            Transparent
        }


        CurrentColor currentColor = CurrentColor.Black;

        [DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool GetCursorPos(out Windows.Graphics.PointInt32 lpPoint);

        private RenderTexture2D target;
        AppWindow appW;
        public MainWindow()
        {
            this.InitializeComponent();
            GetMonitorSize();

            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId WndID = Win32Interop.GetWindowIdFromWindow(hwnd);
            appW = AppWindow.GetFromWindowId(WndID);
            this.Activated += MainWindow_Activated;
            this.VisibilityChanged += MainWindow_VisibilityChanged;
            MainGrid.PointerPressed += MainGrid_PointerPressed;
            MainGrid.PointerReleased += MainGrid_PointerReleased;
            MainGrid.PointerMoved += MainGrid_PointerMoved;
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState == WindowActivationState.Deactivated) return;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WindowId WndID = Win32Interop.GetWindowIdFromWindow(hwnd);
            AppWindow appW = AppWindow.GetFromWindowId(WndID);
            appW.Resize(new Windows.Graphics.SizeInt32 { Width = 120, Height = 480 });
            OverlappedPresenter presenter = appW.Presenter as OverlappedPresenter;
            presenter.IsAlwaysOnTop = true;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.SetBorderAndTitleBar(false, false);

        }

        private async void GetMonitorSize()
        {
            var displayList = await DeviceInformation.FindAllAsync(DisplayMonitor.GetDeviceSelector());

            if (!displayList.Any())
                return;

            var monitorInfo = await DisplayMonitor.FromInterfaceIdAsync(displayList[0].Id);
            screenHeight = monitorInfo.NativeResolutionInRawPixels.Height;
            screenWidth = monitorInfo.NativeResolutionInRawPixels.Width;
        }

        private void StartDrawingLoop()
        {
            Raylib.SetConfigFlags(ConfigFlags.TransparentWindow);
            Raylib.InitWindow(screenWidth, screenHeight, "Title");
            Raylib.SetWindowState(ConfigFlags.UndecoratedWindow);
            target = Raylib.LoadRenderTexture(screenWidth, screenHeight);
            Raylib.SetTargetFPS(120);
            Vector2 previousMousePosition = new Vector2(-1, -1);
            Color color = Color.Black;

            while (!isWindowClosed)
            {

                if (Raylib.IsMouseButtonPressed(MouseButton.Left))
                {
                    previousMousePosition = Raylib.GetMousePosition();


                    switch (currentColor)
                    {
                        case CurrentColor.Black:
                            color = Color.Black;
                            break;
                        case CurrentColor.Red:
                            color = Color.Red;
                            break;
                        case CurrentColor.Yellow:
                            color = Color.Yellow;
                            break;
                        case CurrentColor.Blue:
                            color = Color.Blue;
                            break;
                        case CurrentColor.Transparent:
                            color = Raylib.ColorAlpha(Color.White, 0.1f);
                            break;
                    }



                }
                else if (Raylib.IsMouseButtonDown(MouseButton.Left) && (eraserButton.IsChecked == true ||
                    pencilButton.IsChecked == true))
                {
                    Vector2 currentMousePosition = Raylib.GetMousePosition();
                    Raylib.BeginTextureMode(target);

                    Raylib.DrawCircle((int)previousMousePosition.X, (int)currentMousePosition.Y,
                        BrushThickness[CoboBoxBrushThickness.SelectedIndex], color);

                    Raylib.EndTextureMode();

                    previousMousePosition = currentMousePosition;
                }
                else if (Raylib.IsMouseButtonDown(MouseButton.Left) && shapesButton.IsChecked == true)
                {
                    Raylib.BeginTextureMode(target);

                    if (CoboBoxShapes.SelectedIndex == 0)
                        Raylib.DrawCircle((int)previousMousePosition.X, (int)previousMousePosition.Y,
                            BrushThickness[CoboBoxBrushThickness.SelectedIndex], color);
                    else
                        Raylib.DrawRectangle((int)previousMousePosition.X, (int)previousMousePosition.Y,
                            BrushThickness[CoboBoxBrushThickness.SelectedIndex],
                            BrushThickness[CoboBoxBrushThickness.SelectedIndex], color);

                    Raylib.EndTextureMode();
                }

                Raylib.BeginDrawing();
                Raylib.ClearBackground(new Color(0, 0, 0, 0));
                Raylib.DrawTexturePro(target.Texture,
                    new Rectangle(0, 0, target.Texture.Width,
                    -target.Texture.Height),
                    new Rectangle(0, 0, screenWidth, screenHeight),
                    new Vector2(0, 0), 0, Color.White);
                Raylib.EndDrawing();

            }

            Raylib.UnloadRenderTexture(target);
            Raylib.CloseWindow();
            this.Close();

        }

        private void MainWindow_VisibilityChanged(object sender, WindowVisibilityChangedEventArgs args)
        {
            StartDrawingLoop();
        }

        private void PencilButton_Click(object sender, RoutedEventArgs e)
        {
            if (pencilButton.IsChecked == true)
            {
                eraserButton.IsChecked = false;
                shapesButton.IsChecked = false;
            }

        }

        private void EraserButton_Click(object sender, RoutedEventArgs e)
        {
            currentColor = CurrentColor.Transparent;

            if (eraserButton.IsChecked == true)
            {
                pencilButton.IsChecked = false;
                shapesButton.IsChecked = false;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            Raylib.BeginTextureMode(target);
            Raylib.ClearBackground(new Color(0, 0, 0, 0));
            Raylib.EndTextureMode();
        }

        private void StackPanel_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            StackPanelColor.Background = ((StackPanel)sender).Background;
            switch (((StackPanel)sender).Name)
            {
                case "BlackStackPanel":
                    currentColor = CurrentColor.Black;
                    break;
                case "RedStackPanel":
                    currentColor = CurrentColor.Red;
                    break;
                case "YellowStackPanel":
                    currentColor = CurrentColor.Yellow;
                    break;
                case "BlueStackPanel":
                    currentColor = CurrentColor.Blue;
                    break;
            }
        }

        private void ShapesButton_Click(object sender, RoutedEventArgs e)
        {

            if (shapesButton.IsChecked == true)
            {
                pencilButton.IsChecked = false;
                eraserButton.IsChecked = false;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            isWindowClosed = true;

        }

        private void MainGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {

            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed)
            {

                Windows.Graphics.PointInt32 pt;
                GetCursorPos(out pt);

                if (isMoving)
                    appW.Move(new Windows.Graphics.PointInt32(windowStartX +
                        (pt.X - initialPointerX), windowStartY + (pt.Y - initialPointerY)));


            }
        }
        private void MainGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            (sender as UIElement).ReleasePointerCapture(e.Pointer);
            isMoving = false;
        }

        private void MainGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            ((UIElement)sender).CapturePointer(e.Pointer);
            var properties = e.GetCurrentPoint((UIElement)sender).Properties;
            if (properties.IsLeftButtonPressed)
            {
                ((UIElement)sender).CapturePointer(e.Pointer);
                windowStartX = appW.Position.X;
                windowStartY = appW.Position.Y;
                Windows.Graphics.PointInt32 pt;
                GetCursorPos(out pt);
                initialPointerX = pt.X;
                initialPointerY = pt.Y;

                isMoving = true;
            }
        }

        // If you enjoy this project, you can support it by making a donation!
        // Donation link: https://buymeacoffee.com/_ronniexcoder

        
    }
}
