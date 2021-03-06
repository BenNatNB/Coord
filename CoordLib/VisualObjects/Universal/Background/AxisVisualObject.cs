﻿using System;
using System.Collections.Generic;
using System.Windows;

namespace Coord
{
    public class AxesVisualObject : VisualObject
    {
        protected override Freezable CreateInstanceCore() => new AxesVisualObject();

        public override string Type => "Axes";

        public AxesDirection Direction { get => (AxesDirection)GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }
        public static readonly DependencyProperty DirectionProperty = CreateProperty<AxesVisualObject, AxesDirection>(true, true, true, "Direction");

        protected override IEnumerable<Character> GetCharactersCore(ReadOnlyCoordinatesSystemManager coordinatesSystemManager)
        {
            var fill = Fill;
            var stroke = Stroke;
            var direction = Direction;

            var outRange = coordinatesSystemManager.OutputRange;
            var inRange = coordinatesSystemManager.InputRange;

            var center = coordinatesSystemManager.OrthonormalOrigin;
            double demiThickness = Stroke == null ? 0 : Stroke.Thickness / 2;

            if (direction.HasFlag(AxesDirection.Horizontal) && outRange.HeightContainsRange(center.Y - demiThickness, center.Y + demiThickness, false)) yield return Character.Line(coordinatesSystemManager.ComputeOutCoordinates(new Point(inRange.Left, 0)), coordinatesSystemManager.ComputeOutCoordinates(new Point(inRange.Right, 0))).Color(fill, stroke);
            if (direction.HasFlag(AxesDirection.Vertical) && outRange.WidthContainsRange(center.X - demiThickness, center.X + demiThickness, false)) yield return Character.Line(coordinatesSystemManager.ComputeOutCoordinates(new Point(0, inRange.Bottom)), coordinatesSystemManager.ComputeOutCoordinates(new Point(0, inRange.Top))).Color(fill, stroke);
        }
    }

    [Flags]
    public enum AxesDirection
    {
        None = 0,
        Horizontal = 1,
        Vertical = 2,
        Both = 3
    }
}
