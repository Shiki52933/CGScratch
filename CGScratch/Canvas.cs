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
using System.Numerics;

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
        public double d { get; set; }
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

        public static MyPoint ToMyPoint(DepthPoint p, Color color)
        {
            return new MyPoint
            {
                x = p.point.X, 
                y = p.point.Y, 
                d = p.d,
                h = 1, 
                argb = new ARGB() 
                { 
                    A = color.A, 
                    R = color.R, 
                    G = color.G, 
                    B = color.B 
                }
            };
        }
    }
    public class DepthPoint
    {
        public Point point { get; set; }
        public float d { get; set; }
    }
    public class Utility
    {
        public static void Swap<T>(ref T a, ref T b)
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

        /// <summary>
        /// return intersection point of plane and two points
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Vector3 Intersection(Plane plane, Vector3 a, Vector3 b)
        {
            var t = -Plane.DotCoordinate(plane, a) / Plane.DotNormal(plane, b-a);
            return a + t * (b-a);
        }

        public static Point3d Intersection(Plane plane, Point3d a, Point3d b)
        {
            Vector3 A = new Vector3 { X = (float)a.x, Y = (float)a.y, Z = (float)a.z };
            Vector3 B = new Vector3 { X = (float)b.x, Y = (float)b.y, Z = (float)b.z };
            var inter = Utility.Intersection(plane, A, B);
            return new Point3d { x=inter.X, y=inter.Y, z=inter.Z };
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

        public static Vector3 Point3dToVector3(Point3d point)
        {
            return new Vector3
            {
                X = (float)point.x,
                Y = (float)point.y,
                Z = (float)point.z,
            };
        }
    } 
    public class Canvas
    {
        // basic setting
        public int cw { get; set; }
        public int ch { get; set; }
        double vw, vh;
        public double d { get; set; }

        // data repositories
        private byte[] data;
        private double[,] depths;

        public Canvas(int _cw, int _ch, double _vw = 1, double _vh = 1, double _d = 1)
        {
            cw = _cw; ch = _ch;
            vw = _vw; vh = _vh; d = _d;

            data = new byte[ch*cw*4];
            depths = new double[ch, cw];
        }

        public Point3d CanvasToViewport(int x, int y)
        {
            return new Point3d(x * vw / cw, y * vh / ch, d);
        }
        public Point ViewportToCanvas(double x, double y)
        {
            return new Point { X = (int)(x * cw / vw), Y = (int)(y * ch / vh) };
        }
        public List<Plane> GetClippingPlanes()
        {
            Vector3 origin = new Vector3{ X = 0, Y = 0, Z = 0};
            Vector3 leftUp = new Vector3 { X = (float)(-vw / 2), Y = (float)(vh / 2), Z = (float)d };
            Vector3 rightUp = new Vector3 { X = (float)(vw / 2), Y = (float)(vh / 2),Z = (float)d };
            Vector3 leftDown = new Vector3 { X = (float)(-vw / 2), Y = (float)(-vh / 2), Z = (float)d };
            Vector3 rightDown = new Vector3 { X = (float)(vw / 2), Y = (float)(-vh / 2), Z = (float)d };

            List<Plane> planes = new List<Plane>();

            var leftPlane = Plane.CreateFromVertices(origin, leftUp, leftDown);
            planes.Add(leftPlane);
            var rightPlane = Plane.CreateFromVertices(origin, rightDown, rightUp);
            planes.Add(rightPlane);
            var upPlane = Plane.CreateFromVertices(origin, rightUp, leftUp);
            planes.Add(upPlane);
            var downPlane = Plane.CreateFromVertices(origin, leftDown, rightDown);
            planes.Add(downPlane);
            var z = Plane.CreateFromVertices(leftUp, rightDown, rightUp);
            planes.Add(z);

            return planes;
        }
        public Point ProjectVertex(Point3d v)
        {
            return ViewportToCanvas(v.x * d / v.z, v.y * d / v.z);
        }
        public DepthPoint ProjectVertexDepth(Point3d v)
        {
            var depthPoint = new DepthPoint();
            depthPoint.point = ViewportToCanvas(v.x * d / v.z, v.y * d / v.z);
            depthPoint.d = (float)(1 / v.z);
            return depthPoint;
        }

        public MyPoint ProjectVertexToMyPoint(Point3d v, Color color)
        {
            var point = this.ProjectVertex(v);
            return new MyPoint
            {
                x = point.X,
                y = point.Y,
                d = 1 / v.z,
                h = 1,
                argb = new ARGB()
                {
                    A = color.A,
                    R = color.R,
                    G = color.G,
                    B = color.B
                }
            };
        }

        // add depth info 
        public void PutPixel(int x, int y, Color color, double d=Double.MaxValue)
        {
            int r_x = x + cw / 2;
            int r_y = -y + ch / 2 - 1;

            // boundary check
            if (r_x == -1) ++r_x;
            if (r_x == cw) --r_x;
            if (r_y == -1) ++r_y;
            if (r_y == cw) --r_y;

            if (depths[r_x, r_y] >= d)
                return;

            depths[r_x, r_y] = d;
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
        /// <summary>
        /// draw a line given two points in the canvas
        /// no depth info
        /// </summary>
        /// <param name="P0"></param>
        /// <param name="P1"></param>
        /// <param name="color"></param>
        public void DrawLine(Point P0, Point P1, Color color)
        {
            if (Math.Abs(P1.X - P0.X) > Math.Abs(P1.Y - P0.Y))
            {
                // Line is horizontal-ish
                if (P0.X > P1.X)
                {
                    Utility.Swap(ref P0, ref P1);
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
                    Utility.Swap(ref P0, ref P1);
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
                Utility.Swap(ref P0, ref P1);
            }
            if(P2.y < P0.y)
            {
                Utility.Swap(ref P0, ref P2);
            }
            if(P2.y < P1.y)
            {
                Utility.Swap(ref P1, ref P2);
            }

            var x01 = Line.Interpolate(P0.y, P0.x, P1.y, P1.x);
            var h01 = Line.Interpolate(P0.y, P0.h, P1.y, P1.h);
            var d01 = Line.Interpolate(P0.y, P0.d, P1.y, P1.d);

            var x12 = Line.Interpolate(P1.y, P1.x, P2.y, P2.x);
            var h12 = Line.Interpolate(P1.y, P1.h, P2.y, P2.h);
            var d12 = Line.Interpolate(P1.y, P1.d, P2.y, P2.d);

            var x02 = Line.Interpolate(P0.y, P0.x, P2.y, P2.x);
            var h02 = Line.Interpolate(P0.y, P0.h, P2.y, P2.h);
            var d02 = Line.Interpolate(P0.y, P0.d, P2.y, P2.d);

            x01.Remove(x01.Count - 1);
            var x012 = x01.Concat(x12).ToList();

            h01.Remove(h01.Count - 1);
            var h012 = h01.Concat(h12).ToList();

            d01.Remove(d01.Count - 1);
            var d012 = d01.Concat(d12).ToList();

            List<double> x_left, x_right, h_left, h_right, d_left, d_right;
            int m = x012.Count() / 2;
            if (x02[m] < x012[m])
            {
                x_left = x02;
                h_left = h02;
                d_left = d02;
                x_right = x012;
                h_right= h012;
                d_right = d012;
            }
            else
            {
                x_left = x012;
                h_left = h012;
                d_left = d012;
                x_right = x02;
                h_right = h02;
                d_right = d02;
            }

            for(int y=P0.y; y<=P2.y; ++y) 
            {
                int x_l = (int)x_left[y - P0.y];
                int x_r = (int)x_right[y - P0.y];

                var h_segment = Line.Interpolate(x_l, h_left[y-P0.y], x_r, h_right[y-P0.y]);
                var d_segment = Line.Interpolate(x_l, d_left[y-P0.y], x_r, d_right[y-P0.y]);

                for (int x=x_l; x <= x_r; ++x)
                {
                    Color t_color = Utility.Multiply(color, h_segment[x - x_l]);
                    PutPixel(x, y, t_color, d_segment[x - x_l]);
                }
            }
        }
        public void RenderTriangle(Triangle triangle, List<Point> projected)
        {
            DrawWireframeTriangle(projected[triangle.A], projected[triangle.B], projected[triangle.C], triangle.color);
        }

        /// <summary>
        /// render a triangle with depth
        /// </summary>
        /// <param name="triangle"></param>
        /// <param name="projected"></param>
        public void RenderTriangle(Triangle triangle, List<Point3d> vertices)
        {
            DrawTriangle(vertices[triangle.A], vertices[triangle.B], vertices[triangle.C], triangle.color);
        }

        private void DrawTriangle(Point3d v0, Point3d v1, Point3d v2, Color color)
        {
            // note order of v0, v1, v2 here
            var plane = Plane.CreateFromVertices(
                Utility.Point3dToVector3(v0),
                Utility.Point3dToVector3(v1),
                Utility.Point3dToVector3(v2)
                );
            Vector3 l = Vector3.Zero - Utility.Point3dToVector3(v0);

            // back face culling
            if (Vector3.Dot(plane.Normal, l) <= 0)
                return;

            DrawFilledTriangle(
                ProjectVertexToMyPoint(v0, color), 
                ProjectVertexToMyPoint(v1, color), 
                ProjectVertexToMyPoint(v2, color), 
                color
                );      
        }     
    }
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
