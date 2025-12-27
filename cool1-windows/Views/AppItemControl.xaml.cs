using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cool1Windows.Models;
using Cool1Windows.ViewModels;

namespace Cool1Windows.Views
{
    using WPoint = System.Windows.Point;
    using WDragEventArgs = System.Windows.DragEventArgs;
    using WMouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
    using WMouseEventArgs = System.Windows.Input.MouseEventArgs;
    using WDragDropEffects = System.Windows.DragDropEffects;

    public partial class AppItemControl : System.Windows.Controls.UserControl
    {
        private WPoint _startPoint;
        private bool _isReadyToDrag;

        public AppItemControl()
        {
            InitializeComponent();
        }

        private void Border_PreviewMouseLeftButtonDown(object sender, WMouseButtonEventArgs e)
        {
            // Only drag if clicking the border itself or its non-button children
            // Check if we are clicking on a button - if so, don't start drag
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            while (dep != null && dep != this)
            {
                if (dep is System.Windows.Controls.Button) return;
                dep = VisualTreeHelper.GetParent(dep);
            }

            var window = System.Windows.Window.GetWindow(this);
            if (window != null && window.DataContext is MainViewModel vm && 
                vm.IsEditMode && vm.SortMode == "手动排序")
            {
                _isReadyToDrag = true;
                _startPoint = e.GetPosition(null);
            }
        }

        private void Border_MouseLeftButtonUp(object sender, WMouseButtonEventArgs e)
        {
            _isReadyToDrag = false;
        }

        private void Border_MouseMove(object sender, WMouseEventArgs e)
        {
            if (_isReadyToDrag && e.LeftButton == MouseButtonState.Pressed)
            {
                WPoint mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    if (DataContext is AppInfo appInfo)
                    {
                        _isReadyToDrag = false;
                        System.Windows.DataObject dragData = new System.Windows.DataObject("AppInfoSource", appInfo);
                        try
                        {
                            System.Windows.DragDrop.DoDragDrop(this, dragData, WDragDropEffects.Move);
                        }
                        catch { }
                    }
                }
            }
        }

        private void Border_DragOver(object sender, WDragEventArgs e)
        {
            if (e.Data.GetDataPresent("AppInfoSource"))
            {
                e.Effects = WDragDropEffects.Move;
                
                // Show drop indicator
                WPoint pos = e.GetPosition(this.MainBorder);
                if (pos.Y < this.MainBorder.ActualHeight / 2)
                {
                    TopIndicator.Visibility = Visibility.Visible;
                    BottomIndicator.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TopIndicator.Visibility = Visibility.Collapsed;
                    BottomIndicator.Visibility = Visibility.Visible;
                }
                
                e.Handled = true;
            }
        }

        private void Border_DragLeave(object sender, WDragEventArgs e)
        {
            TopIndicator.Visibility = Visibility.Collapsed;
            BottomIndicator.Visibility = Visibility.Collapsed;
        }

        private void Border_Drop(object sender, WDragEventArgs e)
        {
            _isReadyToDrag = false;
            TopIndicator.Visibility = Visibility.Collapsed;
            BottomIndicator.Visibility = Visibility.Collapsed;

            if (e.Data.GetDataPresent("AppInfoSource"))
            {
                var source = e.Data.GetData("AppInfoSource") as AppInfo;
                var target = DataContext as AppInfo;

                if (source != null && target != null && source != target)
                {
                    WPoint pos = e.GetPosition(this.MainBorder);
                    bool isTop = pos.Y < this.MainBorder.ActualHeight / 2;

                    var window = System.Windows.Window.GetWindow(this);
                    if (window != null && window.DataContext is MainViewModel vm)
                    {
                        vm.ReorderItemToPosition(source, target, isTop);
                    }
                }
                e.Handled = true;
            }
        }
    }
}
