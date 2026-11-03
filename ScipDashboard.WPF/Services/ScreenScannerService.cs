using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScipDashboard.WPF.Services
{
    public sealed class ScreenScanRegion
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public bool IsValid => Width > 0 && Height > 0;
    }

    public sealed class ScreenScannerService
    {
        private const uint SourceCopy = 0x00CC0020;
        private const uint CaptureLayeredWindows = 0x40000000;

        public ScreenScanRegion? GetSavedRegion()
        {
            var region = LocalFeatureStore.Read<ScreenScanRegion>("scanner-region.json");
            return region.IsValid ? region : null;
        }

        public void SaveRegion(ScreenScanRegion region)
        {
            ArgumentNullException.ThrowIfNull(region);
            if (!region.IsValid)
                throw new ArgumentOutOfRangeException(nameof(region), "The scan region must have a positive width and height.");

            LocalFeatureStore.Write("scanner-region.json", region);
        }

        public string CaptureSavedRegionToTemporaryPng()
        {
            var region = GetSavedRegion()
                ?? throw new InvalidOperationException("No screen region is saved. Press F8 or calibrate the scanner region first.");
            return CaptureRegionToTemporaryPng(region);
        }

        private static string CaptureRegionToTemporaryPng(ScreenScanRegion region)
        {
            var screenDc = GetDC(IntPtr.Zero);
            if (screenDc == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not access the desktop for screen capture.");

            var memoryDc = IntPtr.Zero;
            var bitmap = IntPtr.Zero;
            var previousBitmap = IntPtr.Zero;
            var path = Path.Combine(Path.GetTempPath(), $"scip-game-scan-{Guid.NewGuid():N}.png");

            try
            {
                memoryDc = CreateCompatibleDC(screenDc);
                bitmap = CreateCompatibleBitmap(screenDc, region.Width, region.Height);
                if (memoryDc == IntPtr.Zero || bitmap == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not allocate a bitmap for screen capture.");

                previousBitmap = SelectObject(memoryDc, bitmap);
                if (!BitBlt(memoryDc, 0, 0, region.Width, region.Height,
                        screenDc, region.Left, region.Top, SourceCopy | CaptureLayeredWindows))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not capture the saved screen region.");

                var source = Imaging.CreateBitmapSourceFromHBitmap(
                    bitmap, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                source.Freeze();

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                using var output = File.Create(path);
                encoder.Save(output);
                return path;
            }
            catch
            {
                if (File.Exists(path))
                    File.Delete(path);
                throw;
            }
            finally
            {
                if (memoryDc != IntPtr.Zero && previousBitmap != IntPtr.Zero)
                    SelectObject(memoryDc, previousBitmap);
                if (bitmap != IntPtr.Zero)
                    DeleteObject(bitmap);
                if (memoryDc != IntPtr.Zero)
                    DeleteDC(memoryDc);
                ReleaseDC(IntPtr.Zero, screenDc);
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetDC(IntPtr window);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int ReleaseDC(IntPtr window, IntPtr deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC(IntPtr deviceContext);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr deviceContext, int width, int height);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr SelectObject(IntPtr deviceContext, IntPtr graphicsObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool BitBlt(
            IntPtr destinationDc, int destinationX, int destinationY, int width, int height,
            IntPtr sourceDc, int sourceX, int sourceY, uint rasterOperation);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteObject(IntPtr graphicsObject);

        [DllImport("gdi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteDC(IntPtr deviceContext);
    }
}
