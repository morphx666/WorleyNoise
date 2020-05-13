using System;
#if WINFORMS
using System.Drawing;
#else
using Eto.Drawing;
#endif

namespace MorphxLibs {
    public struct PointD {
        public static readonly PointD Empty = new PointD(0, 0);
        public double X;
        public double Y;

        public PointD(double x, double y) {
            X = x;
            Y = y;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }

        public override bool Equals(object obj) {
            return this == (PointD)obj;
        }

        public static bool operator ==(PointD p1, PointD p2) {
            return (p1.X == p2.X) && (p1.Y == p2.Y);
        }

        public static bool operator !=(PointD p1, PointD p2) {
            return !(p1 == p2);
        }

        public static implicit operator PointD(Point p) => new PointD(p.X, p.Y);
        public static implicit operator PointD(PointF p) => new PointD(p.X, p.Y);
        public static implicit operator Point(PointD p) => new Point((int)p.X, (int)p.Y);
        public static implicit operator PointF(PointD p) => new PointF((float)p.X, (float)p.Y);
    }

    public class Vector {
        private double mMagnitude;
        private double mAngle;
        private PointD mOrigin;
        private double angleCos = 1.0;
        private double angleSin = 0.0;
#if WINFORMS
        private Color mColor = Color.White;
#else
        private Color mColor = Colors.White;
#endif

        private readonly int hc = Guid.NewGuid().GetHashCode();

        public delegate void ChangedEventHandler(object sender, EventArgs e);
        public event ChangedEventHandler Changed;

        public object Tag { get; set; }

        public Vector() {
            mAngle = 0.0;
            mMagnitude = 1.0;
            mOrigin = new PointD(0, 0);
        }

        public Vector(double x, double y) {
            Reset(0, 0, x, y);
        }

        public Vector(double magnitude, double angle, PointD origin) {
            mMagnitude = magnitude;
            Angle = angle;
            mOrigin = origin;
        }

        public Vector(PointD origin, PointD destination) {
            Reset(origin.X, origin.Y, destination.X, destination.Y);
        }

        public Vector(PointD origin, PointD destination, Color color) : this(origin, destination) {
            mColor = color;
        }

        public Vector(Vector vector) : this(vector.Magnitude, vector.Angle, vector.Origin) {
            mColor = vector.Color;
        }

        public Vector(double magnitude, double angle, double x, double y) : this(magnitude, angle, new PointD(x, y)) {
        }

        public double Magnitude {
            get { return mMagnitude; }
            set { mMagnitude = value; }
        }

        public double Angle {
            get { return mAngle; }
            set {
                if(value != mAngle) {
                    if(value < 0) value += Constants.PI360;
                    mAngle = value % Constants.PI360;

                    angleCos = Math.Cos(mAngle);
                    angleSin = Math.Sin(mAngle);
                }
            }
        }

        public double AngleCos { get { return angleCos; } }
        public double AngleSin { get { return angleSin; } }

        public PointD Origin {
            get { return mOrigin; }
            set {
                if(mOrigin != value) {
                    mOrigin = value;
                    OnChanged();
                }
            }
        }

        public PointD Destination {
            get { return new PointD(X2, Y2); }
            set { Reset(mOrigin.X, mOrigin.Y, value.X, value.Y); }
        }

        public double X1 {
            get { return mOrigin.X; }
            set { Reset(value, mOrigin.Y, X2, Y2); }
        }

        public double Y1 {
            get { return mOrigin.Y; }
            set { Reset(mOrigin.X, value, X2, Y2); }
        }

        public double X2 {
            get { return mOrigin.X + mMagnitude * angleCos; }
            set { Reset(mOrigin.X, mOrigin.Y, value, Y2); }
        }

        public double Y2 {
            get { return mOrigin.Y + mMagnitude * angleSin; }
            set { Reset(mOrigin.X, mOrigin.Y, X2, value); }
        }

        public double Slope {
            get { return (Y2 - Y1) / (X2 - X1); }
        }

        public Color Color {
            get { return mColor; }
            set { mColor = value; }
        }

        public void Move(double offset) {
            Vector ov = new Vector(offset, mAngle, mOrigin);
            Origin = ov.Destination;
        }

        public void Move(Vector v) {
            double a = mAngle;
            Angle = v.Angle;
            this.Move(v.Magnitude);
            Angle = a;
        }

        public void Translate(double x, double y) {
            PointD dp = Destination;
            mOrigin = new PointD(mOrigin.X + x, mOrigin.Y + y);
            Destination = new PointD(dp.X + x, dp.Y + y);
        }

        public void TranslateAbs(double x, double y) {
            double dx = x - mOrigin.X;
            double dy = y - mOrigin.Y;
            PointD dp = Destination;

            mOrigin = new PointD(x, y);
            Destination = new PointD(dp.X + dx, dp.Y + dy);
        }

        public void TranslateAbs(Vector v) {
            this.TranslateAbs(v.X1, v.Y1);
        }

        public void TranslateAbs(PointD p) {
            this.TranslateAbs(p.X, p.Y);
        }

        public void Transform(double angle, PointD p) {
            double dx = X1 - p.X;
            double dy = Y1 - p.Y;
            double d = Vector.Distance(dx, dy);
            double a = Math.Atan2(dy, dx) + angle;
            double xp1 = p.X + d * Math.Cos(a);
            double yp1 = p.Y + d * Math.Sin(a);
            dx = X2 - p.X;
            dy = Y2 - p.Y;
            d = Vector.Distance(dx, dy);
            a = Math.Atan2(dy, dx) + angle;
            Reset(xp1, yp1, p.X + d * Math.Cos(a), p.Y + d * Math.Sin(a));
        }

