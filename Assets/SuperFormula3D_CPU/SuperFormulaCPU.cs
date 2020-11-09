using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class SuperFormulaCPU : MonoBehaviour
{
    struct SuperFormulaMeshPoint
    {
        public Vector3 vertex;
        public Vector2 uv;
        public Vector3 normal;
    }

    //private
    int xPoints = 100;
    int yPoints = 100;
    int nPoints = 10000;

    List<Vector3> meshVertices;
    List<Vector2> meshUV;
    List<int> meshTriangles;
    List<Vector3> meshNormals;
    LineRenderer lineRenderer;
    Mesh sphereMesh;

    //public
    public SuperShapeCPU superShapeCPU;
    public bool bSuperShapeBackground = true;

    [Range(0.1f, 10f)]
    public float scale = 1.0f;
    public float PI_loops = 1.0f;
    public float m1 = 0;
    public float n11 = 1;
    public float n21 = 1;
    public float n31 = 1;
    
    [Range(0.1f, 12f)]
    public float a1 = 1;
    [Range(0.1f, 12f)]
    public float b1 = 1;

    public float m2 = 0;
    public float n12 = 1;
    public float n22 = 1;
    public float n32 = 1;
    
    [Range(0.1f, 12f)]
    public float a2 = 1;
    [Range(0.1f, 12f)]
    public float b2 = 1;    
    public Material circleMat;

    void Awake()
    {
        nPoints = xPoints * yPoints + 1; 

        //mesh
        sphereMesh = new Mesh();
        meshVertices = new List<Vector3>();
        meshUV = new List<Vector2>();
        meshTriangles = new List<int>();
        meshNormals = new List<Vector3>();

        //initialize line renderer 
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = nPoints;
        Material mat = new Material(Shader.Find("Unlit/Color"));
        lineRenderer.material = mat;
        
        createSFMesh();
        editSFMesh();
    }

    private void OnValidate()
    {
        if (lineRenderer != null)
        {
            editSFMesh();
        }

        if(superShapeCPU != null && bSuperShapeBackground)
        {
            superShapeCPU.m = m2;
            superShapeCPU.n1 = n12;
            superShapeCPU.n2 = n22;
            superShapeCPU.n3 = n32;
            superShapeCPU.a = a2;
            superShapeCPU.b = b2;
            superShapeCPU.EvalSuperFormulaMesh();
        }    
    }

    void Update()
    {
        Graphics.DrawMesh(sphereMesh, transform.position, transform.rotation, circleMat, 0);
    }

    private void OnDestroy()
    {
        if(lineRenderer != null)
        {
            Destroy(lineRenderer.material);
        }
    }
    
    void createSFMesh()
    {
        meshVertices.Clear();
        meshNormals.Clear();
        meshTriangles.Clear();
        meshUV.Clear();

        //Vertices, UV, Normals
        for (int i = 0; i < xPoints; i++)
        {
            double theta = ((double)i / xPoints) *  Math.PI;
            for(int k = 0; k < yPoints; k++)
            {
                double phi = ((double)k / (yPoints-1)) *  Math.PI * 2.0;
                SuperFormulaMeshPoint newPos = SuperFormula3D(m1, n11, n21, n31, a1, b1, theta,
                                                            m2, n12, n22, n32, a2, b2, phi);
                lineRenderer.SetPosition(k + i * yPoints, newPos.vertex);
                meshVertices.Add(newPos.vertex);
                meshNormals.Add(newPos.normal); 
                meshUV.Add(new Vector2(  (float)k / yPoints, 1f - (float)i / xPoints ));
            }
        }

        //last point
        double theta2 = Math.PI;
        double phi2 = Math.PI * 2.0;
        SuperFormulaMeshPoint newPos2 = SuperFormula3D(m1, n11, n21, n31, a1, b1, theta2,
                                                m2, n12, n22, n32, a2, b2, phi2);
        meshVertices.Add(newPos2.vertex);
        meshNormals.Add(newPos2.normal);
        meshUV.Add(Vector2.zero);

        //assign buffers
        sphereMesh.SetVertices(meshVertices);
        sphereMesh.SetUVs(0, meshUV);
        sphereMesh.SetNormals(meshNormals);

        //Triangles
        for( int i = 0; i < xPoints - 1; i++ )
        {
            for( int k = 0; k < yPoints - 1; k++ )
            {
                int current = k + i * (yPoints) ;
                int next = current + yPoints;

                meshTriangles.Add(current);
                meshTriangles.Add(current + 1);
                meshTriangles.Add(next + 1);
        
                meshTriangles.Add(current);
                meshTriangles.Add(next + 1);
                meshTriangles.Add(next);
            }
        }
        //Bottom Cap
        for(int k = 0; k < yPoints; k++ )
        {
            meshTriangles.Add(meshVertices.Count - 1);
            meshTriangles.Add(meshVertices.Count - (k+2) - 1);
            meshTriangles.Add(meshVertices.Count - (k+1) - 1);
        }
        sphereMesh.SetTriangles(meshTriangles, 0);

        //recalc normals
        //sphereMesh.RecalculateNormals();
    }

    void editSFMesh()
    {
        meshVertices.Clear();
        meshNormals.Clear();

        //Vertices
        for (int i = 0; i < xPoints; i++)
        {
            double theta = ((double)i / xPoints) *  Math.PI;
            for(int k = 0; k < yPoints; k++)
            {
                double phi = ((double)k / (yPoints-1)) *  Math.PI * 2.0;
                SuperFormulaMeshPoint newPos = SuperFormula3D(m1, n11, n21, n31, a1, b1, theta,
                                                            m2, n12, n22, n32, a2, b2, phi);
                lineRenderer.SetPosition(k + i * yPoints, newPos.vertex);
                meshVertices.Add(newPos.vertex);
                meshNormals.Add(newPos.normal);
            }
        }
        //last vertex
        double theta2 = Math.PI;
        double phi2 = Math.PI * 2.0;
        SuperFormulaMeshPoint newPos2 = SuperFormula3D(m1, n11, n21, n31, a1, b1, theta2,
                                                m2, n12, n22, n32, a2, b2, phi2);
        meshVertices.Add(newPos2.vertex);
        meshNormals.Add(newPos2.normal);
        sphereMesh.SetVertices(meshVertices);    
        
        //Calculate Smooth Normals
        //Inigo Quilez method without division
        //(with normalization at the end of the accumulation phase).
        //This approach creates smooth normals without taking into accounts
        //smoothing groups and hard edges.
        //This seems to works in CPU while it creates bad artifacts when
        //ported on to compute shader -> Find out why!
        for( int i = 0; i < meshNormals.Count; i++)
        {
            meshNormals[i] = Vector3.zero; //resetting normals
        }
        sphereMesh.SetNormals(meshNormals);
        for( int i = 0; i < meshTriangles.Count; i+=3 )
        {
            int ia = meshTriangles[i];
            int ib = meshTriangles[i+1];
            int ic = meshTriangles[i+2];
            Vector3 e1 = meshVertices[ib] - meshVertices[ia];
            Vector3 e2 = meshVertices[ic] - meshVertices[ia];
            Vector3 norm = Vector3.Cross( e1, e2 );
            meshNormals[ia] += norm; //accumulate face normal in the vertices
            meshNormals[ib] += norm;//that are part of the face
            meshNormals[ic] += norm;
        }
        for( int i = 0; i < meshNormals.Count; i++)
        {
            meshNormals[i].Normalize(); //finally normalize the accumulated normal
        }
        sphereMesh.SetNormals(meshNormals);//setting normals
        //sphereMesh.RecalculateNormals();//unity normal recalculation method
    }

    SuperFormulaMeshPoint SuperFormula3D( double _m1, double _n11, double _n21, double _n31, double _a1, double _b1, double _theta,
                                          double _m2, double _n12, double _n22, double _n32, double _a2, double _b2, double _phi
                                        )
    {
        double r1, r2; //radius
        double t11, t21, t12, t22;
        double x = 0, y = 0, z = 0;

        double theta = _theta * PI_loops;
        
        t11 = Math.Cos(_m1 * theta / 4.0) / _a1;
        t11 = Math.Abs(t11);
        t11 = Math.Pow(t11, _n21);

        t21 = Math.Sin(_m1 * theta / 4.0) / _b1;
        t21 = Math.Abs(t21);
        t21 = Math.Pow(t21, _n31);

        r2 = Math.Pow(t11 + t21, 1.0 / _n11);

        double phi = _phi * PI_loops;

        t12 = Math.Cos(_m2 * phi / 4.0) / _a2;
        t12 = Math.Abs(t12);
        t12 = Math.Pow(t12, _n22);

        t22 = Math.Sin(_m2 * phi / 4.0) / _b2;
        t22 = Math.Abs(t22);
        t22 = Math.Pow(t22, _n32);

        r1 = Math.Pow(t12 + t22, 1.0 / _n12);

        // if(Math.Abs(r1) == 0)
        // {
        //     x = 0;
        //     y = 0;
        //     z = 0;
        // }
        // else
        {
            r1 = 1 / r1;
            r2 = 1 / r2;
            x = r1 * Math.Cos(phi) * r2 * Math.Sin(theta);
            z = r1 * Math.Sin(phi) * r2 * Math.Sin(theta);
            y = r2 * Math.Cos(theta);
        }

        //output
        SuperFormulaMeshPoint sfmp;
        sfmp.vertex = new Vector3((float)x * scale, (float)y * scale, (float)z * scale);
        sfmp.uv = new Vector2( ((sfmp.vertex.x / (float)r1) + 1) * 0.5f, 1-((sfmp.vertex.y / (float)r1) + 1) * 0.5f);
        sfmp.normal = sfmp.vertex.normalized;
        return sfmp;
    }
}
