using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// Il modello di elemento Pagina vuota è documentato all'indirizzo https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x410

namespace Auto_CompactOverlay_UWP
{
    /// <summary>
    /// Pagina vuota che può essere usata autonomamente oppure per l'esplorazione all'interno di un frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public Size SavedSize { get; set; } = new Size
        {
            Width = 400,
            Height = 225
        };

        public bool AutoCompact { get; set; } = true;

        public Size DefaultAspectRatio { get; set; } = new Size
        {
            Width = 3,
            Height = 2
        };

        public MainPage()
        {
            this.InitializeComponent();
            DataContext = this;

            Window.Current.Activated += Current_Activated;
            Window.Current.SizeChanged += Window_SizeChanged;
        }

        #region Window Events
        private async void Current_Activated(object sender, Windows.UI.Core.WindowActivatedEventArgs e)
        {
            if (AutoCompact)
            {
                if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.CodeActivated ||
                    e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.PointerActivated)
                {
                    // It's not a good idea switching automatically in default mode
                    // because if the user try to resize the compact view window it goes
                    // in default mode, so the user can't ever change the size
                    //await ChangeViewModeAsync(ApplicationViewMode.Default);
                    //Debug.WriteLine("Change View Mode -> Default");
                }
                else if (e.WindowActivationState == Windows.UI.Core.CoreWindowActivationState.Deactivated)
                {
                    if (ApplicationView.GetForCurrentView().ViewMode != ApplicationViewMode.CompactOverlay)
                    {
                        await ChangeViewModeAsync(ApplicationViewMode.CompactOverlay);
                        //CompactButton.IsChecked = true;
                        Debug.WriteLine("Change View Mode -> Compact Overlay");
                    }
                }
            }
        }

        private void Window_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.CompactOverlay)
            {
                SavedSize = CalculateNewSize(e.Size, DefaultAspectRatio);
                UpdateText(e.Size.Width, e.Size.Height);
            }
        }
        #endregion

        #region Controls Events
        private async void CompactButton_Click(object sender, RoutedEventArgs e)
        {
            // This is an example with the compact button
            // When compact mode is active it's better to switch out
            // with a double click on the window content (e.g. on the playing video)

            ApplicationViewMode appViewMode;
            if ((sender as ToggleButton).IsChecked ?? false)
            {
                appViewMode = ApplicationViewMode.CompactOverlay;
            }
            else
            {
                appViewMode = ApplicationViewMode.Default;
            }

            await ChangeViewModeAsync(appViewMode);
        }

        private async void Image_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // This is an example with the compact button
            // When compact mode is active it's better to switch out
            // with a double click on the window content (e.g. on the playing video)

            ApplicationViewMode appViewMode;

            if (ApplicationView.GetForCurrentView().ViewMode == ApplicationViewMode.Default)
            {
                appViewMode = ApplicationViewMode.CompactOverlay;
            }
            else
            {
                appViewMode = ApplicationViewMode.Default;
            }

            await ChangeViewModeAsync(appViewMode);
        }

        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateText();
        }
        #endregion

        #region Methods
        private async Task ChangeViewModeAsync(ApplicationViewMode appViewMode)
        {
            var bmp = HeroImage.Source as BitmapImage;
            DefaultAspectRatio = CalculateAspectRatio(bmp.PixelWidth, bmp.PixelHeight);

            ViewModePreferences compactOptions = ViewModePreferences.CreateDefault(ApplicationViewMode.CompactOverlay);

            // Maximum size = 500 x 500
            // Minuimum size = 150 x 181
            // Sometimes not working
            // (when height > width => in portrait aspect ratio, videos have always landscape aspect ratio
            // => not a big problem)
            compactOptions.CustomSize = CalculateNewSize(SavedSize, DefaultAspectRatio);

            bool modeSwitched = await ApplicationView.GetForCurrentView().TryEnterViewModeAsync(appViewMode, compactOptions);
        }

        private void UpdateText()
        {
            double actualWidth = ((Frame)Window.Current.Content).ActualWidth;
            double actualHeight = ((Frame)Window.Current.Content).ActualHeight;

            UpdateText(actualWidth, actualHeight);
        }

        private void UpdateText(double actualWidthDouble, double actualHeightDouble)
        {
            int actualWidth = (int)actualWidthDouble;
            int actualHeight = (int)actualHeightDouble;

            DefaultAspectRatio = CalculateAspectRatio(DefaultAspectRatio);

            //GenericTextBlock.Text = string.Format("Effective: {0} x {1}\r\nCalculated: {2} x {3}\r\nAspect Ratio: {4}:{5}\r\n{6}",
            //    actualWidth,
            //    actualHeight,
            //    SavedSize.Width,
            //    SavedSize.Height,
            //    DefaultAspectRatio.Width,
            //    DefaultAspectRatio.Height,
            //    (actualHeight == SavedSize.Height && actualWidth == SavedSize.Width) ? "Same :D" : "Not Same :(");
        }

        private Size ResizeWindow()
        {
            var actualWidth = ((Frame)Window.Current.Content).ActualWidth;
            var actualHeight = ((Frame)Window.Current.Content).ActualHeight;
            return ResizeWindow(new Size(actualWidth, actualHeight), DefaultAspectRatio);
        }

        private Size ResizeWindow(Size size, Size aspectRatio)
        {
            var newSize = CalculateNewSize(size, aspectRatio);

            Debug.WriteLine($"Size changed! {size.Width}x{size.Height} -> {newSize.Width}x{newSize.Height}");
            bool resized = ApplicationView.GetForCurrentView().TryResizeView(newSize);
            Debug.WriteLine(resized ? "Resized!" : "Not Resized!");

            return newSize;
        }
        #endregion

        #region Some Math
        private Size MaxCompactSize { get; } = new Size
        {
            Width = 500,
            Height = 500
        };

        private Size MinCompactSize { get; } = new Size
        {
            Width = 150,
            Height = 181
        };

        private Size CalculateNewSize(Size actualSize, Size aspectRatio)
        {
            return CalculateNewSize(actualSize.Width, actualSize.Height, aspectRatio);
        }

        private Size CalculateNewSize(double actualWidth, double actualHeight, Size aspectRatio)
        {
            double expHeightDouble = (actualWidth / aspectRatio.Width) * aspectRatio.Height;

            expHeightDouble = expHeightDouble < MinCompactSize.Height ? MinCompactSize.Height : expHeightDouble;
            expHeightDouble = expHeightDouble > MaxCompactSize.Height ? MaxCompactSize.Height : expHeightDouble;

            double expWidthDouble = (expHeightDouble / aspectRatio.Height) * aspectRatio.Width;
            int expHeight = (int)expHeightDouble;
            int expWidth = (int)expWidthDouble;

            return new Size(expWidth, expHeight);
        }

        public Size CalculateAspectRatio(Size size)
        {
            return CalculateAspectRatio((int)size.Width, (int)size.Height);
        }

        public Size CalculateAspectRatio(int width, int height)
        {
            int gcd = GCD(width, height);
            return new Size(width / gcd, height / gcd);
        }

        public int GCD(int a, int b)
        {
            int r;
            while (b != 0)
            {
                r = a % b;
                a = b;
                b = r;
            }
            return a;
        }
        #endregion
    }

    public class SizeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Size sz = (Size)value;
            return $"{sz.Width}:{sz.Height}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string str = (string)value;
            string[] arr = str.Split(':');
            return new Size
            {
                Width = int.Parse(arr[0]),
                Height = int.Parse(arr[1])
            };
        }
    }
}