﻿using System;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace DGP.Genshin.Control.Infrastructure
{
    /// <summary>
    /// 自适应宽度网格
    /// 会将栏数设为最接近设定栏宽的
    /// </summary>
    public sealed class DesiredWidthUniformGrid : UniformGrid
    {
        public double ColumnDesiredWidth
        {
            get => (double)GetValue(ColumnDesiredWidthProperty);

            set => SetValue(ColumnDesiredWidthProperty, value);
        }
        public static readonly DependencyProperty ColumnDesiredWidthProperty =
            DependencyProperty.Register(nameof(ColumnDesiredWidth), typeof(double), typeof(DesiredWidthUniformGrid),
                new PropertyMetadata(0D, OnColumnDesiredWidthChanged));

        private static void OnColumnDesiredWidthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((DesiredWidthUniformGrid)d).SetCorrectColumn();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            SetCorrectColumn();
            base.OnRenderSizeChanged(sizeInfo);
        }

        private void SetCorrectColumn()
        {
            if (ColumnDesiredWidth > 0)
            {
                Columns = (int)Math.Round(ActualWidth / ColumnDesiredWidth);
            }
        }
    }
}
