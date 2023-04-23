using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace CGScratch
{
    public class Triangle
    {
        public int A { get; set; }
        public int B { get; set; }
        public int C { get; set; }
        public ARGB argb { get; set; }
        public string? namedColor { get; set; }
        public Color color
        {
            get
            {
                if(namedColor != null)
                {
                    return Color.FromName(namedColor);
                }
                else
                {
                    return argb.color;
                }
            }
        }
    }
    public class Model
    {
        public string name { get; set; }
        public List<Point3d> vertices { get; set; }
        public List<Triangle> triangles { get; set; }
    }
    public class Transform
    {
        public Point3d scale { get; set; }
        public Point3d rotation { get; set; }
        public Point3d translation { get; set; }
        public Matrix<double> homogeneousMatrix { get; set; }

        public void UpdateMatrix()
        {
            var t_m = Utility.TranslationToMatrix(translation);
            var r_m = Utility.RotateToMatrix(rotation);
            var s_m = Utility.ScaleToMatrix(scale);
            homogeneousMatrix = t_m * r_m * s_m;
        }
    }
    public class Instance
    {
        public string model { get; set; }
        public Transform transform { get; set; }
    }
    public class Camera
    {
        public Point3d position { get; set; }
        public Point3d rotation { get; set; }
        public Matrix<double> homogeneousMatrix
        {
            get
            {
                return (Utility.TranslationToMatrix(position) * Utility.RotateToMatrix(rotation)).Inverse();
            }
        }
    }
    public class TriScene
    {
        public List<Model> models { get; set; }
        public Dictionary<string, Model> namedModels { get; }
        public List<Instance> instances { get; set; }
        public Camera camera { get; set; }

        public TriScene(string model_file, string instance_file, string camera_file)
        {
            // 读models
            string model_str = File.ReadAllText(model_file);
            models = JsonSerializer.Deserialize<List<Model>>(model_str);

            namedModels = new();
            foreach(var m in models)
            {
                namedModels[m.name] = m;
            }

            // 读instances
            string instance_str = File.ReadAllText(instance_file);
            instances = JsonSerializer.Deserialize<List<Instance>>(instance_str);
            foreach(var instance in instances)
            {
                // 准备好齐次矩阵
                instance.transform.UpdateMatrix();
            }

            // 读camera
            string camera_str = File.ReadAllText(camera_file);
            camera = JsonSerializer.Deserialize<Camera>(camera_str);
        }
        public void RenderScene(Canvas canvas)
        {
            var M_camera = this.camera.homogeneousMatrix;

            foreach(var I in instances)
            {
                var M = M_camera * I.transform.homogeneousMatrix;
                RenderModel(canvas, namedModels[I.model], M);
            }
        }
        public void RenderModel(Canvas canvas, Model model, Matrix<double> homoTransform)
        {
            List<Point> projected = new List<Point> ();

            foreach(var V in model.vertices)
            {
                Point3d V_prime = Utility.HomoToPoint3d(homoTransform * Utility.Point3dToHomo(V));
                projected.Add(canvas.ProjectVertex(V_prime));
            }

            foreach(var T in model.triangles)
            {
                canvas.RenderTriangle(T, projected);
            }
        }
    }
}
