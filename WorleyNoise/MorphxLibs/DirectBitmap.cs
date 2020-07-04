#if WINFORMS
using System;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading.Tasks;
#if !ISLIB
using System.Windows.Forms;
#endif

namespace MorphxLibs {
    public class DirectBitmap : IDisposable {
        public readonly Bitmap Bitmap;
        public readonly int Width;
        public readonly int Height;
        public readonly byte[] Bits;
        public readonly Size Size;

        private GCHandle bitsHandle;
        private readonly int w4;
        private readonly int bufferSize;

        //private readonly static ImageConverter imgConverter = new ImageConverter();
        //private readonly static Type imgFormat = typeof(byte[]);

        public DirectBitmap(int w, int h) {
            this.Width = w;
            this.Height = h;
            this.Size = new Size(w, h);

            w4 = w * 4;
            bufferSize = w4 * h;
            Bits = new byte[bufferSize];

            bitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
            this.Bitmap = new Bitmap(w, h, w4, PixelFormat.Format32bppPArgb, bitsHandle.AddrOfPinnedObject());
        }

        public DirectBitmap(Bitmap bmp) : this(bmp.Width, bmp.Height) {
            BitmapData sourceData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadWrite, bmp.PixelFormat);
            IntPtr sourcePointer = sourceData.Scan0;
            int sourceStride = sourceData.Stride;
            int srcBytesPerPixel = sourceStride / bmp.Width;
            int srcOffset;

            int a;
            double pa;

            for(int y = 0; y < bmp.Height; y++) {
                for(int x = 0; x < bmp.Width; x++) {
                    srcOffset = x * srcBytesPerPixel + y * sourceStride;

                    a = (srcBytesPerPixel == 4 ? Marshal.ReadByte(sourcePointer, srcOffset + 3) : 255);
                    pa = a / 255;
                    SetPixel(x, y, Color.FromArgb(a,
                                                 (int)(Marshal.ReadByte(sourcePointer, srcOffset + 2) * pa),
                                                 (int)(Marshal.ReadByte(sourcePointer, srcOffset + 1) * pa),
                                                 (int)(Marshal.ReadByte(sourcePointer, srcOffset + 0) * pa)));
                }
            }

            bmp.UnlockBits(sourceData);
        }

#if WINFORMS && !ISLIB
        public DirectBitmap(Control ctrl) : this(ctrl.DisplayRectangle.Width, ctrl.DisplayRectangle.Height) {
        }
#endif

        public Color GetPixel(int x, int y) {
            if(x < 0 || x >= Width || y < 0 || y >= Height) return Color.Black;
            int offset = y * w4 + x * 4;
            return Color.FromArgb(Bits[offset + 3],
                                  Bits[offset + 2],
                                  Bits[offset + 1],
                                  Bits[offset + 0]);
        }

        public void SetPixel(int x, int y, Color value) {
            if(x < 0 || x >= Width || y < 0 || y >= Height) return;
            SetPixelFast(x, y, value);
        }

        public void SetPixelFast(int x, int y, Color value) {
            int offset = y * w4 + x * 4;
            Bits[offset + 3] = value.A;
            Bits[offset + 2] = value.R;
            Bits[offset + 1] = value.G;
            Bits[offset + 0] = value.B;
        }

        public void SetPixel(int x, int y, int value) {
            if(x < 0 || x >= Width || y < 0 || y >= Height) return;
            int offset = y * w4 + x * 4;
            Bits[offset + 3] = 255;
            Bits[offset + 2] = (byte)(value >> 16);
            Bits[offset + 1] = (byte)((value >> 8) & 255);
            Bits[offset + 0] = (byte)(value & 255);
        }

        public void Dispose() {
            Bitmap.Dispose();
            bitsHandle.Free();
        }
    }

    public static class DirectBitmapExtensions {
        public static void Clear(this DirectBitmap dbmp, Color c) {
            byte[] b = { c.B, c.G, c.R, c.A };
            int bufferSize = dbmp.Height * dbmp.Width * 4;

            int degreeOfParallelism = Environment.ProcessorCount;
            Parallel.For(0, degreeOfParallelism, (int workerId) => {
                int f = bufferSize * workerId / degreeOfParallelism;
                f -= f % 4;
                int t = bufferSize * (workerId + 1) / degreeOfParallelism;
                t -= t % 4;
                for(int i = f; i < t; i += 4) {
                    Array.Copy(b, 0, dbmp.Bits, i, 4);
                }
            });
        }

        public static void DrawLine(this DirectBitmap dbmp, Color c, int x1, int y1, int x2, int y2) {
            int dx = x2 - x1;
            int dy = y2 - y1;
            int l = (int)Math.Sqrt(dx * dx + dy * dy);
            double a = Math.Atan2(dy, dx);
            for(int r = 0; r <= l; r++) {
                dbmp.SetPixel((int)(x1 + r * Math.Cos(-a)), (int)(y1 + r * Math.Sin(a)), c);
            }
        }

        public static void DrawLine(this DirectBitmap dbmp, Color c, Point p1, Point p2) {
            dbmp.DrawLine(c, p1.X, p1.Y, p2.X, p2.Y);
        }

        public static void DrawPolygon(this DirectBitmap dbmp, Color c, Point[] p) {
            int j;
            int l = p.Length;
            for(int i = 0; i < l; i++) {
                j = (i + 1) % l;
                dbmp.DrawLine(c, p[i], p[j]);
            }
        }

        public static void DrawPolygon(this DirectBitmap dbmp, Color c, PointF[] p) {
            Point[] pi = new Point[p.Length];
            for(int i = 0; i < p.Length; i++) {
                pi[i] = new Point((int)p[i].X, (int)p[i].Y);
            }
            dbmp.DrawPolygon(c, pi);
        }

        public static void FillRectangle(this DirectBitmap dbmp, Color c, Rectangle r) {
            FillRectangle(dbmp, c, r.X, r.Y, r.Width, r.Height);
        }

        public static void FillRectangle(this DirectBitmap dbmp, Color c, int x, int y, int w, int h) {
            for(int x1 = x; x1 < x + w; x1++) {
                for(int y1 = y; y1 < y; y1++) {
                    dbmp.SetPixel(x1, y1, c);
                }
            }
        }
    }
}
#endif