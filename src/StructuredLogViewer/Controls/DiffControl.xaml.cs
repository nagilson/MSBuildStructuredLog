using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Microsoft.Build.Logging.StructuredLogger;

namespace StructuredLogViewer.Controls
{
    public partial class DiffControl : UserControl
    {

        public DiffControl()
        {
            this.DataContext = this;
            InitializeComponent();
            ComputeAndDraw();
        }

        public BuildControl BuildControl { get; set; }
        
        private void ComputeAndDraw()
        {
            Draw();
        }

        private void Draw()
        {
           
        }
    }
}
