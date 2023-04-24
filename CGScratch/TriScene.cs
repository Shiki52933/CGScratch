using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Complex;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
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

        public Triangle Copy()
        {
            return new Triangle
            {
                A = this.A,
                B = this.B,
                C = this.C,
                argb = this.argb,
                namedColor = this.namedColor
            };
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
                RenderInstance(canvas, I, M);
            }
        }
        public void RenderInstance(Canvas canvas, Instance instance, Matrix<double> homoTransform)
        {
            // get real coordinates of the instance
            List<Point3d> points = new List<Point3d>();
            Model model = this.namedModels[instance.model];
            foreach(var V in model.vertices)
            {
                Point3d V_prime = Utility.HomoToPoint3d(homoTransform * Utility.Point3dToHomo(V));
                points.Add(V_prime);
            }

            // clip instance
            List<Triangle> triangles = new ();
            model.triangles.ForEach(tri => triangles.Add(tri.Copy()));
            Model unnamedModel = new Model
            {
                name = "unnamed",
                vertices = points, // real coordinates
                triangles = triangles // mutable triangles
            };

            // clip instance 
            var afterClipped = ClipInstanceModel(canvas, unnamedModel);

            // if nothing left, return
            if (afterClipped == null)
                return;

            // debug
            //Console.WriteLine("model has "+afterClipped.triangles.Count.ToString()+" triangles");
            //foreach(var tri in afterClipped.triangles)
            //{
            //    string A = afterClipped.vertices[tri.A].ToString();
            //    string B = afterClipped.vertices[tri.B].ToString();
            //    string C = afterClipped.vertices[tri.C].ToString();
            //    Console.WriteLine(A+' '+B+' '+C+tri.color);
            //    Console.WriteLine(new string('-', 50));
            //}


            //--------------------------------plot clipped scene--------------------------------------//

            // ----------this part ignored depth info------------------//
            //List<Point> projected = new();
            //foreach(var point in afterClipped.vertices)
            //{
            //    projected.Add(canvas.ProjectVertex(point));
            //}
            //foreach(var T in afterClipped.triangles)
            //{
            //    canvas.RenderTriangle(T, projected);
            //}

            //------------------consider depth info----------------------//
            List<Point3d> vertices = afterClipped.vertices; 
            foreach(var T in afterClipped.triangles)
            {
                canvas.RenderTriangle(T, vertices);
            }
        }

        public Model? ClipInstanceModel(Canvas canvas, Model model)
        {
            var planes = canvas.GetClippingPlanes();
            foreach(var plane in planes)
            {
                model = ClipInstanceModelByPlane(plane, model);
                if(model == null)
                {
                    break;
                }
            }
            return model;
        }
        /// <summary>
        /// clip instance(delivered by Model) against plane,
        /// bounding ball check is not done here.
        /// </summary>
        /// <param name="plane"></param>
        /// <param name="model"></param>
        /// <returns></returns>
        public Model? ClipInstanceModelByPlane(Plane plane, Model model)
        {
            // computing signed distance using Math.net
            List<float> signedDistences= new List<float>();
            foreach(var v in model.vertices)
            {
                Vector3 vectorV = new Vector3 { X = (float)v.x, Y = (float)v.y, Z = (float)v.z };
                signedDistences.Add(Plane.DotCoordinate(plane, vectorV));
            }

            // triangles
            for(int i=0; i<model.triangles.Count; ++i)
            {
                var triangle = model.triangles[i];
                var d0 = signedDistences[triangle.A];
                var d1 = signedDistences[triangle.B];
                var d2 = signedDistences[triangle.C];

                // Order of triangle's vertices matters. 
                // This flag indicates whether order is wrong and needs reorder.
                bool orderChanged = false;

                // order by big to small
                if(d0 < d1)
                {
                    int t = triangle.A;
                    triangle.A = triangle.B;
                    triangle.B = t;

                    var tt = d0;
                    d0 = d1;
                    d1 = tt;

                    orderChanged = !orderChanged;
                }
                if(d0 < d2)
                {
                    int t = triangle.A;
                    triangle.A = triangle.C;
                    triangle.C = t;

                    var tt = d0;
                    d0 = d2;
                    d2 = tt;

                    orderChanged = !orderChanged;
                }
                if(d1 < d2)
                {
                    int t = triangle.B;
                    triangle.B = triangle.C;
                    triangle.C = t;

                    var tt = d1;
                    d1 = d2;
                    d2 = tt;

                    orderChanged = !orderChanged;
                }

                if (d2 >= 0)
                {
                    if(orderChanged)
                    {
                        int t = triangle.A;
                        triangle.A = triangle.B;
                        triangle.B = t;
                    }
                    // triangle accepted
                } 
                else if (d0 <= 0)
                {
                    // triangle dejected
                    model.triangles.RemoveAt(i);
                    --i;
                    continue;
                } 
                else if (d1 <= 0)
                {
                    // cut to 1 triangle
                    var Bprime = Utility.Intersection(plane, model.vertices[triangle.A], model.vertices[triangle.B]);
                    var Cprime = Utility.Intersection(plane, model.vertices[triangle.A], model.vertices[triangle.C]);
                    model.vertices.Add(Bprime);
                    model.vertices.Add(Cprime);
                    signedDistences.Add(0);
                    signedDistences.Add(0);
                    if (orderChanged)
                    {
                        triangle.B = model.vertices.Count - 1;
                        triangle.C = model.vertices.Count - 2;
                    }
                    else
                    {
                        triangle.B = model.vertices.Count - 2;
                        triangle.C = model.vertices.Count - 1;
                    }
                }
                else
                {
                    var Aprime = Utility.Intersection(plane, model.vertices[triangle.A], model.vertices[triangle.C]);
                    var Bprime = Utility.Intersection(plane, model.vertices[triangle.B], model.vertices[triangle.C]);
                    model.vertices.Add(Aprime);
                    model.vertices.Add(Bprime);
                    signedDistences.Add(0);
                    signedDistences.Add(0);

                    triangle.C = model.vertices.Count - 2;
                    var ApBBp = new Triangle {
                        A = model.vertices.Count - 2,
                        B = triangle.B,
                        C = model.vertices.Count - 1,
                        argb = triangle.argb,
                        namedColor = triangle.namedColor
                    };

                    if (orderChanged)
                    {
                        int t = triangle.A;
                        triangle.A = triangle.B;
                        triangle.B = t;

                        t = ApBBp.A;
                        ApBBp.A = ApBBp.B;
                        ApBBp.B = t;
                    }

                    model.triangles.Insert(i+1, ApBBp);
                    ++i;
                }
            }

            return model.triangles.Count > 0 ? model : null;
        }
    }
}
