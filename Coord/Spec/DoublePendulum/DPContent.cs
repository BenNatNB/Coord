﻿using BenLib;
using BenLib.WPF;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using static BenLib.Animating;
using static Coord.VisualObjects;
using static System.Math;

namespace Coord
{
    public partial class MainWindow
    {
        public const int FPS = 60;

        private void ConfigurePlane()
        {
            plane.InputRange = new MathRect(-28, -7.5, 65, 32.5);
            plane.Grid.Primary = false;
            plane.Grid.Secondary = false;
            plane.Axes.Direction = AxesDirection.None;
            plane.AxesNumbers.Direction = AxesDirection.None;
            plane.RenderAtChange = false;
            SaveImages = false;
            ImagesPath = "";
        }

        private void OnLoaded()
        {
            ConfigurePlane();

            var dp1 = new DoublePendulum(3 * PI / 7, 3 * PI / 4, 1, 1, 1, 1).Style(FlatBrushes.Alizarin, new Pen(FlatBrushes.Clouds, 3));
            var dp2 = new DoublePendulum(3 * PI / 7, 3 * PI / 4, 1, 1, 1, 1).Style(FlatBrushes.PeterRiver, new Pen(FlatBrushes.Clouds, 3));
            var dp3 = new DoublePendulum(3 * PI / 7, 3 * PI / 4, 1, 1, 1, 1).Style(FlatBrushes.Emerald, new Pen(FlatBrushes.Clouds, 3));
            var dp4 = new DoublePendulum(3 * PI / 7, 3 * PI / 4, 1, 1, 1, 1).Style(FlatBrushes.SunFlower, new Pen(FlatBrushes.Clouds, 3));
            var dp5 = new DoublePendulum(3 * PI / 7, 3 * PI / 4, 1, 1, 1, 1).Style(FlatBrushes.Amethyst, new Pen(FlatBrushes.Clouds, 3));
            plane.VisualObjects.Add(new VisualObjectRenderer(dp1, dp2, dp3, dp4, dp5));

            async Task Animate()
            {
                CompositionTarget.Rendering += UP;
                await Timing.FramesDelay(10000 * 60);
                CompositionTarget.Rendering -= UP;

                void UP(object sender, EventArgs e)
                {
                    dp1.Update(1.0 / 60);
                    dp2.Update(1.0 / 60);
                    dp3.Update(1.0 / 60);
                    dp4.Update(1.0 / 60);
                    dp5.Update(1.0 / 60);
                }
            }

            this.Animate = Animate;
        }
    }
}
