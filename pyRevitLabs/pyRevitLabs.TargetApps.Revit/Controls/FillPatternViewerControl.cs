using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.DB;

using pyRevitLabs.CommonWPF.Converters;

using NLog;

using Image = System.Drawing.Image;
using Matrix = System.Drawing.Drawing2D.Matrix;
using Rectangle = System.Drawing.Rectangle;
using Point = System.Drawing.Point;


namespace pyRevitLabs.TargetApps.Revit.Controls {
    public class FillPatternViewerControl : UserControl, INotifyPropertyChanged {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const float _scale = 400;
        private const float _length = 100;
        private Bitmap _fillPatternImage;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static readonly DependencyProperty FillPatternProperty =
            DependencyProperty.Register(
                "FillPattern",
                typeof(FillPattern),
                typeof(FillPatternViewerControl),
                new FrameworkPropertyMetadata(null, OnFillPatternChanged)
            );

        public FillPattern FillPattern {
            get {
                return (FillPattern)GetValue(FillPatternProperty);
            }
            set {
                SetValue(FillPatternProperty, value);
            }
        }

        public FillPatternViewerControl() {
            InitializeComponent();
        }

        private void InitializeComponent() {
            var containerGrid = new System.Windows.Controls.Grid();
            //containerGrid.ToolTip = FillPattern.Name;

            var patternImage = new System.Windows.Controls.Image();
            var imageSourceBinding = new System.Windows.Data.Binding();
            imageSourceBinding.Path = new PropertyPath("FillPatternImage");
            imageSourceBinding.Converter = new BitmapToImageSourceConverter();
            imageSourceBinding.RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.FindAncestor, typeof(FillPatternViewerControl), ancestorLevel: 1);
            System.Windows.Data.BindingOperations.SetBinding(patternImage, System.Windows.Controls.Image.SourceProperty, imageSourceBinding);

            containerGrid.Children.Add(patternImage);

            this.AddChild(containerGrid);
        }

        // public
        public Image FillPatternImage {
            get {
                if (_fillPatternImage == null)
                    CreateFillPatternImage();
                return _fillPatternImage;
            }
        }

        public void Regenerate() {
            CreateFillPatternImage();
        }

        // private
        private static void OnFillPatternChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            var fpviewerControl = d as FillPatternViewerControl;

            if (fpviewerControl == null) return;

