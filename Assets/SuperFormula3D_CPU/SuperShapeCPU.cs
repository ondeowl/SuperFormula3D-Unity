using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class SuperShapeCPU : MonoBehaviour
{
    int nPoints = 10000;
    [Range(0.1f, 10f)]
    public float scale = 1f;
    public float PI_loops = 1.0f;
    public float m = 0;
    public float n1 = 1;
    public float n2 = 1;
    public float n3 = 1;
    
    [Range(0.1f, 12f)]
    public float a = 1;
    [Range(0.1f, 12f)]
    public float b = 1;

    public Material circleMat;

    LineRenderer lineRenderer;
    Mesh circleMesh;
    
    struct SuperFormulaMeshPoint
    {
        public Vector3 vertex;
        public Vector2 uv;
        public float radius;
    }

    void Awake()
    {
        circleMesh = new Mesh();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = nPoints;
        Material mat = new Material(Shader.Find("Particles/Standard Unlit"));
        lineRenderer.material = mat;

        createSuperFormulaMesh();
    }

    private void OnValidate()
    {
        if (lineRenderer != null)
        {
            createSuperFormulaMesh();
        }    
    }

    void Update()
    {
        Graphics.DrawMesh(circleMesh, transform.position, transform.rotation, circleMat, 0);
    }

    private void OnDestroy()
    {
        if(lineRenderer != null)
        {
            Destroy(lineRenderer.material);
        }
    }
    
    public void EvalSuperFormulaMesh()
    {
        if (lineRenderer != null)
        {
            createSuperFormulaMesh();
        }
    }

    void createSuperFormulaMesh()
    {
        int numVerticesCirc = nPoints;
        List<Vector3> meshVertices = new List<Vector3>();
        List<Vector3> meshNormals = new List<Vector3>();
        List<Vector2> meshUV = new List<Vector2>();
        List<int> meshTriangles = new List<int>();
        
        meshVertices.Add(new Vector3(0,0,0));
        meshUV.Add(new Vector2(0.5f,0.5f));
        for (int i = 0; i < numVerticesCirc; i++)
        {
            double phi = ((double)i / nPoints) *  Math.PI * 2.0 ;
            SuperFormulaMeshPoint newPos = SuperShape2D(m, n1, n2, n3, a, b, phi);
            lineRenderer.SetPosition(i, newPos.vertex);
            meshVertices.Add(newPos.vertex);
            meshUV.Add(newPos.uv);
            meshTriangles.Add(i+1);
            meshTriangles.Add(0);
            meshTriangles.Add(i+2 < numVerticesCirc+1 ? i+2 : 1);
        }
        circleMesh.SetVertices(meshVertices);
        circleMesh.SetTriangles(meshTriangles, 0);
        circleMesh.SetUVs(0, meshUV);
    }

    SuperFormulaMeshPoint SuperShape2D(double _m, double _n1, double _n2, double _n3, double _a, double _b, double _phi)
    {
        double r; //radius
        double t1, t2;
        double x = 0, y = 0;

        double phi = _phi * PI_loops;
        
        t1 = Math.Cos(_m * phi / 4.0) / _a;
        t1 = Math.Abs(t1);
        t1 = Math.Pow(t1, _n2);

        t2 = Math.Sin(_m * phi / 4.0) / _b;
        t2 = Math.Abs(t2);
        t2 = Math.Pow(t2, _n3);

        r = Math.Pow(t1 + t2, 1.0 / _n1);

        if(Math.Abs(r) == 0)
        {
            x = 0;
            y = 0;
        }
        else
        {
            r = 1 / r;
            x = r * Math.Cos(phi);
            y = r * Math.Sin(phi);
        }
        SuperFormulaMeshPoint sfmp;
        sfmp.vertex = new Vector3((float)x * scale, (float)y * scale, 0);
        //sfmp.uv = new Vector2((float)phi / (Mathf.PI * 2.0f * PI_loops), 2);
        sfmp.uv = new Vector2( (((float)x / (float)r) + 1) * 0.5f, 1-(((float)y / (float)r) + 1) * 0.5f);
        sfmp.radius = (float)r;
        return sfmp;
    }
}
