﻿using BenLib.Framework;
using BenLib.Standard;
using BenLib.WPF;
using Coord;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;

namespace CoordAnimation
{
    public class Reference
    {
        public long ID { get; set; }
        public Type Type { get; set; }
        public object Object { get; set; }
        public JToken Token { get; set; }

        public void Deserialize(ReferenceCollection references, object existingValue = null) => Object ??= ValueEditionHelper.FromType(Type).DeserializeCore(Token, references, existingValue);
        public void CopyValues(DependencyObject destination, ReferenceCollection references) => ValueEditionHelper.FromType(Type).DeserializeCore(Token, references, destination);
    }

    public class ReferenceCollection : ICollection<Reference>
    {
        private long m_nextID;
        private readonly List<Reference> m_items;

        bool ICollection<Reference>.IsReadOnly => false;
        public int Count => m_items.Count;

        public Reference this[object obj] => TryGetReferenceByValue(obj, out var r) ? r : throw new ArgumentException();
        public Reference this[long iD] => TryGetReferenceByID(iD, out var r) ? r : throw new ArgumentException();

        public bool TryGetReferenceByValue(object obj, out Reference reference)
        {
            reference = m_items.FirstOrDefault(r => r.Object == obj);
            return reference != null;
        }
        public bool TryGetReferenceByID(long iD, out Reference reference)
        {
            reference = m_items.FirstOrDefault(r => r.ID == iD);
            return reference != null;
        }

        public ReferenceCollection() => m_items = new List<Reference>();
        private ReferenceCollection(IEnumerable<Reference> references) => m_items = new List<Reference>(references);

        public Reference GetOrAdd(object obj)
        {
            if (obj == null) return null;
            if (TryGetReferenceByValue(obj, out var reference)) return reference;
            var r = new Reference { ID = m_nextID++, Object = obj, Type = obj.GetType() };
            m_items.Add(r);
            return r;
        }

        public void Clear()
        {
            m_items.Clear();
            m_nextID = 0;
        }

        public JToken Serialize()
        {
            var result = new JArray();
            foreach (var reference in m_items)
            {
                result.Add(new JObject
                {
                    { "ID", reference.ID },
                    { "Type", TypeEditionHelper.GetString(reference.Type) },
                    { "Value", reference.Token }
                });
            }
            return result;
        }

        public static ReferenceCollection Deserialize(JToken data)
        {
            if (!(data is JArray jArray)) throw new FormatException();
            var result = new ReferenceCollection(jArray.Select(token => new Reference { ID = (long)token["ID"], Type = TypeEditionHelper.FromString((string)token["Type"]), Token = token["Value"] }));
            foreach (var reference in result) reference.Deserialize(result);
            return result;
        }

        public bool Contains(Reference item) => m_items.Contains(item);
        public void CopyTo(Reference[] array, int arrayIndex) => m_items.CopyTo(array, arrayIndex);

        void ICollection<Reference>.Add(Reference item) => throw new InvalidOperationException();
        bool ICollection<Reference>.Remove(Reference item) => throw new InvalidOperationException();

        IEnumerator<Reference> IEnumerable<Reference>.GetEnumerator() => m_items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => m_items.GetEnumerator();
    }

    public static class ValueEditionHelper
    {
        public static FrameworkElement GetEditorFromProperty(DependencyObject owner, DependencyProperty property, IDictionary<string, object> data, bool isAnimatable)
        {
            var helper = FromType(property.PropertyType);
            return
                helper == null ? (FrameworkElement)(owner.GetValue(property) is DependencyObject dependencyObject ? new PropertiesEditor { IsAnimatable = isAnimatable, Object = dependencyObject, Margin = new Thickness(0, 0, -3, 0) } : null) :
                helper.GetEditorFromProperty(owner, property, data, isAnimatable);
        }

