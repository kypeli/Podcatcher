using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Podcatcher
{
	public partial class PodcastPivotItemHeader : UserControl
	{
		public PodcastPivotItemHeader()
		{
			// Required to initialize variables
			InitializeComponent();
		}

        private String _headerText;
        public String HeaderText
        {
            get
            {
                return _headerText;
            }

            set
            {
                _headerText = value;
                this.PivotHeader.Text = _headerText;
            }
        }

        private String _altText;
        public String AltText
        {
            get 
            {
                return _altText;
            }

            set
            {
                _altText = value;
                this.PivotAltLine.Text = _altText;
            }
        }

    }
}