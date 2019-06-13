// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MainViewModel.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// <summary>
//   No color coding, use coloured lights
// </summary>
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Linq;

namespace WPF3DTest
{
    // http://reference.wolfram.com/mathematica/tutorial/ThreeDimensionalSurfacePlots.html

    public enum ColorCoding
    {
        /// <summary>
        /// No color coding, use coloured lights
        /// </summary>
        ByLights,

        /// <summary>
        /// Color code by gradient in y-direction using a gradient brush with white ambient light
        /// </summary>
        ByGradientY
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public double MinX { get; set; }
        public double MinY { get; set; }
        public double MaxX { get; set; }
        public double MaxY { get; set; }
        public int Rows { get; set; }
        public int Columns { get; set; }

        public Func<double, double, double> Function { get; set; }
        public Point3D[,] Data { get; set; }
        public double[,] ColorValues { get; set; }

        public ColorCoding ColorCoding { get; set; }

        public Model3DGroup Lights
        {
            get
            {
                var group = new Model3DGroup();
                switch (ColorCoding)
                {
                    case ColorCoding.ByGradientY:
                        group.Children.Add(new AmbientLight(Colors.White));
                        break;
                    case ColorCoding.ByLights:
                        group.Children.Add(new AmbientLight(Colors.Gray));
                        group.Children.Add(new PointLight(Colors.Red, new Point3D(0, -1000, 0)));
                        group.Children.Add(new PointLight(Colors.Blue, new Point3D(0, 0, 1000)));
                        group.Children.Add(new PointLight(Colors.Green, new Point3D(1000, 1000, 0)));
                        break;
                }
                return group;
            }
        }

        public Brush SurfaceBrush
        {
            get
            {
                // Brush = BrushHelper.CreateGradientBrush(Colors.White, Colors.Blue);
                // Brush = GradientBrushes.RainbowStripes;
                // Brush = GradientBrushes.BlueWhiteRed;
                switch (ColorCoding)
                {
                    case ColorCoding.ByGradientY:
                        // 创建线性渐变画笔
                        GradientStop gradient0 = new GradientStop(Colors.Green, 0.0);
                        //GradientStop gradient1 = new GradientStop(Colors.Blue, 0.6);
                        //GradientStop gradient2 = new GradientStop(Colors.LimeGreen, 0.9);
                        //// GradientStop gradient3 = new GradientStop(Color.FromRgb(200, 0, 0), 0.9);
                        //GradientStop gradient4 = new GradientStop(Colors.Blue, 0.95);
                        GradientStop gradient5 = new GradientStop(Colors.Red, 1.0);
                        GradientStopCollection stopCollection = new GradientStopCollection();
                        stopCollection.Add(gradient0);
                        //stopCollection.Add(gradient1);
                        //stopCollection.Add(gradient2);
                        ////stopCollection.Add(gradient3);
                        //stopCollection.Add(gradient4);
                        stopCollection.Add(gradient5);

                        LinearGradientBrush linear = new LinearGradientBrush(stopCollection, new Point(0, 0), new Point(1, 1));
                        linear.Opacity = 0.1;
                        return linear;
                    case ColorCoding.ByLights:
                        return Brushes.White;
                }
                return null;
            }
        }

        public MainViewModel()
        {
            MinX = 0;
            MaxX = 3;
            MinY = 0;
            MaxY = 3;
            Rows = 91;
            Columns = 91;

            Function = (x, y) => Math.Sin(x * y) * 0.5 * 50;
            ColorCoding = ColorCoding.ByGradientY;

            UpdateModel();
        }

        private void UpdateModel()
        {
            // 添加Data数组
            var file = File.Open("D:\\result.txt", FileMode.Open);
            List<string> txt = new List<string>();
            using (var stream = new StreamReader(file))
            {
                while (!stream.EndOfStream)
                {
                    txt.Add(stream.ReadLine());
                }
            }
            Point3D[] pData = new Point3D[txt.LongCount()];

            for (int i = 0; i < pData.Length; i++)
            {
                List<string> value = txt[i].Split(',').ToList();
                pData[i].X = System.Convert.ToDouble(value[0]) * 50;
                pData[i].Y = System.Convert.ToDouble(value[1]) * 50;
                pData[i].Z = System.Convert.ToDouble(value[2]) * 50;
            }

            int nIndex = 0;
            var pValue = new Point3D[37, 73];
            for (int i = 0; i < 37; i++)
            {
                for (int j = 0; j < 73; j++)
                {
                    pValue[i, j] = pData[nIndex];
                    nIndex++;
                }
            }
            Data = pValue;

            //Data = CreateDataArray(Function);
            //switch (ColorCoding)
            //{
            //    case ColorCoding.ByGradientY:
            //        ColorValues = FindGradientY(Data);
            //        break;
            //    case ColorCoding.ByLights:
            //        ColorValues = null;
            //        break;
            //}
            RaisePropertyChanged("Data");
            RaisePropertyChanged("ColorValues");
            RaisePropertyChanged("SurfaceBrush");
        }

        public Point GetPointFromIndex(int i, int j)
        {
            double x = MinX + (double)j / (Columns - 1) * (MaxX - MinX);
            double y = MinY + (double)i / (Rows - 1) * (MaxY - MinY);
            return new Point(x, y);
        }

        public Point3D[,] CreateDataArray(Func<double, double, double> f)
        {
            var data = new Point3D[Rows, Columns];
            for (int i = 0; i < Rows; i++)
                for (int j = 0; j < Columns; j++)
                {
                    var pt = GetPointFromIndex(i, j);
                    data[i, j] = new Point3D(pt.X * 30, pt.Y * 30, f(pt.X, pt.Y));
                }
            return data;
        }

        // http://en.wikipedia.org/wiki/Numerical_differentiation
        public double[,] FindGradientY(Point3D[,] data)
        {
            int n = data.GetUpperBound(0) + 1;
            int m = data.GetUpperBound(0) + 1;
            var K = new double[n, m];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    // Finite difference approximation
                    var p10 = data[i + 1 < n ? i + 1 : i, j - 1 > 0 ? j - 1 : j];
                    var p00 = data[i - 1 > 0 ? i - 1 : i, j - 1 > 0 ? j - 1 : j];
                    var p11 = data[i + 1 < n ? i + 1 : i, j + 1 < m ? j + 1 : j];
                    var p01 = data[i - 1 > 0 ? i - 1 : i, j + 1 < m ? j + 1 : j];

                    //double dx = p01.X - p00.X;
                    //double dz = p01.Z - p00.Z;
                    //double Fx = dz / dx;

                    double dy = p10.Y - p00.Y;
                    double dz = p10.Z - p00.Z;

                    K[i, j] = dz / dy;
                }
            return K;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string property)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(property));
            }
        }

    }
}