        public static IValueEditionHelper FromType(Type type) =>
            type == typeof(string) ? new StringEditionHelper() :
            type == typeof(float) ? new FloatEditionHelper() :
            type == typeof(double) ? new DoubleEditionHelper() :
            type == typeof(decimal) ? new DecimalEditionHelper() :
            type == typeof(long) ? new LongEditionHelper() :
            type == typeof(ulong) ? new ULongEditionHelper() :
            type == typeof(int) ? new IntEditionHelper() :
            type == typeof(uint) ? new UIntEditionHelper() :
            type == typeof(short) ? new ShortEditionHelper() :
            type == typeof(ushort) ? new UShortEditionHelper() :
            type == typeof(sbyte) ? new SByteEditionHelper() :
            type == typeof(byte) ? new ByteEditionHelper() :
            typeof(Interval<int>).IsAssignableFrom(type) ? new IntIntervalEditionHelper() :
            typeof(Interval<double>).IsAssignableFrom(type) ? new DoubleIntervalEditionHelper() :
            typeof(Interval<decimal>).IsAssignableFrom(type) ? new DecimalIntervalEditionHelper() :
            type == typeof(Point) ? new PointEditionHelper() :
            type == typeof(Vector) ? new VectorEditionHelper() :
            type == typeof(Rect) ? new RectEditionHelper() :
            type == typeof(MathRect) ? new MathRectEditionHelper() :
            type == typeof(Size) ? new SizeEditionHelper() :
            type == typeof(Color) ? new ColorEditionHelper() :
            type == typeof(RectPoint) ? new RectPointEditionHelper() :
            type == typeof(Progress) ? new ProgressEditionHelper() :
            type == typeof(bool) ? new BooleanEditionHelper() :
            type == typeof(bool?) ? new NullableBooleanEditionHelper() :
            type == typeof(Type) ? new TypeEditionHelper() :
            type == typeof(DependencyProperty) ? new DependencyPropertyEditionHelper() :
            typeof(Enum).IsAssignableFrom(type) ? new EnumEditionHelper(type) :
            typeof(IList).IsAssignableFrom(type) ? new ListEditionHelper(type) :
            typeof(IDictionary).IsAssignableFrom(type) ? new DictionaryEditionHelper(type) :
            typeof(FrameworkElement).IsAssignableFrom(type) ? new FrameworkElementEditionHelper(type) :
            typeof(VisualObject).IsAssignableFrom(type) ? new VisualObjectEditionHelper(type) :
            typeof(DependencyObject).IsAssignableFrom(type) || type.IsInterface && App.DependencyObjectTypes.Contains(type) ? new DependencyObjectEditionHelper(type) :
            (IValueEditionHelper)null;

        public static JToken SerializeCore<T>(this T obj, ReferenceCollection references) => FromType(typeof(T)).SerializeCore(obj, references);
        public static JToken Serialize<T>(this T obj, ReferenceCollection references) => FromType(typeof(T)).Serialize(obj, references);

        public static object Deserialize(this JToken data, ReferenceCollection references, object existingValue = default)
        {
            if (data.Type == JTokenType.Null) return null;
            if (data.Type == JTokenType.Integer)
            {
                if (references.TryGetReferenceByID(data.Value<long>(), out var r))
                {
                    if (r.Object is object obj) return obj;
                    else
                    {
                        r.Deserialize(references, existingValue);
                        return r.Object;
                    }
                }
                else throw new ArgumentOutOfRangeException();
            }
            else if (data.Type == JTokenType.String)
            {
                string[] s = Regex.Split(data.Value<string>(), @"^\[(.+)PublicKeyToken=(\w+)\]\[(.+)\]$");
                return FromType(TypeEditionHelper.FromString($"{s[1]}PublicKeyToken={s[2]}")).DeserializeCore(s[3], references, existingValue);
            }
            else throw new ArgumentOutOfRangeException();
        }

        public static T DeserializeCore<T>(this JToken data, ReferenceCollection references, T existingValue = default) => (T)FromType(typeof(T)).DeserializeCore(data, references, existingValue);
    }

