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
using StructuredLogger.Analyzers.Diff;

namespace StructuredLogViewer.Controls
{
    public partial class DiffControl : UserControl
    {
        private DiffModel diffModelReference;

        public DiffControl()
        {
            InitializeComponent();
            this.DataContext = this;
            ComputeAndDraw();
        }

        public BuildControl BuildControl { get; set; }

        public void PopulateDiff(DiffModel model, List<Tuple<string, string>> diffs)
        {
            diffModelReference = model;
            if (diffModelReference == null)
            {
                return;
            }

            foreach (var diff in diffs)
            {
                DiffViewer diffUI = new();
                Expander expanderContainer = new();

                var diffContentA = diff.Item1;
                var diffContentB = diff.Item2;
                diffUI.NewTextHeader = model.difference.binlogAName;
                diffUI.OldTextHeader = model.difference.binlogBName;
                diffUI.OldText = diffContentA;
                diffUI.NewText = diffContentB;

                diffUI.IgnoreCase = true;
                diffUI.IgnoreWhiteSpace = true;
                diffUI.SplitterWidth = 0;
                diffUI.Foreground = SettingsService.UseDarkTheme ? Brushes.White : Brushes.Black;
                diffUI.UnchangedForeground = SettingsService.UseDarkTheme ? Brushes.White : Brushes.Black;
                diffUI.HeaderForeground = SettingsService.UseDarkTheme ? Brushes.White : Brushes.Black;
                diffUI.DeletedForeground = SettingsService.UseDarkTheme ? Brushes.White : Brushes.Black;
                diffUI.InsertedForeground = SettingsService.UseDarkTheme ? Brushes.White : Brushes.Black;
                expanderContainer.Header = diffContentA.Substring(0, diffContentA.IndexOf(Environment.NewLine));
                expanderContainer.Content = diffUI;

                diffsView.Children.Add(expanderContainer);
            }
        }

        private void OnFilterModeChange(object sender, EventArgs e)
        {
            if (diffModelReference == null)
            {
                return;
            }

            diffsView.Children.RemoveRange(1, diffsView.Children.Count - 1);
            PopulateDiff(diffModelReference, new DiffPlexDiffDataAdapter((bool)useFilter.IsChecked).Adapt(diffModelReference.difference));
        }

        private void ComputeAndDraw()
        {
            Draw();
        }

        private void Draw()
        {

        }
    }
}
