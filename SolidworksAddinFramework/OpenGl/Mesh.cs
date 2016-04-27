using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.LinearAlgebra.Double;
using SolidworksAddinFramework.Geometry;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace SolidworksAddinFramework.OpenGl
{
    public class Mesh : IRenderable
    {
        private readonly IReadOnlyList<TriangleWithNormals> _OriginalTriangleVerticies;
        private IReadOnlyList<Edge3> _OriginalEdgeVertices;
        private readonly Color _Color;





        public Mesh(IBody2 body, Color color)
        {
            if (body == null) throw new ArgumentNullException(nameof(body));

            var faceList = body.GetFaces().CastArray<IFace2>();
            var tess = GetTess(body, faceList);
            var tris = Tesselate(faceList, tess)
                .Buffer(3,3)
                .Select(b=>new TriangleWithNormals(b[0],b[1],b[2])).ToList();
            Edges = new List<Edge3>();
            TrianglesWithNormals = tris.ToList();
            _OriginalTriangleVerticies = TrianglesWithNormals;
            _OriginalEdgeVertices = Edges;
            _Color = color;
        }

        public Mesh(Color color, IEnumerable<Triangle> enumerable,IReadOnlyList<Edge3> edges = null)
        {
            TrianglesWithNormals = enumerable.Select(p=>(TriangleWithNormals)p).ToList();
            Edges = edges;
            _Color = color;
        }
        public Mesh(Color color,IEnumerable<TriangleWithNormals> enumerable,IReadOnlyList<Edge3> edges = null)
        {
            TrianglesWithNormals = enumerable.ToList();
            Edges = edges ?? new List<Edge3>();
            _Color = color;
        }


        public IReadOnlyList<Edge3> Edges { get; private set; }

        public IReadOnlyList<TriangleWithNormals> TrianglesWithNormals { get; set; }

        public IReadOnlyList<Triangle> Triangles => 
            TrianglesWithNormals
                .Select(b => (Triangle) b )
                .ToList();


        public void Render(DateTime time)
        {
            MeshRender.Render(this, _Color);
        }

        public static List<PointDirection3> Tesselate(IFace2[] faceList, ITessellation tess)
        {
            var r = new List<PointDirection3>();
            // performance improvement
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int index = 0; index < faceList.Length; index++)
            {
                var face = faceList[index];
                foreach (var facet in tess.GetFaceFacets(face).CastArray<int>())
                {
                    var finIds = tess.GetFacetFins(facet).CastArray<int>();
                    var vertexIds = finIds
                        .SelectMany(finId => tess.GetFinVertices(finId).CastArray<int>())
                        .DistinctUntilChanged()
                        .SkipLast(1)
                        .ToList();

                    var vertexs = vertexIds
                        .Select(vId => tess.GetVertexPoint(vId).CastArray<double>())
                        .ToList();

                    var normals = vertexIds
                        .Select(vId => tess.GetVertexNormal(vId).CastArray<double>())
                        .ToList();

                    // Unroll loop for performance
                    r.Add(new PointDirection3(vertexs[0].ToVector3D(), normals[0].ToVector3D()));
                    r.Add(new PointDirection3(vertexs[1].ToVector3D(), normals[1].ToVector3D()));
                    r.Add(new PointDirection3(vertexs[2].ToVector3D(), normals[2].ToVector3D()));
                }
            }

            return r;
        }

        public static ITessellation GetTess(IBody2 body, IFace2[] faceList)
        {
            var tess = (ITessellation)body.GetTessellation(faceList);
            tess.NeedFaceFacetMap = true;
            tess.NeedVertexParams = true;
            tess.NeedVertexNormal = true;
            tess.ImprovedQuality = true;
            tess.NeedEdgeFinMap = true;
            tess.CurveChordTolerance = 0.001 / 10;
            tess.SurfacePlaneTolerance = 0.001 / 10;
            tess.MatchType = (int)swTesselationMatchType_e.swTesselationMatchFacetTopology;
            tess.Tessellate();
            return tess;

        }

        /// <summary>
        /// Apply the transform to the ORIGINAL mesh that was created. Multiple
        /// calls are NOT cumulative.
        /// </summary>
        /// <param name="transform"></param>
        public void ApplyTransform(Matrix4x4 transform)
        {
            Vector3 scale;
            Quaternion rotation;
            Vector3 translation;
            Matrix4x4.Decompose(transform, out scale, out rotation, out translation);

            TrianglesWithNormals = _OriginalTriangleVerticies
                .Select(pn => pn.ApplyTransform(transform, rotation))
                .ToList();

            Edges = _OriginalEdgeVertices
                .Select(pn => pn.ApplyTransform(transform))
                .ToList();

        }
    }
}