        public PointD? Intersects(Vector v) {
            double d = (X1 - X2) * (v.Y1 - v.Y2) - (Y1 - Y2) * (v.X1 - v.X2);
            double t = ((X1 - v.X1) * (v.Y1 - v.Y2) - (Y1 - v.Y2) * (v.X1 - v.X2)) / d;
            double u = -((X1 - X2) * (Y1 - v.Y1) - (Y1 - Y2) * (X1 - v.X1)) / d;

            if((t >= 0.0) && (t <= 1.0) && (u >= 0)) {
                PointD p = new PointD(X1 + t * (X2 - X1), Y1 + t * (Y2 - Y1));
                return p;
            } else {
                return null;
            }
        }

        public bool Intersects(RectangleF r) {
            return (X1 >= r.Left) && (X1 <= r.Right) && (Y1 >= r.Top) && (Y1 <= r.Bottom);
        }

        public void Reset(double px1, double py1, double px2, double py2) {
            Vector v = Vector.FromPoints(px1, py1, px2, py2);
            mMagnitude = v.Magnitude;
            mOrigin = v.Origin;
            Angle = v.Angle;
        }

        public void Reset(Vector v) {
            mMagnitude = v.Magnitude;
            mOrigin = v.Origin;
            Angle = v.Angle;
        }

        public static Vector FromPoints(double px1, double py1, double px2, double py2) {
            Vector v = new Vector();
            double dx = px2 - px1;
            double dy = py2 - py1;

            v.Angle = Math.Atan2(dy, dx);
            v.Magnitude = Vector.Distance(dx, dy);
            v.Origin = new PointD(px1, py1);

            return v;
        }

        public static Vector FromPoints(double px1, double py1, double px2, double py2, Color c) {
            Vector v = Vector.FromPoints(px1, py1, px2, py2);
            v.Color = c;
            return v;
        }

        public static Vector Normalize(PointD p1, PointD p2) {
            Vector v = Vector.FromPoints(p1.X, p1.Y, p2.X, p2.Y);
            v.Magnitude = 1.0;
            return v;
        }

        public static Vector Normalize(Vector v) {
            return new Vector(v) { Magnitude = 1.0 };
        }

        public static Vector Empty {
            get => new Vector(0, 0, 0, 0);
        }

        public static bool operator ==(Vector v1, Vector v2) {
            return (v1?.Angle == v2?.Angle) && (v1?.Magnitude == v2?.Magnitude);
        }

        public static bool operator !=(Vector v1, Vector v2) {
            return !(v1 == v2);
        }

        public static Vector operator +(Vector v1, Vector v2) {
            Vector v3 = new Vector(v2) { Origin = v1.Destination };
            return new Vector(v1.Origin, v3.Destination);
        }

        public static Vector operator -(Vector v1, Vector v2) {
            Vector v3 = new Vector(v2) { Origin = v1.Origin };
            return new Vector(v3.Destination, v1.Destination);
        }

        public static Vector operator *(Vector v1, double s) {
            return new Vector(v1.Magnitude * s, v1.Angle, v1.Origin);
        }

        public static Vector operator *(double s, Vector v1) {
            return v1 * s;
        }

        public static Vector operator /(Vector v1, double s) {
            return v1 * (1 / s);
        }

        public static double Pow(Vector v1, double power) {
            if(power == 2.0) return Vector.Dot(v1, v1);
            return Math.Pow(v1.Magnitude, power);
        }

        public static double Dot(Vector v1, Vector v2) {
            double a = Math.Abs(v1.Angle - v2.Angle);
            return v1.Magnitude * v2.Magnitude * Math.Cos(a);
        }

        public static double Cross(Vector v1, Vector v2) {
            double rx = v1.X2 - v1.X1;
            double ry = v1.Y2 - v1.Y1;
            double tx = v2.X2 - v2.X1;
            double ty = v2.Y2 - v2.Y1;
            return rx * ty - ry * tx;
        }

        public static Vector Cross(Vector v1, double s) {
            return new Vector(v1.Origin, new PointD(v1.X1 + -s * (v1.Y2 - v1.Y1),
                                                    v1.Y1 + s * (v1.X2 - v1.X1)));
        }

        public static Vector Cross(double s, Vector v1) {
            return Cross(v1, s);
        }

        public static double Distance(double x1, double y1, double x2, double y2) {
            return Distance(x2 - x1, y2 - y1);
        }

        public static double Distance(double dx, double dy) {
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public static double Distance(PointD p1, PointD p2) {
            return Distance(p1.X, p1.Y, p2.X, p2.Y);
        }

        public virtual void Paint(Graphics g, Color c, float w = 2, double scale = 1.0) {
            using(Pen p = new Pen(c, w)) Paint(g, p, scale);
        }

        public virtual void Paint(Graphics g, Pen p, double scale = 1.0) {
            if(mMagnitude == 0.0) return;
            if(scale == 1.0) {
                g.DrawLine(p, (float)X1, (float)Y1, (float)X2, (float)Y2);
            } else {
                mMagnitude *= scale;
                Paint(g, p);
                mMagnitude /= scale;
            }
        }

        private void OnChanged() {
            Changed?.Invoke(this, new EventArgs());
        }

        public override string ToString() {
            return string.Format("Magnitude: {0:F2}{8}Angle: {1:F2}{8}({2:F2}, {3:F2})-({4:F2}, {5:F2}){8}y = {6:F2}x + {7:F2}",
                                mMagnitude,
                                mAngle * Constants.ToDeg,
                                X1, Y1, X2, Y2,
                                Slope, X1,
                                " " + Environment.NewLine
                            );
        }

        public override bool Equals(object obj) {
            return this == (Vector)obj;
        }

        public override int GetHashCode() {
            return hc;
        }
    }
}