    public interface IValueEditionHelper
    {
        JToken Serialize(object obj, ReferenceCollection references);
        object DeserializeCore(JToken data, ReferenceCollection references, object existingValue = null);
        JToken SerializeCore(object obj, ReferenceCollection references);

        bool IsAnimatable { get; }
        FrameworkElement GetEditorFromProperty(DependencyObject owner, DependencyProperty property, IDictionary<string, object> data, bool isAnimatable);
        FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable);
        DependencyProperty BindingProperty { get; }
    }

    public abstract class ValueEditionHelper<T> : IValueEditionHelper
    {
        public virtual bool IsAnimatable => true;

        public FrameworkElement GetEditorFromProperty(DependencyObject owner, DependencyProperty property, IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = GetEditor(data, isAnimatable);
            editor.SetBinding(BindingProperty, new Binding(property.Name) { Source = owner, Mode = (((owner as Freezable)?.IsFrozen ?? false) || property.ReadOnly) ? BindingMode.OneTime : BindingMode.TwoWay });
            return isAnimatable && IsAnimatable ? CreateAnimatablePropertyEditor(editor, owner, property) : editor;
        }

        private static FrameworkElement CreateAnimatablePropertyEditor(FrameworkElement editor, DependencyObject dependencyObject, DependencyProperty dependencyProperty)
        {
            var result = AnimatablePropertyEditor.Create<T>(editor, dependencyObject, dependencyProperty);
            result.DataContext.RequestDataFocus += (sender, e) => App.Scene.Timeline.SetKeyFrames((AbsoluteKeyFrameCollection<T>)e.Param1);
            return result;
        }

        public virtual FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => null;
        public virtual DependencyProperty BindingProperty { get; }

        public JToken Serialize(T obj, ReferenceCollection references)
        {
            if (obj == null) return JValue.CreateNull();
            if (obj.GetType() is Type t && t.IsValueType) return $"[{TypeEditionHelper.GetString(t)}][{SerializeCore(obj, references)}]";
            else
            {
                var r = references.GetOrAdd(obj);
                if (r.Token == null) r.Token = SerializeCore(obj, references);
                return r.ID;
            }
        }

        public abstract T DeserializeCore(JToken data, ReferenceCollection references, T existingValue = default);
        public abstract JToken SerializeCore(T obj, ReferenceCollection references);

        JToken IValueEditionHelper.Serialize(object obj, ReferenceCollection references) => Serialize((T)obj, references);
        object IValueEditionHelper.DeserializeCore(JToken data, ReferenceCollection references, object existingValue) => DeserializeCore(data, references, existingValue is T exV ? exV : default);
        JToken IValueEditionHelper.SerializeCore(object obj, ReferenceCollection references) => SerializeCore((T)obj, references);
    }

    public class StringEditionHelper : ValueEditionHelper<string>
    {
        public override string DeserializeCore(JToken data, ReferenceCollection references, string existingValue) => data.Value<string>();
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new SwitchableTextBox();

        public override JToken SerializeCore(string obj, ReferenceCollection references) => obj;
        public override DependencyProperty BindingProperty => SwitchableTextBox.TextProperty;
    }

    public class FloatEditionHelper : ValueEditionHelper<float>
    {
        public override float DeserializeCore(JToken data, ReferenceCollection references, float existingValue) => float.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new FloatEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is float minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is float maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(float obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => FloatEditor.ValueProperty;
    }

    public class DoubleEditionHelper : ValueEditionHelper<double>
    {
        public override double DeserializeCore(JToken data, ReferenceCollection references, double existingValue) => double.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new DoubleEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is double minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is double maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(double obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => DoubleEditor.ValueProperty;
    }

    public class DecimalEditionHelper : ValueEditionHelper<decimal>
    {
        public override decimal DeserializeCore(JToken data, ReferenceCollection references, decimal existingValue) => decimal.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new DecimalEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is decimal minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is decimal maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(decimal obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => DecimalEditor.ValueProperty;
    }

    public class LongEditionHelper : ValueEditionHelper<long>
    {
        public override long DeserializeCore(JToken data, ReferenceCollection references, long existingValue) => long.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new LongEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is long minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is long maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(long obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => LongEditor.ValueProperty;
    }

    public class ULongEditionHelper : ValueEditionHelper<ulong>
    {
        public override ulong DeserializeCore(JToken data, ReferenceCollection references, ulong existingValue) => ulong.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new ULongEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is ulong minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is ulong maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(ulong obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => ULongEditor.ValueProperty;
    }

    public class IntEditionHelper : ValueEditionHelper<int>
    {
        public override int DeserializeCore(JToken data, ReferenceCollection references, int existingValue) => int.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new IntEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is int minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is int maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(int obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => IntEditor.ValueProperty;
    }

    public class UIntEditionHelper : ValueEditionHelper<uint>
    {
        public override uint DeserializeCore(JToken data, ReferenceCollection references, uint existingValue) => uint.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new UIntEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is uint minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is uint maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(uint obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => UIntEditor.ValueProperty;
    }

    public class ShortEditionHelper : ValueEditionHelper<short>
    {
        public override short DeserializeCore(JToken data, ReferenceCollection references, short existingValue) => short.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new ShortEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is short minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is short maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(short obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => ShortEditor.ValueProperty;
    }

    public class UShortEditionHelper : ValueEditionHelper<ushort>
    {
        public override ushort DeserializeCore(JToken data, ReferenceCollection references, ushort existingValue) => ushort.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new UShortEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is ushort minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is ushort maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(ushort obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => UShortEditor.ValueProperty;
    }

    public class SByteEditionHelper : ValueEditionHelper<sbyte>
    {
        public override sbyte DeserializeCore(JToken data, ReferenceCollection references, sbyte existingValue) => sbyte.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new SByteEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is sbyte minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is sbyte maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(sbyte obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => SByteEditor.ValueProperty;
    }

    public class ByteEditionHelper : ValueEditionHelper<byte>
    {
        public override byte DeserializeCore(JToken data, ReferenceCollection references, byte existingValue) => byte.Parse(data.Value<string>(), CultureInfo.InvariantCulture);
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var editor = new ByteEditor();
            if (data != null)
            {
                if (data.TryGetValue("min", out object min) && min is byte minValue) editor.MinValue = minValue;
                if (data.TryGetValue("max", out object max) && max is byte maxValue) editor.MaxValue = maxValue;
            }
            return editor;
        }

        public override JToken SerializeCore(byte obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => ByteEditor.ValueProperty;
    }

    public class PointEditionHelper : ValueEditionHelper<Point>
    {
        public override Point DeserializeCore(JToken data, ReferenceCollection references, Point existingValue) => Point.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new PointEditor();

        public override JToken SerializeCore(Point obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => PointEditor.PointProperty;
    }

    public class VectorEditionHelper : ValueEditionHelper<Vector>
    {
        public override Vector DeserializeCore(JToken data, ReferenceCollection references, Vector existingValue) => Vector.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new VectorEditor();

        public override JToken SerializeCore(Vector obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => VectorEditor.VectorProperty;
    }

    public class RectEditionHelper : ValueEditionHelper<Rect>
    {
        public override Rect DeserializeCore(JToken data, ReferenceCollection references, Rect existingValue) => Rect.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new RectEditor { Height = 120 };

        public override JToken SerializeCore(Rect obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => RectEditor.RectProperty;
    }

    public class MathRectEditionHelper : ValueEditionHelper<MathRect>
    {
        public override MathRect DeserializeCore(JToken data, ReferenceCollection references, MathRect existingValue) => MathRect.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new RectEditor { Height = 120 };

        public override JToken SerializeCore(MathRect obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => RectEditor.RectProperty;
    }

    public class SizeEditionHelper : ValueEditionHelper<Size>
    {
        public override Size DeserializeCore(JToken data, ReferenceCollection references, Size existingValue) => Size.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new SizeEditor { Height = 120 };

        public override JToken SerializeCore(Size obj, ReferenceCollection references) => obj.ToString(CultureInfo.InvariantCulture);
        public override DependencyProperty BindingProperty => SizeEditor.SizeProperty;
    }

    public class ColorEditionHelper : ValueEditionHelper<Color>
    {
        public override Color DeserializeCore(JToken data, ReferenceCollection references, Color existingValue) => Imaging.ColorFromHex(uint.Parse(data.Value<string>(), NumberStyles.HexNumber));
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new ColorEditor { Height = 180 };

        public override JToken SerializeCore(Color obj, ReferenceCollection references) => obj.ToHex();
        public override DependencyProperty BindingProperty => ColorEditor.ColorProperty;
    }

    public class RectPointEditionHelper : ValueEditionHelper<RectPoint>
    {
        public override RectPoint DeserializeCore(JToken data, ReferenceCollection references, RectPoint existingValue) => RectPoint.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new RectPointEditor { Height = 120 };

        public override JToken SerializeCore(RectPoint obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => RectPointEditor.RectPointProperty;
    }

    public class ProgressEditionHelper : ValueEditionHelper<Progress>
    {
        public override Progress DeserializeCore(JToken data, ReferenceCollection references, Progress existingValue) => Progress.Parse(data.Value<string>());

        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new ProgressEditor();

        public override JToken SerializeCore(Progress obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => ProgressEditor.ProgressProperty;
    }

    public class BooleanEditionHelper : ValueEditionHelper<bool>
    {
        public override bool DeserializeCore(JToken data, ReferenceCollection references, bool existingValue) => bool.Parse(data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new CheckBox();

        public override JToken SerializeCore(bool obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => ToggleButton.IsCheckedProperty;
    }

    public class NullableBooleanEditionHelper : ValueEditionHelper<bool?>
    {
        public override bool? DeserializeCore(JToken data, ReferenceCollection references, bool? existingValue)
        {
            string s = data.Value<string>();
            return s == "null" ? new bool?() : bool.Parse(s);
        }

        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new CheckBox { IsThreeState = true };

        public override JToken SerializeCore(bool? obj, ReferenceCollection references) => obj?.ToString() ?? "null";
        public override DependencyProperty BindingProperty => ToggleButton.IsCheckedProperty;
    }

    public class IntIntervalEditionHelper : ValueEditionHelper<Interval<int>>
    {
        public override Interval<int> DeserializeCore(JToken data, ReferenceCollection references, Interval<int> existingValue) => Interval<int>.Parse(data.Value<string>(), s => int.Parse(s));
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new IntervalEditor { IntervalType = IntervalType.Int };

        public override JToken SerializeCore(Interval<int> obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => IntervalEditor.IntIntervalProperty;
    }

    public class DoubleIntervalEditionHelper : ValueEditionHelper<Interval<double>>
    {
        public override Interval<double> DeserializeCore(JToken data, ReferenceCollection references, Interval<double> existingValue) => Interval<double>.Parse(data.Value<string>(), s => double.Parse(s));
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new IntervalEditor { IntervalType = IntervalType.Double };

        public override JToken SerializeCore(Interval<double> obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => IntervalEditor.DoubleIntervalProperty;
    }

    public class DecimalIntervalEditionHelper : ValueEditionHelper<Interval<decimal>>
    {
        public override Interval<decimal> DeserializeCore(JToken data, ReferenceCollection references, Interval<decimal> existingValue) => Interval<decimal>.Parse(data.Value<string>(), s => decimal.Parse(s));
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new IntervalEditor { IntervalType = IntervalType.Decimal };

        public override JToken SerializeCore(Interval<decimal> obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => IntervalEditor.DecimalIntervalProperty;
    }

    public class EnumEditionHelper : ValueEditionHelper<Enum>
    {
        public EnumEditionHelper(Type enumType) => EnumType = enumType;

        public Type EnumType { get; }

        public override Enum DeserializeCore(JToken data, ReferenceCollection references, Enum existingValue) => (Enum)Enum.Parse(EnumType, data.Value<string>());
        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new EnumEditor();

        public override JToken SerializeCore(Enum obj, ReferenceCollection references) => obj.ToString();
        public override DependencyProperty BindingProperty => EnumEditor.EnumerationProperty;
    }

    public class DependencyPropertyEditionHelper : ValueEditionHelper<DependencyProperty>
    {
        public override DependencyProperty DeserializeCore(JToken data, ReferenceCollection references, DependencyProperty existingValue) => FromString(data.Value<string>());
        public static DependencyProperty FromString(string data)
        {
            string[] name = Regex.Split(data, @"^(\w+)\[(.+)\]$");
            var type = TypeEditionHelper.FromString(name[2]);
            return DependencyPropertyDescriptor.FromName(name[1], type, typeof(DependencyObject))?.DependencyProperty;
        }

        public override JToken SerializeCore(DependencyProperty obj, ReferenceCollection references) => GetString(obj);
        public static string GetString(DependencyProperty dp) => $"{dp.Name}[{TypeEditionHelper.GetString(dp.OwnerType)}]";
    }

    public class TypeEditionHelper : ValueEditionHelper<Type>
    {
        public override Type DeserializeCore(JToken data, ReferenceCollection references, Type existingValue) => FromString(data.Value<string>());
        public static Type FromString(string data) => Type.GetType(data);

        public override JToken SerializeCore(Type obj, ReferenceCollection references) => GetString(obj);
        public static string GetString(Type type) => type.AssemblyQualifiedName;
    }

    public class ListEditionHelper : ValueEditionHelper<IList>
    {
        public override bool IsAnimatable => false;
        public ListEditionHelper(Type type) => Type = type;

        public Type Type { get; }

        public override IList DeserializeCore(JToken data, ReferenceCollection references, IList existingValue)
        {
            if (!(data is JArray jArray)) throw new FormatException();

            existingValue ??= (IList)Activator.CreateInstance(Type);
            foreach (var token in jArray) existingValue.Add(token.Deserialize(references));
            return existingValue;
        }

        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable)
        {
            var type = Type;
            var editor = new ListEditor { IsAnimatable = isAnimatable, Type = type, Margin = new Thickness(0, 0, -3, 0) };
            var genericIList = type.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>));
            if (genericIList != null) editor.ItemType = genericIList.GenericTypeArguments[0];
            return editor;
        }

        public override JToken SerializeCore(IList obj, ReferenceCollection references)
        {
            var result = new JArray();
            foreach (object item in obj) { if (ValueEditionHelper.FromType(item.GetType()) is IValueEditionHelper helper) result.Add(helper.Serialize(item, references)); }
            return result;
        }
        public override DependencyProperty BindingProperty => PropertiesEditorBase.ObjectProperty;
    }

    public class DictionaryEditionHelper : ValueEditionHelper<IDictionary>
    {
        public override bool IsAnimatable => false;
        public DictionaryEditionHelper(Type type) => Type = type;

        public Type Type { get; }

        public override IDictionary DeserializeCore(JToken data, ReferenceCollection references, IDictionary existingValue)
        {
            if (!(data is JArray jArray)) throw new FormatException();

            existingValue ??= (IDictionary)Activator.CreateInstance(Type);
            foreach (var token in jArray) existingValue.Add(token["Key"].Deserialize(references), token["Value"].Deserialize(references));
            return existingValue;
        }

        public override JToken SerializeCore(IDictionary obj, ReferenceCollection references)
        {
            var result = new JArray();
            var e = obj.GetEnumerator();
            while (e.MoveNext())
            {
                var (key, value) = (e.Key, e.Value);
                var entry = new JObject();
                if (ValueEditionHelper.FromType(key.GetType()) is IValueEditionHelper keyHelper) entry.Add("Key", keyHelper.Serialize(key, references));
                if (ValueEditionHelper.FromType(value.GetType()) is IValueEditionHelper valueHelper) entry.Add("Value", valueHelper.Serialize(value, references));
                if (entry.Count > 0) result.Add(entry);
            }
            return result;
        }
    }

    public class DependencyObjectEditionHelper : ValueEditionHelper<DependencyObject>
    {
        public virtual bool IsDeep => true;
        public override bool IsAnimatable => false;
        public DependencyObjectEditionHelper(Type type) => Type = type;

        public Type Type { get; }

        public override DependencyObject DeserializeCore(JToken data, ReferenceCollection references, DependencyObject existingValue)
        {
            if (!(data is JObject jObject)) throw new FormatException();

            var type = Type;
            existingValue ??= (DependencyObject)Activator.CreateInstance(type);
            foreach (var kvp in jObject)
            {
                var dp = DependencyPropertyEditionHelper.FromString(kvp.Key);
                if (dp == PropertiesAnimation.AnimationDataProperty) kvp.Value.Deserialize(references, existingValue.GetOrCreateAnimationData());
                else if (dp != null && !dp.ReadOnly) existingValue.SetValue(dp, kvp.Value.Deserialize(references));
            }
            return existingValue;
        }

        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new PropertiesEditor { IsAnimatable = isAnimatable, Type = Type, Margin = new Thickness(0, 0, -3, 0) };

        public override JToken SerializeCore(DependencyObject obj, ReferenceCollection references)
        {
            var type = Type;
            bool isDeep = IsDeep;
            var result = new JObject();
            var e = obj.GetLocalValueEnumerator();
            while (e.MoveNext())
            {
                var (dp, v) = (e.Current.Property, e.Current.Value);
                if ((isDeep || dp.OwnerType == type) && ValueEditionHelper.FromType(dp.PropertyType) is IValueEditionHelper helper) result.Add(DependencyPropertyEditionHelper.GetString(dp), helper.Serialize(v, references));
            }
            return result;
        }

        public override DependencyProperty BindingProperty => PropertiesEditorBase.ObjectProperty;
    }

    public class VisualObjectEditionHelper : DependencyObjectEditionHelper
    {
        public bool IsPointing => Type != typeof(VisualObject);

        public VisualObjectEditionHelper() : base(typeof(VisualObject)) { }
        public VisualObjectEditionHelper(Type type) : base(type) { }

        public override FrameworkElement GetEditor(IDictionary<string, object> data, bool isAnimatable) => new VisualObjectSelector { Selection = App.Scene.Plane.Selection, Pointing = IsPointing ? Type : null };
        public override DependencyProperty BindingProperty => VisualObjectSelector.VisualObjectProperty;
    }

    public class FrameworkElementEditionHelper : DependencyObjectEditionHelper
    {
        public override bool IsDeep => false;
        public FrameworkElementEditionHelper(Type type) : base(type) { }
    }

    /*public class PlaneEditionHelper : DependencyObjectEditionHelper
    {
        public override bool IsDeep => false;
        public PlaneEditionHelper() : base(typeof(Plane)) { }

        public override DependencyObject DeserializeCore(JToken data, ReferenceCollection references, DependencyObject existingValue)
        {
            var p = (Plane)base.DeserializeCore(data, references, existingValue);
            references[ValueEditionHelper.FromType(typeof(CoordinatesSystemManager)).Deserialize(data["CoordinatesSysyemManager"], references)].CopyValues(p.CoordinatesSystemManager, references);
            return p;
        }

        public override JToken SerializeCore(DependencyObject obj, ReferenceCollection references)
        {
            var p = (Plane)obj;
            var result = (JObject)base.SerializeCore(obj, references);
            result.Add("CoordinatesSysyemManager", p.CoordinatesSystemManager.Serialize(references));
            return result;
        }
    }*/
}
