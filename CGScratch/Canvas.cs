using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using MathNet.Numerics.LinearAlgebra.Complex;
using MathNet.Numerics.LinearAlgebra;
using System.ComponentModel;

namespace CGScratch
{
    public struct ARGB
    {
        public int A { get; set; }
        public int R { get; set; }
        public int G { get; set; }
        public int B { get; set; }
        public Color color
        {
            get
            {
                return Color.FromArgb(A, R, G, B);
                //BitConverter.ToInt32(argb, 0)
            }
        }
    }
    public class MyPoint
    {
        public int x { get; set; }
        public int y { get; set; }
        public double h { get; set; }
        public ARGB argb { get; set; }
        public Color color
        {
            get
            {
                return Color.FromArgb(this.argb.A, this.argb.R, this.argb.G, this.argb.B);
                //BitConverter.ToInt32(argb, 0)
            }
        }
    }
    public class Utility
    {
        public static void Swap<T>(T a, T b)
        {
            T t = a;
            a = b;
            b = t;
        }
        public static Color Multiply(Color color, double intensity)
        {
            int A = color.A; A = (int)(A * intensity); A = Math.Min(A, 255);
            int R = color.R; R = (int)(R * intensity); R = Math.Min(R, 255);
            int G = color.G; G = (int)(G * intensity); G = Math.Min(G, 255);
            int B = color.B; B = (int)(B * intensity); B = Math.Min(B, 255);

            return Color.FromArgb(A, R, G, B);
        }
        public static Color ColorPlus(Color a, Color b)
        {
            int A = a.A + b.A; A = Math.Min(A, 255);
            int R = a.R + b.R; R = Math.Min(R, 255);
            int G = a.G + b.G; G = Math.Min(G, 255);
            int B = a.B + b.B; B = Math.Min(B, 255);
            return Color.FromArgb(A, R, G, B);
        }
        // 返回齐次矩阵
        public static Matrix<double> RotateToMatrix(Point3d rotate)
        {
            double theta_x = rotate.x / 180 * Math.PI;
            double theta_y = rotate.y / 180 * Math.PI;
            double theta_z = rotate.z / 180 * Math.PI;
            var rotate_x = Matrix<double>.Build.Dense(4, 4,
                new[] {1, 0, 0, 0,
                       0, Math.Cos(theta_x), Math.Sin(theta_x), 0,
                       0, -Math.Sin(theta_x), Math.Cos(theta_x), 0,
                       0, 0, 0, 1
                });
            var rotate_y = Matrix<double>.Build.Dense(4, 4,
                new[] {Math.Cos(theta_y), 0, -Math.Sin(theta_y), 0,
                       0, 1, 0, 0,
                       Math.Sin(theta_y), 0, Math.Cos(theta_y), 0,
                       0, 0, 0, 1
                });
            var rotate_z = Matrix<double>.Build.Dense(4, 4,
                new[] {Math.Cos(theta_z), -Math.Sin(theta_z), 0, 0,
                       Math.Sin(theta_z), Math.Cos(theta_z), 0, 0,
                       0, 0, 1, 0,
                       0, 0, 0, 1
                });
            var rotation = rotate_x * rotate_y * rotate_z;
            return rotation;
        }
        // 返回齐次矩阵
        public static Matrix<double> TranslationToMatrix(Point3d trans)
        {
            return Matrix<double>.Build.Dense(4, 4, new[]
            {
                1,0,0,0,
                0,1,0,0,
                0,0,1,0,
                trans.x, trans.y, trans.z, 1
            });
        }
        // 返回齐次矩阵
        public static Matrix<double> ScaleToMatrix(Point3d scale)
        {
            return Matrix<double>.Build.Dense(4, 4, new[]
            {
                scale.x, 0, 0, 0,
                0, scale.y, 0, 0,
                0, 0, scale.z, 0,
                0, 0, 0, 1
            });
        }

        public static Matrix<double> Point3dToHomo(Point3d point)
        {
            return Matrix<double>.Build.Dense(4, 1, new[]
            {
                point.x, point.y, point.z, 1
            });
        }
        public static Point3d HomoToPoint3d(Matrix<double> point)
        {
            return new Point3d
            {
                x = point[0, 0] / point[3, 0],
                y = point[1, 0] / point[3, 0],
                z = point[2, 0] / point[3, 0]
            };
        }
    } 
    public class Canvas
    {
        public int cw { get; set; }
        public int ch { get; set; }
        double vw, vh;
        public double d { get; set; }
        private byte[] data;

        public Canvas(int _cw, int _ch, double _vw = 1, double _vh = 1, double _d = 1)
        {
            cw = _cw; ch = _ch;
            vw = _vw; vh = _vh; d = _d;
            data = new byte[ch*cw*4];
        }

