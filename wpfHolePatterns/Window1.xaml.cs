using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using GeoPatterns;


namespace wpfHolePattern
{

    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window, INotifyPropertyChanged
    {
        private double _ZoomScale = 20;
        private HolePattern _ActivePattern;

        public ObservableCollection<HolePattern> HolePatterns { get; set; }
        //public HolePattern HolePattern1 { get; set; }
        //public HolePattern HolePattern2 { get; set; }

        // Canvas sketch related fields
        public double SketchX0 { get; set; }
        public double SketchY0 { get; set; }
        public double SketchWidth { get; set; }
        public double SketchHeight { get; set; }
        public bool ShowLabels {get; set; }
        
        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        public HolePattern ActivePattern
        {
            get
            {
                return _ActivePattern;
            }
            set
            {
                if (_ActivePattern != null)
                {
                    _ActivePattern.CurrentHole = null;
                }
                _ActivePattern = value;
                NotifyPropertyChanged("ActivePattern");
            }
        }

        public double ZoomScale
        {
            get
            {
                return _ZoomScale;
            }
            set
            {
                _ZoomScale = value;
                this.RedrawScreen();
                //var aScTrans = new ScaleTransform(value, value,SketchX0,SketchY0);
                //this.canvas1.RenderTransform = aScTrans;
            }
        }


        public Window1()
        {
            this.PropertyChanged += new PropertyChangedEventHandler(Window1_PropertyChanged);

            HolePatterns = new ObservableCollection<HolePattern>();
            ShowLabels = true;
            InitializeComponent();

            //-- Add some sample patterns
            CircularHolePattern SamplePattern = CreateNewCircularPattern();
            SamplePattern.BoltCirDia = 20.0;
            SamplePattern.HoleCount = 6;
            SamplePattern.StartAngle = 0;
            SamplePattern.HoleType = Hole.HoleTypes.Tapped;

            //-- Add some sample patterns
            SamplePattern = CreateNewCircularPattern();
            SamplePattern.BoltCirDia = 10.0;
            SamplePattern.HoleCount = 6;
            SamplePattern.StartAngle = 0;
            SamplePattern.HoleType = Hole.HoleTypes.CounterBored;

            SetActivePattern(HolePatterns[0]);

            // Setup bindings between UI controls and this class
            // (Move this binding to the XAML) <bg>
            //lvHolePatterns.DataContext = this;
            //lvHolePatterns.ItemsSource = HolePatterns;

            // Set certain UI controls to point to contexts here in the code-behind
            HoleType1.ItemsSource = Enum.GetValues(typeof(Hole.HoleTypes));

            canvas1.MouseLeftButtonDown += new MouseButtonEventHandler(canvas1_MouseLeftButtonDown);

        }

        void canvas1_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            int ClickMargin = 5;// Adjust here as desired. Span is in both directions of selected point.
            var ClickMarginPointList = new Collection<Point>();
            Point ClickedPoint = e.GetPosition(canvas1);
            Point ClickMarginPoint = new Point();
            for (int x = -1 * ClickMargin; x <= ClickMargin; x++)
            {
                for (int y = -1 * ClickMargin; y <= ClickMargin; y++)
                {
                    ClickMarginPoint.X = ClickedPoint.X + x;
                    ClickMarginPoint.Y = ClickedPoint.Y + y;
                    ClickMarginPointList.Add(ClickMarginPoint);
                }
            }

            foreach (Point p in ClickMarginPointList)
            {
                HitTestResult SelectedCanvasItem = System.Windows.Media.VisualTreeHelper.HitTest(canvas1, p);
                if (SelectedCanvasItem.VisualHit.GetType().BaseType == typeof(Shape))
                {
                    var SelectedShapeTag = SelectedCanvasItem.VisualHit.GetValue(Shape.TagProperty);
                    if (SelectedShapeTag != null && SelectedShapeTag.GetType().BaseType == typeof(Hole))
                    {
                        Hole SelectedHole = (Hole)SelectedShapeTag;
                        SetActivePattern(SelectedHole.ParentPattern);
                        SelectedHole.ParentPattern.CurrentHole = SelectedHole;
                        return; //Get out, we're done.
                    }
                }
            }
        }

