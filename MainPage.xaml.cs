using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media.Editing;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;


namespace VideoBug
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void ExportScreenshot(object sender, RoutedEventArgs e)
        {
            ExportScreenshotAsync(Colours);
        }

        private void CopyScreenshot(object sender, RoutedEventArgs e)
        {
            CopyScreenshotAsync(Colours);
        }

        private void ExportVideo(object sender, RoutedEventArgs e)
        {
            ExportVideoAsync(Colours);
        }        

        public async void ExportScreenshotAsync(UIElement content)
        {
            FileSavePicker picker = new FileSavePicker()
            {
                SuggestedFileName = "Screenshot",
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save Screenshot"
            };
            picker.FileTypeChoices.Add("PNG Image", new string[] { ".png" });
            var file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                await RenderContentToRasterStreamAsync(content, await file.OpenAsync(FileAccessMode.ReadWrite));
            }
        }

        public static async Task RenderContentToRasterStreamAsync(UIElement content, IRandomAccessStream stream)
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(content);

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();

            var displayInformation = DisplayInformation.GetForCurrentView();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
            encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Premultiplied,
                (uint)renderTargetBitmap.PixelWidth,
                (uint)renderTargetBitmap.PixelHeight,
                displayInformation.RawDpiX,
                displayInformation.RawDpiY,
                pixels);

            await encoder.FlushAsync();
        }

        public static async void CopyScreenshotAsync(UIElement content)
        {
            var stream = new InMemoryRandomAccessStream();
            await RenderContentToRasterStreamAsync(content, stream);

            var dataPackage = new DataPackage();
            dataPackage.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));

            Clipboard.SetContent(dataPackage);
        }

        public async void ExportVideoAsync(UIElement content)
        {
            var picker = new FileSavePicker()
            {
                SuggestedFileName = "Video",
                SuggestedStartLocation = PickerLocationId.Desktop,
                CommitButtonText = "Save Video"
            };
            picker.FileTypeChoices.Add("H.264 video", new string[] { ".mp4" });

            StorageFile file = await picker.PickSaveFileAsync();

            if (file != null)
            {
                await RenderContentToVideo(file, content);
            }
        }

        public async Task RenderContentToVideo(StorageFile file, UIElement content)
        {
            var renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(content);

            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();
            var pixels = pixelBuffer.ToArray();

            var rendertarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), 400, 400, 96);
            rendertarget.SetPixelBytes(pixels, 0, 0, renderTargetBitmap.PixelWidth, renderTargetBitmap.PixelHeight);

            var composition = new MediaComposition();
            composition.Clips.Add(MediaClip.CreateFromSurface(rendertarget, TimeSpan.FromSeconds(0)));
            composition.Clips.Add(MediaClip.CreateFromSurface(rendertarget, TimeSpan.FromSeconds(1)));

            var profile = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD1080p);
            await composition.RenderToFileAsync(file, MediaTrimmingPreference.Fast, profile);
        }


        private void RedrawCanvas(CanvasControl sender, CanvasDrawEventArgs args)
        {
            args.DrawingSession.FillRectangle(0, 0, 200, 50, Colors.Red);
            args.DrawingSession.FillRectangle(0, 50, 200, 50, Colors.Green);
            args.DrawingSession.FillRectangle(0, 100, 200, 50, Colors.Blue);

            var colours = new byte[]
            {
                255, 0, 0, 0,
                0, 255, 0, 0,
                0, 0, 255, 0
            };

            CanvasBitmap bitmap = CanvasBitmap.CreateFromBytes(args.DrawingSession, colours, 1, 3, DirectXPixelFormat.R8G8B8A8UIntNormalized);
            args.DrawingSession.DrawImage(bitmap, new Rect(0, 150, 200, 150));
        }
    }
}
