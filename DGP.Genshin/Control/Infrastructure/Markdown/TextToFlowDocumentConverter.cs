﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace DGP.Genshin.Control.Infrastructure.Markdown
{
    public class TextToFlowDocumentConverter : DependencyObject, IValueConverter
    {
        public Markdown? Markdown
        {
            get => (Markdown)GetValue(MarkdownProperty);

            set => SetValue(MarkdownProperty, value);
        }
        public static readonly DependencyProperty MarkdownProperty =
            DependencyProperty.Register(nameof(Markdown), typeof(Markdown), typeof(TextToFlowDocumentConverter));

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is null)
            {
                return null;
            }

            string text = (string)value;

            Markdown engine = Markdown ?? mMarkdown.Value;

            return engine.Transform(text);
        }

        /// <summary>
        /// Converts a value. 
        /// </summary>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        private readonly Lazy<Markdown> mMarkdown
            = new(() => new Markdown());
    }
}
