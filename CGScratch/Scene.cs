using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace CGScratch
{
    public class Ball
    {
        public Point3d center { get; set; }
        public double radius { get; set; }
        public ARGB argb { get; set; }
        public Color color
        {
            get
            {
                return Color.FromArgb(this.argb.A, this.argb.R, this.argb.G, this.argb.B);
                //BitConverter.ToInt32(argb, 0)
            }
        }
        public double specular { get; set; }
        public double reflective { get; set; }
        public Ball(Point3d center, double radius)
        {
            this.center = center;
            this.radius = radius;
        }
        public (double, double) IntersectRay(Point3d origin, Point3d direction)
        {
            Point3d co = origin - center;

            double a = direction * direction;
            double b = 2 * (co * direction);
            double c = co * co - radius * radius;

            double discriminant = b * b - 4 * a * c;

            if (discriminant < 0) return (double.PositiveInfinity, double.PositiveInfinity);

            double t1 = (-b + Math.Sqrt(discriminant)) / (2 * a);
            double t2 = (-b - Math.Sqrt(discriminant)) / (2 * a);
            return (t1, t2);
        }
    }
    public class Light
    {
        public string type { get; set; }
        public double intensity { get; set; }
        public Point3d position { get; set; }
        public Point3d direction { get; set; }
    }
    public class Scene
    {
        public List<Ball> balls;
        public List<Light> lights;

        public Scene(string objectsFile, string lightsFile)
        {
            string json1 = File.ReadAllText(objectsFile);
            balls = JsonSerializer.Deserialize<List<Ball>>(json1);
            string json2 = File.ReadAllText(lightsFile);
            lights = JsonSerializer.Deserialize<List<Light>>(json2);
        }
        public (double, Ball?) ClosestIntersection(Point3d origin, Point3d direction, double t_min, double t_max)
        {
            double closest_t = double.PositiveInfinity;
            Ball closest_ball = null;
            foreach (Ball ball in balls)
            {
                var (t1, t2) = ball.IntersectRay(origin, direction);
                if (t1 < closest_t && t1 >= t_min && t1 <= t_max)
                {
                    closest_t = t1;
                    closest_ball = ball;
                }
                if (t2 < closest_t && t2 >= t_min && t2 <= t_max)
                {
                    closest_t = t2;
                    closest_ball = ball;
                }
            }
            return (closest_t, closest_ball);
        }
        public Color TraceRay(Point3d origin, Point3d pos, int depth, double lower, double upper)
        {
            var (closest_t, closest_ball) = ClosestIntersection(origin, pos, lower, upper);

            if (closest_ball == null)
            {
                return Color.Black;
            }
            else
            {
                var P = origin + (closest_t * pos);
                var N = P - closest_ball.center;
                N = N / N.length;
                var localColor = Utility.Multiply(closest_ball.color, ComputeLighting(P, N, 0 - pos, closest_ball.specular));

                double r = closest_ball.reflective;
                if (depth <= 0 || r <= 0)
                {
                    return localColor;
                }

                var R = (0 - pos).ReflectBy(N);
                var reflected_color = TraceRay(P, R, depth - 1, 1e-3, double.PositiveInfinity);

                return Utility.ColorPlus(Utility.Multiply(localColor, 1 - r), Utility.Multiply(reflected_color, r));
            }
        }

        public double ComputeLighting(Point3d P, Point3d N, Point3d V, double s)
        {
            double i = 0;
            foreach (var light in lights)
            {
                if (light.type == "ambient")
                {
                    i += light.intensity;
                }
                else
                {
                    Point3d L; double t_max;
                    if (light.type == "point")
                    {
                        L = light.position - P;
                        t_max = 1;
                    }
                    else
                    {
                        L = light.direction;
                        t_max = double.PositiveInfinity;
                    }

                    var (shadow_t, shadow_object) = ClosestIntersection(P, L, 1e-3, t_max);
                    if (shadow_object != null)
                    {
                        continue;
                    }

                    var n_dot_l = N * L;
                    if (n_dot_l > 0)
                    {
                        i += light.intensity * n_dot_l / N.length / L.length;
                    }

                    if (s != -1)
                    {
                        var R = 2 * (N * L) * N - L;
                        var r_dot_v = R * V;
                        if (r_dot_v > 0)
                        {
                            i += light.intensity * Math.Pow(r_dot_v / R.length / V.length, s);
                        }
                    }
                }
            }
            return i;
        }
        public void DrawLine(Point P0, Point P1, Color color, Canvas canvas)
        {
            if (Math.Abs(P1.X - P0.X) > Math.Abs(P1.Y - P0.Y))
            {
                // Line is horizontal-ish
                if (P0.X > P1.X)
                {
                    Point t = P0;
                    P0 = P1;
                    P1 = t;
                }
                var ys = Line.Interpolate(P0.X, P0.Y, P1.X, P1.Y);
                for (int x = P0.X; x <= P1.X; ++x)
                {
                    canvas.PutPixel(x, (int)ys[x - P0.X], color);
                }
            }
            else
            {
                // vartival-ish
                if (P0.Y > P1.Y)
                {
                    Point t = P0;
                    P0 = P1;
                    P1 = t;
                }
                var xs = Line.Interpolate(P0.Y, P0.X, P1.Y, P1.X);
                for (int y = P0.Y; y <= P1.Y; ++y)
                {
                    canvas.PutPixel((int)xs[y - P0.Y], y, color);
                }
            }
        }
    }
}
