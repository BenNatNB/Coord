﻿using BenLib.Framework;
using BenLib.Standard;
using System;
using System.Globalization;
using System.Windows;

namespace Coord
{
    internal class RectPointValueInterpolationHelper : ValueInterpolationHelper<RectPoint> { protected override RectPoint InterpolateCore(RectPoint start, RectPoint end, double progress) => new RectPoint(Num.Interpolate(start.XProgress, end.XProgress, progress), Num.Interpolate(start.YProgress, end.YProgress, progress)); }

    /// <summary>
    /// Point d'un rectangle
    /// </summary>
    public readonly struct RectPoint : IEquatable<RectPoint>
    {
        static RectPoint() => ValueInterpolationHelper<RectPoint>.Default = new RectPointValueInterpolationHelper();

        public RectPoint(double xProgress, double yProgress) : this()
        {
            XProgress = xProgress;
            YProgress = yProgress;
        }

        public double XProgress { get; }
        public double YProgress { get; }

        public bool IsNaN => double.IsNaN(XProgress) || double.IsNaN(YProgress);
        public RectPoint Opposite => new RectPoint(1 - XProgress, 1 - YProgress);

        public Point GetPoint(Rect rect) => new Point(rect.Left + (rect.Right - rect.Left) * XProgress, rect.Top + (rect.Bottom - rect.Top) * YProgress);

        public static RectPoint TopLeft { get; } = new RectPoint(0, 0);
        public static RectPoint TopRight { get; } = new RectPoint(1, 0);
        public static RectPoint BottomRight { get; } = new RectPoint(1, 1);
        public static RectPoint BottomLeft { get; } = new RectPoint(0, 1);
        public static RectPoint Left { get; } = new RectPoint(0, 0.5);
        public static RectPoint Top { get; } = new RectPoint(0.5, 1);
        public static RectPoint Right { get; } = new RectPoint(1, 0.5);
        public static RectPoint Bottom { get; } = new RectPoint(0.5, 0);
        public static RectPoint Center { get; } = new RectPoint(0.5, 0.5);
        public static RectPoint NaN { get; } = new RectPoint(double.NaN, double.NaN);

        public override bool Equals(object obj) => obj is RectPoint point && Equals(point);
        public bool Equals(RectPoint other) => XProgress == other.XProgress && YProgress == other.YProgress;

        public override int GetHashCode()
        {
            int hashCode = 2140781431;
            hashCode = hashCode * -1521134295 + XProgress.GetHashCode();
            hashCode = hashCode * -1521134295 + YProgress.GetHashCode();
            return hashCode;
        }

        public void Deconstruct(out double xProgress, out double yProgress) => (xProgress, yProgress) = (XProgress, YProgress);

        public static bool operator ==(RectPoint left, RectPoint right) => left.Equals(right);
        public static bool operator !=(RectPoint left, RectPoint right) => !(left == right);

        public override string ToString() => this switch
        {
            (0, 0) => "TopLeft",
            (1, 0) => "TopRight",
            (1, 1) => "BottomRight",
            (0, 1) => "BottomLeft",
            (0, 0.5) => "Left",
            (0.5, 1) => "Top",
            (1, 0.5) => "Right",
            (0.5, 0) => "Bottom",
            (0.5, 0.5) => "Center",
            _ => IsNaN ? "NaN" : $"{XProgress.ToString(CultureInfo.InvariantCulture)};{YProgress.ToString(CultureInfo.InvariantCulture)}",
        };

        private static RectPoint FromString(string s)
        {
            string[] ss = s.Split(';');
            if (ss.Length != 2) throw new FormatException();
            return new RectPoint(double.Parse(ss[0]), double.Parse(ss[1]));
        }

        public static RectPoint Parse(string s) => s switch
        {
            "TopLeft" => TopLeft,
            "TopRight" => TopRight,
            "BottomRight" => BottomRight,
            "BottomLeft" => BottomLeft,
            "Left" => Left,
            "Top" => Top,
            "Right" => Right,
            "Bottom" => Bottom,
            "Center" => Center,
            "NaN" => NaN,
            _ => FromString(s)
        };
    }
}
