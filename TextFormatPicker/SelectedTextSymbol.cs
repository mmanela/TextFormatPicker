using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using Microsoft.VisualStudio.Text.Classification;

namespace MattManela.TextFormatPicker
{
    public class SelectedTextSymbol : INotifyPropertyChanged
    {
        private bool italic;
        private Color foregroundColor;
        private Color backgroundColor;
        private bool bold;

        [DisplayName("Symbol")]
        [Description("The selected symbol")]
        public string Symbol { get; internal set; }

        [DisplayName("Classification Name")]
        [Description("The name of clasification that determined this symbols color")]
        public string ClassificationName { get; internal set; }


        [DisplayName("Foreground Color")]
        [Description("The foreground color of the text")]
        [Editor(typeof (ColorEditor), typeof (UITypeEditor))]
        public Color ForegroundColorForEditor
        {
            get { return foregroundColor; }
            set
            {
                if (value != foregroundColor)
                {
                    foregroundColor = value;
                    NotifyPropertyChanged("ForegroundColor");
                }
            }
        }

        [DisplayName("Background Color")]
        [Description("The background color of the text")]
        [Editor(typeof (ColorEditor), typeof (UITypeEditor))]
        public Color BackgroundColorForEditor
        {
            get { return backgroundColor; }
            set
            {
                if (value != backgroundColor)
                {
                    backgroundColor = value;
                    NotifyPropertyChanged("BackgroundColor");
                }
            }
        }

        [DisplayName("Bold")]
        [Description("The bold property of the text")]
        public bool Bold
        {
            get { return bold; }
            set
            {
                if (value != bold)
                {
                    bold = value;
                    NotifyPropertyChanged("Bold");
                }
            }
        }

        [DisplayName("Italic")]
        [Description("The italic property of the text")]
        public bool Italic
        {
            get { return italic; }
            set
            {
                if (value != italic)
                {
                    italic = value;
                    NotifyPropertyChanged("Italic");
                }
            }
        }


        [Browsable(false)]
        public System.Windows.Media.Color ForegroundColor
        {
            set { foregroundColor = ToDrawingColor(value); }
            get { return ToWpfColor(foregroundColor); }
        }


        [Browsable(false)]
        public System.Windows.Media.Color BackgroundColor
        {
            set { backgroundColor = ToDrawingColor(value); }
            get { return ToWpfColor(backgroundColor); }
        }

        [Browsable(false)]
        public IClassificationType ClassificationType { get; set; }

        [Browsable(false)]
        public IClassificationFormatMap ClassificationMap { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        private Color ToDrawingColor(System.Windows.Media.Color wpfColor)
        {
            return Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
        }

        private System.Windows.Media.Color ToWpfColor(Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }
    }
}