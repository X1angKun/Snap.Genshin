﻿using ModernWpf.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace DGP.Genshin.Control.Infrastructure.CachedImage
{
    [TypeConverter(typeof(IconElementConverter))]
    public abstract class CachedIconElementBase : FrameworkElement
    {
        private protected CachedIconElementBase()
        {
        }

        #region Foreground

        /// <summary>
        /// Identifies the Foreground dependency property.
        /// </summary>
        public static readonly DependencyProperty ForegroundProperty =
                TextElement.ForegroundProperty.AddOwner(
                        typeof(CachedIconElementBase),
                        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush,
                            FrameworkPropertyMetadataOptions.Inherits,
                            OnForegroundPropertyChanged));

        private static void OnForegroundPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((CachedIconElementBase)sender).OnForegroundPropertyChanged(args);
        }

        private void OnForegroundPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
            BaseValueSource baseValueSource = DependencyPropertyHelper.GetValueSource(this, args.Property).BaseValueSource;
            _isForegroundDefaultOrInherited = baseValueSource <= BaseValueSource.Inherited;
            UpdateShouldInheritForegroundFromVisualParent();
        }

        /// <summary>
        /// Gets or sets a brush that describes the foreground color.
        /// </summary>
        /// <returns>
        /// The brush that paints the foreground of the control.
        /// </returns>
        [Bindable(true), Category("Appearance")]
        public Brush Foreground
        {
            get => (Brush)GetValue(ForegroundProperty);

            set => SetValue(ForegroundProperty, value);
        }

        #endregion

        #region VisualParentForeground

        private static readonly DependencyProperty VisualParentForegroundProperty =
            DependencyProperty.Register(
                nameof(VisualParentForeground),
                typeof(Brush),
                typeof(CachedIconElementBase),
                new PropertyMetadata(null, OnVisualParentForegroundPropertyChanged));

        private protected Brush VisualParentForeground
        {
            get => (Brush)GetValue(VisualParentForegroundProperty);

            set => SetValue(VisualParentForegroundProperty, value);
        }

        private static void OnVisualParentForegroundPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((CachedIconElementBase)sender).OnVisualParentForegroundPropertyChanged(args);
        }

        private protected virtual void OnVisualParentForegroundPropertyChanged(DependencyPropertyChangedEventArgs args)
        {
        }

        #endregion

        private protected bool ShouldInheritForegroundFromVisualParent
        {
            get => _shouldInheritForegroundFromVisualParent;

            private set
            {
                if (_shouldInheritForegroundFromVisualParent != value)
                {
                    _shouldInheritForegroundFromVisualParent = value;

                    if (_shouldInheritForegroundFromVisualParent)
                    {
                        SetBinding(VisualParentForegroundProperty,
                            new Binding
                            {
                                Path = new PropertyPath(TextElement.ForegroundProperty),
                                Source = VisualParent
                            });
                    }
                    else
                    {
                        ClearValue(VisualParentForegroundProperty);
                    }

                    OnShouldInheritForegroundFromVisualParentChanged();
                }
            }
        }

        private protected virtual void OnShouldInheritForegroundFromVisualParentChanged()
        {
        }

        private void UpdateShouldInheritForegroundFromVisualParent()
        {
            ShouldInheritForegroundFromVisualParent =
                _isForegroundDefaultOrInherited &&
                Parent != null &&
                VisualParent != null &&
                Parent != VisualParent;
        }

        private protected UIElementCollection Children
        {
            get
            {
                EnsureLayoutRoot();
                Debug.Assert(_layoutRoot is not null);
                return _layoutRoot.Children;
            }
        }

        private protected abstract void InitializeChildren();

        protected override int VisualChildrenCount
        {
            get => 1;
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index == 0)
            {
                EnsureLayoutRoot();
                Debug.Assert(_layoutRoot is not null);
                return _layoutRoot;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            EnsureLayoutRoot();
            Debug.Assert(_layoutRoot is not null);
            _layoutRoot.Measure(availableSize);
            return _layoutRoot.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            EnsureLayoutRoot();
            Debug.Assert(_layoutRoot is not null);
            _layoutRoot.Arrange(new Rect(new Point(), finalSize));
            return finalSize;
        }

        protected override void OnVisualParentChanged(DependencyObject oldParent)
        {
            base.OnVisualParentChanged(oldParent);
            UpdateShouldInheritForegroundFromVisualParent();
        }

        private void EnsureLayoutRoot()
        {
            if (_layoutRoot != null)
            {
                return;
            }

            _layoutRoot = new Grid
            {
                Background = Brushes.Transparent,
                SnapsToDevicePixels = true,
            };
            InitializeChildren();

            AddVisualChild(_layoutRoot);
        }

        private Grid? _layoutRoot;
        private bool _isForegroundDefaultOrInherited = true;
        private bool _shouldInheritForegroundFromVisualParent;
    }
}
