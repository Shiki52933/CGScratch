using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using CGScratch;


// start of test region...
var test0 = new TestModelInstance();
test0.Main();
var test = new TestPlotCubes();
test.Main();
// end of test region


const int cw = 512;
const int ch = 512;
int cores = Environment.ProcessorCount;

Canvas canvas = new Canvas(cw, ch);
Scene scene = new Scene("./json/scene.json", "./json/light.json");

foreach(var b in scene.balls)
{
    Console.WriteLine(b.center.ToString());
    Console.WriteLine(b.radius);
    Console.WriteLine(b.color.ToString());
}
Console.WriteLine(cores.ToString() + " threads working...");


Point3d origin = new Point3d(0, 0, 0);

Stopwatch stopwatch = Stopwatch.StartNew();


List<Task> tasks = new List<Task>();
for(int i=0; i < cores; i++)
{
    int inf = -cw / 2 + i * cw / cores;
    int sup = -cw / 2 + (i + 1) * cw / cores;
    sup = Math.Min(sup, cw/2);
    var task = Task.Run(() =>
    {
        for (int x = inf; x < sup; ++x)
        {
            for (int y = -ch / 2; y < ch / 2; ++y)
            {
                var D = canvas.CanvasToViewport(x, y);
                var color = scene.TraceRay(origin, D, 3, canvas.d, double.MaxValue);
                canvas.PutPixel(x, y, color);
            }
        }
    });
    tasks.Add(task);
}
foreach(var task in tasks)
{ task.Wait(); }


/*
for (int x= -cw/2; x<cw/2; ++x)
{
    for (int y = -ch / 2; y < ch / 2; ++y)
    {
        var D = canvas.CanvasToViewport(x, y);
        var color = scene.TraceRay(origin, D, 3, canvas.d, double.MaxValue);
        canvas.PutPixel(x, y, color);
    }
}
*/

stopwatch.Stop();
Console.WriteLine("plot scene used "+stopwatch.ElapsedMilliseconds+" ms");
canvas.Save("picture.png", ImageFormat.Png);

// Graphics canvas = Graphics.FromImage(bitmap);
Console.Write("Press any key to continue . . . ");
Console.ReadKey(true);


public struct Point3d
{
    public double x { get; set; }
    public double y { get; set; }
    public double z { get; set; }
    public double length
    {
        get
        {
            return Math.Sqrt(this * this);
        }
    }
    public override string ToString() => "x="+x.ToString()+" y="+y.ToString()+" z="+z.ToString();
    public Point3d(double _x, double _y, double _z)
    {
        x = _x;
        y = _y;
        z = _z;
    }
    public static double operator*(Point3d a, Point3d b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    public static Point3d operator*(double a, Point3d b)
    {
        return new Point3d(a * b.x, a * b.y, a * b.z);
    }
    public static Point3d operator/(Point3d b, double a)
    {
        return new Point3d(b.x/a, b.y/a, b.z/a);
    }
    public static Point3d operator+(Point3d a, Point3d b)
    {
        return new Point3d(a.x + b.x, a.y + b.y, a.z + b.z);
    }
    public static Point3d operator-(Point3d a, Point3d b)
    {
        return new Point3d(a.x - b.x, a.y - b.y, a.z - b.z);
    }
    public static Point3d operator -(double a, Point3d b)
    {
        return new Point3d(a - b.x, a - b.y, a - b.z);
    }
    public Point3d ReflectBy(Point3d N)
    {
        return 2 * (this * N) * N - this;
    }
};
