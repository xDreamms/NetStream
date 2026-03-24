using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace NetStream
{
    /// <summary>
    /// Avalonia için modern bir WrapPanel implementasyonu.
    /// Avalonia 11 ile uyumlu, ItemsRepeater ile kullanılabilir.
    /// </summary>
    public class VirtualizingWrapPanel : WrapPanel
    {
        private Size _availableSpace;
        private double _takenSpace;
        private int _canBeRemoved;
        private double _averageItemSize;
        private int _averageCount;
        private double _pixelOffset;
        private double _crossAxisOffset;
        private bool _forceRemeasure;
        private List<double> _lineLengths = new List<double>();
        private double _takenLineSpace;
        private Size _previousMeasure;

        /// <summary>
        /// Panelin tamamen dolu olup olmadığını belirtir
        /// </summary>
        public bool IsFull
        {
            get
            {
                return Orientation == Orientation.Vertical ?
                    _takenSpace >= _availableSpace.Width :
                    _takenSpace >= _availableSpace.Height;
            }
        }

        /// <summary>
        /// Panel için kaydırma yönünü belirtir
        /// </summary>
        public Orientation ScrollDirection => Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        
        /// <summary>
        /// Ortalama öğe boyutunu belirtir
        /// </summary>
        public double AverageItemSize => _averageItemSize;

        /// <summary>
        /// Piksel taşmasını belirtir
        /// </summary>
        public double PixelOverflow
        {
            get
            {
                var bounds = Orientation == Orientation.Vertical ?
                    _availableSpace.Width : _availableSpace.Height;
                return Math.Max(0, _takenSpace - bounds);
            }
        }

        /// <summary>
        /// Piksel kaydırma değerini belirtir
        /// </summary>
        public double PixelOffset
        {
            get { return _pixelOffset; }
            set
            {
                if (_pixelOffset != value)
                {
                    _pixelOffset = value;
                    InvalidateArrange();
                }
            }
        }

        /// <summary>
        /// Çapraz eksen kaydırma değerini belirtir
        /// </summary>
        public double CrossAxisOffset
        {
            get { return _crossAxisOffset; }
            set
            {
                if (_crossAxisOffset != value)
                {
                    _crossAxisOffset = value;
                    InvalidateArrange();
                }
            }
        }

        /// <summary>
        /// Kaydırma birimi değerini belirtir
        /// </summary>
        public int ScrollUnit => _lineLengths.Count == 0 ? 1 : _averageCount / _lineLengths.Count;

        /// <summary>
        /// Panelin ölçüsünü yeniden hesaplamaya zorlar
        /// </summary>
        public void ForceInvalidateMeasure()
        {
            InvalidateMeasure();
            _forceRemeasure = true;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (availableSize.Width == double.PositiveInfinity)
            {
                availableSize = new Size(5, availableSize.Height);
            }

            if (_forceRemeasure || availableSize != _previousMeasure)
            {
                _forceRemeasure = false;
                _availableSpace = availableSize;
                _previousMeasure = availableSize;
                UpdateControls();
            }

            return base.MeasureOverride(availableSize);
        }

        /// <summary>
        /// Kontrolleri günceller
        /// </summary>
        private void UpdateControls()
        {
            // Burada ihtiyaç olursa kontrol güncellemesi yapılabilir
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _availableSpace = finalSize;
            _canBeRemoved = 0;
            _takenSpace = 0;
            _takenLineSpace = 0;
            _lineLengths.Clear();
            _averageItemSize = 0;
            _averageCount = 0;
            var result = Arrange(finalSize);
            _takenSpace += _pixelOffset;
            UpdateControls();
            return result;
        }

        protected Size Arrange(Size finalSize)
        {
            double accumulatedV = 0;
            var uvFinalSize = CreateUVSize(finalSize);
            var lineSize = CreateUVSize();
            int firstChildInLineindex = 0;
            for (int index = 0; index < Children.Count; index++)
            {
                var child = Children[index];
                var childSize = CreateUVSize(child.DesiredSize);
                if (lineSize.U + childSize.U <= uvFinalSize.U) // same line
                {
                    lineSize.U += childSize.U;
                    lineSize.V = Math.Max(lineSize.V, childSize.V);
                    _takenLineSpace += childSize.U;
                }
                else // moving to next line
                {
                    var controlsInLine = GetContolsBetween(firstChildInLineindex, index);
                    ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
                    accumulatedV += lineSize.V;
                    lineSize = childSize;
                    firstChildInLineindex = index;
                    _takenLineSpace = childSize.U;
                }
            }

            if (firstChildInLineindex < Children.Count)
            {
                var controlsInLine = GetContolsBetween(firstChildInLineindex, Children.Count);
                ArrangeLine(accumulatedV, lineSize.V, controlsInLine);
            }
            return finalSize;
        }
        private IEnumerable<Control> GetContolsBetween(int first, int last)
        {
            return Children.Skip(first).Take(last - first);
        }

        private void ArrangeLine(double v, double lineV, IEnumerable<Control> contols)
        {
            double u = 0;
            bool isHorizontal = (this.Orientation == Orientation.Horizontal);
            foreach (var child in contols)
            {
                var childSize = CreateUVSize(child.DesiredSize);
                var x = isHorizontal ? u : v;
                var y = isHorizontal ? v : u;
                var width = isHorizontal ? childSize.U : lineV;
                var height = isHorizontal ? lineV : childSize.U;

                var rect = new Rect(
                   x - _crossAxisOffset,
                   y - _pixelOffset,
                   width,
                   height);
                child.Arrange(rect);
                u += childSize.U;
                AddToAverageItemSize(childSize.V);

                if (rect.Bottom >= _takenSpace)
                {
                    _takenSpace = rect.Bottom;
                }
                if (rect.Y >= _availableSpace.Height)
                {
                    ++_canBeRemoved;
                }
            }
            _lineLengths.Add(u);
        }
        protected override void ChildrenChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            base.ChildrenChanged(sender, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Control control in e.NewItems)
                    {
                        UpdateAdd(control);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (Control control in e.OldItems)
                    {
                        UpdateRemove(control);
                    }
                    break;
            }
        }


        private void UpdateAdd(Control child)
        {
            var bounds = Bounds;
            var gap = 0;

            child.Measure(_availableSpace);
            //     ++_averageCount;
            var height = child.DesiredSize.Height;
            var width = child.DesiredSize.Width;

            if (Orientation == Orientation.Horizontal)
            {
                if (_takenLineSpace + width > _availableSpace.Width)
                {
                    _takenSpace += height + gap;
                    _takenLineSpace = width;
                    _lineLengths.Add(width);
                }
                else
                {
                    _takenLineSpace += width;
                    if (_lineLengths.Count == 0) _lineLengths.Add(0);
                    _lineLengths[_lineLengths.Count - 1] += width;
                }
                AddToAverageItemSize(height);
            }
            else
            {
                if (_takenLineSpace + height > _availableSpace.Height)
                {
                    _takenSpace += width + gap;
                    _takenLineSpace = height;
                }
                else
                {
                    _takenLineSpace += height;
                }
                AddToAverageItemSize(width);
            }
        }

        private void UpdateRemove(Control child)
        {
            var bounds = Bounds;
            var gap = 0;

            var height = child.DesiredSize.Height;
            var width = child.DesiredSize.Width;

            if (Orientation == Orientation.Horizontal)
            {
                if (_takenLineSpace - width <= 0)
                {
                    _takenSpace -= height + gap;
                    _lineLengths.RemoveAt(_lineLengths.Count - 1);
                    _takenLineSpace = _lineLengths.Count > 0 ? _lineLengths.Last() : 0;
                }
                else
                {
                    _takenLineSpace -= width;
                }
                AddToAverageItemSize(height);
            }
            else
            {
                if (_takenLineSpace - height <= 0)
                {
                    _takenSpace -= width + gap;
                    _takenLineSpace = _availableSpace.Height;
                }
                else
                {
                    _takenLineSpace -= height;
                }
                AddToAverageItemSize(width);
            }

            if (_canBeRemoved > 0)
            {
                --_canBeRemoved;
            }
        }

        private void AddToAverageItemSize(double value)
        {
            ++_averageCount;
            _averageItemSize += (value - _averageItemSize) / _averageCount;
        }

        private void RemoveFromAverageItemSize(double value)
        {
            _averageItemSize = ((_averageItemSize * _averageCount) - value) / (_averageCount - 1);
            --_averageCount;
        }

        private UVSize CreateUVSize(Size size) => new UVSize(Orientation, size);

        private UVSize CreateUVSize() => new UVSize(Orientation);

        /// <summary>
        /// Used to not not write sepearate code for horizontal and vertical orientation.
        /// U is direction in line. (x if orientation is horizontal)
        /// V is direction of lines. (y if orientation is horizonral)
        /// </summary>
        [DebuggerDisplay("U = {U} V = {V}")]
        private struct UVSize
        {
            private readonly Orientation _orientation;

            internal double U;

            internal double V;

            private UVSize(Orientation orientation, double width, double height)
            {
                U = V = 0d;
                _orientation = orientation;
                Width = width;
                Height = height;
            }

            internal UVSize(Orientation orientation, Size size)
                : this(orientation, size.Width, size.Height)
            {
            }

            internal UVSize(Orientation orientation)
            {
                U = V = 0d;
                _orientation = orientation;
            }

            private double Width
            {
                get { return (_orientation == Orientation.Horizontal ? U : V); }
                set
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        U = value;
                    }
                    else
                    {
                        V = value;
                    }
                }
            }

            private double Height
            {
                get { return (_orientation == Orientation.Horizontal ? V : U); }
                set
                {
                    if (_orientation == Orientation.Horizontal)
                    {
                        V = value;
                    }
                    else
                    {
                        U = value;
                    }
                }
            }

            public Size ToSize()
            {
                return new Size(Width, Height);
            }
        }
    }
}