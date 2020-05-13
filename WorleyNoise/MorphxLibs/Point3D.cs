using System;

namespace MorphxLibs {
    public class Point3D {
        public int X;
        public int Y;
        public int Z;

        public Point3D() : this(0, 0, 0) { }

        public Point3D(int x, int y, int z) {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(Point3D p) {
            double dx = X - p.X;
            double dy = Y - p.Y;
            double dz = Z - p.Z;
            return Math.Sqrt((dx * dx) + (dy * dy) + (dz * dz));
        }
    }
}