using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PopArt.Resources;

// Directives
using Microsoft.Devices;
using System.IO;
using System.IO.IsolatedStorage;
using Microsoft.Xna.Framework.Media;
using System.Windows.Media;

using System.Windows.Media.Imaging;
using Nokiadeveloper;

using Nokia.Graphics;
using Nokia.Graphics.Imaging;
using Nokia.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.WindowsRuntime;

using NativeFilters;

namespace PopArt
{
    public partial class MainPage : PhoneApplicationPage
    {
        PhotoCamera cam;
        MediaLibrary library = new MediaLibrary();
        bool PopArtCartoon = true;

        // Constructor
        public MainPage()
        {
            InitializeComponent();           
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {

            // Check to see if the camera is available on the phone.
            if ((PhotoCamera.IsCameraTypeSupported(CameraType.Primary) == true) ||
                 (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing) == true))
            {
                // Initialize the camera, when available.
                if (PhotoCamera.IsCameraTypeSupported(CameraType.FrontFacing))
                {
                    // Use front-facing camera if available.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.FrontFacing);
                }
                else
                {
                    // Otherwise, use standard camera on back of phone.
                    cam = new Microsoft.Devices.PhotoCamera(CameraType.Primary);
                }

                // Event is fired when the PhotoCamera object has been initialized.
                cam.Initialized += new EventHandler<Microsoft.Devices.CameraOperationCompletedEventArgs>(cam_Initialized);

                // Event is fired when the capture sequence is complete.
                cam.CaptureCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_CaptureCompleted);

                // Event is fired when the capture sequence is complete and an image is available.
                cam.CaptureImageAvailable += new EventHandler<Microsoft.Devices.ContentReadyEventArgs>(cam_CaptureImageAvailable);

                // Event is fired when the capture sequence is complete and a thumbnail image is available.
                cam.CaptureThumbnailAvailable += new EventHandler<ContentReadyEventArgs>(cam_CaptureThumbnailAvailable);

                // The event is fired when autofocus is complete.
                cam.AutoFocusCompleted += new EventHandler<CameraOperationCompletedEventArgs>(cam_AutoFocusCompleted);

                //Set the VideoBrush source to the camera.
                viewfinderBrush.SetSource(cam);
            }
            else
            {
                // The camera is not supported on the phone.
                System.Diagnostics.Debug.WriteLine("A Camera is not available on this phone.");                
                
                // Disable UI.
                ShutterButton.IsEnabled = false;
            }
        }
        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e)
        {
            if (cam != null)
            {
                // Dispose camera to minimize power consumption and to expedite shutdown.
                cam.Dispose();

                // Release memory, ensure garbage collection.
                cam.Initialized -= cam_Initialized;
                cam.CaptureCompleted -= cam_CaptureCompleted;
                cam.CaptureImageAvailable -= cam_CaptureImageAvailable;
                cam.CaptureThumbnailAvailable -= cam_CaptureThumbnailAvailable;
                cam.AutoFocusCompleted -= cam_AutoFocusCompleted;
            }
        }

        async void cam_CaptureImageAvailable(object sender, Microsoft.Devices.ContentReadyEventArgs e)
        {
            string dateformat = Convert.ToString(DateTime.Now.Year) + DateTime.Now.Month.ToString("d2") + DateTime.Now.Day.ToString("d2") + "_" + DateTime.Now.Hour.ToString("d2") + "_" + DateTime.Now.Minute.ToString("d2") + "_" + DateTime.Now.Second.ToString("d2");
            string fileName = "WP_" + dateformat + "_POPART.jpg";

            try
            {
                var halftoneNative = new NativeFilters.Halftone();
                halftoneNative.CellSize = 20;
                using (var halftoneEffect = new DelegatingEffect(new StreamImageSource(e.ImageStream), halftoneNative))
                
                //using (var halftoneEffect = new HalftoneEffect(new StreamImageSource(e.ImageStream)))
                using (var filterEffect = new FilterEffect(halftoneEffect))
                using (var wbRender = new JpegRenderer(filterEffect))
                {
                    if (PopArtCartoon == true)
                    {                        
                        filterEffect.Filters = new[] {  new CartoonFilter(true) };
                    }
                                                            
                    var result = await wbRender.RenderAsync();
                                        
                    System.Diagnostics.Debug.WriteLine("Captured image available, saving photo.");

                    // Save photo to the media library camera roll.
                    library.SavePictureToCameraRoll(fileName, result.AsStream());                                        
                    System.Diagnostics.Debug.WriteLine("Photo has been saved to camera roll.");
                }
            }
            finally
            {
                // Close image stream
                e.ImageStream.Close();
            }

        }

        // Informs when thumbnail photo has been taken, saves to the local folder
        // User will select this image in the Photos Hub to bring up the full-resolution. 
        public async void cam_CaptureThumbnailAvailable(object sender, ContentReadyEventArgs e)
        {

            using (var halftoneEffect = new HalftoneEffect(new StreamImageSource(e.ImageStream)))
            using (var filterEffect = new FilterEffect(halftoneEffect))
            using (var wbRender = new JpegRenderer(filterEffect))
            {
                if (PopArtCartoon == true)
                {
                    filterEffect.Filters = new[] { new CartoonFilter(true) };
                }

                halftoneEffect.CellSize = 10;
                //wbRender.Source = halftoneEffect;
                var result = await wbRender.RenderAsync();

                this.Dispatcher.BeginInvoke(delegate()
                {                    
                    var imageSource = new BitmapImage();
                    imageSource.SetSource(result.AsStream());
                
                    this.PreviewImage.Source = imageSource;
                    imageSource = null;
                    GC.Collect(); GC.WaitForPendingFinalizers();
                });
            }            
        }

        void cam_Initialized(object sender, Microsoft.Devices.CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
            {                
                System.Diagnostics.Debug.WriteLine("Camera Initialized");
            }
        }

        private void ShutterButton_Click(object sender, RoutedEventArgs e)
        {
            if (cam != null)
            {
                if (cam.IsFocusSupported == true)
                {
                    //Focus when a capture is not in progress.
                    try
                    {
                        cam.Focus();
                    }
                    catch (Exception focusError)
                    {
                        // Cannot focus when a capture is in progress.
                        System.Diagnostics.Debug.WriteLine(focusError.Message);
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Camera does not support programmable autofocus.");
                }

                try
                {
                    // Start image capture.
                    cam.CaptureImage();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.Message);                    
                }
            }
        }

        void cam_CaptureCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            
        }

        void cam_AutoFocusCompleted(object sender, CameraOperationCompletedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Autofocus has completed.");
        }

        protected override void OnOrientationChanged(OrientationChangedEventArgs e)
        {
            if (cam != null)
            {
                // LandscapeRight rotation when camera is on back of phone.
                int landscapeRightRotation = 180;

                // Change LandscapeRight rotation for front-facing camera.
                if (cam.CameraType == CameraType.FrontFacing) landscapeRightRotation = -180;

                // Rotate video brush from camera.
                if (e.Orientation == PageOrientation.LandscapeRight)
                {
                    // Rotate for LandscapeRight orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = landscapeRightRotation };
                }
                else
                {
                    // Rotate for standard landscape orientation.
                    viewfinderBrush.RelativeTransform =
                        new CompositeTransform() { CenterX = 0.5, CenterY = 0.5, Rotation = 0 };
                }
            }

            base.OnOrientationChanged(e);
        }

        private void PopArtEffect_Checked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PopArtEffect_Checked");
            PopArtCartoon = true;
        }

        private void PopArtEffect_Unchecked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("PopArtEffect_Unchecked");
            PopArtCartoon = false;
        }


    }
}