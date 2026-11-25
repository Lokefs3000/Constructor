using System;
using System.Runtime.CompilerServices;

namespace Interop.PhysX
{
    public unsafe struct PxPhysics
    {
        /// <inheritdoc cref="PhysXInterop.PxPhysics_release()" />
        public void release()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            PhysXInterop.PxPhysics_release(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createAggregate(uint, uint, PxAggregateFilterHint, )" />
        public PxAggregate* createAggregate(uint maxActor, uint maxShape, PxAggregateFilterHint filterHint)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createAggregate(@this, maxActor, maxShape, filterHint);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbAggregates()" />
        public uint getNbAggregates()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbAggregates(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createTriangleMesh(PxInputStream*)" />
        public PxTriangleMesh* createTriangleMesh(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createTriangleMesh(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbTriangleMeshes()" />
        public uint getNbTriangleMeshes()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbTriangleMeshes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getTriangleMeshes(PxTriangleMesh**uintuint)" />
        public uint getTriangleMeshes(PxTriangleMesh** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getTriangleMeshes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createTetrahedronMesh(PxInputStream*)" />
        public PxTetrahedronMesh* createTetrahedronMesh(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createTetrahedronMesh(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbTetrahedronMeshes()" />
        public uint getNbTetrahedronMeshes()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbTetrahedronMeshes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getTetrahedronMeshes(PxTetrahedronMesh**uintuint)" />
        public uint getTetrahedronMeshes(PxTetrahedronMesh** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getTetrahedronMeshes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createHeightField(PxInputStream*)" />
        public PxHeightField* createHeightField(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createHeightField(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbHeightFields()" />
        public uint getNbHeightFields()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbHeightFields(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getHeightFields(PxHeightField**uintuint)" />
        public uint getHeightFields(PxHeightField** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getHeightFields(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createConvexMesh(PxInputStream*)" />
        public PxConvexMesh* createConvexMesh(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createConvexMesh(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbConvexMeshes()" />
        public uint getNbConvexMeshes()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbConvexMeshes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getConvexMeshes(PxConvexMesh**uintuint)" />
        public uint getConvexMeshes(PxConvexMesh** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getConvexMeshes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableVolumeMesh(PxInputStream*)" />
        public PxDeformableVolumeMesh* createDeformableVolumeMesh(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableVolumeMesh(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createBVH(PxInputStream*)" />
        public PxBVH* createBVH(PxInputStream* stream)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createBVH(@this, stream);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbBVHs()" />
        public uint getNbBVHs()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbBVHs(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getBVHs(PxBVH**uintuint)" />
        public uint getBVHs(PxBVH** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getBVHs(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createScene(PxSceneDesc)" />
        public PxScene* createScene(PxSceneDesc sceneDesc)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createScene(@this, sceneDesc);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbScenes()" />
        public uint getNbScenes()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbScenes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getScenes(PxScene**uintuint)" />
        public uint getScenes(PxScene** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getScenes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createRigidStatic(PxTransform)" />
        public PxRigidStatic* createRigidStatic(PxTransform pose)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createRigidStatic(@this, pose);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createRigidDynamic(PxTransform)" />
        public PxRigidDynamic* createRigidDynamic(PxTransform pose)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createRigidDynamic(@this, pose);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createPruningStructure(PxRigidActor**uint)" />
        public PxPruningStructure* createPruningStructure(PxRigidActor** actors, uint nbActors)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createPruningStructure(@this, actors, nbActors);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_material(PxGeometry*PxMaterial*boolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxMaterial* material, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_material(@this, geometry, material, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_deformableVolume(PxGeometry*PxDeformableVolumeMaterial*boolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxDeformableVolumeMaterial* material, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_deformableVolume(@this, geometry, material, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_deformableSurface(PxGeometry*PxDeformableSurfaceMaterial*boolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxDeformableSurfaceMaterial* material, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_deformableSurface(@this, geometry, material, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_materials(PxGeometry*PxMaterial**ushortboolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxMaterial** materials, ushort materialCount, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_materials(@this, geometry, materials, materialCount, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_deformableVolumes(PxGeometry*PxDeformableVolumeMaterial**ushortboolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxDeformableVolumeMaterial** materials, ushort materialCount, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_deformableVolumes(@this, geometry, materials, materialCount, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createShape_deformableSurfaces(PxGeometry*PxDeformableSurfaceMaterial**ushortboolPxShapeFlag)" />
        public PxShape* createShape(PxGeometry* geometry, PxDeformableSurfaceMaterial** materials, ushort materialCount, bool isExclusive, PxShapeFlag shapeFlags)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createShape_deformableSurfaces(@this, geometry, materials, materialCount, isExclusive, shapeFlags);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbShapes()" />
        public uint getNbShapes()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbShapes(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getShapes(PxShape**uintuint)" />
        public uint getShapes(PxShape** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getShapes(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createConstraint(PxRigidActor*PxRigidActor*PxConstraintConnector*PxConstraintShaderTableuint)" />
        public PxConstraint* createConstraint(PxRigidActor* actor0, PxRigidActor* actor1, PxConstraintConnector* connector, PxConstraintShaderTable shaders, uint dataSize)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createConstraint(@this, actor0, actor1, connector, shaders, dataSize);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbConstraints()" />
        public uint getNbConstraints()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbConstraints(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getConstraints(PxConstraint**uintuint)" />
        public uint getConstraints(PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getConstraints(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createArticulationReducedCoordinate()" />
        public PxArticulationReducedCoordinate* createArticulationReducedCoordinate()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createArticulationReducedCoordinate(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbArticulations()" />
        public uint getNbArticulations()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbArticulations(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableAttachment(PxDeformableAttachmentData)" />
        public PxDeformableAttachment* createDeformableAttachment(PxDeformableAttachmentData data)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableAttachment(@this, data);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableElementFilter(PxDeformableElementFilterData)" />
        public PxDeformableElementFilter* createDeformableElementFilter(PxDeformableElementFilterData data)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableElementFilter(@this, data);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableSurface(PxCudaContextManager*)" />
        public PxDeformableSurface* createDeformableSurface(PxCudaContextManager* cudaContextManager)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableSurface(@this, cudaContextManager);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableVolume(PxCudaContextManager*)" />
        public PxDeformableVolume* createDeformableVolume(PxCudaContextManager* cudaContextManager)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableVolume(@this, cudaContextManager);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createPBDParticleSystem(PxCudaContextManager*uintfloat)" />
        public PxPBDParticleSystem* createPBDParticleSystem(PxCudaContextManager* cudaContextManager, uint maxNeighborhood, float neighborhoodScale)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createPBDParticleSystem(@this, cudaContextManager, maxNeighborhood, neighborhoodScale);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createParticleBuffer(uintPxCudaContextManager*)" />
        public PxParticleBuffer* createParticleBuffer(uint maxParticles, PxCudaContextManager* cudaContextManager)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createParticleBuffer(@this, maxParticles, cudaContextManager);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createParticleAndDiffuseBuffer(uintuintPxCudaContextManager*)" />
        public PxParticleAndDiffuseBuffer* createParticleAndDiffuseBuffer(uint maxParticles, uint maxDiffuseParticles, PxCudaContextManager* cudaContextManager)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createParticleAndDiffuseBuffer(@this, maxParticles, maxDiffuseParticles, cudaContextManager);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createMaterial(floatfloatfloat)" />
        public PxMaterial* createMaterial(float staticFriction, float dynamicFriction, float restitution)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createMaterial(@this, staticFriction, dynamicFriction, restitution);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbMaterials()" />
        public uint getNbMaterials()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbMaterials(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getMaterials(PxMaterial**uintuint)" />
        public uint getMaterials(PxMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableSurfaceMaterial(floatfloatfloatfloatfloatfloatfloat)" />
        public PxDeformableSurfaceMaterial* createDeformableSurfaceMaterial(float youngs, float poissons, float dynamicFriction, float thickness, float bendingStiffness, float elasticityDamping, float bendingDamping)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableSurfaceMaterial(@this, youngs, poissons, dynamicFriction, thickness, bendingStiffness, elasticityDamping, bendingDamping);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbDeformableSurfaceMaterials()" />
        public uint getNbDeformableSurfaceMaterials()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbDeformableSurfaceMaterials(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getDeformableSurfaceMaterials(PxDeformableSurfaceMaterial**uintuint)" />
        public uint getDeformableSurfaceMaterials(PxDeformableSurfaceMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getDeformableSurfaceMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createDeformableVolumeMaterial(floatfloatfloatfloat)" />
        public PxDeformableVolumeMaterial* createDeformableVolumeMaterial(float youngs, float poissons, float dynamicFriction, float elasticityDamping)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createDeformableVolumeMaterial(@this, youngs, poissons, dynamicFriction, elasticityDamping);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbDeformableVolumeMaterials()" />
        public uint getNbDeformableVolumeMaterials()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbDeformableVolumeMaterials(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getDeformableVolumeMaterials(PxDeformableVolumeMaterial**uintuint)" />
        public uint getDeformableVolumeMaterials(PxDeformableVolumeMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getDeformableVolumeMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_createPDBMaterial(floatfloatfloatfloatfloatfloatfloatfloatfloatfloatfloat)" />
        public PxPBDMaterial* createPDBMaterial(float friction, float damping, float adhesion, float viscosity, float vorticityConfinement, float surfaceTension, float cohesion, float lift, float drag, float cflCoefficient, float gravityScale)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_createPDBMaterial(@this, friction, damping, adhesion, viscosity, vorticityConfinement, surfaceTension, cohesion, lift, drag, cflCoefficient, gravityScale);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getNbPBDMaterials()" />
        public uint getNbPBDMaterials()
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getNbPBDMaterials(@this);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_getPBDMaterials(PxPBDMaterial**uintuint)" />
        public uint getPBDMaterials(PxPBDMaterial** userBuffer, uint bufferSize, uint startIndex = 0)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            return PhysXInterop.PxPhysics_getPBDMaterials(@this, userBuffer, bufferSize, startIndex);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_registerDeletionListener(PxDeletionListener*PxDeletionEventFlagbool)" />
        public void registerDeletionListener(PxDeletionListener* observer, PxDeletionEventFlag deletionEvents, bool restrictedObjectSet)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            PhysXInterop.PxPhysics_registerDeletionListener(@this, observer, deletionEvents, restrictedObjectSet);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_unregisterDeletionListener(PxDeletionListener*)" />
        public void unregisterDeletionListener(PxDeletionListener* observer)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            PhysXInterop.PxPhysics_unregisterDeletionListener(@this, observer);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_registerDeletionListenerObjects(PxDeletionListener*PxBase**uint)" />
        public void registerDeletionListenerObjects(PxDeletionListener* observer, PxBase** observables, uint observableCount)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            PhysXInterop.PxPhysics_registerDeletionListenerObjects(@this, observer, observables, observableCount);
        }

        /// <inheritdoc cref="PhysXInterop.PxPhysics_unregisterDeletionListenerObjects(PxDeletionListener*PxBase**uint)" />
        public void unregisterDeletionListenerObjects(PxDeletionListener* observer, PxBase** observables, uint observableCount)
        {
            PxPhysics* @this = (PxPhysics*)Unsafe.AsPointer(ref Unsafe.AsRef<PxPhysics>(ref this));
            PhysXInterop.PxPhysics_unregisterDeletionListenerObjects(@this, observer, observables, observableCount);
        }
    }
}
