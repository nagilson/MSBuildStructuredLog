using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using DiffPlex.DiffBuilder;
using DiffPlex.Wpf.Controls;
using Microsoft.Build.Logging.StructuredLogger;


namespace StructuredLogViewer.Controls
{
    public partial class DiffControl : UserControl
    {
        public ObservableCollection<SideBySideDiffViewer> DifferencesViewers { get; set; }

        public DiffControl()
        {
            this.DifferencesViewers = new ObservableCollection<SideBySideDiffViewer>();
            InitializeComponent();
            this.DataContext = this;
            ComputeAndDraw();
        }

        public BuildControl BuildControl { get; set; }
        
        private void ComputeAndDraw()
        {
            Draw();
        }

        private void Draw()
        {

            soloDiff.OldText = "old";
            soloDiff.NewText = "new";

            DiffPlex.Wpf.Controls.SideBySideDiffViewer differ = new SideBySideDiffViewer();
            differ.SetDiffModel("old", "new");
            DifferencesViewers.Add(differ);
            DifferencesViewers.Add(differ);

            Console.WriteLine("");
        }
    }
}
