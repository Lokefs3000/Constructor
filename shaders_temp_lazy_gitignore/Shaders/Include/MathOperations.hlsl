#ifndef MATHOPERATION_HLSL
#define MATHOPERATION_HLSL

#include "WorldBuffers.hlsl"

float LinearizeDepth(float depth)
{
    return cbCameraData.ZNear * cbCameraData.ZFar / (cbCameraData.ZFar + depth * (cbCameraData.ZNear - cbCameraData.ZFar));
}

// https://www.shadertoy.com/view/3s33zj
float3x3 AdjugateModelToNormal( in float4x4 m )
{
    return float3x3(cross(m[1].xyz, m[2].xyz), 
                    cross(m[2].xyz, m[0].xyz), 
                    cross(m[0].xyz, m[1].xyz));

    /*
    // alternative way to write the adjoint

    return mat3( 
     m[1].yzx*m[2].zxy-m[1].zxy*m[2].yzx,
     m[2].yzx*m[0].zxy-m[2].zxy*m[0].yzx,
     m[0].yzx*m[1].zxy-m[0].zxy*m[1].yzx );
    */
    
    /*
    // alternative way to write the adjoint

    return mat3( 
     m[1][1]*m[2][2]-m[1][2]*m[2][1],
     m[1][2]*m[2][0]-m[1][0]*m[2][2],
     m[1][0]*m[2][1]-m[1][1]*m[2][0],
     m[0][2]*m[2][1]-m[0][1]*m[2][2],
	 m[0][0]*m[2][2]-m[0][2]*m[2][0],
     m[0][1]*m[2][0]-m[0][0]*m[2][1],
     m[0][1]*m[1][2]-m[0][2]*m[1][1],
     m[0][2]*m[1][0]-m[0][0]*m[1][2],
     m[0][0]*m[1][1]-m[0][1]*m[1][0] );
    */
}

#endif