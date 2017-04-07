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


/// <summary>
/// 
/// Hole Patterns program to demonstrate MVVM pattern in C# and WPF canvas desktop top.
/// Author: Matt Slay (https://github.com/mattslay)
/// 
/// </summary>

namespace GeoPatterns
{
     public class LinePatterns
     {
         public static DoubleCollection CenterLinePattern(double LineTypeScaleFactor)
         {
             var PatternDef = new DoubleCollection();
              // Define Bolt Circle Centerline Pattern for Sketch Bolt Circles
              PatternDef.Add(10 * LineTypeScaleFactor);
              PatternDef.Add(5 * LineTypeScaleFactor);
              PatternDef.Add(2 * LineTypeScaleFactor);
              PatternDef.Add(5 * LineTypeScaleFactor);
             return PatternDef;
         }

         //-- Define Hidden Line Pattern for use as outer circle of Tapped Holes
         public static DoubleCollection HiddenLinePattern(double LineTypeScaleFactor)
         {
             var PatternDef = new DoubleCollection();
              PatternDef.Add(5 * LineTypeScaleFactor);
              PatternDef.Add(5 * LineTypeScaleFactor);
             return PatternDef;
         }

     }

     

    
    public partial class HolePattern : INotifyPropertyChanged, INotifyCollectionChanged
    {
        protected Hole.HoleTypes _HoleType;

        protected int _HoleCount;
        protected double _StartAngle;
        protected double _StrokeThickness = 1;
        protected Brush _StrokeColor = Brushes.Black;
        protected double _CurrentHoleStrokeWidth = 2;
        protected Brush _CurrentHoleColor = Brushes.Red;
        protected Brush _CurrentHoleFill = Brushes.Yellow;

        private double _SketchX0;
        private double _SketchY0;
        private double _HoleDia;

        private Hole _CurrentHole;
        private string _PatternShortDescription;
        private string _PatternName;

        public Type HoleClass;
        
