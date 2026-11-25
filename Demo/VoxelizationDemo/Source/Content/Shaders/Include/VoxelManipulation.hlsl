#ifndef VOXELMANIPULATION_HLSL
#define VOXELMANIPULATION_HLSL

struct VoxelSpaceInfo
{
    float3 VoxelOrigin;
    uint3 VoxelSize;
};

ConstantBuffer<VoxelSpaceInfo> cbVoxelInfo;
RWTexture3D<uint> txVoxelBuffer;

int3 GetVoxelPositionFromWorld(float3 worldPos, out bool isWithinSpace)
{
    worldPos -= cbVoxelInfo.VoxelOrigin;

    int3 localSpace = (int3)(worldPos * 4) + cbVoxelInfo.VoxelSize / 2;
    isWithinSpace = all(abs(localSpace) < cbVoxelInfo.VoxelSize);

    return localSpace;
}

#endif