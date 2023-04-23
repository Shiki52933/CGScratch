using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CGScratch
{
    public class TestFilledTriangle
    {
        public void Main(string[] args)
        {
            Canvas canvas = new(512, 512);
            ARGB argb = new ARGB { A = 255, R = 0, G = 255, B = 0 };
            MyPoint a = new MyPoint{
                x = -100, y = -100, h = 0.3, argb = argb
            };
            MyPoint b = new MyPoint
            {
                x = 100,
                y = -100,
                h = 0.5,
                argb = argb
            };
            MyPoint c = new MyPoint
            {
                x = 0,
                y = 200,
                h = 1,
                argb = argb
            };

            Console.WriteLine("start plot triangle...");
            Stopwatch stopwatch= Stopwatch.StartNew();
            canvas.DrawFilledTriangle(a, b, c, argb.color);
            stopwatch.Stop();
            Console.WriteLine("end plot triangle, used "+stopwatch.ElapsedMilliseconds.ToString()+"ms");
            canvas.Save("TestFilledTri.png", ImageFormat.Png);

        }
    }
    public class TestModelInstance
    {
        public void Main()
        {
            TriScene triScene = new TriScene("./json/models.json", "./json/instances.json", "./json/camera.json");
            foreach(var t in triScene.models)
            {
                Console.WriteLine(JsonSerializer.Serialize(t));
                Console.WriteLine(new string('-', 80));
            }
            foreach (var t in triScene.instances)
            {
                Console.WriteLine(JsonSerializer.Serialize(t));
                Console.WriteLine(new string('-', 80));
            }
            Console.WriteLine(JsonSerializer.Serialize(triScene.camera));
            Console.WriteLine(new string('-', 80));
        }
    }
    public class TestPlotCubes
    {
        public void Main()
        {
            TriScene triScene = new TriScene("./json/models.json", "./json/instances.json", "./json/camera.json");
            Canvas canvas = new(512, 512, 5, 5, 1);

            Console.WriteLine("enter plot cubes...");
            triScene.RenderScene(canvas);
            canvas.Save("testCubes.png", ImageFormat.Png);
            Console.WriteLine("plot cubes ended...");
        }
    }
}