        void Window1_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActivePattern")
            {
                //HolePatternStackPanel.DataContext = ActivePattern;  Binding moved to XAML
                //CoordinateGrid1.ItemsSource = ActivePattern != null ? ActivePattern.HoleList : null;
                this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new InvokeDelegate(SetFocusOnPatternName));
                //this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new InvokeDelegate({txtPatternName.Focus();});
                //var a =new Delegate(){txtPatternName.Focus();}
                RedrawScreen();
            }
        }




        private delegate void InvokeDelegate();
        private void SetFocusOnPatternName()
        {
            //txtPatternName.Focus();
        }

        // Override the OnRender method, so that I can draw on the canvas
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext); //Call the default method (I think that's what this does)

            // Update Canvas properties based on current canvas size
            SketchWidth = canvas1.ActualWidth;
            SketchHeight = canvas1.ActualHeight;
            SketchX0 = SketchWidth / 2;
            SketchY0 = SketchHeight / 2;

            RedrawScreen();
        }

        void HolePattern_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            HolePattern SendingPattern = (HolePattern)sender;
            if (SendingPattern != ActivePattern)
            {
                SetActivePattern(SendingPattern);
            }
            else
            {
                RedrawScreen();
            }
        }

        void HolePattern_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BoltCirDia" || e.PropertyName == "CurrentHole")
            {
                RedrawScreen();
            }
        }

        public void DrawGridLines()
        {
            int LineSpacing = 20;
            double LineWidth = 0.5;
            Brush LineColor = Brushes.Gray;

            //-- Vertical Grid Lines (work from Center to Left, then Center to Right)
            for (double x = 0; x <= SketchX0; x += LineSpacing)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Line VerticalGraphLine = new Line();
                    VerticalGraphLine.X1 = SketchX0 + x * y;
                    VerticalGraphLine.Y1 = 0;
                    VerticalGraphLine.X2 = VerticalGraphLine.X1;
                    VerticalGraphLine.Y2 = SketchY0 * 2;
                    VerticalGraphLine.Stroke = LineColor;
                    VerticalGraphLine.StrokeThickness = LineWidth;
                    canvas1.Children.Add(VerticalGraphLine);
                }
            }

            //-- Horizontal Grid Lines (work from Center Up, then Center down)
            for (double x = 0; x <= SketchY0; x += LineSpacing)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    Line HorizontalGraphLine = new Line();
                    HorizontalGraphLine.X1 = 0;
                    HorizontalGraphLine.Y1 = SketchY0 + x * y;
                    HorizontalGraphLine.X2 = SketchX0 * 2;
                    HorizontalGraphLine.Y2 = HorizontalGraphLine.Y1;
                    HorizontalGraphLine.Stroke = LineColor;
                    HorizontalGraphLine.StrokeThickness = LineWidth;
                    canvas1.Children.Add(HorizontalGraphLine);
                }
            }

        }

        public void DrawBoltCircles()
        {
            foreach (HolePattern Pattern in HolePatterns)
            {
                //if (HolePattern.SketchBoltCirRad > 0)
                //{
                if (Pattern.GetType() == typeof(CircularHolePattern))
                {
                    Ellipse SketchBoltCircle = new Ellipse();
                    SketchBoltCircle.Width = ((CircularHolePattern)Pattern).BoltCirDia * ZoomScale;
                    SketchBoltCircle.Height = SketchBoltCircle.Width;
                    var StrokeColor = Pattern == ActivePattern ? Brushes.Red : Brushes.Gray;
                    SketchBoltCircle.Stroke = StrokeColor;
                    SketchBoltCircle.Margin = new Thickness(SketchX0 + Pattern.StartX * ZoomScale - SketchBoltCircle.Width / 2,
                                                            SketchY0 - Pattern.StartY * ZoomScale - SketchBoltCircle.Width / 2,
                                                            0, 0);
                    SketchBoltCircle.StrokeDashArray = LinePatterns.CenterLinePattern(1);
                    canvas1.Children.Add(SketchBoltCircle);
                }
                //}

            }
            // Draw Horizontal line across graph
            Line HorizontalCL = new Line();
            HorizontalCL.X1 = 10;
            HorizontalCL.Y1 = SketchY0;
            HorizontalCL.X2 = SketchX0 * 2 - 10;
            HorizontalCL.Y2 = SketchY0;
            HorizontalCL.Stroke = Brushes.Black;
            HorizontalCL.StrokeThickness = 1;
            canvas1.Children.Add(HorizontalCL);

            // Draw vertical line across graph
            Line VerticalCL = new Line();
            VerticalCL.X1 = SketchX0;
            VerticalCL.Y1 = 10;
            VerticalCL.X2 = SketchX0;
            VerticalCL.Y2 = SketchY0 * 2 - 10;
            VerticalCL.Stroke = Brushes.Black;
            VerticalCL.StrokeThickness = 1;
            canvas1.Children.Add(VerticalCL);

        }

        public void DrawHoles()
        {
            // Iterate over each HolePattern in the HolePatterns collection... 
            foreach (HolePattern HolePattern in HolePatterns)
            {
                // Now iterate over each Hole in the HoleList of the current HolePattern...
                // This code adds the HoleEntity, HoleDecorator, and HoleLabel to the canvas
                // The method HolePattern.UpdateHoles() must be called ahead of this to set the sketch positions for these objects
                // Canvas clearing should be handled outside this method as needed.
                foreach (Hole Hole in HolePattern.HoleList)
                {

                    Hole.CanvasX = SketchX0 + (Hole.AbsX * _ZoomScale);
                    Hole.CanvasY = SketchY0 - (Hole.AbsY * _ZoomScale);
                    Hole.HoleEntity.Style = (Style)this.FindResource("EllipseStyle");
                    canvas1.Children.Add(Hole.HoleEntity);
                    canvas1.Children.Add(Hole.HoleDecorator);
                    if (this.ShowLabels == true){ canvas1.Children.Add(Hole.HoleLabel);}
                }
            }
        }

        private void RedrawScreen()
        {
            canvas1.Children.Clear();
            //UpdateSketchBoltCircles(); 
            DrawGridLines();
            DrawBoltCircles();
            DrawHoles();

        }

        private void UpdateSketchBoltCircles()
        {
            if (HolePatterns.Count == 0)
            {
                return;
            }

            // determine how big the sketch bolt cicrle can be on the canvas
            double MaxSketchBoltCirRad = (SketchWidth > SketchHeight ? SketchY0 : SketchX0) * 0.95;

            // Determine the largest bolt circle dia in the HolePattern collection
            // Will use this below in a ratio fashion to determine SketchBoltCirRad for each HolePattern
            //double MaxBoltCirDiaInHolePatternCollection = (from a in HolePatterns
            //select a.BoltCirDia).Max();

            double MaxBoltCirDiaInHolePatternCollection = 50;


            //-- Update HolePatterns with new Canvas sketch values
            foreach (CircularHolePattern HolePattern in HolePatterns)
            {
                if (HolePattern.BoltCirDia > 0)
                {
                    HolePattern.SketchBoltCirRad = HolePattern.BoltCirDia / MaxBoltCirDiaInHolePatternCollection * MaxSketchBoltCirRad;
                }
                else
                {
                    HolePattern.SketchBoltCirRad = 0;
                }

                HolePattern.SketchX0 = SketchX0;
                HolePattern.SketchY0 = SketchY0;
            }
        }

        private void ToggleHoleVisiblity(Hole SelectedHole)
        {
            SelectedHole.Visible = (SelectedHole.Visible) ? false : true;
            UpdateSketchBoltCircles();
        }

        private void SetActivePattern(HolePattern NewActivePattern)
        {
            ActivePattern = NewActivePattern;
            //txtPatternName.Focus();
            //--- Moved all this to event hanlder on ActivePattern property
            //HolePatternStackPanel.DataContext = ActivePattern;
            //CoordinateGrid1.ItemsSource = NewActivePattern != null ? ActivePattern.HoleList : null;
            //RedrawScreen();
        }

        private void DeletePattern(HolePattern HolePatternToRemove)
        {
            HolePatterns.Remove(HolePatternToRemove);
            //RedrawScreen();
        }

        private CircularHolePattern CreateNewCircularPattern()
        {
            var CreatedPattern = CreateNewPattern(typeof(CircularHolePattern));
            return (CircularHolePattern)CreatedPattern;

        }

        private SingleLineHolePattern CreateNewLinePattern()
        {
            var CreatedPattern = CreateNewPattern(typeof(SingleLineHolePattern));
            return (SingleLineHolePattern)CreatedPattern;

        }

        private HolePattern CreateNewPattern(Type PatternTypeToCreate)
        {
            var NewHolePattern = (HolePattern)Activator.CreateInstance(PatternTypeToCreate);
            NewHolePattern.PatternName = NewHolePattern.PatternShortDescription; // + " "Pattern #" + (HolePatterns.Count + 1).ToString();
            this.AddPattern(NewHolePattern);
            this.SetActivePattern(NewHolePattern);
            return NewHolePattern;
        }



        private void AddPattern(HolePattern NewHolePattern)
        {
            HolePatterns.Add(NewHolePattern);
            NewHolePattern.PropertyChanged += new PropertyChangedEventHandler(HolePattern_PropertyChanged);
            NewHolePattern.CollectionChanged += new NotifyCollectionChangedEventHandler(HolePattern_CollectionChanged);
        }

        private void btnDraw_Click(object sender, RoutedEventArgs e)
        {
            RedrawScreen();
        }

        private void btnHoleSizeDecrease_Click(object sender, RoutedEventArgs e)
        {
            ActivePattern.ChangeHoleSketchSize(-1);
        }

        private void btnHoleSizeIncrease_Click(object sender, RoutedEventArgs e)
        {
            ActivePattern.ChangeHoleSketchSize(1);
        }

        private void CoordinateGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var lvRef = (ListView)sender;
            Hole SelectedHole = (Hole)lvRef.SelectedItem;
            ToggleHoleVisiblity(SelectedHole);
        }

        //http://www.madprops.org/blog/enter-to-tab-in-wpf/
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            var uie = e.OriginalSource as UIElement;
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                uie.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            }
        }

        private void TextBox_PreviewGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ((TextBox)sender).SelectAll();
        }

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            PrintDialog prtDlg = new PrintDialog();
            if (prtDlg.ShowDialog() == true)
            {
                prtDlg.PrintVisual(winBoltCircle, "Bolt Hole Patterns");
            }
        }

        #region ****** Stuff that has been moved to XAML binding  *****
        ////-- Note this behavior has been refactored to XAML binding
        //private void CoordinateGrid1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //var lvRef = (ListView)sender;
        //    //HolePattern1.CurrentHole = (HolePattern.Hole)lvRef.SelectedItem;
        //    ActivePattern.CurrentHole = (Hole)((ListView)sender).SelectedItem;
        //    RedrawScreen();
        //}

        //private void CoordinateGrid1_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    //HolePattern1.CurrentHole = null;
        //    //RedrawScreen();
        //}

        //private void CoordinateGrid1_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    CoordinateGrid1_SelectionChanged(sender, null);
        //}

        //private void lvHolePatterns_GotFocus(object sender, RoutedEventArgs e)
        //{
        //    //var a=((ListView)sender).SelectedItem;
        //    //SetActivePattern((HolePattern)lvHolePatterns.SelectedItem);
        //    //txtPatternName.Focus();
        //}

        //private void lvHolePatterns_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    //ListView lvSender = (ListView)sender;
        //    //SetActivePattern((HolePattern)lvSender.SelectedItem);
        //    //this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new focus(SetActivePattern),
        //    //                          (HolePattern)lvSender.SelectedItem);
        //    //this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new focus(SetFocusOnPatternName), null);
        //} 
        #endregion

        private void btnAddCircularPattern_Click(object sender, RoutedEventArgs e)
        {
            CreateNewCircularPattern();
        }

        private void btnAddLinePattern_Click(object sender, RoutedEventArgs e)
        {
            CreateNewLinePattern();
        }

        private void btnHolePatternDelete_Click(object sender, RoutedEventArgs e)
        {
            var PatternToDelete = (HolePattern)lvHolePatterns.SelectedItem;
            DeletePattern(PatternToDelete);
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            RedrawScreen();
        }

    }

    [ValueConversion(typeof(object), typeof(string))]
    public class FormattingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string formatString = parameter as string;
            if (formatString != null)
            {
                return string.Format(culture, formatString, value);
            }
            else
            {
                return value.ToString();
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // we don't intend this to ever be called
            return null;
        }
    }

    [TemplatePartAttribute(Name = "PART_SpinnerUp", Type = typeof(Button))]
    [TemplatePartAttribute(Name = "PART_SpinnerDown", Type = typeof(Button))]
    public class cdsSpinner : TextBox
    {

        //Sample XAML usage:
        //<local:cdsSpinner x:Name="txtHoleCount"
        //                  Text="{Binding Path=HolePattern1.HoleCount, ElementName=winBoltCircle, UpdateSourceTrigger=PropertyChanged}"
        //                  Style="{StaticResource cdsSpinnerStyle}" >

        private Button _SpinUp;
        private Button _SpinDown;

        public cdsSpinner()
        {
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            _SpinUp = (Button)Template.FindName("PART_SpinnerUp", this);
            if (_SpinUp != null)
                _SpinUp.Click += new RoutedEventHandler(_SpinUp_Click);
            _SpinDown = (Button)Template.FindName("PART_SpinnerDown", this);
            if (_SpinDown != null)
                _SpinDown.Click += new RoutedEventHandler(_SpinDown_Click);
        }

        void _SpinUp_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int val;
            int.TryParse(Text, out val);
            val += 1;
            Text = val.ToString();
        }

        void _SpinDown_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            int val;
            int.TryParse(Text, out val);
            val -= 1;
            Text = val.ToString();
        }

    }


}
