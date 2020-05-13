using System;
using System.Drawing;

namespace MorphxLibs {
    [Serializable()]
    public class HLSRGB {
        private byte mRed = 0;
        private byte mGreen = 0;
        private byte mBlue = 0;

        private double mHue = 0;
        private double mLuminance = 0;
        private double mSaturation = 0;
        private byte mAlpha;

        public struct HueLumSat {
            public double Hue { get; set; }
            public double Lum { get; set; }
            public double Sat { get; set; }

            public HueLumSat(double hue, double lum, double sat) {
                Hue = hue;
                Lum = lum;
                Sat = sat;
            }
        }

        public HLSRGB(Color color) {
            mAlpha = color.A;
            mRed = color.R;
            mGreen = color.G;
            mBlue = color.B;
        }

        public HLSRGB(double hue, double luminance, double saturation) {
            mAlpha = 255;
            mHue = hue;
            mLuminance = luminance;
            mSaturation = saturation;
        }

        public HLSRGB(byte alpha, byte red, byte green, byte blue) {
            mAlpha = alpha;
            mRed = red;
            mGreen = green;
            mBlue = blue;
        }

        public HLSRGB(HLSRGB hlsrgb) {
            mRed = hlsrgb.Red;
            mBlue = hlsrgb.Blue;
            mGreen = hlsrgb.Green;
            mLuminance = hlsrgb.Luminance;
            mHue = hlsrgb.Hue;
            mSaturation = hlsrgb.Saturation;
        }

        public HLSRGB() {
        }

        public byte Red {
            get {
                return mRed;
            }
            set {
                mRed = value;
            }
        }

        public byte Green {
            get {
                return mGreen;
            }
            set {
                mGreen = value;
            }
        }

        public byte Blue {
            get {
                return mBlue;
            }
            set {
                mBlue = value;
            }
        }

        public double Luminance {
            get {
                return mLuminance;
            }
            set {
                mLuminance = ChkLum(value);
            }
        }

        public double Hue {
            get {
                return mHue;
            }
            set {
                mHue = ChkHue(value);
            }
        }

        public double Saturation {
            get {
                return mSaturation;
            }
            set {
                mSaturation = ChkSat(value);
            }
        }

        public HueLumSat HLS {
            get {
                ToHLS();
                return new HueLumSat(mHue, mLuminance, mSaturation);
            }
            set {
                mHue = ChkHue(value.Hue);
                mLuminance = ChkLum(value.Lum);
                mSaturation = ChkSat(value.Sat);
            }
        }

        public Color Color {
            get {
                ToRGB();
                return Color.FromArgb(mAlpha, mRed, mGreen, mBlue);
            }
            set {
                mAlpha = Color.A;
                mRed = value.R;
                mGreen = value.G;
                mBlue = value.B;
            }
        }

        public void LightenColor(double lightenBy) {
            mLuminance *= (1.0F + lightenBy);
            if(mLuminance > 1.0F)
                Luminance = 1.0F;
        }

        public int Alpha {
            get {
                return mAlpha;
            }
            set {
                mAlpha = ChkAlpha(value);
            }
        }

        public void DarkenColor(double darkenBy) {
            Luminance *= darkenBy;
        }

        private void ToHLS() {
            byte minval = Math.Min(mRed, Math.Min(mGreen, mBlue));
            byte maxval = Math.Max(mRed, Math.Max(mGreen, mBlue));

            double mdiff = (double)maxval - (double)minval;
            double msum = (double)maxval + (double)minval;

            mLuminance = msum / 510.0F;

            if(maxval == minval) {
                mSaturation = 0.0F;
                mHue = 0.0F;
            } else {
                double rnorm = (maxval - mRed) / mdiff;
                double gnorm = (maxval - mGreen) / mdiff;
                double bnorm = (maxval - mBlue) / mdiff;

                if(mLuminance <= 0.5F)
                    mSaturation = mdiff / msum;
                else
                    mSaturation = mdiff / (510.0F - msum);

                if(mRed == maxval)
                    mHue = 60.0F * (6.0F + bnorm - gnorm);
                if(mGreen == maxval)
                    mHue = 60.0F * (2.0F + rnorm - bnorm);
                if(mBlue == maxval)
                    mHue = 60.0F * (4.0F + gnorm - rnorm);
                if(mHue > 360.0F)
                    mHue = Hue - 360.0F;
            }
        }

        private void ToRGB() {
            if(mSaturation == 0.0) {
                mAlpha = 255;
                mRed = System.Convert.ToByte(mLuminance * 255.0F);
                mGreen = mRed;
                mBlue = mRed;
            } else {
                double rm1;
                double rm2;

                if(mLuminance <= 0.5F)
                    rm2 = mLuminance + mLuminance * mSaturation;
                else
                    rm2 = mLuminance + mSaturation - mLuminance * mSaturation;
                rm1 = 2.0F * mLuminance - rm2;
                mRed = ToRGB1(rm1, rm2, mHue + 120.0F);
                mGreen = ToRGB1(rm1, rm2, mHue);
                mBlue = ToRGB1(rm1, rm2, mHue - 120.0F);
            }
        }

        private byte ToRGB1(double rm1, double rm2, double rh) {
            if(rh > 360.0F)
                rh -= 360.0F;
            else if(rh < 0.0F)
                rh += 360.0F;

            if((rh < 60.0F))
                rm1 = rm1 + (rm2 - rm1) * rh / 60.0F;
            else if((rh < 180.0F))
                rm1 = rm2;
            else if((rh < 240.0F))
                rm1 = rm1 + (rm2 - rm1) * (240.0F - rh) / 60.0F;

            // TODO: Fix this... we shouldn't have to use a Try/Catch
            try {
                return System.Convert.ToByte(rm1 * 255);
            } catch {
                return System.Convert.ToByte(255);
            }
        }

        private double ChkHue(double value) {
            if(value < 0.0F)
                value = Math.Abs((360.0F + value) % 360.0F);
            else if(value > 360.0F)
                value %= 360.0F;

            return value;
        }

        private double ChkLum(double value) {
            if((value < 0.0F) || (value > 1.0F)) {
                if(value < 0.0F)
                    value = 0;
                else if(value > 1.0F)
                    value = 1.0F;
            }

            return value;
        }

        private double ChkSat(double value) {
            if(value < 0.0F)
                value = 0;
            else if(value > 1.0F)
                value = 1.0F;

            return value;
        }

        private byte ChkAlpha(int value) {
            if(value < 0)
                value = 0;
            else if(value > 255)
                value = 255;

            return (byte)value;
        }
    }
}