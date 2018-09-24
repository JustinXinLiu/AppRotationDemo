using System;
using System.Numerics;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Microsoft.Toolkit.Uwp.UI.Animations.Expressions;
using Microsoft.Toolkit.Uwp.UI.Extensions;
using EF = Microsoft.Toolkit.Uwp.UI.Animations.Expressions.ExpressionFunctions;

namespace AppRotationDemo
{
    /// <summary>
    /// Due to the simplicity of the demo, please make sure you turn your device clockwise to the portrait mode,
    /// lock the orientation and then start the app. Then turn the device anti-clockwise to see the effect.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Accelerometer _accelerometer;
        private uint _desiredReportInterval;

        private CompositionPropertySet _reading;
        private double _progress;

        public MainPage()
        {
            InitializeComponent();

            var compositor = Window.Current.Compositor;

            SetBackgroundImageSize();

            void SetBackgroundImageSize()
            {
                var view = DisplayInformation.GetForCurrentView();

                // Get the screen resolution (APIs available from 14393 onward).
                var resolution = new Size(view.ScreenWidthInRawPixels, view.ScreenHeightInRawPixels);

                // Calculate the screen size in effective pixels. 
                // Note the height of the Windows Taskbar is ignored here since the app will only be given the maximum available size.
                var scale = view.ResolutionScale == ResolutionScale.Invalid ? 1 : view.RawPixelsPerViewPixel;
                var bounds = new Size(resolution.Width / scale, resolution.Height / scale);

                BackgroundImage.Width = BackgroundImage.Height = Math.Max(bounds.Width, bounds.Height) * Math.Sqrt(2);
                var topMargin = (bounds.Height - BackgroundImage.Height) / 2;
                var leftMargin = (bounds.Width - BackgroundImage.Width) / 2;
                ImageHost.Margin = new Thickness(leftMargin, topMargin, 0, 0);
            }

            Loaded += (sender, args) =>
            {
                SetupAccelerometer();

                void SetupAccelerometer()
                {
                    _accelerometer = Accelerometer.GetDefault();

                    if (_accelerometer != null)
                    {
                        // Select a report interval that is both suitable for the purposes of the app and supported by the sensor.
                        // This value will be used later to activate the sensor.
                        var minReportInterval = _accelerometer.MinimumReportInterval;
                        _desiredReportInterval = minReportInterval > 16 ? minReportInterval : 16;
                        // Establish the report interval.
                        _accelerometer.ReportInterval = _desiredReportInterval;

                        // Whenever the app is not present...
                        Window.Current.VisibilityChanged += (s, e) =>
                        {
                            if (e.Visible)
                            {
                                RunAccelerometer();
                            }
                            else
                            {
                                StopAccelerometer();
                            }
                        };
                    }
                }

                _reading = compositor.CreatePropertySet();
                _reading.InsertScalar("Progress", 0);

                if (_accelerometer != null)
                {
                    RunRotationExpressionAnimation();
                }

                void RunRotationExpressionAnimation()
                {
                    var progressExpressionNode = _reading.GetReference().GetScalarProperty("Progress");

                    var imageVisual = VisualExtensions.GetVisual(BackgroundImage);
                    imageVisual.CenterPoint = new Vector3((float)BackgroundImage.Width / 2, (float)BackgroundImage.Height / 2, 0);

                    // Maintain the rotation of the Image visual.
                    imageVisual.StartAnimation(nameof(Visual.RotationAngleInDegrees), progressExpressionNode * 90);

                    var titleVisual = VisualExtensions.GetVisual(Title);
                    titleVisual.CenterPoint = new Vector3(Title.RenderSize.ToVector2() / 2, 0);

                    // Maintain the rotation of the Title visual.
                    titleVisual.StartAnimation(nameof(Visual.RotationAngleInDegrees), progressExpressionNode * 90);

                    // Also move the Title visual during the rotation.
                    var centeredOffset = new Vector3((float)(BackgroundImage.Width - Title.ActualWidth) / 2, (float)(BackgroundImage.Height - Title.ActualHeight) / 2, 0.0f);
                    var leftOffset = new Vector3((float)BackgroundImage.Width / 3, (float)BackgroundImage.Height / 3, 0.0f);
                    titleVisual.StartAnimation(nameof(Visual.Offset), EF.Lerp(leftOffset, centeredOffset, EF.Abs(progressExpressionNode)));
                }
            };
        }

        /// <summary>
        /// Restore the default report interval.
        /// </summary>
        private void RunAccelerometer()
        {
            _accelerometer.ReportInterval = _desiredReportInterval;
            _accelerometer.ReadingChanged += OnAccelerometerReadingChanged;
        }

        /// <summary>
        /// Reset the default report interval to release resources while the sensor is not in use.
        /// </summary>
        private void StopAccelerometer()
        {
            _accelerometer.ReportInterval = 0;
            _accelerometer.ReadingChanged -= OnAccelerometerReadingChanged;
        }


        private async void OnAccelerometerReadingChanged(Accelerometer sender, AccelerometerReadingChangedEventArgs e)
        {
            var progress = 1 - Math.Round(e.Reading.AccelerationX, 2);

            if (Math.Abs(_progress - progress) > 1 / 180.0)
            {
                _progress = _progress.Lerp(progress, 0.1);
            }

            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => _reading.InsertScalar("Progress", (float)_progress));
        }
    }

    public static class Extensions
    {
        public static double Lerp(this double value1, double value2, double by)
        {
            return value1 * (1 - by) + value2 * by;
        }
    }
}