using Eto.Drawing;
using Eto.Forms;
using Eto.Serialization.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WorleyNoise {
    public class MainForm : Form {
        protected Drawable Canvas;

        private int resolution = 6;
        private readonly int maxFeatures = 15;
        private readonly int featureRadius = 8;
        private Color pixelsColor = Colors.White;

        private double overflow = 0.0;
        private Size surfaceSize;
        private bool isClosing = false;
        private bool hasChanged = false;
        private bool drawFeatures = true;
        private double maxDistance;

        private (Rectangle Point, SolidBrush Color)[] pixels;
        private int pixelsCount;
        private Point3D[] features = Array.Empty<Point3D>();
        private double[] distances;
        private readonly Random rnd = new Random();
        private readonly object syncObj = new object();

        private int fr2;
        private int r2;

        private Point3D selFeature = null;
        private bool isDragging;

        public enum Scales {
            Linear,
            Logarithmic,
            Exponential
        }

        private Scales scale = Scales.Logarithmic;
        private Func<double, double> map = null;
        private double mapMin = 0.0;
        private int z = 0;

        public MainForm() {
            JsonReader.Load(this);

            this.Closing += (_, __) => isClosing = true;
            this.SizeChanged += (_, __) => { lock(syncObj) CreateFeatures(); };

            Canvas.Shown += (_, __) => {
                //this.Location = new Point((int)((this.Screen.WorkingArea.Width - this.Width) / 2),
                //                          (int)((this.Screen.WorkingArea.Height - this.Height) / 2));

                Task.Run(() => {
                    while(!isClosing) {
                        Thread.Sleep(30);
                        if(hasChanged) Application.Instance.Invoke(() => this.Invalidate());

                    }
                });
            };

            Canvas.Paint += RenderPixels;

            Canvas.MouseDown += (_, __) => isDragging = selFeature != null;

            Canvas.MouseMove += (object s, MouseEventArgs e) => {
                if(isDragging) {
                    selFeature.X = (int)e.Location.X / resolution;
                    selFeature.Y = (int)e.Location.Y / resolution;
                    lock(syncObj) CreatePixels();
                } else {
                    Point3D f = GetFeatureAtPoint(e.Location.X, e.Location.Y);
                    if(f != selFeature) {
                        this.Cursor = f == null ? Cursors.Default : Cursors.Pointer;
                        lock(syncObj) {
                            selFeature = f;
                            hasChanged = true;
                        }
                    }
                }
            };

            Canvas.MouseUp += (_, __) => {
                this.Cursor = Cursors.Default;
                isDragging = false;

                lock(syncObj) {
                    selFeature = null;
                    hasChanged = true;
                }
            };
        }

        private Point3D GetFeatureAtPoint(float x, float y) {
            foreach(Point3D f in features)
                if(x >= f.X * resolution + r2 - fr2 &&
                   x <= f.X * resolution + r2 - fr2 + featureRadius &&
                   y >= f.Y * resolution + r2 - fr2 &&
                   y < +f.Y * resolution + r2 - fr2 + featureRadius)
                    return f;

            return null;
        }

        private void CreateFeatures() {
            surfaceSize = new Size(this.ClientSize.Width / resolution,
                                   this.ClientSize.Height / resolution);

            double o = overflow / 2.0;
            double w = surfaceSize.Width * (1.0 + overflow);
            double h = surfaceSize.Height * (1.0 + overflow);

            pixels = new (Rectangle, SolidBrush)[surfaceSize.Width * surfaceSize.Height];

            distances = new double[maxFeatures];
            features = new Point3D[maxFeatures];
            for(int i = 0; i < maxFeatures; i++) {
                features[i] = new Point3D((int)(rnd.NextDouble() * w - (w * o)),
                                          (int)(rnd.NextDouble() * h - (h * o)),
                                          0);
            }

            fr2 = featureRadius / 2;
            r2 = resolution / 2;

            SetMapFunction();
            SetMaxDistance();
            CreatePixels();
        }

        private void SetMapFunction() {
            switch(scale) {
                case Scales.Linear:
                    map = v => (v - mapMin) / maxDistance;
                    mapMin = 0.0;
                    break;
                case Scales.Logarithmic:
                    map = v => v > 0 ? Math.Log10(v - mapMin) / Math.Log10(maxDistance) : mapMin;
                    mapMin = -1.0;
                    break;
                case Scales.Exponential:
                    map = v => Math.Exp(v - mapMin) / Math.Exp(maxDistance);
                    mapMin = 0.0;
                    break;
            }
        }

        private void SetMaxDistance() {
            double f = 1.0;

            switch(scale) {
                case Scales.Linear:
                    f = 2.0;
                    break;
                case Scales.Logarithmic:
                    f = 2.0;
                    break;
                case Scales.Exponential:
                    f = 4.0;
                    break;
            }

            maxDistance = (new Point3D()).DistanceTo(new Point3D(surfaceSize.Width / 2,
                                                                 surfaceSize.Height / 2,
                                                                 z / 2)) / f;
        }

        private void CreatePixels() {
            Point3D p = new Point3D(0, 0, z);
            double alpha;
            double minDistance;

            pixelsCount = 0;
            for(p.Y = 0; p.Y < surfaceSize.Height; p.Y++) {
                for(p.X = 0; p.X < surfaceSize.Width; p.X++) {
                    for(int j = 0; j < maxFeatures; j++)
                        distances[j] = features[j].DistanceTo(p);
                    Array.Sort(distances);
                    minDistance = distances[0]; // n

                    alpha = Math.Min(1.0, Math.Max(0.0, 1.0 - map.Invoke(minDistance)));
                    if(alpha > 0) {
                        pixelsColor.A = (float)alpha;
                        pixels[pixelsCount++] = (new Rectangle(p.X * resolution,
                                                               p.Y * resolution,
                                                               resolution,
                                                               resolution),
                                                               new SolidBrush(pixelsColor));
                    }
                }
            }

            hasChanged = true;

#if DEBUG
            Application.Instance.Invoke(() => this.Title = $"{pixelsCount:N0} ({100.0 * pixelsCount / pixels.Length:N2})%");
#endif
        }

        private void RenderPixels(object s, PaintEventArgs e) {
            Graphics g = e.Graphics;

            g.AntiAlias = false;
            g.PixelOffsetMode = PixelOffsetMode.Half;

            for(int i = 0; i < pixelsCount; i++)
                g.FillRectangle(pixels[i].Color, pixels[i].Point);

            if(drawFeatures)
                for(int i = 0; i < features.Length; i++) {
                    g.FillEllipse(features[i] == selFeature ? Colors.Red : Colors.Blue,
                                    features[i].X * resolution + r2 - fr2,
                                    features[i].Y * resolution + r2 - fr2,
                                    featureRadius, featureRadius);
                }

            hasChanged = false;
        }
    }
}