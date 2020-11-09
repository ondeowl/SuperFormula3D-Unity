struct MeshPoint
{
	float3 pos; 
	float3 normal;
	float4 tangent;
	float2 uv;
	
	int3 tanInt1;
	int3 tanInt2;
	int3 normalInt;
};

static float PI = 3.14159265359;

//Buffers
RWStructuredBuffer <MeshPoint> meshVBuff;
RWStructuredBuffer <uint> trianglesBuff;
RWStructuredBuffer <float3> meshNormalsBuff;
RWStructuredBuffer <float3> idBuff;

//SuperFormula params
float pi_loops = 1.0;
float sf_M_1 = 3.0;
float sf_N1_1 = 0.5;
float sf_N2_1 = 0.5;
float sf_N3_1 = 0.5;
float sf_M_2 = 0.5;
float sf_N1_2 = 0.5;
float sf_N2_2 = 0.5;
float sf_N3_2 = 0.5;
float a1 = 1, b1 = 1, a2 = 1, b2 = 1;
float sf_scale = 1.0;

//resolution
int xPoints = 100;
int yPoints = 100;
//int numIndices = 1;

//non riesco a capire perché % e mod mi restituiscono risultati sbagliati.
uint myModuloOp(uint a, uint b)
{
	return a - (b * floor(a / b));
}

float3 safeNormalize(float3 v)
{
	if(length(v)>0)
	{
		return normalize(v);
	}
	return float3(0,1,0);
}

float CalculateSuperShapeRadius(float m, float n1,float n2,float n3,float angle, float a, float b)
{
	float r;
	float t1,t2;

	t1 = cos(m * angle / 4) / a;
	t1 = abs(t1);
	t1 = pow(t1, n2);

	t2 = sin(m * angle / 4) / b;
	t2 = abs(t2);
	t2 = pow(t2, n3);

	r = pow(t1 + t2, 1 / n1);
	//r = (r != 0) ? (1.0 / r) : r;
	r = 1 / r;
	return r;
}

float3 SuperFormula3D(uint i, uint j)
{ 
	//SUPERFORMULA
	float theta = PI * (float(i) / (xPoints));
	float r2 = CalculateSuperShapeRadius(sf_M_1, sf_N1_1, sf_N2_1, sf_N3_1, theta, a1, b1) ;
	
	float phi =  (2.0 * PI) * (float(j) / (yPoints-2));
	float r1 = CalculateSuperShapeRadius(sf_M_2, sf_N1_2, sf_N2_2, sf_N3_2, phi, a2, b2) ;

	//MAP TO SPHERE
	float x =  r1 * cos(phi) * r2 * sin(theta);
	float y =  r1 * sin(phi) * r2 * sin(theta);
	float z =  r2 * cos(theta);

	float3 outPos = float3(x,z,y) * sf_scale;
	return outPos;
}


[numthreads(32,32,1)]
void CalculateSuperFormula3D (uint3 id : SV_DispatchThreadID)
{
	//uint cID = id.x;
	uint cID = (id.x + id.y * (yPoints));

	uint i = uint(cID / (xPoints));
	uint j = myModuloOp(cID, yPoints);
	j = (yPoints-1) == j ? myModuloOp(cID-1, yPoints) : j;
	// if(j == yPoints-1)
	// {
	// 	j = myModuloOp(cID-1, yPoints);
	// }
	if (cID == (xPoints*yPoints)) 
	{
		i = xPoints;
		j = yPoints-1; 
	}

	float3 sfNewPos = SuperFormula3D(i, j);
	meshVBuff[cID].pos = sfNewPos;

	float3 d = sfNewPos;
    float u = 1 - (0.5 + (atan2(d.z, -d.x) / (2 * PI * ((float)(yPoints) / yPoints)) ));
    float v = 0.5 + (sin(d.y) / PI);

	meshVBuff[cID].uv = clamp(float2(u,v),0,1);
	meshVBuff[cID].normalInt = int3(0,0,0); //reset normal and tangents for accumulation
	meshVBuff[cID].tanInt1 = int3(0,0,0);
	meshVBuff[cID].tanInt2 = int3(0,0,0);
	
	//DEBUG BUFFER
	//idBuff[cID] = float3(cID, i, j) ;
}

