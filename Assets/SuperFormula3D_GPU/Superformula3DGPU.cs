using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Superformula3DGPU : MonoBehaviour 
{
    //Mesh point structure
    struct MeshPoint
    {
        public Vector3 pos;
        public Vector3 normal;
        public Vector4 tangent;
        public Vector2 uv;

        public Vector3Int normalInt;
        public Vector3Int tan1Int;
        public Vector3Int tan2Int;
    }

    //Buffers
    private ComputeBuffer meshPointsBuffer;
    private ComputeBuffer trianglesBuffer;
    private ComputeBuffer debugBuff;
    private ComputeBuffer buffwithArgs;
    List<MeshPoint> meshPoints;
    List<int> meshTriangles;

    int sfKernel = 0;
    int normalsKernel = 1;
    int tangentsKernel = 2;
    uint sfKThreadGSizeX, sfKThreadGSizeY, sfKThreadGSizeZ;
    uint normKThreadGSizeX, normKThreadGSizeY, normKThreadGSizeZ;
    uint tanKThreadGSizeX, tanKThreadGSizeY, tanKThreadGSizeZ;
    private Bounds bnds;
    private bool bInit = false;
    private bool bNeedsUpdate = false;

    //resolution
    int xPoints = 20;
    int yPoints = 20;
    int nPoints = 400;

    [Header("SuperFormula")]
    [SerializeField] private ComputeShader computeShader_SF;
    public Material sf_mat;
    public Material sf_mat_wireframe;
    public bool bDrawWireframe = false;
    
    [Range(10, 500)]
    public int sf_resolution = 500;
    [Range(0.1f, 10f)]
    public float sf_scale = 1.0f;
    [Range(0.1f, 10f)]
    public float PI_loops = 1.0f;
    
    [Header("SuperShape_1")]
    [Min(0.0f)]
    public float m1 = 0;
    [Min(0.0f)]
    public float n11 = 1;
    [Min(0.0f)]
    public float n21 = 1;
    [Min(0.0f)]
    public float n31 = 1;
    [Range(0.1f, 4f)]
    public float a1 = 1;
    [Range(0.1f, 4f)]
    public float b1 = 1;

    [Header("SuperShape_2")]
    [Min(0.0f)]
    public float m2 = 0;
    [Min(0.0f)]
    public float n12 = 1;
    [Min(0.0f)]
    public float n22 = 1;
    [Min(0.0f)]
    public float n32 = 1;
    [Range(0.1f, 4f)]
    public float a2 = 1;
    [Range(0.1f, 4f)]
    public float b2 = 1;

    private void Start()
    {
        //init lists
        meshPoints = new List<MeshPoint>();
        meshTriangles = new List<int>();
        
        //Kernels
        sfKernel = computeShader_SF.FindKernel("CalculateSuperFormula3D");
        normalsKernel = computeShader_SF.FindKernel("ComputeFaceNormals");
        tangentsKernel = computeShader_SF.FindKernel("SolveTangents");
        computeShader_SF.GetKernelThreadGroupSizes(sfKernel, out sfKThreadGSizeX, out sfKThreadGSizeY, out sfKThreadGSizeZ );
        computeShader_SF.GetKernelThreadGroupSizes(normalsKernel, out normKThreadGSizeX, out normKThreadGSizeY, out normKThreadGSizeZ );
        computeShader_SF.GetKernelThreadGroupSizes(tangentsKernel, out tanKThreadGSizeX, out tanKThreadGSizeY, out tanKThreadGSizeZ );
        
        //Resolution
        InitializeBuffers();

        //First Dispatch
        DispatchComputeSFMesh();        

        bInit = true;
        bnds.extents = new Vector3(100,100,100);
    }

    // void OnValidate()
    // {
    //     Debug.Log("OnValidate");
        
    // }
    
    void OnValidate()
    {
        #if UNITY_EDITOR
            bNeedsUpdate = true;
        #endif
    }

    void Update()
    {
        bnds.center = transform.position;
        
        if(bInit)
        {
            if (sf_resolution != xPoints)
            {
                //reinit mesh data
                DisposeBuffers();
                InitializeBuffers(); //reinitialize buffers
                bNeedsUpdate = true;
            }
            //
           //recalculate mesh 
           //DispatchComputeSFMesh(); 
        }
        if (bNeedsUpdate)
        {
            //recalculate mesh 
            DispatchComputeSFMesh();
            bNeedsUpdate = false;    
        }
         
        if(!bDrawWireframe)
        {
            //Assign Buffers to material
            sf_mat.SetBuffer("pointBuffer", meshPointsBuffer);
            sf_mat.SetBuffer("trianglesBuffer", trianglesBuffer);
            sf_mat.SetMatrix("TRSmatrix", this.transform.localToWorldMatrix);    
            //Draw triangles
            Graphics.DrawProcedural(sf_mat, bnds, MeshTopology.Triangles, trianglesBuffer.count, 1, Camera.main, null, ShadowCastingMode.On, true );
            //Graphics.DrawProceduralIndirect(sf_mat, bnds, MeshTopology.Triangles, buffwithArgs);
        }
        else
        {
            //Assign Buffers to material
            sf_mat_wireframe.SetBuffer("pointBuffer", meshPointsBuffer);
            sf_mat_wireframe.SetBuffer("trianglesBuffer", trianglesBuffer);
            sf_mat_wireframe.SetMatrix("TRSmatrix", this.transform.localToWorldMatrix);    
            //Draw wireframe
            Graphics.DrawProcedural(sf_mat_wireframe, bnds, MeshTopology.Lines, trianglesBuffer.count, 1, Camera.main, null, ShadowCastingMode.On, true );
        }        
    }

    private void OnDestroy()
    {
        DisposeBuffers();
    }

    public void DispatchComputeSFMesh()
    {
        SetSuperformulaParameters();
        computeShader_SF.SetInt("numIndices", meshTriangles.Count);
        computeShader_SF.SetInt("xPoints", xPoints);
        computeShader_SF.SetInt("yPoints", yPoints);
        
        //superformula kernel dispatch
        //normals are cleared each loop before recalculation
        computeShader_SF.SetBuffer(sfKernel, "meshVBuff", meshPointsBuffer);
        computeShader_SF.Dispatch(sfKernel, xPoints / (int)sfKThreadGSizeX + 1, yPoints / (int)sfKThreadGSizeY + 1, 1); 
        
        //normals kernel dispatch
        computeShader_SF.SetBuffer(normalsKernel, "meshVBuff", meshPointsBuffer);
        computeShader_SF.SetBuffer(normalsKernel, "trianglesBuff", trianglesBuffer);
        computeShader_SF.Dispatch(normalsKernel, meshTriangles.Count / (int)normKThreadGSizeX / 3 , (int)normKThreadGSizeY / 3, 1);

        //tangents kernel dispatch
        computeShader_SF.SetBuffer(tangentsKernel, "meshVBuff", meshPointsBuffer);
        computeShader_SF.Dispatch(tangentsKernel, nPoints / (int)tanKThreadGSizeX + 1, 1, 1);
    }

    void InitializeBuffers()
    {
        ///resolution
        xPoints = yPoints = sf_resolution;
        
        //initialize empty mesh data (modeled on a sphere)
        InitSFMesh();

        nPoints = meshPoints.Count;
        Debug.Log("Mesh Resolution: ");
        Debug.Log("numvertices: " + nPoints);
        Debug.Log("numtriangles: " + meshTriangles.Count);
        Debug.Log("numfaces: " + meshTriangles.Count / 3);
        
        meshPointsBuffer = new ComputeBuffer(meshPoints.Count, (3 + 3 + 4 + 2) * sizeof(float) + (3 + 3 + 3) * sizeof(int));
        meshPointsBuffer.SetData(meshPoints);

        trianglesBuffer = new ComputeBuffer(meshTriangles.Count, sizeof(int));
        trianglesBuffer.SetData(meshTriangles);

        //DEBUG OUTPUT
        // List<Vector3>idData = new List<Vector3>();
        // //debugBuff = new ComputeBuffer((meshTriangles.Count) / 3, sizeof(float) * 3);
        // debugBuff = new ComputeBuffer(meshPoints.Count, sizeof(float) * 3);
        // debugBuff.SetData(idData);

        // Setup argbuffer for indirect
        // uint indexCountPerInstance = 3;
        // uint instanceCount = (uint)trianglesBuffer.count;
        // uint startIndexLocation = 0;
        // uint baseVertexLocation = 0;
        // uint startInstanceLocation = 0;
        // uint[] args = new uint[] { indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation };
        // buffwithArgs = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        // buffwithArgs.SetData(args);
    }

    void DisposeBuffers()
    {
        if (meshPointsBuffer != null) meshPointsBuffer.Release();
        if (trianglesBuffer != null) trianglesBuffer.Release();
        if (debugBuff != null) debugBuff.Release();
        if (buffwithArgs != null) buffwithArgs.Release();
    }

    void InitSFMesh()
    {
        meshPoints.Clear();
        meshTriangles.Clear();
        
        // // //First point
        // MeshPoint firstP = new MeshPoint();
        // firstP.pos = Vector3.zero;
        // firstP.uv = Vector2.zero;
        // meshPoints.Add(firstP);

        //precomputed uvs, dummy vertices and normals
        for (int i = 0; i < xPoints; i++)
        {
            double theta = ((double)i / xPoints) *  Math.PI;
            for(int k = 0; k < yPoints; k++)
            {
                double phi = ((double)k / (yPoints-1)) *  Math.PI * 2.0;
                MeshPoint newPoint = new MeshPoint();
                newPoint.uv = new Vector2( (float)k / yPoints, 1f - ((float)i / xPoints) );
                newPoint.pos = Vector3.zero;
                newPoint.normalInt = Vector3Int.zero;
                meshPoints.Add(newPoint);
            }
        }

        //last point
        MeshPoint lastP = new MeshPoint();
        lastP.pos = Vector3.zero;
        lastP.uv = Vector2.one;
        //lastP.normal = Vector3.zero;
        meshPoints.Add(lastP);
        
        // //add pole (top)
        // for(int k = 0; k < yPoints; k++ )
        // {
        //     meshTriangles.Add(k+2);
        //     meshTriangles.Add(k+1);
        //     meshTriangles.Add(k);
        // }
        //triangles
        for( int i = 0; i < xPoints -1 ; i++ )
        {
            for( int k = 0; k < yPoints  ; k++ )
            {
                int current = (k + i * (yPoints)) % (meshPoints.Count-1);// % nPoints;
                int next = (current + yPoints) % (meshPoints.Count-1);// % nPoints;
                if (k != yPoints-1)
                {
                    meshTriangles.Add(current);
                    meshTriangles.Add(current + 1);
                    meshTriangles.Add(next + 1);

                    meshTriangles.Add(current);
                    meshTriangles.Add(next + 1);
                    meshTriangles.Add(next);
                }
                else{
                    meshTriangles.Add(current);
                    meshTriangles.Add(current + 1);
                    meshTriangles.Add(next + 1);

                    meshTriangles.Add(current);
                    meshTriangles.Add(next + 1);
                    meshTriangles.Add(next);
                }
                
            }
        }
        //add pole (bottom)
        for(int k = 0; k < yPoints; k++ )
        {
            meshTriangles.Add(meshPoints.Count - 1);
            meshTriangles.Add(meshPoints.Count - (k+2) - 1);
            meshTriangles.Add(meshPoints.Count - (k+1) - 1);
        }
    }

    private void SetSuperformulaParameters()
    {
        computeShader_SF.SetInt("xPoints", xPoints);
        computeShader_SF.SetInt("yPoints", yPoints);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_scale"), sf_scale);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_M_1"), m1);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_M_2"), m2);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N1_1"), n11);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N1_2"), n12);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N2_1"), n21);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N2_2"), n22);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N3_1"), n31);
        computeShader_SF.SetFloat(Shader.PropertyToID("sf_N3_2"), n32);
        computeShader_SF.SetFloat(Shader.PropertyToID("a1"), a1);
        computeShader_SF.SetFloat(Shader.PropertyToID("a2"), a2);
        computeShader_SF.SetFloat(Shader.PropertyToID("b1"), b1);
        computeShader_SF.SetFloat(Shader.PropertyToID("b2"), b2);
    }
    public void setM1(string stringV)
    {
        m1 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setM2(string stringV)
    {
        m2 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN11(string stringV)
    {
        n11 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN12(string stringV)
    {
        n12 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN21(string stringV)
    {
        n21 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN22(string stringV)
    {
        n22 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN31(string stringV)
    {
        n31 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setN32(string stringV)
    {
        n32 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setA1(string stringV)
    {
        a1 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setB1(string stringV)
    {
        b1 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setA2(string stringV)
    {
        a2 = float.Parse(stringV);
        bNeedsUpdate = true;
    }
    public void setB2(string stringV)
    {
        b2 = float.Parse(stringV);
        bNeedsUpdate = true;
    }

    public void setNeedsUpdate(bool bUpd = true)
    {
        bNeedsUpdate = bUpd;
    }
}


