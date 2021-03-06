﻿using BenLib.Framework;
using BenLib.Standard;
using Coord;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CoordAnimation
{
    /// <summary>
    /// Logique d'interaction pour Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        private readonly TimelineCursor m_cursor = new TimelineCursor();
        private IAbsoluteKeyFrameCollection m_keyFrames;
        private bool m_setting;

        public VisualObject Curves { get; private set; }
        public VisualObject KeyFrames { get; private set; }

        public Timeline()
        {
            InitializeComponent();

            TimelineGraphics.Cursor = CharacterFromResource("CUR").WithData("CUR");
            TimelineGraphics.DiscreteKeyFrame = (CharacterFromResource("DL").WithData("DL"), CharacterFromResource("DR").WithData("DR"));
            TimelineGraphics.LinearKeyFrame = (CharacterFromResource("LL").WithData("LL"), CharacterFromResource("LR").WithData("LR"));
            TimelineGraphics.EasingKeyFrame = (CharacterFromResource("EL").WithData("EL"), CharacterFromResource("ER").WithData("ER"));
            TimelineGraphics.SplineKeyFrame = (CharacterFromResource("SL").WithData("SL"), CharacterFromResource("SR").WithData("SR"));
            Character CharacterFromResource(string name) => (TryFindResource(name) as Shape).ToCharacter();

            PropertiesAnimation.DataRemoved += (sender, e) => { if (e.Param1 == m_keyFrames) SetKeyFrames<object>(null); };
        }

        public void SetKeyFrames<T>(AbsoluteKeyFrameCollection<T> keyFrames)
        {
            Curves?.Destroy();
            KeyFrames?.Destroy();

            if (keyFrames == null)
            {
                Curves = null;
                KeyFrames = null;
            }
            else
            {
                var t = new TimelineKeyFrames<T> { KeyFrames = keyFrames };
                var curves = new CurveVisualObject { Series = new TimelineCurveSeries<T> { KeyFrames = keyFrames }, IsSelectable = false }.Style(new Pen(Brushes.White, 1));

                t.SelectionChanged += (sender, e) =>
                {
                    if (e.OriginalSource is TimelineKeyFrame<T> tkf)
                    {
                        m_setting = true;
                        var kf = tkf.Selection.IsEmpty ? null : tkf.Focus < 1 ? tkf.KeyFrame : tkf.NextKeyFrame;
                        keyFramesEditor.Type = kf == null ? null : typeof(AbsoluteKeyFrame<T>);
                        keyFramesEditor.Object = kf;
                        m_setting = false;
                    }
                };
                t.Changed += (sender, e) =>
                {
                    plane.RenderdWithoutCache(t);
                    plane.RenderdWithoutCache(curves);
                    UpdateCursorPoint(PropertiesAnimation.GeneralTime);
                };

                plane.Items.Insert(1, curves);
                plane.Items.Add(t);

                KeyFrames = t;
                Curves = curves;
            }

            m_keyFrames = keyFrames;
            UpdateCursorPoint(PropertiesAnimation.GeneralTime);
        }

        private void UpdatePlaneDirection(object sender, EventArgs e) => plane.MoveDirection = Input.IsAltPressed() ? AxesDirection.Vertical : AxesDirection.Horizontal;

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            plane.Items = new VisualObjectCollection();

            plane.Selection.AllAtOnce = true;
            plane.Selection.AllowMultiple = false;
            plane.CoordinatesSystemManager.MinCellSize = 45;
            plane.CoordinatesSystemManager.MaxCellSize = 90;
            plane.CoordinatesSystemManager.InputRangeLimits = new MathRect(0, double.NaN, long.MaxValue, double.NaN);
            plane.Grid.Primary = plane.Grid.Secondary = false;
            plane.Axes.Direction = AxesDirection.None;
            plane.AxesNumbers.Direction = AxesDirection.None;
            plane.CoordinatesSystemManager.InputRange = new MathRect(0, 0, 300, 1);

            PropertiesAnimation.GeneralTimeChanged += OnGeneralTimeChanged;

            plane.Items.AddRange(new TimelineLimits(), new TimelineHeader(), m_cursor);
        }

        private void OnGeneralTimeChanged(object sender, PropertyChangedExtendedEventArgs<long> e) => UpdateCursorPoint(e.NewValue);
        private void UpdateCursorPoint(long time)
        {
            m_cursor.InCurrentPoint = new Point(time, m_keyFrames?.ProgressAt(time, true) ?? double.NaN);
            plane.Render(m_cursor);
        }

        private void KeyFramesEditor_ObjectChanged(object sender, PropertyChangedExtendedEventArgs<object> e)
        {
            if (!m_setting && m_keyFrames is IAbsoluteKeyFrameCollection keyFrames && e.OldValue is IAbsoluteKeyFrame oldValue)
            {
                if (e.NewValue is IAbsoluteKeyFrame newValue)
                {
                    newValue.FramesCount = oldValue.FramesCount;
                    newValue.Value = oldValue.Value;
                    keyFrames[keyFrames.IndexOf(oldValue)] = newValue;
                }
                else keyFrames[keyFrames.IndexOf(oldValue)] = null;
            }
        }
    }
}