[numthreads(10,10,1)]
void ComputeFaceNormals (uint3 id : SV_DispatchThreadID)
{
	uint cID = id.x * 3;
	
	//Smooth surface normal calculation. 
	//Accumulate Normals and normalize in vertex shader.
	//https://www.iquilezles.org/www/articles/normals/normals.htm
	//https://www.bytehazard.com/articles/vertnorm.html - utile
	uint ia = trianglesBuff[cID];
	uint ib = trianglesBuff[cID+1];
	uint ic = trianglesBuff[cID+2];
	float3 v1 = meshVBuff[ia].pos;
	float3 v2 = meshVBuff[ib].pos;
	float3 v3 = meshVBuff[ic].pos;
	float3 e1 = v2 - v1;
	float3 e2 = v3 - v1;
	float3 norm  = cross( e1, e2 );
	
	//Float normals to int normals
	int3 normI = (int3)(1000000 * norm);
	
	//ok così ci siamo: perdo un po' di precisione castando a int
	//ma non è un gran problema in questo caso
	InterlockedAdd(meshVBuff[ia].normalInt.x, normI.x);
	InterlockedAdd(meshVBuff[ia].normalInt.y, normI.y);
	InterlockedAdd(meshVBuff[ia].normalInt.z, normI.z);
	InterlockedAdd(meshVBuff[ib].normalInt.x, normI.x);
	InterlockedAdd(meshVBuff[ib].normalInt.y, normI.y);
	InterlockedAdd(meshVBuff[ib].normalInt.z, normI.z);
	InterlockedAdd(meshVBuff[ic].normalInt.x, normI.x);
	InterlockedAdd(meshVBuff[ic].normalInt.y, normI.y);
	InterlockedAdd(meshVBuff[ic].normalInt.z, normI.z);

	//TANGENTS
	float2 tc1 = meshVBuff[ia].uv;
    float2 tc2 = meshVBuff[ib].uv;
    float2 tc3 = meshVBuff[ic].uv;

	float s1 = tc2.x - tc1.x;
	float s2 = tc3.x - tc1.x;
	float t1 = tc2.y - tc1.y;
	float t2 = tc3.y - tc1.y;

	float r = 1.0 / (s1 * t2 - s2 * t1);
	
	int3 sdir = (int3) (1000000 *  ( r * ((t2 * e1) - (t1 * e2)) ) );//float3 sdir = r * ((t2 * e1) - (t1*e2));
	int3 tdir = (int3) (1000000 *  ( r * ((s1 * e2) - (s2 * e1)) ) );

	InterlockedAdd(meshVBuff[ia].tanInt1.x, sdir.x);
	InterlockedAdd(meshVBuff[ia].tanInt1.y, sdir.y);
	InterlockedAdd(meshVBuff[ia].tanInt1.z, sdir.z);
	InterlockedAdd(meshVBuff[ib].tanInt1.x, sdir.x);
	InterlockedAdd(meshVBuff[ib].tanInt1.y, sdir.y);
	InterlockedAdd(meshVBuff[ib].tanInt1.z, sdir.z);
	InterlockedAdd(meshVBuff[ic].tanInt1.x, sdir.x);
	InterlockedAdd(meshVBuff[ic].tanInt1.y, sdir.y);
	InterlockedAdd(meshVBuff[ic].tanInt1.z, sdir.z);

	InterlockedAdd(meshVBuff[ia].tanInt2.x, tdir.x);
	InterlockedAdd(meshVBuff[ia].tanInt2.y, tdir.y);
	InterlockedAdd(meshVBuff[ia].tanInt2.z, tdir.z);
	InterlockedAdd(meshVBuff[ib].tanInt2.x, tdir.x);
	InterlockedAdd(meshVBuff[ib].tanInt2.y, tdir.y);
	InterlockedAdd(meshVBuff[ib].tanInt2.z, tdir.z);
	InterlockedAdd(meshVBuff[ic].tanInt2.x, tdir.x);
	InterlockedAdd(meshVBuff[ic].tanInt2.y, tdir.y);
	InterlockedAdd(meshVBuff[ic].tanInt2.z, tdir.z);

	// altrimenti: singolo loop in un unico gruppo e thread
	// NEIN - povera scheda video
	//  for( int i = 0; i < numIndices; i += 3 )
    //     {
    //         uint ia = trianglesBuff[i];
    //         uint ib = trianglesBuff[i+1];
    //         uint ic = trianglesBuff[i+2];
    //         float3 e1 = meshVBuff[ib].pos - meshVBuff[ia].pos;
    //         float3 e2 = meshVBuff[ic].pos - meshVBuff[ia].pos;
    //         float3 norm  = cross( e1, e2 );
    //         meshVBuff[ia].normal += norm; //accumulate face normal in the vertices
    //         meshVBuff[ib].normal += norm;//that are part of the face
    //         meshVBuff[ic].normal += norm;
    //     }
}

[numthreads(32,1,1)]
void SolveTangents (uint3 id : SV_DispatchThreadID)
{
	//https://forum.unity.com/threads/how-to-calculate-mesh-tangents.38984/
	uint i = id.x;
	float3 n = (float3) (meshVBuff[i].normalInt) / 1000000.0;
	float3 t = (float3) (meshVBuff[i].tanInt1) / 1000000.0;
	float3 t2 = (float3) (meshVBuff[i].tanInt2) / 1000000.0;

	// Gram-Schmidt orthogonalize
    float3 finalT = safeNormalize(t.xyz - n * dot(n, t.xyz));
	
	meshVBuff[i].normal = n;
	meshVBuff[i].tangent.xyz = finalT.xyz;
	
	// Calculate handedness
    meshVBuff[i].tangent.w = (dot(cross(n, t), t2) > 0) ? -1 : 1;// * 2 - 1; //? -1.0f : 1.0f;

}