            fpviewerControl.OnPropertyChanged("FillPattern");
            fpviewerControl.CreateFillPatternImage();
        }

        private void CreateFillPatternImage() {
            var width = (ActualWidth == 0 ? Width : ActualWidth) == 0 ? 100 : (ActualWidth == 0 ? Width : ActualWidth);
            if (double.IsNaN(width))
                width = 100;

            var height = (ActualHeight == 0 ? Height : ActualHeight) == 0 ? 30 : (ActualHeight == 0 ? Height : ActualHeight);
            if (double.IsNaN(height))
                height = 30;

            _fillPatternImage = new Bitmap((int)width, (int)height);

            using (var gfx = Graphics.FromImage(_fillPatternImage)) {
                var rect = new Rectangle(0, 0, (int)width, (int)height);
                gfx.FillRectangle(Brushes.White, rect);
                DrawFillPattern(gfx, width, height);
            }

            OnPropertyChanged("FillPatternImage");
        }

        private void DrawFillPattern(Graphics gfx, double width, double height) {
            // verify fill pattern
            var fillPattern = FillPattern;
            if (fillPattern == null)
                return;

            // determine drawing scale
            float matrixScale;
            if (fillPattern.Target == FillPatternTarget.Model)
                matrixScale = _scale;
            else
                matrixScale = _scale * 10;

            // prepare pen
            var pen = new Pen(System.Drawing.Color.Black) {
                Width = 1f / matrixScale
            };

            try {
                // setup rectangle and center
                var viewRect = new Rectangle(0, 0, (int)width, (int)height);
                var centerX = (viewRect.Left + viewRect.Left + viewRect.Width) / 2;
                var centerY = (viewRect.Top + viewRect.Top + viewRect.Height) / 2;

                gfx.ResetTransform();

                // draw each fill grid
                foreach (var fillGrid in fillPattern.GetFillGrids()) {
                    // radian to degree: inline math is faster
                    var degreeAngle = (float)(fillGrid.Angle * 180 / Math.PI);

                    // setup pen dash
                    float dashLength = 1;
                    var segments = fillGrid.GetSegments();
                    if (segments.Count > 0) {
                        pen.DashPattern = segments.Select(Convert.ToSingle).ToArray();
                        dashLength = pen.DashPattern.Sum();
                    }

                    gfx.ResetTransform();
                    // determine offset and rotation
                    var offset = (-10) * dashLength;
                    var rotateMatrix = new Matrix();
                    rotateMatrix.Rotate(degreeAngle);

                    var matrix = new Matrix(1, 0, 0, -1, centerX, centerY);
                    matrix.Scale(matrixScale, matrixScale);
                    matrix.Translate((float)fillGrid.Origin.U, (float)fillGrid.Origin.V);

                    // make a copy for backward move
                    var backMatrix = matrix.Clone();

                    matrix.Multiply(rotateMatrix);
                    matrix.Translate(offset, 0);

                    backMatrix.Multiply(rotateMatrix);
                    backMatrix.Translate(offset, 0);

                    int safety = 250;
                    double alternator = 0;

                    // draw moving forward
                    //while (LineIntersectsRect(matrix, viewRect) && safety > 0) {
                    while (IntersectsWith(matrix.OffsetX, matrix.OffsetY, viewRect) && safety > 0) {
                        gfx.Transform = matrix;
                        gfx.DrawLine(pen, 0, 0, _length, 0);

                        matrix.Translate((float)fillGrid.Shift, (float)fillGrid.Offset);

                        alternator += fillGrid.Shift;
                        if (Math.Abs(alternator) > Math.Abs(offset)) {
                            matrix.Translate(offset, 0);
                            alternator = 0d;
                        }

                        --safety;
                    }

                    // draw moving backward
                    safety = 250;
                    alternator = 0;
                    while (LineIntersectsRect(backMatrix, viewRect) && safety > 0) {
                        gfx.Transform = backMatrix;
                        gfx.DrawLine(pen, 0, 0, _length, 0);

                        backMatrix.Translate(-(float)fillGrid.Shift, -(float)fillGrid.Offset);

                        alternator += fillGrid.Shift;
                        if (Math.Abs(alternator) > Math.Abs(offset)) {
                            backMatrix.Translate(offset, 0);
                            alternator = 0d;
                        }

                        --safety;
                    }
                }
            }
            catch (Exception ex) {
                logger.Debug(ex.ToString());
            }
        }

        private bool IntersectsWith(float X, float Y, Rectangle rect) {
            float tmin = (rect.Left - X) / (X + 200);
            float tmax = (rect.Right - X) / (X + 200);

            if (tmin > tmax) {
                var temp = tmin;
                tmin = tmax;
                tmax = temp;
            }

            float tymin = (rect.Bottom - Y) / Y;
            float tymax = (rect.Top - Y) / Y;

            if (tymin > tymax) {
                var temp = tymin;
                tymin = tymax;
                tymax = temp;
            }

            if ((tmin > tymax) || (tymin > tmax))
                return false;

            return true;
        }

        private bool LineIntersectsRect(Matrix rayMatrix, Rectangle rect) {
            Matrix m = rayMatrix.Clone();
            m.Translate(200, 0);
            return LineIntersectsRect(new Point((int)rayMatrix.OffsetX, (int)rayMatrix.OffsetY),
                                      new Point((int)m.OffsetX, (int)m.OffsetY),
                                      rect);
        }

        private bool LineIntersectsRect(Point p1, Point p2, Rectangle rect) {
            return LineIntersectsLine(p1, p2, new Point(rect.X, rect.Y), new Point(rect.X + rect.Width, rect.Y))
                || LineIntersectsLine(p1, p2, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height))
                || LineIntersectsLine(p1, p2, new Point(rect.X + rect.Width, rect.Y + rect.Height), new Point(rect.X, rect.Y + rect.Height))
                || LineIntersectsLine(p1, p2, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X, rect.Y))
                || (rect.Contains(p1) && rect.Contains(p2));
        }

        private bool LineIntersectsLine(Point l1p1, Point l1p2, Point l2p1, Point l2p2) {
            try {
                long d = (l1p2.X - l1p1.X) * (l2p2.Y - l2p1.Y) - (l1p2.Y - l1p1.Y) * (l2p2.X - l2p1.X);
                if (d == 0) return false;

                long q = (l1p1.Y - l2p1.Y) * (l2p2.X - l2p1.X) - (l1p1.X - l2p1.X) * (l2p2.Y - l2p1.Y);
                long r = q / d;

                long q1 = (l1p1.Y - l2p1.Y) * (l1p2.X - l1p1.X);
                long q2 = (l1p1.X - l2p1.X) * (l1p2.Y - l1p1.Y);

                q = q1 - q2;
                long s = q / d;

                if (r < 0 || r > 1 || s < 0 || s > 1)
                    return false;

                return true;
            }
            catch (OverflowException) {
                return false;
            }
        }
    }
}