        public ObservableCollection<Hole> HoleList { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        // Interface method
        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                UpdateHoles();
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        // Interface method
        public void NotifyCollectionChanged()
        {
            if (CollectionChanged != null)
            {
                CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public HolePattern() //Constructor
        {
            HoleList = new ObservableCollection<Hole>();
            HoleCount = 0;
            HoleDia = 20.0;
            HoleType = Hole.HoleTypes.Drilled;
        }

        virtual public void CreateHoles()
        {
            this.HoleList.Clear();

            if (this.HoleCount > 0 && this.HoleClass!=null && this.HoleClass.BaseType==typeof(Hole))
            {
                for (int x = 1; x <= this.HoleCount; x++)
                {
                    var NewHole = (Hole)Activator.CreateInstance(HoleClass);
                    //NewHole.HoleEntity.MouseLeftButtonDown += new MouseButtonEventHandler(Hole_MouseActivity);
                    NewHole.ParentPattern = this; //Tell the Hole who is Parent HolePattern is
                    NewHole.HoleType = this.HoleType;
                    this.HoleList.Add(NewHole);
                }
            }
            this.UpdateHoles();
        }


        void Hole_MouseActivity(object sender, MouseButtonEventArgs e)
        {
            Ellipse SelectedEllipse = sender as Ellipse;
            CurrentHole = (Hole)SelectedEllipse.Tag;
            NotifyCollectionChanged();
        }

        virtual public void UpdateHoles()
        {
            //CurrentHole = CurrentHole; // To cause it to repaint it
            NotifyCollectionChanged(); //Announce that HoleList collection has changed!

        }

        public string PatternShortDescription
        {
            get
            {
                return _PatternShortDescription;
            }
            set
            {
                _PatternShortDescription = value;
            }
        }

        public string PatternName
        {
            get { return _PatternName; }
            set
            {
                _PatternName = value;
                NotifyPropertyChanged("PatternName");
            }
        }

        public double StartAngle
        {
            get { return _StartAngle; }
            set
            {
                _StartAngle = value;
                NotifyPropertyChanged("StartAngle");
            }
        }

        virtual public int HoleCount
        {
            get { return _HoleCount; }
            set
            {
                if (value >= 0)
                {
                    _HoleCount = value;
                }
                else
                {
                    _HoleCount = 0;
                }
                CreateHoles();
                NotifyPropertyChanged("HoleCount");
            }
        }

        public double HoleDia
        {
            get { return _HoleDia; }
            set
            {
                if (value != _HoleDia)
                {
                    _HoleDia = value;
                    SetHoleSizeOnHoles(_HoleDia);
                    NotifyPropertyChanged("HoleDia");
                }
            }
        }

        private void SetHoleSizeOnHoles(double NewHoleDia)
        {
            foreach (Hole Hole in HoleList)
            {
                Hole.HoleDia = NewHoleDia; 
            }
        }

        public Hole.HoleTypes HoleType
        {
            get { return _HoleType; }
            set
            {
                if (value != _HoleType)
                {
                    _HoleType = value;
                    SetHoleTypeOnHoles(_HoleType);
                    NotifyPropertyChanged("HoleType");
                }
            }
        }

        public double SketchX0
        {
            get
            {
                return _SketchX0;
            }
            set
            {
                if (value != _SketchX0)
                {
                    _SketchX0 = value;
                    NotifyPropertyChanged("SketchX0");
                }
            }
        }

        public double SketchY0
        {
            get
            {
                return _SketchY0;
            }
            set
            {
                if (value != _SketchY0)
                {
                    _SketchY0 = value;
                    NotifyPropertyChanged("SketchY0");
                }
            }
        }

        public Hole CurrentHole
        {
            get
            {
                return _CurrentHole;
            }
            set
            {
                UpdateCurrentHole(value);
                _CurrentHole = value;
                NotifyPropertyChanged("CurrentHole");
            }
        }

        //--- Methods ---------------------------

        public void ChangeHoleCount(int ChangeAmount)
        {
            if (this.HoleCount + ChangeAmount >= 0)
            {
                this.HoleCount += ChangeAmount;
            }

        }

        public void ChangeStartAngle(int ChangeAmount)
        {
            this.StartAngle += ChangeAmount;
        }

        public void ChangeHoleSketchSize(int ChangeAmount)
        {
            if (this.HoleDia + ChangeAmount > 0)
            {
                this.HoleDia += ChangeAmount;
            }
        }

        public void SetHoleTypeOnHoles(Hole.HoleTypes HoleTpye)
        {
            foreach (Hole Hole in HoleList)
            {
                Hole.HoleType = this.HoleType;
            }
        }

        private void UpdateCurrentHole(Hole value)
        {
            //Note: this works directly on the HoleEntity, and bypasses the properties on the Hole object
            if (_CurrentHole != null && value!=_CurrentHole)
            {
                // Reset Current Hole back to default value of this pattern
                _CurrentHole.HoleEntity.StrokeThickness = _StrokeThickness;
                _CurrentHole.HoleEntity.Stroke = _StrokeColor;
                _CurrentHole.HoleEntity.Fill = null;
                _CurrentHole.HoleLabel.Foreground = Brushes.White;
            }

            if (value != null)
            {
                // Now apply formatting to passed in hole, which will become the new CurrentHole
                value.HoleEntity.StrokeThickness = _CurrentHoleStrokeWidth;
                value.HoleEntity.Stroke = _CurrentHoleColor;
                value.HoleEntity.Fill = _CurrentHoleFill;
                value.HoleLabel.Foreground = Brushes.Black;
            }

        }

        protected double _StartX;
        protected double _StartY;

        public double StartX
        {
            get
            {
                return _StartX; ;
            }
            set
            {
                _StartX = value;
                NotifyPropertyChanged("StartX");
            }
        }

        public double StartY
        {
            get
            {
                return _StartY;
            }
            set
            {
                _StartY = value;
                NotifyPropertyChanged("StartY");
            }
        }


    }

    public class CircularHolePattern : HolePattern
    {
        private double _BoltCirDia;
        private double _AngularSpacing;
        private double _SketchBoltCirRad;

        public CircularHolePattern()
        {
            PatternShortDescription="Circular Pattern";
            HoleClass = typeof(CircularPatternHole);
            SketchBoltCirRad = 0;
        }

        public double BoltCirDia
        {
            get { return _BoltCirDia; }
            set
            {
                _BoltCirDia = value;
                NotifyPropertyChanged("BoltCirDia");
            }
        }

        public double AngularSpacing
        {
            get { return _AngularSpacing; }
            set
            {
                _AngularSpacing = value;
                NotifyPropertyChanged("AngularSpacing");
            }
        }

        public double SketchBoltCirRad
        {
            get
            {
                return _SketchBoltCirRad;
            }
            set
            {
                if (value != _SketchBoltCirRad)
                {
                    _SketchBoltCirRad = value;
                    NotifyPropertyChanged("SketchBoltCirRad");
                }
            }
        }


        public override void UpdateHoles()
        {
            int x=1;
            double CurrentAngle = this.StartAngle;

            foreach (CircularPatternHole Hole in this.HoleList)
            {
                Hole.Angle = CurrentAngle;
                Hole.BoltCirRad = _BoltCirDia / 2;
                Hole.HoleNumber = x;
                Hole.AbsX = _StartX + (_BoltCirDia / 2 * Math.Cos(MathHelper.dtr(CurrentAngle)));
                Hole.AbsY = _StartY + (_BoltCirDia / 2 * Math.Sin(MathHelper.dtr(CurrentAngle)));

                CurrentAngle = this.StartAngle + x * this.AngularSpacing; // Increment the angle for the next Hole
                x++;
            }
            base.UpdateHoles();

        }

        public override int HoleCount
        {
            get { return _HoleCount; }
            set
            {
                if (value >= 0)
                {
                    _HoleCount = value;
                }
                else
                {
                    _HoleCount = 0;
                }
                if (value > 0)
                {
                    _AngularSpacing = 360.0 / _HoleCount;
                }
                else
                {
                    _AngularSpacing = 0;
                }
                CreateHoles();
                NotifyPropertyChanged("HoleCount");
            }
        }

    }

    public class SingleLineHolePattern : HolePattern
    {
        private double _Spacing;

        public SingleLineHolePattern()
        {
            PatternShortDescription = "Line Pattern";
            HoleClass = typeof(SingleLineHole);
            Spacing = 10;
            HoleCount = 5;
            //this.CreateHoles();
            //this.UpdateHoles();
        }

        public double Spacing
        {
            get { return _Spacing; }
            set
            {
                _Spacing = value;
                NotifyPropertyChanged("Spacing");
            }
        }

        //public override void CreateHoles()
        //{
        //    base.CreateHoles();
        //}

        public override void UpdateHoles()
        {
            int x = 1;
            foreach (SingleLineHole Hole in this.HoleList)
            {
                Hole.HoleNumber = x;
                Hole.AbsX = StartX + (x - 1) * this.Spacing * Math.Cos(MathHelper.dtr(StartAngle));
                Hole.AbsY = StartY + (x-1) * this.Spacing * Math.Sin(MathHelper.dtr(StartAngle));
                x++;
            }
            base.UpdateHoles();
        }

    }


    //public class Circle:Shape
    //{
    //    public Hole HoleObject;
    //    public HolePattern ParentPattern;
        

    //    public Circle()
    //    {
    //    }
    
    //}

    public class Hole : INotifyPropertyChanged
    {
        #region Field Definitions
        private double _AbsX;
        private double _AbsY;
        private double _CanvasX { get; set; }
        private double _CanvasY { get; set; }
        private bool _Visible;
        private double _HoleDia = 20;
        private HoleTypes _HoleType;
        private int _HoleNumber;
        private double _StrokeThickness = 1;
        private Brush _StrokeColor = Brushes.Black;
        private HolePattern _ParentPattern;
        #endregion

        public enum HoleTypes { Drilled, Tapped, CounterBored, CounterSunk };
        public Ellipse HoleEntity = new Ellipse();
        public Ellipse HoleDecorator = new Ellipse();
        public TextBlock HoleLabel = new TextBlock();

        ////-- Define Hidden Line Pattern for use as outer circle of Tapped Holes
        //private static DoubleCollection HiddenLinePattern = new DoubleCollection(new double[] { 5, 5 });

        public int HoleNumber
        {
            get
             {
                return _HoleNumber;
             }
            set
            {
                _HoleNumber = value;
                HoleLabel.Text = value.ToString();
            }
        }
        public double HoleLabelX { get; set; }
        public double HoleLabelY { get; set; }
        public string AbsXDisplay { get; set; }
        public string AbsYDisplay { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        //public event MouseEventHandler MouseActivity;

        // Constructor
        public Hole()
        {
            //_HoleDia = 20.0;
            _Visible = true;
            //this.ParentPattern = WhoIsTheParent;
            HoleEntity.Tag = this;
            HoleEntity.Width = _HoleDia;
            HoleEntity.Height = _HoleDia;

            HoleDecorator.Tag = this;
            HoleDecorator.Width = 0;
            HoleDecorator.Height = 0;

            ////--Setup HoleLabel (position happens in UpdateHoles()---------
            //HoleLabel.Text = x.ToString();
            HoleLabel.TextAlignment = TextAlignment.Center;
            HoleLabel.Foreground = Brushes.White;
            HoleLabel.FontSize = 12;

            this.StrokeThickness = _StrokeThickness;
            this.StrokeColor = _StrokeColor;
            //HoleEntity.Stroke = Brushes.Black;
            //HoleDecorator.Stroke = HoleEntity.Stroke;
            //HoleDecorator.StrokeThickness = HoleEntity.StrokeThickness;
            //HiddenLinePattern=DoubleCollection(new double[]{5, 5});
        }

        public void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

        #region Properties
        public HolePattern ParentPattern
        {
            get
            {
                return _ParentPattern;
            }
            set
            {
                _ParentPattern = value;
            }
        }
        
        public bool Visible
        {
            get { return _Visible; }
            set
            {
                _Visible = value;
                HoleEntity.Visibility = value ? Visibility.Visible : Visibility.Hidden;
                HoleDecorator.Visibility = HoleEntity.Visibility;
                SetCoordDisplayValues();
                NotifyPropertyChanged("Visible");
            }
        }

        public double AbsX
        {
            get { return _AbsX; }
            set
            {
                _AbsX = value;
                SetCoordDisplayValues();
                NotifyPropertyChanged("AbsX");
            }
        }

        public double AbsY
        {
            get { return _AbsY; }
            set
            {
                _AbsY = value;
                SetCoordDisplayValues();
                NotifyPropertyChanged("AbsY");
            }
        }

        private void SetCoordDisplayValues()
        {
            AbsXDisplay = HoleEntity.Visibility == Visibility.Visible ? String.Format("{0:f4}", _AbsX) : "";
            AbsYDisplay = HoleEntity.Visibility == Visibility.Visible ? String.Format("{0:f4}", _AbsY) : "";
            NotifyPropertyChanged("AbsXDisplay");
            NotifyPropertyChanged("AbsYDisplay");
        }

        public double CanvasX
        {
            get { return _CanvasX; }
            set
            {
                if (value == _CanvasX) { return; }
                _CanvasX = value;
                UpdateEntities();
                NotifyPropertyChanged("CanvasX");
            }
        }

        public double CanvasY
        {
            get { return _CanvasY; }
            set
            {
                if (value == _CanvasY) { return; }
                _CanvasY = value;
                UpdateEntities();
                NotifyPropertyChanged("CanvasY");
            }
        }

        public HoleTypes HoleType
        {
            get { return _HoleType; }
            set
            {
                if (value != _HoleType)
                {
                    _HoleType = value;
                    UpdateHoleType();
                    NotifyPropertyChanged("HoleType");
                }
            }
        }

        public double HoleDia
        {
            get { return _HoleDia; }
            set
            {
                if (value != _HoleDia)
                {
                    _HoleDia = value;
                    HoleEntity.Width = value;
                    HoleEntity.Height = value;
                    UpdateHoleType(); // Will re-size decorator to match new primary hole size
                    NotifyPropertyChanged("HoleDia");
                }
            }
        }

        public double StrokeThickness
        {
            get { return _StrokeThickness; }
            //Setting this StrokeThickness will also set Decorator
            set
            {
                _StrokeThickness = value;
                this.HoleEntity.StrokeThickness = value;
                this.HoleDecorator.StrokeThickness = value;
                NotifyPropertyChanged("StrokeThickness");
            }
        }

        public Brush StrokeColor
        {
            get { return _StrokeColor; }
            //Setting this StrokeThickness will also set Decorator
            set
            {
                _StrokeColor = value;
                this.HoleEntity.Stroke = value;
                this.HoleDecorator.Stroke = value;
                NotifyPropertyChanged("StrokeColor");
            }
        }

        #endregion

        #region Methods

        private void UpdateEntities()
        {
            //-- Update Margins for graph positioning
            HoleEntity.Margin = new Thickness(CanvasX - HoleDia / 2, CanvasY - HoleDia / 2, 0, 0);
            HoleDecorator.Margin = new Thickness(CanvasX - HoleDecorator.Width / 2, CanvasY - HoleDecorator.Width / 2, 0, 0);
            HoleLabel.Margin = new Thickness((CanvasX * 1.0) - HoleLabel.FontSize * .3, (CanvasY * 1.0) - HoleLabel.FontSize * .6, 0, 0);
        }

        private void UpdateHoleType()
        {
            switch (this.HoleType)
            {
                case HoleTypes.Drilled: //Drilled only
                    HoleDecorator.Visibility = Visibility.Hidden;
                    break;
                case HoleTypes.Tapped: // Drilled & Tapped
                    HoleDecorator.Visibility = (this.Visible == true) ? Visibility.Visible : Visibility.Hidden;
                    HoleDecorator.Width = HoleEntity.Width * 1.2;
                    HoleDecorator.Height = HoleDecorator.Width;
                    HoleDecorator.StrokeDashArray = LinePatterns.HiddenLinePattern(1);
                    break;
                case HoleTypes.CounterBored: // Drilled & CounterBored
                    HoleDecorator.Visibility = (this.Visible == true) ? Visibility.Visible : Visibility.Hidden;
                    HoleDecorator.Width = HoleEntity.Width * 1.5;
                    HoleDecorator.Height = HoleDecorator.Width;
                    HoleDecorator.StrokeDashArray = null;
                    break;
                case HoleTypes.CounterSunk: // Drilled & CounterSunk
                    HoleDecorator.Visibility = (this.Visible == true) ? Visibility.Visible : Visibility.Hidden;
                    HoleDecorator.Width = HoleEntity.Width * 1.8;
                    HoleDecorator.Height = HoleDecorator.Width;
                    HoleDecorator.StrokeDashArray = null;
                    break;
            }
            UpdateEntities();
        }

        #endregion

    }

    public class CircularPatternHole : Hole
    {
        private double _Angle;
        private double _BoltCirRad;

        public CircularPatternHole() { }

        public double BoltCirRad
        {
            get { return _BoltCirRad; }
            set
            {
                _BoltCirRad = value;
                //AbsX = (this.BoltCirRad * Math.Cos(MathHelper.dtr(this.Angle)));
                //AbsY = (this.BoltCirRad * Math.Sin(MathHelper.dtr(this.Angle)));
                NotifyPropertyChanged("BoltCirclRadius");
            }
        }

        public double Angle
        {
            get { return _Angle; }
            set
            {
                _Angle = value;
                //AbsX = (this.BoltCirRad * Math.Cos(MathHelper.dtr(this.Angle)));
                //AbsY = (this.BoltCirRad * Math.Sin(MathHelper.dtr(this.Angle)));
                NotifyPropertyChanged("Angle");
            }
        }
    
    }
    
    public class SingleLineHole : Hole
    {

        public SingleLineHole() { }


    }


    public class MathHelper
    {
        public static double dtr(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }
    }

}
