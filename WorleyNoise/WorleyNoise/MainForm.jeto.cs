using System;
using System.Collections.Generic;
using Eto.Forms;
using Eto.Drawing;
using Eto.Serialization.Json;
using System.Threading.Tasks;
using System.Threading;

namespace WorleyNoise {
    public class MainForm : Form {
        protected Drawable Canvas;

        private int resolution = 8;
        private readonly int maxFeatures = 20;
        private double overflow = 0.2;
        private Size surfaceSize;
        private bool isClosing = false;
        private bool hasChanged = false;

        private readonly List<(Point Point, Color Color)> pixels = new List<(Point Point, Color Color)>();
        private readonly List<Point3D> features = new List<Point3D>();
        private readonly int featureRadius = 8;
        private readonly Random rnd = new Random();
        private readonly object syncObj = new object();

        private int fr2;
        private int r2;

        private Point3D selFeature = null;

        public enum Scales {
            Linear,
            Logarithmic,
            Exponential
        }

        private Scales scale = Scales.Linear;

        public MainForm() {
            JsonReader.Load(this);

            this.Closing += (_, __) => isClosing = true;
            this.SizeChanged += (_, __) => { lock(syncObj) CreateFeatures(); };

            Canvas.Shown += (_, __) => {
                Task.Run(() => {
                    while(!isClosing) {
                        Thread.Sleep(33);
                        if(hasChanged) Application.Instance.Invoke(() => this.Invalidate());
                    }
                });
            };

            Canvas.Paint += (object s, PaintEventArgs e) => {
                Graphics g = e.Graphics;

                lock(syncObj) {
                    pixels.ForEach((p) => g.FillRectangle(p.Color,
                                                          p.Point.X,
                                                          p.Point.Y,
                                                          resolution, resolution));

                    features.ForEach((f) => g.FillEllipse(Colors.Red,
                                                          f.X * resolution + r2 - fr2,
                                                          f.Y * resolution + r2 - fr2,
                                                          featureRadius, featureRadius));

                    hasChanged = false;
                }
            };

            Canvas.MouseMove += (object s, MouseEventArgs e) => {
                this.Cursor = GetFeatureAtPoint(e.Location.X, e.Location.Y) == null ? Cursors.Default : Cursors.Pointer;
            };

            Canvas.MouseDown += (object s, MouseEventArgs e) => {
                selFeature = GetFeatureAtPoint(e.Location.X, e.Location.Y);
            };

            Canvas.MouseMove += (object s, MouseEventArgs e) => {
                if(selFeature != null) {
                    selFeature.X = (int)e.Location.X / resolution;
                    selFeature.Y = (int)e.Location.Y / resolution;
                    CreatePixels();
                }
            };

            Canvas.MouseUp += (_, __) => {
                this.Cursor = Cursors.Default;
                selFeature = null;
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

            features.Clear();
            for(int i = 0; i < maxFeatures; i++) {
                features.Add(new Point3D((int)(rnd.NextDouble() * w - (w * o)),
                                         (int)(rnd.NextDouble() * h - (h * o)),
                                         0));
            }

            fr2 = featureRadius / 2;
            r2 = resolution / 2;

            CreatePixels();
        }

        private void CreatePixels() {
            double d;
            double a = 0.0;
            double f = 1.0;

            switch(scale) {
                case Scales.Linear:
                    f = 4.0;
                    break;
                case Scales.Logarithmic:
                    f = 40.0;
                    break;
                case Scales.Exponential:
                    f = 8.0;
                    break;
            }

            double maxDistance = (new Point3D()).DistanceTo(new Point3D(surfaceSize.Width / 2,
                                                                        surfaceSize.Height / 2,
                                                                        0)) / f;

            pixels.Clear();
            for(int y = 0; y < surfaceSize.Height; y++) {
                for(int x = 0; x < surfaceSize.Width; x++) {
                    Point3D p = new Point3D(x, y, 0);
                    double minDistance = double.MaxValue;
                    for(int j = 0; j < maxFeatures; j++) {
                        d = features[j].DistanceTo(p);
                        if(d < minDistance) minDistance = d;
                    }

                    switch(scale) {
                        case Scales.Linear:
                            a = MapLinear(minDistance, 0, maxDistance);
                            break;
                        case Scales.Logarithmic:
                            a = MapLog(minDistance, 0, maxDistance);
                            break;
                        case Scales.Exponential:
                            a = MapExp(minDistance, 0, maxDistance);
                            break;
                    }

                    a = Math.Max(0.0, 1.0 - a);
                    if(a > 0) pixels.Add((new Point(x * resolution, y * resolution),
                                         Color.FromArgb(255, 255, 255,
                                         (int)(255.0 * a))));
                }
            }

            hasChanged = true;
        }

        private double MapLinear(double v, double min, double max) {
            return (v - min) / max;
        }

        private double MapLog(double v, double min, double max) {
            return v == 0 ? min : Math.Log10(v) / max;
        }

        private double MapExp(double v, double min, double max) {
            return Math.Exp(v - min) / Math.Exp(max);
        }
    }
}