        public Point3d CanvasToViewport(int x, int y)
        {
            return new Point3d(x * vw / cw, y * vh / ch, d);
        }
        public Point ViewportToCanvas(double x, double y)
        {
            return new Point { X = (int)(x * cw / vw), Y = (int)(y * ch / vh) };
        }
        public Point ProjectVertex(Point3d v)
        {
            return ViewportToCanvas(v.x * d / v.z, v.y * d / v.z);
        }
        public void PutPixel(int x, int y, Color color)
        {
            int r_x = x + cw / 2;
            int r_y = -y + ch / 2 - 1;

            // boundary check
            if(r_x < 0 || r_x >= cw) { return; }
            if(r_y < 0 || r_y >= ch) { return; }

            data[(r_y * cw + r_x) * 4] = color.B;
            data[(r_y * cw + r_x) * 4 + 1] = color.G;
            data[(r_y * cw + r_x) * 4 + 2] = color.R;
            data[(r_y * cw + r_x) * 4 + 3] = color.A;
        }
        public void Save(string filename, ImageFormat format)
        {
            var bitmap = new Bitmap(cw, ch);
            Rectangle rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            var bmpData = bitmap.LockBits(
                rect,
                System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bitmap.PixelFormat
                );
            IntPtr ptr = bmpData.Scan0;
            System.Runtime.InteropServices.Marshal.Copy(data, 0, ptr, cw*ch*4);
            bitmap.UnlockBits(bmpData);
            bitmap.Save(filename, ImageFormat.Png);
        }

        public void DrawLine(Point P0, Point P1, Color color)
        {
            if (Math.Abs(P1.X - P0.X) > Math.Abs(P1.Y - P0.Y))
            {
                // Line is horizontal-ish
                if (P0.X > P1.X)
                {
                    Utility.Swap(P0, P1);
                }
                var ys = Line.Interpolate(P0.X, P0.Y, P1.X, P1.Y);
                for (int x = P0.X; x <= P1.X; ++x)
                {
                    PutPixel(x, (int)ys[x - P0.X], color);
                }
            }
            else
            {
                // vartival-ish
                if (P0.Y > P1.Y)
                {
                    Utility.Swap(P0, P1);
                }
                var xs = Line.Interpolate(P0.Y, P0.X, P1.Y, P1.X);
                for (int y = P0.Y; y <= P1.Y; ++y)
                {
                    PutPixel((int)xs[y - P0.Y], y, color);
                }
            }
        }
        public void DrawWireframeTriangle(Point v0, Point v1, Point v2, Color color)
        {
            DrawLine(v0, v1, color);
            DrawLine(v1, v2, color);
            DrawLine(v2, v0, color);
        }
        public void DrawFilledTriangle(MyPoint P0, MyPoint P1, MyPoint P2, Color color)
        {
            if(P1.y < P0.y)
            {
                Utility.Swap(P0, P1);
            }
            if(P2.y < P0.y)
            {
                Utility.Swap(P0, P2);
            }
            if(P2.y < P1.y)
            {
                Utility.Swap(P1, P2);
            }

            var x01 = Line.Interpolate(P0.y, P0.x, P1.y, P1.x);
            var h01 = Line.Interpolate(P0.y, P0.h, P1.y, P1.h);

            var x12 = Line.Interpolate(P1.y, P1.x, P2.y, P2.x);
            var h12 = Line.Interpolate(P1.y, P1.h, P2.y, P2.h);

            var x02 = Line.Interpolate(P0.y, P0.x, P2.y, P2.x);
            var h02 = Line.Interpolate(P0.y, P0.h, P2.y, P2.h);

            x01.Remove(x01.Count - 1);
            var x012 = x01.Concat(x12).ToList();

            h01.Remove(h01.Count - 1);
            var h012 = h01.Concat(h12).ToList();

            List<double> x_left, x_right, h_left, h_right;
            int m = x012.Count() / 2;
            if (x02[m] < x012[m])
            {
                x_left = x02;
                h_left = h02;
                x_right = x012;
                h_right= h012;
            }
            else
            {
                x_left = x012;
                h_left = h012;
                x_right = x02;
                h_right = h02;
            }

            for(int y=P0.y; y<=P2.y; ++y) 
            {
                int x_l = (int)x_left[y - P0.y];
                int x_r = (int)x_right[y - P0.y];

                var h_segment = Line.Interpolate(x_l, h_left[y-P0.y], x_r, h_right[y-P0.y]);
                for (int x=x_l; x <= x_r; ++x)
                {
                    Color t_color = Utility.Multiply(color, h_segment[x - x_l]);
                    PutPixel(x, y, t_color);
                }
            }
        }
        public void RenderTriangle(Triangle triangle, List<Point> projected)
        {
            DrawWireframeTriangle(projected[triangle.A], projected[triangle.B], projected[triangle.C], triangle.color);
        }
    };
    public class Line
    {
        public static List<double> Interpolate(int i0, double d0, int i1, double d1)
        {
            if(i0 == i1)    return new List<double> { d0 };
            List<double> result = new List<double>();
            double a = (d1 - d0) / (i1 - i0);
            for(; i0 <= i1; i0++) 
            {
                result.Add(d0);
                d0 += a;                
            }
            return result;
        }
    }
}
