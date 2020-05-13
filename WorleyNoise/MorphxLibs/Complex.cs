using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace MorphxLibs {
    public class Complex {
        public double R;
        public double I;

        public Complex() { }

        public Complex(double r) {
            this.R = r;
        }

        public Complex(double r, double i) {
            this.R = r;
            this.I = i;
        }

        public double Abs() {
            return Magnitude();
        }

        public double Power() {
            return R * R + I * I;
        }

        public double PowerRoot() {
            return Math.Sqrt(Power());
        }

        public double Power2() {
            return Math.Abs(R) + Math.Abs(I);
        }

        public double Power2Root() {
            return Math.Sqrt(Power2());
        }

        public Complex Conjugate() {
            return new Complex(R, -I);
        }

        public double Magnitude() {
            return Math.Sqrt(Power());
        }

        public static Complex operator +(Complex n1, Complex n2) {
            return new Complex(n1.R + n2.R, n1.I + n2.I);
        }

        public static Complex operator +(Complex n1, double n2) {
            return new Complex(n1.R + n2, n1.I);
        }

        public static Complex operator +(double n1, Complex n2) {
            return new Complex(n1 + n2.R, n2.I);
        }

        public static Complex operator -(Complex n1, Complex n2) {
            return new Complex(n1.R - n2.R, n1.I - n2.I);
        }

        public static Complex operator -(Complex n1, double n2) {
            return new Complex(n1.R - n2, n1.I);
        }

        public static Complex operator -(double n1, Complex n2) {
            return new Complex(n1 - n2.R, n2.I);
        }

        public static Complex operator *(Complex n1, Complex n2) {
            return new Complex(n1.R * n2.R - n1.I * n2.I, n1.I * n2.R + n2.I * n1.R);
        }

        public static Complex operator *(Complex n1, double n2) {
            return new Complex(n1.R * n2, n1.I * n2);
        }

        public static Complex operator *(double n1, Complex n2) {
            return new Complex(n1 * n2.R, n1 * n2.I);
        }

        public static Complex operator /(Complex n1, double n2) {
            return new Complex(n1.R / n2, n1.I / n2);
        }

        public static Complex Pow(Complex n1, Complex n2) {
            throw new NotImplementedException();
        }

        public static Complex Pow(Complex n1, double n2) {
            Complex r = n1;
            for(int i = 0; i < n2; i++) {
                r *= n1;
            }
            return r;
        }

        public static Complex Pow(double n1, Complex n2) {
            double ab = Math.Pow(n1, n2.R);
            double ln1 = Math.Log(n1);
            double r = ab * Math.Cos(n2.I * ln1);
            double i = ab * Math.Sin(n2.I * ln1);

            return new Complex(r, i);
        }

        public static Complex From(double value) {
            return new Complex(value);
        }

        public static Complex[] From(double[] values) {
            return (from d in values select Complex.From(d)).ToArray();
        }

        public override string ToString() {
            return $"{R:F2} + {I:F2}i";
        }
    }
}
