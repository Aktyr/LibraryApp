using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;

namespace LibraryApp.WPF_CustomControls
{
    public static class FieldHighlighter
    {
        private static readonly Brush ErrorBrush = Brushes.Red;
        private static readonly Thickness ErrorThickness = new Thickness(2);

        public static void HighlightField(Control control)
        {
            if (control != null)
            {
                control.BorderBrush = ErrorBrush;
                control.BorderThickness = ErrorThickness;
            }
        }

        public static void HighlightFields(params Control[] controls)
        {
            foreach (var control in controls)
            {
                HighlightField(control);
            }
        }

        public static void ResetFieldColor(Control control)
        {
            if (control != null)
            {
                control.ClearValue(Control.BorderBrushProperty);
                control.ClearValue(Control.BorderThicknessProperty);
            }
        }

        public static void ResetAllFields(params Control[] controls)
        {
            foreach (var control in controls)
            {
                ResetFieldColor(control);
            }
        }

        public static void ResetAllFields(IEnumerable<Control> controls)
        {
            foreach (var control in controls)
            {
                ResetFieldColor(control);
            }
        }
    }
}