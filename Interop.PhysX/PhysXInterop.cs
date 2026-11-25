using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[assembly: DisableRuntimeMarshalling]

namespace Interop.PhysX
{
    public static unsafe partial class PhysXInterop
    {
        /// <summary>*<br/>\brief Destroys the instance it is called on.<br/><br/>The operation will fail, if there are still modules referencing the foundation object. Release all dependent modules<br/>prior to calling this method.<br/><br/>\see PxCreateFoundation()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_release")]
        public static partial void PxFoundation_release(PxFoundation* @this);

        /// <summary>*<br/>retrieves error callback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_getErrorCallback")]
        public static partial PxErrorCallback* PxFoundation_getErrorCallback(PxFoundation* @this);

        /// <summary>*<br/>Sets mask of errors to report.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_setErrorLevel")]
        public static partial void PxFoundation_setErrorLevel(PxFoundation* @this, PxErrorCode mask = PxErrorCode.eMASK_ALL);

        /// <summary>*<br/>Retrieves mask of errors to be reported.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_getErrorLevel")]
        public static partial PxErrorCode PxFoundation_getErrorLevel(PxFoundation* @this);

        /// <summary>*<br/>Retrieves the allocator this object was created with.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_getAllocatorCallback")]
        public static partial PxAllocatorCallback* PxFoundation_getAllocatorCallback(PxFoundation* @this);

        /// <summary>*<br/>Retrieves if allocation names are being passed to allocator callback.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_getReportAllocationNames")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxFoundation_getReportAllocationNames(PxFoundation* @this);

        /// <summary>*<br/>Set if allocation names are being passed to allocator callback.<br/>\details Enabled by default in debug and checked build, disabled by default in profile and release build.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_setReportAllocationNames")]
        public static partial void PxFoundation_setReportAllocationNames(PxFoundation* @this, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>Set if allocation names are being passed to allocator callback.<br/>\details Enabled by default in debug and checked build, disabled by default in profile and release build.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_registerAllocationListener")]
        public static partial void PxFoundation_registerAllocationListener(PxFoundation* @this, PxAllocationListener* listener);

        /// <summary>*<br/>Set if allocation names are being passed to allocator callback.<br/>\details Enabled by default in debug and checked build, disabled by default in profile and release build.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_deregisterAllocationListener")]
        public static partial void PxFoundation_deregisterAllocationListener(PxFoundation* @this, PxAllocationListener* listener);

        /// <summary>*<br/>Set if allocation names are being passed to allocator callback.<br/>\details Enabled by default in debug and checked build, disabled by default in profile and release build.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_registerErrorCallback")]
        public static partial void PxFoundation_registerErrorCallback(PxFoundation* @this, PxErrorCallback* callback);

        /// <summary>*<br/>Set if allocation names are being passed to allocator callback.<br/>\details Enabled by default in debug and checked build, disabled by default in profile and release build.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxFoundation_deregisterErrorCallback")]
        public static partial void PxFoundation_deregisterErrorCallback(PxFoundation* @this, PxErrorCallback* callback);

        /// <summary>*<br/>\brief Destroys the instance it is called on.<br/><br/>Use this release method to destroy an instance of this class. Be sure<br/>to not keep a reference to this object after calling release.<br/>Avoid release calls while a scene is simulating (in between simulate() and fetchResults() calls).<br/><br/>Note that this must be called once for each prior call to PxCreatePhysics, as<br/>there is a reference counter. Also note that you mustn't destroy the PxFoundation instance (holding the allocator, error callback etc.)<br/>until after the reference count reaches 0 and the SDK is actually removed.<br/><br/>Releasing an SDK will also release any objects created through it (scenes, triangle meshes, convex meshes, heightfields, shapes etc.),<br/>provided the user hasn't already done so.<br/><br/>\note Releasing the PxPhysics instance is a prerequisite to releasing the PxFoundation instance.<br/><br/>\see PxCreatePhysics() PxFoundation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_release")]
        public static partial void PxPhysics_release(PxPhysics* @this);

        /// <summary>*<br/>\brief Creates an aggregate with the specified maximum size and filtering hint.<br/><br/>The previous API used "bool enableSelfCollision" which should now silently evaluates<br/>to a PxAggregateType::eGENERIC aggregate with its self-collision bit.<br/><br/>Use PxAggregateType::eSTATIC or PxAggregateType::eKINEMATIC for aggregates that will<br/>only contain static or kinematic actors. This provides faster filtering when used in<br/>combination with PxPairFilteringMode.<br/><br/>\param	[in] maxActor		The maximum number of actors that may be placed in the aggregate.<br/>\param	[in] maxShape		The maximum number of shapes that may be placed in the aggregate.<br/>\param	[in] filterHint		The aggregate's filtering hint.<br/>\return The new aggregate.<br/><br/>\see PxAggregate PxAggregateFilterHint PxAggregateType PxPairFilteringMode<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createAggregate")]
        public static partial PxAggregate* PxPhysics_createAggregate(PxPhysics* @this, uint maxActor, uint maxShape, PxAggregateFilterHint filterHint);

        /// <summary>*<br/>\brief Return the number of aggregates that currently exist.<br/><br/>\return Number of aggregates.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbAggregates")]
        public static partial uint PxPhysics_getNbAggregates(PxPhysics* @this);

        /// <summary>*<br/>\brief Creates a triangle mesh object.<br/><br/>This can then be instanced into #PxShape objects.<br/><br/>\param	[in] stream	The triangle mesh stream.<br/>\return The new triangle mesh.<br/><br/>\see PxTriangleMesh PxMeshPreprocessingFlag PxTriangleMesh.release() PxInputStream PxTriangleMeshFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createTriangleMesh")]
        public static partial PxTriangleMesh* PxPhysics_createTriangleMesh(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Return the number of triangle meshes that currently exist.<br/><br/>\return Number of triangle meshes.<br/><br/>\see getTriangleMeshes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbTriangleMeshes")]
        public static partial uint PxPhysics_getNbTriangleMeshes(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of triangle mesh pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the triangle meshes in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive triangle mesh pointers.<br/>\param	[in]  bufferSize	The number of triangle mesh pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first mesh pointer to be retrieved.<br/>\return The number of triangle mesh pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbTriangleMeshes() PxTriangleMesh<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getTriangleMeshes")]
        public static partial uint PxPhysics_getTriangleMeshes(PxPhysics* @this, PxTriangleMesh** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a tetrahedron mesh object.<br/><br/>This can then be instanced into #PxShape objects.<br/><br/>\param[in] stream The tetrahedron mesh stream.<br/>\return The new tetrahedron mesh.<br/><br/>\see PxTetrahedronMesh PxMeshPreprocessingFlag PxTetrahedronMesh.release() PxInputStream PxTriangleMeshFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createTetrahedronMesh")]
        public static partial PxTetrahedronMesh* PxPhysics_createTetrahedronMesh(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Return the number of tetrahedron meshes that currently exist.<br/><br/>\return Number of tetrahedron meshes.<br/><br/>\see getTetrahedronMeshes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbTetrahedronMeshes")]
        public static partial uint PxPhysics_getNbTetrahedronMeshes(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of tetrahedron mesh pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the tetrahedron meshes in the array is not specified.<br/><br/>\param[out] userBuffer The buffer to receive tetrahedron mesh pointers.<br/>\param[in] bufferSize The number of tetrahedron mesh pointers which can be stored in the buffer.<br/>\param[in] startIndex Index of first mesh pointer to be retrieved.<br/>\return The number of tetrahedron mesh pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbTetrahedronMeshes() PxTetrahedronMesh<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getTetrahedronMeshes")]
        public static partial uint PxPhysics_getTetrahedronMeshes(PxPhysics* @this, PxTetrahedronMesh** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a heightfield object from previously cooked stream.<br/><br/>This can then be instanced into #PxShape objects.<br/><br/>\param	[in] stream	The heightfield mesh stream.<br/>\return The new heightfield.<br/><br/>\see PxHeightField PxHeightField.release() PxInputStream<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createHeightField")]
        public static partial PxHeightField* PxPhysics_createHeightField(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Return the number of heightfields that currently exist.<br/><br/>\return Number of heightfields.<br/><br/>\see getHeightFields()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbHeightFields")]
        public static partial uint PxPhysics_getNbHeightFields(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of heightfield pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the heightfields in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive heightfield pointers.<br/>\param	[in]  bufferSize	The number of heightfield pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first heightfield pointer to be retrieved.<br/>\return The number of heightfield pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbHeightFields() PxHeightField<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getHeightFields")]
        public static partial uint PxPhysics_getHeightFields(PxPhysics* @this, PxHeightField** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a convex mesh object.<br/><br/>This can then be instanced into #PxShape objects.<br/><br/>\param	[in] stream	The stream to load the convex mesh from.<br/>\return The new convex mesh.<br/><br/>\see PxConvexMesh PxConvexMesh.release() PxInputStream createTriangleMesh() PxConvexMeshGeometry PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createConvexMesh")]
        public static partial PxConvexMesh* PxPhysics_createConvexMesh(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Return the number of convex meshes that currently exist.<br/><br/>\return Number of convex meshes.<br/><br/>\see getConvexMeshes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbConvexMeshes")]
        public static partial uint PxPhysics_getNbConvexMeshes(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of convex mesh pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the convex meshes in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive convex mesh pointers.<br/>\param	[in]  bufferSize	The number of convex mesh pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first convex mesh pointer to be retrieved.<br/>\return The number of convex mesh pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbConvexMeshes() PxConvexMesh<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getConvexMeshes")]
        public static partial uint PxPhysics_getConvexMeshes(PxPhysics* @this, PxConvexMesh** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a deformable volume mesh object.<br/><br/>\param[in] stream The deformable volume mesh stream.<br/>\return The new deformable volume mesh.<br/><br/>\see createTetrahedronMesh<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableVolumeMesh")]
        public static partial PxDeformableVolumeMesh* PxPhysics_createDeformableVolumeMesh(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Creates a bounding volume hierarchy.<br/><br/>\param	[in] stream	The stream to load the BVH from.<br/>\return The new BVH.<br/><br/>\see PxBVH PxInputStream<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createBVH")]
        public static partial PxBVH* PxPhysics_createBVH(PxPhysics* @this, PxInputStream* stream);

        /// <summary>*<br/>\brief Return the number of bounding volume hierarchies that currently exist.<br/><br/>\return Number of bounding volume hierarchies.<br/><br/>\see PxBVH getBVHs()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbBVHs")]
        public static partial uint PxPhysics_getNbBVHs(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of bounding volume hierarchy pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the BVHs in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive BVH pointers.<br/>\param	[in]  bufferSize	The number of BVH pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first BVH pointer to be retrieved.<br/>\return The number of BVH pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbBVHs() PxBVH<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getBVHs")]
        public static partial uint PxPhysics_getBVHs(PxPhysics* @this, PxBVH** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a scene.<br/><br/>\note Every scene uses a Thread Local Storage slot. This imposes a platform specific limit on the<br/>number of scenes that can be created.<br/><br/>\param	[in] sceneDesc	Scene descriptor. See #PxSceneDesc<br/>\return The new scene object.<br/><br/>\see PxScene PxScene.release() PxSceneDesc<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createScene")]
        public static partial PxScene* PxPhysics_createScene(PxPhysics* @this, PxSceneDesc sceneDesc);

        /// <summary>*<br/>\brief Gets number of created scenes.<br/><br/>\return The number of scenes created.<br/><br/>\see getScenes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbScenes")]
        public static partial uint PxPhysics_getNbScenes(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of scene pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the scene pointers in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive scene pointers.<br/>\param	[in]  bufferSize	The number of scene pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first scene pointer to be retrieved.<br/>\return The number of scene pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbScenes() PxScene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getScenes")]
        public static partial uint PxPhysics_getScenes(PxPhysics* @this, PxScene** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a static rigid actor with the specified pose and all other fields initialized<br/>to their default values.<br/><br/>\param	[in] pose	The initial pose of the actor. Must be a valid transform.<br/><br/>\see PxRigidStatic<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createRigidStatic")]
        public static partial PxRigidStatic* PxPhysics_createRigidStatic(PxPhysics* @this, PxTransform pose);

        /// <summary>*<br/>\brief Creates a dynamic rigid actor with the specified pose and all other fields initialized<br/>to their default values.<br/><br/>\param	[in] pose	The initial pose of the actor. Must be a valid transform.<br/><br/>\see PxRigidDynamic<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createRigidDynamic")]
        public static partial PxRigidDynamic* PxPhysics_createRigidDynamic(PxPhysics* @this, PxTransform pose);

        /// <summary>*<br/>\brief Creates a pruning structure from actors.<br/><br/>\note Every provided actor needs at least one shape with the eSCENE_QUERY_SHAPE flag set.<br/>\note Both static and dynamic actors can be provided.<br/>\note It is not allowed to pass in actors which are already part of a scene.<br/>\note Articulation links cannot be provided.<br/><br/>\param	[in] actors		Array of actors to add to the pruning structure. Must be non NULL.<br/>\param	[in] nbActors	Number of actors in the array. Must be >0.<br/>\return Pruning structure created from given actors, or NULL if any of the actors did not comply with the above requirements.<br/>\see PxActor PxPruningStructure<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createPruningStructure")]
        public static partial PxPruningStructure* PxPhysics_createPruningStructure(PxPhysics* @this, PxRigidActor** actors, uint nbActors);

        /// <summary>*<br/>\brief Creates a shape which may be attached to multiple actors<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] material		The material for the shape<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_material")]
        public static partial PxShape* PxPhysics_createShape_material(PxPhysics* @this, PxGeometry* geometry, PxMaterial* material, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Creates a shape which may be attached to exactly one deformable volume actor<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] material		The material for the shape<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_deformableVolume")]
        public static partial PxShape* PxPhysics_createShape_deformableVolume(PxPhysics* @this, PxGeometry* geometry, PxDeformableVolumeMaterial* material, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Creates a shape which may be attached to exactly one deformable surface actor<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] material		The material for the shape<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_deformableSurface")]
        public static partial PxShape* PxPhysics_createShape_deformableSurface(PxPhysics* @this, PxGeometry* geometry, PxDeformableSurfaceMaterial* material, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Creates a shape which may be attached to multiple actors<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] materials		The materials for the shape<br/>\param	[in] materialCount	The number of materials<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/>\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_materials")]
        public static partial PxShape* PxPhysics_createShape_materials(PxPhysics* @this, PxGeometry* geometry, PxMaterial** materials, ushort materialCount, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Creates a shape which may be attached to multiple actors<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] materials		The materials for the shape<br/>\param	[in] materialCount	The number of materials<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/>\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_deformableVolumes")]
        public static partial PxShape* PxPhysics_createShape_deformableVolumes(PxPhysics* @this, PxGeometry* geometry, PxDeformableVolumeMaterial** materials, ushort materialCount, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Creates a shape which may be attached to multiple actors<br/><br/>The shape will be created with a reference count of 1.<br/><br/>\param	[in] geometry		The geometry for the shape<br/>\param	[in] materials		The materials for the shape<br/>\param	[in] materialCount	The number of materials<br/>\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared<br/>\param	[in] shapeFlags		The PxShapeFlags to be set<br/>\return The shape<br/><br/>\note Shared shapes are not mutable when they are attached to an actor<br/>\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.<br/><br/>\see PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createShape_deformableSurfaces")]
        public static partial PxShape* PxPhysics_createShape_deformableSurfaces(PxPhysics* @this, PxGeometry* geometry, PxDeformableSurfaceMaterial** materials, ushort materialCount, [MarshalAs(UnmanagedType.I1)] bool isExclusive, PxShapeFlag shapeFlags);

        /// <summary>*<br/>\brief Return the number of shapes that currently exist.<br/><br/>\return Number of shapes.<br/><br/>\see getShapes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbShapes")]
        public static partial uint PxPhysics_getNbShapes(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of shape pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the shapes in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive shape pointers.<br/>\param	[in]  bufferSize	The number of shape pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first shape pointer to be retrieved<br/>\return The number of shape pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbShapes() PxShape<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getShapes")]
        public static partial uint PxPhysics_getShapes(PxPhysics* @this, PxShape** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a constraint shader.<br/><br/>\note A constraint shader will get added automatically to the scene the two linked actors belong to. Either, but not both, of actor0 and actor1 may<br/>be NULL to denote attachment to the world.<br/><br/>\param	[in] actor0		The first actor<br/>\param	[in] actor1		The second actor<br/>\param	[in] connector	The connector object, which the SDK uses to communicate with the infrastructure for the constraint<br/>\param	[in] shaders	The shader functions for the constraint<br/>\param	[in] dataSize	The size of the data block for the shader<br/><br/>\return The new constraint shader.<br/><br/>\see PxConstraint<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createConstraint")]
        public static partial PxConstraint* PxPhysics_createConstraint(PxPhysics* @this, PxRigidActor* actor0, PxRigidActor* actor1, PxConstraintConnector* connector, PxConstraintShaderTable shaders, uint dataSize);

        /// <summary>*<br/>\brief Return the number of constraints that currently exist.<br/><br/>\return Number of constraints.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbConstraints")]
        public static partial uint PxPhysics_getNbConstraints(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the constraints that currently exist into a user buffer.<br/><br/>\param[out] userBuffer The buffer to receive constraint pointers.<br/>\param[in] bufferSize The number of constraint pointers which can be stored in the buffer.<br/>\param[in] startIndex Index of first constraint pointer to be retrieved.<br/>\return Number of constraint pointers written to the buffer.<br/><br/>\see getNbConstraints() PxConstraint<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getConstraints")]
        public static partial uint PxPhysics_getConstraints(PxPhysics* @this, PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a reduced-coordinate articulation with all fields initialized to their default values.<br/><br/>\return the new articulation<br/><br/>\see PxArticulationReducedCoordinate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createArticulationReducedCoordinate")]
        public static partial PxArticulationReducedCoordinate* PxPhysics_createArticulationReducedCoordinate(PxPhysics* @this);

        /// <summary>*<br/>\brief Return the number of articulations that currently exist.<br/><br/>\return Number of articulations.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbArticulations")]
        public static partial uint PxPhysics_getNbArticulations(PxPhysics* @this);

        /// <summary>*<br/>\brief Creates an attachment between two actors, based on the provided PxDeformableAttachmentData. At least one of the actors must be a deformable.<br/><br/>An attachment is a collection of one or more positional constraints between a point on one actor and a point on another actor.<br/>Attachments between two rigid objects are not permitted, use joints instead.<br/><br/>\note The attachment is only active when both actors are added to the same scene or one of the actors is NULL.<br/><br/>\param[in] data Attachment data.<br/>\return The PxDeformableAttachment created if successful, NULL otherwise.<br/>\see PxDeformableAttachmentData, PxDeformableAttachment<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableAttachment")]
        public static partial PxDeformableAttachment* PxPhysics_createDeformableAttachment(PxPhysics* @this, PxDeformableAttachmentData data);

        /// <summary>*<br/>\brief Creates an element-level collision filter between two actors, based on the provided PxDeformableElementFilterData. At least one of the actors must be a deformable.<br/><br/>Element filters define how parts of deformable actors are excluded from collisions.<br/>They are usually added to avoid conflicting attachment and contact constraints.<br/><br/>\note The element filter is only active when both actors are added to the same scene or one of the actors is NULL.<br/><br/>\param[in] data Element filter data.<br/>\return The PxDeformableElementFilter created if successful, NULL otherwise.<br/>\see PxDeformableElementFilterData, PxDeformableElementFilter<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableElementFilter")]
        public static partial PxDeformableElementFilter* PxPhysics_createDeformableElementFilter(PxPhysics* @this, PxDeformableElementFilterData data);

        /// <summary>*<br/>\brief Creates a deformable surface with all fields initialized to their default values.<br/><br/>\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.<br/>\return the new deformable surface<br/><br/>\see PxDeformableSurface<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableSurface")]
        public static partial PxDeformableSurface* PxPhysics_createDeformableSurface(PxPhysics* @this, PxCudaContextManager* cudaContextManager);

        /// <summary>*<br/>\brief Creates a FEM-based deformable volume with all fields initialized to their default values.<br/><br/>\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.<br/>\return the new deformable volume<br/><br/>\see PxDeformableVolume<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableVolume")]
        public static partial PxDeformableVolume* PxPhysics_createDeformableVolume(PxPhysics* @this, PxCudaContextManager* cudaContextManager);

        /// <summary>*<br/>\brief Creates a particle system with a position-based dynamics (PBD) solver.<br/><br/>A PBD particle system can be used to simulate particle systems with fluid and granular particles. It also allows simulating cloth using<br/>mass-spring constraints and rigid bodies by shape matching the bodies with particles.<br/><br/>In order to accelerate neighborhood finding for particle-particle interactions (e.g.: for fluid density constraints) a regular grid is used.<br/>This grid is built every time step but may provide inaccurate neighborhood information during the solver iterations. The neighborhood scale<br/>parameter can be used to configure the grid such that it provides a more conservative neighborhood at the cost of run-time performance.<br/>The grid cell width is defined as 2*particleContactOffset*neighborhoodScale. \see PxParticleSystem::setParticleContactOffset.<br/>The neighborhood scale parameter should typically not be much larger than 1.<br/><br/>The maxNeighborhood defines how many particles fit into the neighborhood, at the cost of memory.<br/><br/>Both maxNeighborhood and neighborhoodScale should be set as low as possible for performance, but high enough to not cause any behavioral degredation.<br/><br/>\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.<br/>\param[in] maxNeighborhood The maximum number of particles considered in neighborhood-based particle interaction calculations.<br/>\param[in] neighborhoodScale Scale applied to the particle contact offset used to define the particle neighborhood size spatially. <b>Range:</b> (1.0, PX_MAX_F32)<br/>\return the new particle system<br/><br/>\see PxPBDParticleSystem<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createPBDParticleSystem")]
        public static partial PxPBDParticleSystem* PxPhysics_createPBDParticleSystem(PxPhysics* @this, PxCudaContextManager* cudaContextManager, uint maxNeighborhood, float neighborhoodScale);

        /// <summary>*<br/>\brief Create particle buffer to simulate fluid/granular material.<br/><br/>\param[in] maxParticles The maximum number of particles in this buffer.<br/>\param[in] cudaContextManager The PxCudaContextManager this buffer is tied to.<br/>\return PxParticleBuffer instance<br/><br/>\see PxParticleBuffer<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createParticleBuffer")]
        public static partial PxParticleBuffer* PxPhysics_createParticleBuffer(PxPhysics* @this, uint maxParticles, PxCudaContextManager* cudaContextManager);

        /// <summary>*<br/>\brief Create a particle buffer for fluid dynamics with diffuse particles. Diffuse particles are used to simulate fluid effects<br/>such as foam, spray and bubbles.<br/><br/>\param[in] maxParticles The maximum number of particles in this buffer.<br/>\param[in] maxDiffuseParticles The max number of diffuse particles in this buffer.<br/>\param[in] cudaContextManager The PxCudaContextManager this buffer is tied to.<br/>\return PxParticleAndDiffuseBuffer instance<br/><br/>\see PxParticleAndDiffuseBuffer, PxDiffuseParticleParams<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createParticleAndDiffuseBuffer")]
        public static partial PxParticleAndDiffuseBuffer* PxPhysics_createParticleAndDiffuseBuffer(PxPhysics* @this, uint maxParticles, uint maxDiffuseParticles, PxCudaContextManager* cudaContextManager);

        /// <summary>*<br/>\brief Creates a new rigid body material with certain default properties.<br/><br/>\return The new rigid body material.<br/><br/>\param	[in] staticFriction		The coefficient of static friction<br/>\param	[in] dynamicFriction	The coefficient of dynamic friction<br/>\param	[in] restitution		The coefficient of restitution (if in range [0,1]) or the spring stiffness for compliant contact (if in range (-PX_MAX_REAL, 0))<br/><br/>\see PxMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createMaterial")]
        public static partial PxMaterial* PxPhysics_createMaterial(PxPhysics* @this, float staticFriction, float dynamicFriction, float restitution);

        /// <summary>*<br/>\brief Return the number of rigid body materials that currently exist.<br/><br/>\return Number of rigid body materials.<br/><br/>\see getMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbMaterials")]
        public static partial uint PxPhysics_getNbMaterials(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of rigid body material pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the materials in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive material pointers.<br/>\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first material pointer to be retrieved.<br/>\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbMaterials() PxMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getMaterials")]
        public static partial uint PxPhysics_getMaterials(PxPhysics* @this, PxMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a new surface deformable material with certain default properties.<br/><br/>\return The new surface deformable material.<br/><br/>\param	[in] youngs					The young's modulus<br/>\param	[in] poissons				The poissons's ratio<br/>\param	[in] dynamicFriction		The dynamic friction coefficient<br/>\param	[in] thickness				The thickness of the surface<br/>\param	[in] bendingStiffness		The bending stiffness of the surface<br/>\param	[in] elasticityDamping		The damping of the surface with respect to stretch and shear<br/>\param	[in] bendingDamping			The damping of the surface with respect to bending<br/><br/>\see PxDeformableSurfaceMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableSurfaceMaterial")]
        public static partial PxDeformableSurfaceMaterial* PxPhysics_createDeformableSurfaceMaterial(PxPhysics* @this, float youngs, float poissons, float dynamicFriction, float thickness, float bendingStiffness, float elasticityDamping, float bendingDamping);

        /// <summary>*<br/>\brief Return the number of deformable surface materials that currently exist.<br/><br/>\return Number of deformable surface materials.<br/><br/>\see getDeformableSurfaceMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbDeformableSurfaceMaterials")]
        public static partial uint PxPhysics_getNbDeformableSurfaceMaterials(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of deformable surface material pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the materials in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive material pointers.<br/>\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first material pointer to be retrieved.<br/>\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbDeformableSurfaceMaterials() PxDeformableSurfaceMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getDeformableSurfaceMaterials")]
        public static partial uint PxPhysics_getDeformableSurfaceMaterials(PxPhysics* @this, PxDeformableSurfaceMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a new deformable volume material with certain default properties.<br/><br/>\return The new deformable volume material.<br/><br/>\param	[in] youngs					The young's modulus<br/>\param	[in] poissons				The poissons's ratio<br/>\param	[in] dynamicFriction		The dynamic friction coefficient<br/>\param	[in] elasticityDamping		The elasticity damping<br/><br/>\see PxDeformableVolumeMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createDeformableVolumeMaterial")]
        public static partial PxDeformableVolumeMaterial* PxPhysics_createDeformableVolumeMaterial(PxPhysics* @this, float youngs, float poissons, float dynamicFriction, float elasticityDamping);

        /// <summary>*<br/>\brief Return the number of deformable volume materials that currently exist.<br/><br/>\return Number of materials.<br/><br/>\see getDeformableVolumeMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbDeformableVolumeMaterials")]
        public static partial uint PxPhysics_getNbDeformableVolumeMaterials(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of deformable volume material pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the materials in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive material pointers.<br/>\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first material pointer to be retrieved.<br/>\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbDeformableVolumeMaterials() PxDeformableVolumeMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getDeformableVolumeMaterials")]
        public static partial uint PxPhysics_getDeformableVolumeMaterials(PxPhysics* @this, PxDeformableVolumeMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Creates a new PBD material with certain default properties.<br/><br/>\param	[in]  friction				The friction parameter<br/>\param	[in]  damping				The velocity damping parameter<br/>\param	[in]  adhesion				The adhesion parameter<br/>\param	[in]  viscosity				The viscosity parameter<br/>\param	[in]  vorticityConfinement	The vorticity confinement coefficient<br/>\param	[in]  surfaceTension		The surface tension coefficient<br/>\param	[in]  cohesion				The cohesion parameter<br/>\param	[in]  lift					The lift parameter<br/>\param	[in]  drag					The drag parameter<br/>\param	[in]  cflCoefficient		The Courant-Friedrichs-Lewy(cfl) coefficient<br/>\param	[in]  gravityScale			The gravity scale<br/>\return The new PBD material.<br/><br/>\see PxPBDMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_createPDBMaterial")]
        public static partial PxPBDMaterial* PxPhysics_createPDBMaterial(PxPhysics* @this, float friction, float damping, float adhesion, float viscosity, float vorticityConfinement, float surfaceTension, float cohesion, float lift, float drag, float cflCoefficient, float gravityScale);

        /// <summary>*<br/>\brief Return the number of PBD materials that currently exist.<br/><br/>\return Number of PBD materials.<br/><br/>\see getPBDMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getNbPBDMaterials")]
        public static partial uint PxPhysics_getNbPBDMaterials(PxPhysics* @this);

        /// <summary>*<br/>\brief Writes the array of PBD material pointers to a user buffer.<br/><br/>Returns the number of pointers written.<br/><br/>The ordering of the materials in the array is not specified.<br/><br/>\param	[out] userBuffer	The buffer to receive material pointers.<br/>\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.<br/>\param	[in]  startIndex	Index of first material pointer to be retrieved.<br/>\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.<br/><br/>\see getNbPBDMaterials() PxPBDMaterial<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_getPBDMaterials")]
        public static partial uint PxPhysics_getPBDMaterials(PxPhysics* @this, PxPBDMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Register a deletion listener. Listeners will be called whenever an object is deleted.<br/><br/>It is illegal to register or unregister a deletion listener while deletions are being processed.<br/><br/>\note By default a registered listener will receive events from all objects. Set the restrictedObjectSet parameter to true on registration and use #registerDeletionListenerObjects to restrict the received events to specific objects.<br/><br/>\note The deletion events are only supported on core PhysX objects. In general, objects in extension modules do not provide this functionality, however, in the case of PxJoint objects, the underlying PxConstraint will send the events.<br/><br/>\param	[in] observer				Observer object to send notifications to.<br/>\param	[in] deletionEvents			The deletion event types to get notified of.<br/>\param	[in] restrictedObjectSet	If false, the deletion listener will get events from all objects, else the objects to receive events from have to be specified explicitly through #registerDeletionListenerObjects.<br/><br/>\see PxDeletionListener unregisterDeletionListener<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_registerDeletionListener")]
        public static partial void PxPhysics_registerDeletionListener(PxPhysics* @this, PxDeletionListener* observer, PxDeletionEventFlag deletionEvents, [MarshalAs(UnmanagedType.I1)] bool restrictedObjectSet);

        /// <summary>*<br/>\brief Unregister a deletion listener.<br/><br/>It is illegal to register or unregister a deletion listener while deletions are being processed.<br/><br/>\param	[in] observer	Observer object to stop sending notifications to.<br/><br/>\see PxDeletionListener registerDeletionListener<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_unregisterDeletionListener")]
        public static partial void PxPhysics_unregisterDeletionListener(PxPhysics* @this, PxDeletionListener* observer);

        /// <summary>*<br/>\brief Register specific objects for deletion events.<br/><br/>This method allows for a deletion listener to limit deletion events to specific objects only.<br/><br/>\note It is illegal to register or unregister objects while deletions are being processed.<br/><br/>\note The deletion listener has to be registered through #registerDeletionListener() and configured to support restricted object sets prior to this method being used.<br/><br/>\param	[in] observer			Observer object to send notifications to.<br/>\param	[in] observables		List of objects for which to receive deletion events. Only PhysX core objects are supported. In the case of PxJoint objects, the underlying PxConstraint can be used to get the events.<br/>\param	[in] observableCount	Size of the observables list.<br/><br/>\see PxDeletionListener unregisterDeletionListenerObjects<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_registerDeletionListenerObjects")]
        public static partial void PxPhysics_registerDeletionListenerObjects(PxPhysics* @this, PxDeletionListener* observer, PxBase** observables, uint observableCount);

        /// <summary>*<br/>\brief Unregister specific objects for deletion events.<br/><br/>This method allows to clear previously registered objects for a deletion listener (see #registerDeletionListenerObjects()).<br/><br/>\note It is illegal to register or unregister objects while deletions are being processed.<br/><br/>\note The deletion listener has to be registered through #registerDeletionListener() and configured to support restricted object sets prior to this method being used.<br/><br/>\param	[in] observer			Observer object to stop sending notifications to.<br/>\param	[in] observables		List of objects for which to not receive deletion events anymore.<br/>\param	[in] observableCount	Size of the observables list.<br/><br/>\see PxDeletionListener registerDeletionListenerObjects<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxPhysics_unregisterDeletionListenerObjects")]
        public static partial void PxPhysics_unregisterDeletionListenerObjects(PxPhysics* @this, PxDeletionListener* observer, PxBase** observables, uint observableCount);

        /// <summary>*<br/>\brief Deletes the scene.<br/><br/>Removes any actors and constraint shaders from this scene<br/>(if the user hasn't already done so).<br/><br/>Be sure	to not keep a reference to this object after calling release.<br/>Avoid release calls while the scene is simulating (in between simulate() and fetchResults() calls).<br/><br/>\see PxPhysics.createScene()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_release")]
        public static partial void PxScene_release(PxScene* @this);

        /// <summary>*<br/>\brief Sets a scene flag. You can only set one flag at a time.<br/><br/>\note Not all flags are mutable and changing some will result in an error. Please check #PxSceneFlag to see which flags can be changed.<br/><br/>\see PxSceneFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setFlag")]
        public static partial void PxScene_setFlag(PxScene* @this, PxSceneFlag flag, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>\brief Get the scene flags.<br/><br/>\return The scene flags. See #PxSceneFlag<br/><br/>\see PxSceneFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFlags")]
        public static partial PxSceneFlag PxScene_getFlags(PxScene* @this);

        /// <summary>*<br/>\brief Set new scene limits.<br/><br/>\note Increase the maximum capacity of various data structures in the scene. The new capacities will be<br/>at least as large as required to deal with the objects currently in the scene. Further, these values<br/>are for preallocation and do not represent hard limits.<br/><br/>\param[in] limits Scene limits.<br/>\see PxSceneLimits<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setLimits")]
        public static partial void PxScene_setLimits(PxScene* @this, PxSceneLimits limits);

        /// <summary>*<br/>\brief Get current scene limits.<br/>\return Current scene limits.<br/>\see PxSceneLimits<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getLimits")]
        public static partial PxSceneLimits PxScene_getLimits(PxScene* @this);

        /// <summary>*<br/>\brief Call this method to retrieve the Physics SDK.<br/><br/>\return The physics SDK this scene is associated with.<br/><br/>\see PxPhysics<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getPhysics")]
        public static partial PxPhysics* PxScene_getPhysics(PxScene* @this);

        /// <summary>*<br/>\brief Retrieves the scene's internal timestamp, increased each time a simulation step is completed.<br/><br/>\return scene timestamp<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getTimestamp")]
        public static partial uint PxScene_getTimestamp(PxScene* @this);

        /// <summary>*<br/>\brief Sets a name string for the Scene that can be retrieved with getName().<br/><br/>This is for debugging and is not used by the SDK. The string is not copied by the SDK,<br/>only the pointer is stored.<br/><br/>\param[in] name String to set the objects name to.<br/><br/><b>Default:</b> NULL<br/><br/>\see getName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setName")]
        public static partial void PxScene_setName(PxScene* @this, byte* name);

        /// <summary>*<br/>\brief Retrieves the name string set with setName().<br/><br/>\return Name string associated with the Scene.<br/><br/>\see setName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getName")]
        public static partial byte* PxScene_getName(PxScene* @this);

        /// <summary>*<br/>\brief Adds an articulation to this scene.<br/><br/>\note If the articulation is already assigned to a scene (see #PxArticulationReducedCoordinate::getScene), the call is ignored and an error is issued.<br/><br/>\param[in] articulation The articulation to add to the scene.<br/>\return True if success<br/><br/>\see PxArticulationReducedCoordinate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addArticulation")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addArticulation(PxScene* @this, PxArticulationReducedCoordinate* articulation);

        /// <summary>*<br/>\brief Removes an articulation from this scene.<br/><br/>\note If the articulation is not part of this scene (see #PxArticulationReducedCoordinate::getScene), the call is ignored and an error is issued.<br/><br/>\note If the articulation is in an aggregate it will be removed from the aggregate.<br/><br/>\param[in] articulation The articulation to remove from the scene.<br/>\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame.<br/>Only applies to PxArticulationReducedCoordinate and PxRigidActor types.<br/><br/>\see PxArticulationReducedCoordinate, PxAggregate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_removeArticulation")]
        public static partial void PxScene_removeArticulation(PxScene* @this, PxArticulationReducedCoordinate* articulation, [MarshalAs(UnmanagedType.I1)] bool wakeOnLostTouch = true);

        /// <summary>*<br/>\brief Adds an actor to this scene.<br/><br/>\note If the actor is already assigned to a scene (see #PxActor::getScene), the call is ignored and an error is issued.<br/>\note If the actor has an invalid constraint, in checked builds the call is ignored and an error is issued.<br/><br/>\note You can not add individual articulation links (see #PxArticulationLink) to the scene. Use #addArticulation() instead.<br/><br/>\note If the actor is a PxRigidActor then each assigned PxConstraint object will get added to the scene automatically if<br/>it connects to another actor that is part of the scene already.<br/><br/>\note When a BVH is provided the actor shapes are grouped together.<br/>The scene query pruning structure inside PhysX SDK will store/update one<br/>bound per actor. The scene queries against such an actor will query actor<br/>bounds and then make a local space query against the provided BVH, which is in actor's local space.<br/><br/>\param[in] actor	Actor to add to scene.<br/>\param[in] bvh		BVH for actor shapes.<br/>\return True if success<br/><br/>\see PxActor, PxConstraint::isValid(), PxBVH<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addActor")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addActor(PxScene* @this, PxActor* actor, PxBVH* bvh = null);

        /// <summary>*<br/>\brief Adds actors to this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.<br/><br/>\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use addActor() instead.<br/>For articulation links, use addArticulation().<br/><br/>\note If one of the actors is already assigned to a scene (see #PxActor::getScene), the call is ignored and an error is issued.<br/><br/>\note If an actor in the array contains an invalid constraint, in checked builds the call is ignored and an error is issued.<br/>\note If an actor in the array is a PxRigidActor then each assigned PxConstraint object will get added to the scene automatically if<br/>it connects to another actor that is part of the scene already.<br/><br/>\note this method is optimized for high performance.<br/><br/>\param[in] actors Array of actors to add to scene.<br/>\param[in] nbActors Number of actors in the array.<br/>\return True if success<br/><br/>\see PxActor, PxConstraint::isValid()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addActors")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addActors(PxScene* @this, PxActor** actors, uint nbActors);

        /// <summary>*<br/>\brief Adds a pruning structure together with its actors to this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.<br/><br/>\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use addActor() instead.<br/>For articulation links, use addArticulation().<br/><br/>\note If an actor in the pruning structure contains an invalid constraint, in checked builds the call is ignored and an error is issued.<br/>\note For all actors in the pruning structure each assigned PxConstraint object will get added to the scene automatically if<br/>it connects to another actor that is part of the scene already.<br/><br/>\note This method is optimized for high performance.<br/><br/>\note Merging a PxPruningStructure into an active scene query optimization AABB tree might unbalance the tree. A typical use case for<br/>PxPruningStructure is a large world scenario where blocks of closely positioned actors get streamed in. The merge process finds the<br/>best node in the active scene query optimization AABB tree and inserts the PxPruningStructure. Therefore using PxPruningStructure<br/>for actors scattered throughout the world will result in an unbalanced tree.<br/><br/>\param[in] pruningStructure Pruning structure for a set of actors.<br/>\return True if success<br/><br/>\see PxPhysics::createPruningStructure, PxPruningStructure<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addActors_pruningStructure")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addActors_pruningStructure(PxScene* @this, PxPruningStructure* pruningStructure);

        /// <summary>*<br/>\brief Removes an actor from this scene.<br/><br/>\note If the actor is not part of this scene (see #PxActor::getScene), the call is ignored and an error is issued.<br/><br/>\note You can not remove individual articulation links (see #PxArticulationLink) from the scene. Use #removeArticulation() instead.<br/><br/>\note If the actor is a PxRigidActor then all assigned PxConstraint objects will get removed from the scene automatically.<br/><br/>\note If the actor is in an aggregate it will be removed from the aggregate.<br/><br/>\param[in] actor Actor to remove from scene.<br/>\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCoordinate and PxRigidActor types.<br/><br/>\see PxActor, PxAggregate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_removeActor")]
        public static partial void PxScene_removeActor(PxScene* @this, PxActor* actor, [MarshalAs(UnmanagedType.I1)] bool wakeOnLostTouch = true);

        /// <summary>*<br/>\brief Removes actors from this scene. Only supports actors of type PxRigidStatic and PxRigidDynamic.<br/><br/>\note This method only supports actors of type PxRigidStatic and PxRigidDynamic. For other actors, use removeActor() instead.<br/>For articulation links, use removeArticulation().<br/><br/>\note If some actor is not part of this scene (see #PxActor::getScene), the actor remove is ignored and an error is issued.<br/><br/>\note You can not remove individual articulation links (see #PxArticulationLink) from the scene. Use #removeArticulation() instead.<br/><br/>\note If the actor is a PxRigidActor then all assigned PxConstraint objects will get removed from the scene automatically.<br/><br/>\param[in] actors Array of actors to add to scene.<br/>\param[in] nbActors Number of actors in the array.<br/>\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCooridnate and PxRigidActor types.<br/><br/>\see PxActor<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_removeActors")]
        public static partial void PxScene_removeActors(PxScene* @this, PxActor** actors, uint nbActors, [MarshalAs(UnmanagedType.I1)] bool wakeOnLostTouch = true);

        /// <summary>*<br/>\brief Adds an aggregate to this scene.<br/><br/>\note If the aggregate is already assigned to a scene (see #PxAggregate::getScene), the call is ignored and an error is issued.<br/>\note If the aggregate contains an actor with an invalid constraint, in checked builds the call is ignored and an error is issued.<br/><br/>\note If the aggregate already contains actors, those actors are added to the scene as well.<br/><br/>\param[in] aggregate Aggregate to add to scene.<br/>\return True if success<br/><br/>\see PxAggregate, PxConstraint::isValid()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addAggregate")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addAggregate(PxScene* @this, PxAggregate* aggregate);

        /// <summary>*<br/>\brief Removes an aggregate from this scene.<br/><br/>\note If the aggregate is not part of this scene (see #PxAggregate::getScene), the call is ignored and an error is issued.<br/><br/>\note If the aggregate contains actors, those actors are removed from the scene as well.<br/><br/>\param[in] aggregate Aggregate to remove from scene.<br/>\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCoordinate and PxRigidActor types.<br/><br/>\see PxAggregate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_removeAggregate")]
        public static partial void PxScene_removeAggregate(PxScene* @this, PxAggregate* aggregate, [MarshalAs(UnmanagedType.I1)] bool wakeOnLostTouch = true);

        /// <summary>*<br/>\brief Adds objects in the collection to this scene.<br/><br/>This function adds the following types of objects to this scene: PxRigidActor (except PxArticulationLink), PxAggregate, PxArticulationReducedCoordinate.<br/>This method is typically used after deserializing the collection in order to populate the scene with deserialized objects.<br/><br/>\note If the collection contains an actor with an invalid constraint, in checked builds the call is ignored and an error is issued.<br/><br/>\param[in] collection Objects to add to this scene. See #PxCollection<br/>\return True if success<br/><br/>\see PxCollection, PxConstraint::isValid()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addCollection")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_addCollection(PxScene* @this, PxCollection* collection);

        /// <summary>*<br/>\brief Retrieve the number of actors of certain types in the scene. For supported types, see PxActorTypeFlags.<br/><br/>\param[in] types Combination of actor types.<br/>\return the number of actors.<br/><br/>\see getActors()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbActors")]
        public static partial uint PxScene_getNbActors(PxScene* @this, PxActorTypeFlag types);

        /// <summary>*<br/>\brief Retrieve an array of all the actors of certain types in the scene. For supported types, see PxActorTypeFlags.<br/><br/>\param[in] types Combination of actor types to retrieve.<br/>\param[out] userBuffer The buffer to receive actor pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first actor pointer to be retrieved<br/>\return Number of actors written to the buffer.<br/><br/>\see getNbActors()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getActors")]
        public static partial uint PxScene_getActors(PxScene* @this, PxActorTypeFlag types, PxActor** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Queries the PxScene for a list of the PxActors whose transforms have been<br/>updated during the previous simulation step. Only includes actors of type PxRigidDynamic and PxArticulationLink.<br/><br/>\note PxSceneFlag::eENABLE_ACTIVE_ACTORS must be set.<br/><br/>\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored and NULL will be returned.<br/><br/>\note This list may contain actors that have been released after fetchResults() of the previous simulation step. It is the user's<br/>responsibility to track such actors and avoid dereferencing the corresponding pointers.<br/><br/>\param[out] nbActorsOut The number of actors returned.<br/><br/>\return A pointer to the list of active PxActors generated during the last call to fetchResults().<br/><br/>\see PxActor<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getActiveActors")]
        public static partial PxActor** PxScene_getActiveActors(PxScene* @this, uint* nbActorsOut);

        /// <summary>*<br/>\brief Retrieve the number of deformable surfaces in the scene.<br/><br/>\return the number of deformable surfaces.<br/><br/>See getDeformableSurfaces()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbDeformableSurfaces")]
        public static partial uint PxScene_getNbDeformableSurfaces(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve an array of all the deformable surfaces in the scene.<br/><br/>\param[out] userBuffer The buffer to write the deformable surface pointers to<br/>\param[in] bufferSize Size of the provided user buffer<br/>\param[in] startIndex Index of first deformable surface pointer to be retrieved<br/>\return Number of deformable surfaces written to the buffer<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getDeformableSurfaces")]
        public static partial uint PxScene_getDeformableSurfaces(PxScene* @this, PxDeformableSurface** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Retrieve the number of deformable volumes in the scene.<br/><br/>\return the number of deformable volumes.<br/><br/>\see getActors()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbDeformableVolumes")]
        public static partial uint PxScene_getNbDeformableVolumes(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve an array of all the deformable volumes in the scene.<br/><br/>\param[out] userBuffer The buffer to receive actor pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first actor pointer to be retrieved<br/>\return Number of actors written to the buffer.<br/><br/>\see getNbActors()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getDeformableVolumes")]
        public static partial uint PxScene_getDeformableVolumes(PxScene* @this, PxDeformableVolume** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Retrieve the number of particle systems of the requested type in the scene.<br/><br/>\return the number particle systems.<br/><br/>\see getPBDParticleSystems()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbPBDParticleSystems")]
        public static partial uint PxScene_getNbPBDParticleSystems(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve an array of all the particle systems of the requested type in the scene.<br/><br/>\param[out] userBuffer The buffer to receive particle system pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first particle system pointer to be retrieved<br/>\return Number of particle systems written to the buffer.<br/><br/>\see getNbPBDParticleSystems()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getPDBParticleSystems")]
        public static partial uint PxScene_getPDBParticleSystems(PxScene* @this, PxPBDParticleSystem** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Returns the number of articulations in the scene.<br/><br/>\return the number of articulations in this scene.<br/><br/>\see getArticulations()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbArticulations")]
        public static partial uint PxScene_getNbArticulations(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve all the articulations in the scene.<br/><br/>\param[out] userBuffer The buffer to receive articulations pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first articulations pointer to be retrieved<br/>\return Number of articulations written to the buffer.<br/><br/>\see getNbArticulations()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getArticulations")]
        public static partial uint PxScene_getArticulations(PxScene* @this, PxArticulationReducedCoordinate** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Returns the number of constraint shaders in the scene.<br/><br/>\return the number of constraint shaders in this scene.<br/><br/>\see getConstraints()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbConstraints")]
        public static partial uint PxScene_getNbConstraints(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve all the constraint shaders in the scene.<br/><br/>\param[out] userBuffer The buffer to receive constraint shader pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first constraint pointer to be retrieved<br/>\return Number of constraint shaders written to the buffer.<br/><br/>\see getNbConstraints()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getConstraints")]
        public static partial uint PxScene_getConstraints(PxScene* @this, PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Returns the number of aggregates in the scene.<br/><br/>\return the number of aggregates in this scene.<br/><br/>\see getAggregates()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbAggregates")]
        public static partial uint PxScene_getNbAggregates(PxScene* @this);

        /// <summary>*<br/>\brief Retrieve all the aggregates in the scene.<br/><br/>\param[out] userBuffer The buffer to receive aggregates pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first aggregate pointer to be retrieved<br/>\return Number of aggregates written to the buffer.<br/><br/>\see getNbAggregates()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getAggregates")]
        public static partial uint PxScene_getAggregates(PxScene* @this, PxAggregate** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Specifies the dominance behavior of contacts between two actors with two certain dominance groups.<br/><br/>It is possible to assign each actor to a dominance groups using #PxActor::setDominanceGroup().<br/><br/>With dominance groups one can have all contacts created between actors act in one direction only. This is useful, for example, if you<br/>want an object to push debris out of its way and be unaffected,while still responding physically to forces and collisions<br/>with non-debris objects.<br/><br/>Whenever a contact between two actors (a0, a1) needs to be solved, the groups (g0, g1) of both<br/>actors are retrieved. Then the PxDominanceGroupPair setting for this group pair is retrieved with getDominanceGroupPair(g0, g1).<br/><br/>In the contact, PxDominanceGroupPair::dominance0 becomes the dominance setting for a0, and<br/>PxDominanceGroupPair::dominance1 becomes the dominance setting for a1. A dominanceN setting of 1.0f, the default,<br/>will permit aN to be pushed or pulled by a(1-N) through the contact. A dominanceN setting of 0.0f, will however<br/>prevent aN to be pushed by a(1-N) via the contact. Thus, a PxDominanceGroupPair of (1.0f, 0.0f) makes<br/>the interaction one-way.<br/><br/><br/>The matrix sampled by getDominanceGroupPair(g1, g2) is initialised by default such that:<br/><br/>if g1 == g2, then (1.0f, 1.0f) is returned<br/>if g1 <  g2, then (0.0f, 1.0f) is returned<br/>if g1 >  g2, then (1.0f, 0.0f) is returned<br/><br/>In other words, we permit actors in higher groups to be pushed around by actors in lower groups by default.<br/><br/>These settings should cover most applications, and in fact not overriding these settings may likely result in higher performance.<br/><br/>It is not possible to make the matrix asymetric, or to change the diagonal. In other words:<br/><br/>* it is not possible to change (g1, g2) if (g1==g2)<br/>* if you set<br/><br/>(g1, g2) to X, then (g2, g1) will implicitly and automatically be set to ~X, where:<br/><br/>~(1.0f, 1.0f) is (1.0f, 1.0f)<br/>~(0.0f, 1.0f) is (1.0f, 0.0f)<br/>~(1.0f, 0.0f) is (0.0f, 1.0f)<br/><br/>These two restrictions are to make sure that contacts between two actors will always evaluate to the same dominance<br/>setting, regardless of the order of the actors.<br/><br/>Dominance settings are currently specified as floats 0.0f or 1.0f because in the future we may permit arbitrary<br/>fractional settings to express 'partly-one-way' interactions.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake actors up automatically.<br/><br/>\see getDominanceGroupPair() PxDominanceGroup PxDominanceGroupPair PxActor::setDominanceGroup() PxActor::getDominanceGroup()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setDominanceGroupPair")]
        public static partial void PxScene_setDominanceGroupPair(PxScene* @this, PxDominanceGroup group1, PxDominanceGroup group2, PxDominanceGroupPair dominance);

        /// <summary>*<br/>\brief Samples the dominance matrix.<br/><br/>\see setDominanceGroupPair() PxDominanceGroup PxDominanceGroupPair PxActor::setDominanceGroup() PxActor::getDominanceGroup()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getDominanceGroupPair")]
        public static partial PxDominanceGroupPair PxScene_getDominanceGroupPair(PxScene* @this, PxDominanceGroup group1, PxDominanceGroup group2);

        /// <summary>*<br/>\brief Return the cpu dispatcher that was set in PxSceneDesc::cpuDispatcher when creating the scene with PxPhysics::createScene<br/><br/>\see PxSceneDesc::cpuDispatcher, PxPhysics::createScene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCpuDispatcher")]
        public static partial PxCpuDispatcher* PxScene_getCpuDispatcher(PxScene* @this);

        /// <summary>*<br/>\brief Return the CUDA context manager that was set in PxSceneDesc::cudaContextManager when creating the scene with PxPhysics::createScene<br/><br/><b>Platform specific:</b> Applies to PC GPU only.<br/><br/>\see PxSceneDesc::cudaContextManager, PxPhysics::createScene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCudaContextManager")]
        public static partial PxCudaContextManager* PxScene_getCudaContextManager(PxScene* @this);

        /// <summary>*<br/>\brief Reserves a new client ID.<br/><br/>PX_DEFAULT_CLIENT is always available as the default clientID.<br/>Additional clients are returned by this function. Clients cannot be released once created.<br/>An error is reported when more than a supported number of clients (currently 128) are created.<br/><br/>\see PxClientID<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_createClient")]
        public static partial PxClientID PxScene_createClient(PxScene* @this);

        /// <summary>*<br/>\brief Sets a user notify object which receives special simulation events when they occur.<br/><br/>\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[in] callback User notification callback. See #PxSimulationEventCallback.<br/><br/>\see PxSimulationEventCallback getSimulationEventCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setSimulationEventCallback")]
        public static partial void PxScene_setSimulationEventCallback(PxScene* @this, PxSimulationEventCallback* callback);

        /// <summary>*<br/>\brief Retrieves the simulationEventCallback pointer set with setSimulationEventCallback().<br/><br/>\return The current user notify pointer. See #PxSimulationEventCallback.<br/><br/>\see PxSimulationEventCallback setSimulationEventCallback()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getSimulationEventCallback")]
        public static partial PxSimulationEventCallback* PxScene_getSimulationEventCallback(PxScene* @this);

        /// <summary>*<br/>\brief Sets a user callback object, which receives callbacks on all contacts generated for specified actors.<br/><br/>\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[in] callback Asynchronous user contact modification callback. See #PxContactModifyCallback.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setContactModifyCallback")]
        public static partial void PxScene_setContactModifyCallback(PxScene* @this, PxContactModifyCallback* callback);

        /// <summary>*<br/>\brief Sets a user callback object, which receives callbacks on all CCD contacts generated for specified actors.<br/><br/>\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[in] callback Asynchronous user contact modification callback. See #PxCCDContactModifyCallback.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setCCDContactModifyCallback")]
        public static partial void PxScene_setCCDContactModifyCallback(PxScene* @this, PxCCDContactModifyCallback* callback);

        /// <summary>*<br/>\brief Retrieves the PxContactModifyCallback pointer set with setContactModifyCallback().<br/><br/>\return The current user contact modify callback pointer. See #PxContactModifyCallback.<br/><br/>\see PxContactModifyCallback setContactModifyCallback()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getContactModifyCallback")]
        public static partial PxContactModifyCallback* PxScene_getContactModifyCallback(PxScene* @this);

        /// <summary>*<br/>\brief Retrieves the PxContactModifyCallback pointer set with setContactModifyCallback().<br/><br/>\return The current user contact modify callback pointer. See #PxContactModifyCallback.<br/><br/>\see PxContactModifyCallback setContactModifyCallback()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCCDContactModifyCallback")]
        public static partial PxCCDContactModifyCallback* PxScene_getCCDContactModifyCallback(PxScene* @this);

        /// <summary>*<br/>\brief Sets a broad-phase user callback object.<br/><br/>\note Do not set the callback while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[in] callback	Asynchronous broad-phase callback. See #PxBroadPhaseCallback.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setBroadPhaseCallback")]
        public static partial void PxScene_setBroadPhaseCallback(PxScene* @this, PxBroadPhaseCallback* callback);

        /// <summary>*<br/>\brief Retrieves the PxBroadPhaseCallback pointer set with setBroadPhaseCallback().<br/><br/>\return The current broad-phase callback pointer. See #PxBroadPhaseCallback.<br/><br/>\see PxBroadPhaseCallback setBroadPhaseCallback()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getBroadPhaseCallback")]
        public static partial PxBroadPhaseCallback* PxScene_getBroadPhaseCallback(PxScene* @this);

        /// <summary>*<br/>\brief Sets the shared global filter data which will get passed into the filter shader.<br/><br/>\note It is the user's responsibility to ensure that changing the shared global filter data does not change the filter output value for existing pairs.<br/>If the filter output for existing pairs does change nonetheless then such a change will not take effect until the pair gets refiltered.<br/>resetFiltering() can be used to explicitly refilter the pairs of specific objects.<br/><br/>\note The provided data will get copied to internal buffers and this copy will be used for filtering calls.<br/><br/>\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[in] data The shared global filter shader data.<br/>\param[in] dataSize Size of the shared global filter shader data (in bytes).<br/><br/>\see getFilterShaderData() PxSceneDesc.filterShaderData PxSimulationFilterShader<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setFilterShaderData")]
        public static partial void PxScene_setFilterShaderData(PxScene* @this, void* data, uint dataSize);

        /// <summary>*<br/>\brief Gets the shared global filter data in use for this scene.<br/><br/>\note The reference points to a copy of the original filter data specified in #PxSceneDesc.filterShaderData or provided by #setFilterShaderData().<br/><br/>\return Shared filter data for filter shader.<br/><br/>\see getFilterShaderDataSize() setFilterShaderData() PxSceneDesc.filterShaderData PxSimulationFilterShader<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFilterShaderData")]
        public static partial void* PxScene_getFilterShaderData(PxScene* @this);

        /// <summary>*<br/>\brief Gets the size of the shared global filter data (#PxSceneDesc.filterShaderData)<br/><br/>\return Size of shared filter data [bytes].<br/><br/>\see getFilterShaderData() PxSceneDesc.filterShaderDataSize PxSimulationFilterShader<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFilterShaderDataSize")]
        public static partial uint PxScene_getFilterShaderDataSize(PxScene* @this);

        /// <summary>*<br/>\brief Gets the custom collision filter shader in use for this scene.<br/><br/>\return Filter shader class that defines the collision pair filtering.<br/><br/>\see PxSceneDesc.filterShader PxSimulationFilterShader<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFilterShader")]
        public static partial PxSimulationFilterShader* PxScene_getFilterShader(PxScene* @this);

        /// <summary>*<br/>\brief Gets the custom collision filter callback in use for this scene.<br/><br/>\return Filter callback class that defines the collision pair filtering.<br/><br/>\see PxSceneDesc.filterCallback PxSimulationFilterCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFilterCallback")]
        public static partial PxSimulationFilterCallback* PxScene_getFilterCallback(PxScene* @this);

        /// <summary>*<br/>\brief Marks the object to reset interactions and re-run collision filters in the next simulation step.<br/><br/>This call forces the object to remove all existing collision interactions, to search anew for existing contact<br/>pairs and to run the collision filters again for found collision pairs.<br/><br/>\note The operation is supported for PxRigidActor objects only.<br/><br/>\note All persistent state of existing interactions will be lost and can not be retrieved even if the same collison pair<br/>is found again in the next step. This will mean, for example, that you will not get notified about persistent contact<br/>for such an interaction (see #PxPairFlag::eNOTIFY_TOUCH_PERSISTS), the contact pair will be interpreted as newly found instead.<br/><br/>\note Lost touch contact reports will be sent for every collision pair which includes this shape, if they have<br/>been requested through #PxPairFlag::eNOTIFY_TOUCH_LOST or #PxPairFlag::eNOTIFY_THRESHOLD_FORCE_LOST.<br/><br/>\note This is an expensive operation, don't use it if you don't have to.<br/><br/>\note Can be used to retrieve collision pairs that were killed by the collision filters (see #PxFilterFlag::eKILL)<br/><br/>\note It is invalid to use this method if the actor has not been added to a scene already.<br/><br/>\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/><b>Sleeping:</b> Does wake up the actor.<br/><br/>\param[in] actor The actor for which to re-evaluate interactions.<br/>\return True if success<br/><br/>\see PxSimulationFilterShader PxSimulationFilterCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_resetFiltering")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_resetFiltering(PxScene* @this, PxActor* actor);

        /// <summary>*<br/>\brief Marks the object to reset interactions and re-run collision filters for specified shapes in the next simulation step.<br/><br/>This is a specialization of the resetFiltering(PxActor& actor) method and allows to reset interactions for specific shapes of<br/>a PxRigidActor.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/><b>Sleeping:</b> Does wake up the actor.<br/><br/>\param[in] actor The actor for which to re-evaluate interactions.<br/>\param[in] shapes The shapes for which to re-evaluate interactions.<br/>\param[in] shapeCount Number of shapes in the list.<br/><br/>\see PxSimulationFilterShader PxSimulationFilterCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_resetFiltering_shapes")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_resetFiltering_shapes(PxScene* @this, PxRigidActor* actor, PxShape** shapes, uint shapeCount);

        /// <summary>*<br/>\brief Gets the pair filtering mode for kinematic-kinematic pairs.<br/><br/>\return Filtering mode for kinematic-kinematic pairs.<br/><br/>\see PxPairFilteringMode PxSceneDesc<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getKinematicKinematicFilteringMode")]
        public static partial PxPairFilteringMode PxScene_getKinematicKinematicFilteringMode(PxScene* @this);

        /// <summary>*<br/>\brief Gets the pair filtering mode for static-kinematic pairs.<br/><br/>\return Filtering mode for static-kinematic pairs.<br/><br/>\see PxPairFilteringMode PxSceneDesc<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getStaticKinematicFilteringMode")]
        public static partial PxPairFilteringMode PxScene_getStaticKinematicFilteringMode(PxScene* @this);

        /// <summary>*<br/>\brief Advances the simulation by an elapsedTime time.<br/><br/>\note Large elapsedTime values can lead to instabilities. In such cases elapsedTime<br/>should be subdivided into smaller time intervals and simulate() should be called<br/>multiple times for each interval.<br/><br/>Calls to simulate() should pair with calls to fetchResults():<br/>Each fetchResults() invocation corresponds to exactly one simulate()<br/>invocation; calling simulate() twice without an intervening fetchResults()<br/>or fetchResults() twice without an intervening simulate() causes an error<br/>condition.<br/><br/>scene->simulate();<br/>...do some processing until physics is computed...<br/>scene->fetchResults();<br/>...now results of run may be retrieved.<br/><br/><br/>\param[in] elapsedTime Amount of time to advance simulation by. The parameter has to be larger than 0, else the resulting behavior will be undefined. <b>Range:</b> (0, PX_MAX_F32)<br/>\param[in] completionTask if non-NULL, this task will have its refcount incremented in simulate(), then<br/>decremented when the scene is ready to have fetchResults called. So the task will not run until the<br/>application also calls removeReference().<br/>\param[in] scratchMemBlock a memory region for physx to use for temporary data during simulation. This block may be reused by the application<br/>after fetchResults returns. Must be aligned on a 16-byte boundary<br/>\param[in] scratchMemBlockSize the size of the scratch memory block. Must be a multiple of 16K.<br/>\param[in] controlSimulation if true, the scene controls its PxTaskManager simulation state. Leave<br/>true unless the application is calling the PxTaskManager start/stopSimulation() methods itself.<br/>\return True if success<br/><br/>\see fetchResults() checkResults()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_simulate")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_simulate(PxScene* @this, float elapsedTime, PxBaseTask* completionTask = null, void* scratchMemBlock = null, uint scratchMemBlockSize = 0, [MarshalAs(UnmanagedType.I1)] bool controlSimulation = true);

        /// <summary>*<br/>\brief Performs dynamics phase of the simulation pipeline.<br/><br/>\note Calls to advance() should follow calls to fetchCollision(). An error message will be issued if this sequence is not followed.<br/><br/>\param[in] completionTask if non-NULL, this task will have its refcount incremented in advance(), then<br/>decremented when the scene is ready to have fetchResults called. So the task will not run until the<br/>application also calls removeReference().<br/>\return True if success<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_advance")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_advance(PxScene* @this, PxBaseTask* completionTask = null);

        /// <summary>*<br/>\brief Performs collision detection for the scene over elapsedTime<br/><br/>\note Calls to collide() should be the first method called to simulate a frame.<br/><br/><br/>\param[in] elapsedTime Amount of time to advance simulation by. The parameter has to be larger than 0, else the resulting behavior will be undefined. <b>Range:</b> (0, PX_MAX_F32)<br/>\param[in] completionTask if non-NULL, this task will have its refcount incremented in collide(), then<br/>decremented when the scene is ready to have fetchResults called. So the task will not run until the<br/>application also calls removeReference().<br/>\param[in] scratchMemBlock a memory region for physx to use for temporary data during simulation. This block may be reused by the application<br/>after fetchResults returns. Must be aligned on a 16-byte boundary<br/>\param[in] scratchMemBlockSize the size of the scratch memory block. Must be a multiple of 16K.<br/>\param[in] controlSimulation if true, the scene controls its PxTaskManager simulation state. Leave<br/>true unless the application is calling the PxTaskManager start/stopSimulation() methods itself.<br/>\return True if success<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_collide")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_collide(PxScene* @this, float elapsedTime, PxBaseTask* completionTask = null, void* scratchMemBlock = null, uint scratchMemBlockSize = 0, [MarshalAs(UnmanagedType.I1)] bool controlSimulation = true);

        /// <summary>*<br/>\brief This checks to see if the simulation run has completed.<br/><br/>This does not cause the data available for reading to be updated with the results of the simulation, it is simply a status check.<br/>The bool will allow it to either return immediately or block waiting for the condition to be met so that it can return true<br/><br/>\param[in] block When set to true will block until the condition is met.<br/>\return True if the results are available.<br/><br/>\see simulate() fetchResults()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_checkResults")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_checkResults(PxScene* @this, [MarshalAs(UnmanagedType.I1)] bool block = false);

        /// <summary>*<br/>This method must be called after collide() and before advance(). It will wait for the collision phase to finish. If the user makes an illegal simulation call, the SDK will issue an error<br/>message.<br/><br/>\param[in] block When set to true will block until the condition is met, which is collision must finish running.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_fetchCollision")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_fetchCollision(PxScene* @this, [MarshalAs(UnmanagedType.I1)] bool block = false);

        /// <summary>*<br/>This is the big brother to checkResults() it basically does the following:<br/><br/>\code<br/>if ( checkResults(block) )<br/>{<br/>fire appropriate callbacks<br/>swap buffers<br/>return true<br/>}<br/>else<br/>return false<br/><br/>\endcode<br/><br/>\param[in] block When set to true will block until results are available.<br/>\param[out] errorState Used to retrieve hardware error codes. A non zero value indicates an error.<br/>\return True if the results have been fetched.<br/><br/>\see simulate() checkResults()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_fetchResults")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_fetchResults(PxScene* @this, [MarshalAs(UnmanagedType.I1)] bool block = false, uint* errorState = null);

        /// <summary>*<br/>This call performs the first section of fetchResults, and returns a pointer to the contact streams output by the simulation. It can be used to process contact pairs in parallel, which is often a limiting factor<br/>for fetchResults() performance.<br/><br/>After calling this function and processing the contact streams, call fetchResultsFinish(). Note that writes to the simulation are not<br/>permitted between the start of fetchResultsStart() and the end of fetchResultsFinish().<br/><br/>\param[in] block When set to true will block until results are available.<br/>\param[out] contactPairs an array of pointers to contact pair headers<br/>\param[out] nbContactPairs the number of contact pairs<br/>\return True if the results have been fetched.<br/><br/>\see simulate() checkResults() fetchResults() fetchResultsFinish()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_fetchResultsStart")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_fetchResultsStart(PxScene* @this, PxContactPairHeader** contactPairs, uint* nbContactPairs, [MarshalAs(UnmanagedType.I1)] bool block = false);

        /// <summary>*<br/>This call processes all event callbacks in parallel. It takes a continuation task, which will be executed once all callbacks have been processed.<br/><br/>This is a utility function to make it easier to process callbacks in parallel using the PhysX task system. It can only be used in conjunction with<br/>fetchResultsStart(...) and fetchResultsFinish(...)<br/><br/>\param[in] continuation The task that will be executed once all callbacks have been processed.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_processCallbacks")]
        public static partial void PxScene_processCallbacks(PxScene* @this, PxBaseTask* continuation);

        /// <summary>*<br/>This call performs the second section of fetchResults.<br/><br/>It must be called after fetchResultsStart() returns and contact reports have been processed.<br/><br/>Note that once fetchResultsFinish() has been called, the contact streams returned in fetchResultsStart() will be invalid.<br/><br/>\param[out] errorState Used to retrieve hardware error codes. A non zero value indicates an error.<br/><br/>\see simulate() checkResults() fetchResults() fetchResultsStart()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_fetchResultsFinish")]
        public static partial void PxScene_fetchResultsFinish(PxScene* @this, uint* errorState = null);

        /// <summary>*<br/>This call performs the synchronization of particle system data copies.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_fetchResultsParticleSystem")]
        public static partial void PxScene_fetchResultsParticleSystem(PxScene* @this);

        /// <summary>*<br/>\brief Clear internal buffers and free memory.<br/><br/>This method can be used to clear buffers and free internal memory without having to destroy the scene. Can be useful if<br/>the physics data gets streamed in and a checkpoint with a clean state should be created.<br/><br/>\note It is not allowed to call this method while the simulation is running. The call will fail.<br/><br/>\param[in] sendPendingReports When set to true pending reports will be sent out before the buffers get cleaned up (for instance lost touch contact/trigger reports due to deleted objects).<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_flushSimulation")]
        public static partial void PxScene_flushSimulation(PxScene* @this, [MarshalAs(UnmanagedType.I1)] bool sendPendingReports = false);

        /// <summary>*<br/>\brief Sets a constant gravity for the entire scene.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.<br/><br/>\param[in] vec A new gravity vector(e.g. PxVec3(0.0f,-9.8f,0.0f) ) <b>Range:</b> force vector<br/><br/>\see PxSceneDesc.gravity getGravity()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setGravity")]
        public static partial void PxScene_setGravity(PxScene* @this, PxVec3 vec);

        /// <summary>*<br/>\brief Retrieves the current gravity setting.<br/><br/>\return The current gravity for the scene.<br/><br/>\see setGravity() PxSceneDesc.gravity<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getGravity")]
        public static partial PxVec3 PxScene_getGravity(PxScene* @this);

        /// <summary>*<br/>\brief Set the bounce threshold velocity.  Collision speeds below this threshold will not cause a bounce.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::bounceThresholdVelocity, getBounceThresholdVelocity<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setBounceThresholdVelocity")]
        public static partial void PxScene_setBounceThresholdVelocity(PxScene* @this, float t);

        /// <summary>*<br/>\brief Return the bounce threshold velocity.<br/><br/>\see PxSceneDesc.bounceThresholdVelocity, setBounceThresholdVelocity<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getBounceThresholdVelocity")]
        public static partial float PxScene_getBounceThresholdVelocity(PxScene* @this);

        /// <summary>*<br/>\brief Sets the maximum number of CCD passes<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] ccdMaxPasses Maximum number of CCD passes<br/><br/>\see PxSceneDesc.ccdMaxPasses getCCDMaxPasses()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setCCDMaxPasses")]
        public static partial void PxScene_setCCDMaxPasses(PxScene* @this, uint ccdMaxPasses);

        /// <summary>*<br/>\brief Gets the maximum number of CCD passes.<br/><br/>\return The maximum number of CCD passes.<br/><br/>\see PxSceneDesc::ccdMaxPasses setCCDMaxPasses()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCCDMaxPasses")]
        public static partial uint PxScene_getCCDMaxPasses(PxScene* @this);

        /// <summary>*<br/>\brief Set the maximum CCD separation.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::ccdMaxSeparation, getCCDMaxSeparation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setCCDMaxSeparation")]
        public static partial void PxScene_setCCDMaxSeparation(PxScene* @this, float t);

        /// <summary>*<br/>\brief Gets the maximum CCD separation.<br/><br/>\return The maximum CCD separation.<br/><br/>\see PxSceneDesc::ccdMaxSeparation setCCDMaxSeparation()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCCDMaxSeparation")]
        public static partial float PxScene_getCCDMaxSeparation(PxScene* @this);

        /// <summary>*<br/>\brief Set the CCD threshold.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::ccdThreshold, getCCDThreshold<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setCCDThreshold")]
        public static partial void PxScene_setCCDThreshold(PxScene* @this, float t);

        /// <summary>*<br/>\brief Gets the CCD threshold.<br/><br/>\return The CCD threshold.<br/><br/>\see PxSceneDesc::ccdThreshold setCCDThreshold()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getCCDThreshold")]
        public static partial float PxScene_getCCDThreshold(PxScene* @this);

        /// <summary>*<br/>\brief Set the max bias coefficient.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::maxBiasCoefficient, getMaxBiasCoefficient<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setMaxBiasCoeffecient")]
        public static partial void PxScene_setMaxBiasCoeffecient(PxScene* @this, float t);

        /// <summary>*<br/>\brief Gets the max bias coefficient.<br/><br/>\return The max bias coefficient.<br/><br/>\see PxSceneDesc::maxBiasCoefficient setMaxBiasCoefficient()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getMaxBiasCoeffecient")]
        public static partial float PxScene_getMaxBiasCoeffecient(PxScene* @this);

        /// <summary>*<br/>\brief Set the friction offset threshold.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::frictionOffsetThreshold, getFrictionOffsetThreshold<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setFrictionOffsetThreshold")]
        public static partial void PxScene_setFrictionOffsetThreshold(PxScene* @this, float t);

        /// <summary>*<br/>\brief Gets the friction offset threshold.<br/><br/>\see PxSceneDesc::frictionOffsetThreshold, setFrictionOffsetThreshold<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFrictionOffsetThreshold")]
        public static partial float PxScene_getFrictionOffsetThreshold(PxScene* @this);

        /// <summary>*<br/>\brief Set the friction correlation distance.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\see PxSceneDesc::frictionCorrelationDistance, getFrictionCorrelationDistance<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setFrictionCorrelationDistance")]
        public static partial void PxScene_setFrictionCorrelationDistance(PxScene* @this, float t);

        /// <summary>*<br/>\brief Gets the friction correlation distance.<br/><br/>\see PxSceneDesc::frictionCorrelationDistance, setFrictionCorrelationDistance<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getFrictionCorrelationDistance")]
        public static partial float PxScene_getFrictionCorrelationDistance(PxScene* @this);

        /// <summary>*<br/>\brief Return the solver model.<br/>\see PxSolverType, PxSceneDesc::solverType<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getSolverType")]
        public static partial PxSolverType PxScene_getSolverType(PxScene* @this);

        /// <summary>*<br/>\brief Function that lets you set debug visualization parameters.<br/><br/>Returns false if the value passed is out of range for usage specified by the enum.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] param	Parameter to set. See #PxVisualizationParameter<br/>\param[in] value	The value to set, see #PxVisualizationParameter for allowable values. Setting to zero disables visualization for the specified property, setting to a positive value usually enables visualization and defines the scale factor.<br/>\return False if the parameter is out of range.<br/><br/>\see getVisualizationParameter PxVisualizationParameter getRenderBuffer()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setVisualizationParameter")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_setVisualizationParameter(PxScene* @this, PxVisualizationParameter param, float value);

        /// <summary>*<br/>\brief Function that lets you query debug visualization parameters.<br/><br/>\param[in] paramEnum The Parameter to retrieve.<br/>\return The value of the parameter.<br/><br/>\see setVisualizationParameter PxVisualizationParameter<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getVisualizationParameter")]
        public static partial float PxScene_getVisualizationParameter(PxScene* @this, PxVisualizationParameter paramEnum);

        /// <summary>*<br/>\brief Defines a box in world space to which visualization geometry will be (conservatively) culled. Use a non-empty culling box to enable the feature, and an empty culling box to disable it.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] box the box to which the geometry will be culled. Empty box to disable the feature.<br/>\see setVisualizationParameter getVisualizationCullingBox getRenderBuffer()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setVisualizationCullingBox")]
        public static partial void PxScene_setVisualizationCullingBox(PxScene* @this, PxBounds3 box);

        /// <summary>*<br/>\brief Retrieves the visualization culling box.<br/><br/>\return the box to which the geometry will be culled.<br/>\see setVisualizationParameter setVisualizationCullingBox<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getVisualizationCullingBox")]
        public static partial PxBounds3 PxScene_getVisualizationCullingBox(PxScene* @this);

        /// <summary>*<br/>\brief Retrieves the render buffer.<br/><br/>This will contain the results of any active visualization for this scene.<br/><br/>\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will result in undefined behaviour.<br/><br/>\return The render buffer.<br/><br/>\see PxRenderBuffer<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getRenderBuffer")]
        public static partial PxRenderBuffer PxScene_getRenderBuffer(PxScene* @this);

        /// <summary>*<br/>\brief Call this method to retrieve statistics for the current simulation step.<br/><br/>\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\param[out] stats Used to retrieve statistics for the current simulation step.<br/><br/>\see PxSimulationStatistics<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getSimulationStatistics")]
        public static partial void PxScene_getSimulationStatistics(PxScene* @this, PxSimulationStatistics* stats);

        /// <summary>*<br/>\brief Returns broad-phase type.<br/><br/>\return Broad-phase type<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getBroadPhaseType")]
        public static partial PxBroadPhaseType PxScene_getBroadPhaseType(PxScene* @this);

        /// <summary>*<br/>\brief Gets broad-phase caps.<br/><br/>\param[out]	caps	Broad-phase caps<br/>\return True if success<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getBroadPhaseCaps")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_getBroadPhaseCaps(PxScene* @this, PxBroadPhaseCaps* caps);

        /// <summary>*<br/>\brief Returns number of regions currently registered in the broad-phase.<br/><br/>\return Number of regions<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbBroadPhaseRegions")]
        public static partial uint PxScene_getNbBroadPhaseRegions(PxScene* @this);

        /// <summary>*<br/>\brief Gets broad-phase regions.<br/><br/>\param[out]	userBuffer	Returned broad-phase regions<br/>\param[in]	bufferSize	Size of userBuffer<br/>\param[in]	startIndex	Index of first desired region, in [0 ; getNbRegions()[<br/>\return Number of written out regions<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getBroadPhaseRegions")]
        public static partial uint PxScene_getBroadPhaseRegions(PxScene* @this, PxBroadPhaseRegionInfo** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Adds a new broad-phase region.<br/><br/>The bounds for the new region must be non-empty, otherwise an error occurs and the call is ignored.<br/><br/>Note that by default, objects already existing in the SDK that might touch this region will not be automatically<br/>added to the region. In other words the newly created region will be empty, and will only be populated with new<br/>objects when they are added to the simulation, or with already existing objects when they are updated.<br/><br/>It is nonetheless possible to override this default behavior and let the SDK populate the new region automatically<br/>with already existing objects overlapping the incoming region. This has a cost though, and it should only be used<br/>when the game can not guarantee that all objects within the new region will be added to the simulation after the<br/>region itself.<br/><br/>Objects automatically move from one region to another during their lifetime. The system keeps tracks of what<br/>regions a given object is in. It is legal for an object to be in an arbitrary number of regions. However if an<br/>object leaves all regions, or is created outside of all regions, several things happen:<br/>- collisions get disabled for this object<br/>- if a PxBroadPhaseCallback object is provided, an "out-of-bounds" event is generated via that callback<br/>- if a PxBroadPhaseCallback object is not provided, a warning/error message is sent to the error stream<br/><br/>If an object goes out-of-bounds and user deletes it during the same frame, neither the out-of-bounds event nor the<br/>error message is generated.<br/><br/>\param[in]	region			User-provided region data<br/>\param[in]	populateRegion	Automatically populate new region with already existing objects overlapping it<br/>\return Handle for newly created region, or 0xffffffff in case of failure.<br/>\see	PxBroadPhaseRegion PxBroadPhaseCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_addBroadPhaseRegions")]
        public static partial uint PxScene_addBroadPhaseRegions(PxScene* @this, PxBroadPhaseRegion region, [MarshalAs(UnmanagedType.I1)] bool populateRegion = false);

        /// <summary>*<br/>\brief Removes a new broad-phase region.<br/><br/>If the region still contains objects, and if those objects do not overlap any region any more, they are not<br/>automatically removed from the simulation. Instead, the PxBroadPhaseCallback::onObjectOutOfBounds notification<br/>is used for each object. Users are responsible for removing the objects from the simulation if this is the<br/>desired behavior.<br/><br/>If the handle is invalid, or if a valid handle is removed twice, an error message is sent to the error stream.<br/><br/>\param[in]	handle	Region's handle, as returned by PxScene::addBroadPhaseRegion.<br/>\return True if success<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_removeBroadPhaseRegion")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxScene_removeBroadPhaseRegion(PxScene* @this, uint handle);

        /// <summary>*<br/>\brief Get the task manager associated with this scene<br/><br/>\return the task manager associated with the scene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getTaskManager")]
        public static partial PxTaskManager* PxScene_getTaskManager(PxScene* @this);

        /// <summary>*<br/>\brief Lock the scene for reading from the calling thread.<br/><br/>When the PxSceneFlag::eREQUIRE_RW_LOCK flag is enabled lockRead() must be<br/>called before any read calls are made on the scene.<br/><br/>Multiple threads may read at the same time, no threads may read while a thread is writing.<br/>If a call to lockRead() is made while another thread is holding a write lock<br/>then the calling thread will be blocked until the writing thread calls unlockWrite().<br/><br/>\note Lock upgrading is *not* supported, that means it is an error to<br/>call lockRead() followed by lockWrite().<br/><br/>\note Recursive locking is supported but each lockRead() call must be paired with an unlockRead().<br/><br/>\param file String representing the calling file, for debug purposes<br/>\param line The source file line number, for debug purposes<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_lockRead")]
        public static partial void PxScene_lockRead(PxScene* @this, byte* file = null, uint line = 0);

        /// <summary>*<br/>\brief Unlock the scene from reading.<br/><br/>\note Each unlockRead() must be paired with a lockRead() from the same thread.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_unlockRead")]
        public static partial void PxScene_unlockRead(PxScene* @this);

        /// <summary>*<br/>\brief Lock the scene for writing from this thread.<br/><br/>When the PxSceneFlag::eREQUIRE_RW_LOCK flag is enabled lockWrite() must be<br/>called before any write calls are made on the scene.<br/><br/>Only one thread may write at a time and no threads may read while a thread is writing.<br/>If a call to lockWrite() is made and there are other threads reading then the<br/>calling thread will be blocked until the readers complete.<br/><br/>Writers have priority. If a thread is blocked waiting to write then subsequent calls to<br/>lockRead() from other threads will be blocked until the writer completes.<br/><br/>\note If multiple threads are waiting to write then the thread that is first<br/>granted access depends on OS scheduling.<br/><br/>\note Recursive locking is supported but each lockWrite() call must be paired<br/>with an unlockWrite().<br/><br/>\note If a thread has already locked the scene for writing then it may call<br/>lockRead().<br/><br/>\param file String representing the calling file, for debug purposes<br/>\param line The source file line number, for debug purposes<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_lockWrite")]
        public static partial void PxScene_lockWrite(PxScene* @this, byte* file = null, uint line = 0);

        /// <summary>*<br/>\brief Unlock the scene from writing.<br/><br/>\note Each unlockWrite() must be paired with a lockWrite() from the same thread.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_unlockWrite")]
        public static partial void PxScene_unlockWrite(PxScene* @this);

        /// <summary>*<br/>\brief set the cache blocks that can be used during simulate().<br/><br/>Each frame the simulation requires memory to store contact, friction, and contact cache data. This memory is used in blocks of 16K.<br/>Each frame the blocks used by the previous frame are freed, and may be retrieved by the application using PxScene::flushSimulation()<br/><br/>This call will force allocation of cache blocks if the numBlocks parameter is greater than the currently allocated number<br/>of blocks, and less than the max16KContactDataBlocks parameter specified at scene creation time.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] numBlocks The number of blocks to allocate.<br/><br/>\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() getNbContactDataBlocksUsed getMaxNbContactDataBlocksUsed<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setNbContactDataBlocks")]
        public static partial void PxScene_setNbContactDataBlocks(PxScene* @this, uint numBlocks);

        /// <summary>*<br/>\brief get the number of cache blocks currently used by the scene<br/><br/>This function may not be called while the scene is simulating<br/><br/>\return the number of cache blocks currently used by the scene<br/><br/>\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() setNbContactDataBlocks() getMaxNbContactDataBlocksUsed()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getNbContactDataBlocksUsed")]
        public static partial uint PxScene_getNbContactDataBlocksUsed(PxScene* @this);

        /// <summary>*<br/>\brief get the maximum number of cache blocks used by the scene<br/><br/>This function may not be called while the scene is simulating<br/><br/>\return the maximum number of cache blocks everused by the scene<br/><br/>\see PxSceneDesc.nbContactDataBlocks PxSceneDesc.maxNbContactDataBlocks flushSimulation() setNbContactDataBlocks() getNbContactDataBlocksUsed()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getMaxNbContactDataBlocksUsed")]
        public static partial uint PxScene_getMaxNbContactDataBlocksUsed(PxScene* @this);

        /// <summary>*<br/>\brief Return the value of PxSceneDesc::contactReportStreamBufferSize that was set when creating the scene with PxPhysics::createScene<br/><br/>\see PxSceneDesc::contactReportStreamBufferSize, PxPhysics::createScene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getContactReportStreamBufferSize")]
        public static partial uint PxScene_getContactReportStreamBufferSize(PxScene* @this);

        /// <summary>*<br/>\brief Sets the number of actors required to spawn a separate rigid body solver thread.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] solverBatchSize Number of actors required to spawn a separate rigid body solver thread.<br/><br/>\see PxSceneDesc.solverBatchSize getSolverBatchSize()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setSolverBatchSize")]
        public static partial void PxScene_setSolverBatchSize(PxScene* @this, uint solverBatchSize);

        /// <summary>*<br/>\brief Retrieves the number of actors required to spawn a separate rigid body solver thread.<br/><br/>\return Current number of actors required to spawn a separate rigid body solver thread.<br/><br/>\see PxSceneDesc.solverBatchSize setSolverBatchSize()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getSolverBatchSize")]
        public static partial uint PxScene_getSolverBatchSize(PxScene* @this);

        /// <summary>*<br/>\brief Sets the number of articulations required to spawn a separate rigid body solver thread.<br/><br/>\note Do not use this method while the simulation is running.<br/><br/>\param[in] solverBatchSize Number of articulations required to spawn a separate rigid body solver thread.<br/><br/>\see PxSceneDesc.solverBatchSize getSolverArticulationBatchSize()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setSolverArticulationBatchSize")]
        public static partial void PxScene_setSolverArticulationBatchSize(PxScene* @this, uint solverBatchSize);

        /// <summary>*<br/>\brief Retrieves the number of articulations required to spawn a separate rigid body solver thread.<br/><br/>\return Current number of articulations required to spawn a separate rigid body solver thread.<br/><br/>\see PxSceneDesc.solverBatchSize setSolverArticulationBatchSize()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getSolverArticulationBatchSize")]
        public static partial uint PxScene_getSolverArticulationBatchSize(PxScene* @this);

        /// <summary>*<br/>\brief Returns the wake counter reset value.<br/><br/>\return Wake counter reset value<br/><br/>\see PxSceneDesc.wakeCounterResetValue<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getWakeCounterResetValue")]
        public static partial float PxScene_getWakeCounterResetValue(PxScene* @this);

        /// <summary>*<br/>\brief Shift the scene origin by the specified vector.<br/><br/>The poses of all objects in the scene and the corresponding data structures will get adjusted to reflect the new origin location<br/>(the shift vector will get subtracted from all object positions).<br/><br/>\note It is the user's responsibility to keep track of the summed total origin shift and adjust all input/output to/from PhysX accordingly.<br/><br/>\note Do not use this method while the simulation is running. Calls to this method while the simulation is running will be ignored.<br/><br/>\note Make sure to propagate the origin shift to other dependent modules (for example, the character controller module etc.).<br/><br/>\note This is an expensive operation and we recommend to use it only in the case where distance related precision issues may arise in areas far from the origin.<br/><br/>\param[in] shift Translation vector to shift the origin by.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_shiftOrigin")]
        public static partial void PxScene_shiftOrigin(PxScene* @this, PxVec3 shift);

        /// <summary>*<br/>\brief Returns the Pvd client associated with the scene.<br/>\return the client, NULL if no PVD supported.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getScenePvdClient")]
        public static partial PxPvdSceneClient* PxScene_getScenePvdClient(PxScene* @this);

        /// <summary>*<br/>\brief Get the PxGpuDynamicsMemoryConfig that was passed into PxPhysics::createScene() as part of PxSceneDesc.<br/><br/>\note This will return the values passed as initial configuration, for the actual minimal configuration that would be needed<br/>for a specific simulation of a scene, see PxSimulationStatistics::gpuDynamicsMemoryConfigStatistics.<br/><br/>\return The PxGpuDynamicsMemoryConfig used during scene creation.<br/><br/>\see PxSceneDesc::gpuDynamicsConfig, PxSimulationStatistics::gpuDynamicsMemoryConfigStatistics<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getGpuDynamicsConfig")]
        public static partial PxGpuDynamicsMemoryConfig PxScene_getGpuDynamicsConfig(PxScene* @this);

        /// <summary>*<br/>\brief Get the direct-GPU API instance for this scene.<br/><br/>\see PxDirectGPUAPI for the supported direct-GPU operations.<br/><br/>Each object of PxDirectGPUAPI is directly associated with a PxScene, and there is only one PxDirectGPUAPI object per scene.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_getDirectGPUAPI")]
        public static partial PxDirectGPUAPI* PxScene_getDirectGPUAPI(PxScene* @this);

        /// <summary>*<br/>\brief Sets the post-solve callback for deformable surface GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable surface solver finishes.<br/>\param postSolveCallback Pointer to the callback implementation.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setDeformableSurfaceGpuPostSolveCallback")]
        public static partial void PxScene_setDeformableSurfaceGpuPostSolveCallback(PxScene* @this, PxPostSolveCallback* postSolveCallback);

        /// <summary>*<br/>\brief Sets the post-solve callback for deformable volume GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable volume solver finishes.<br/>\param postSolveCallback Pointer to the callback implementation.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_setDeformableVolumeGpuPostSolveCallback")]
        public static partial void PxScene_setDeformableVolumeGpuPostSolveCallback(PxScene* @this, PxPostSolveCallback* postSolveCallback);

        /// <summary>*<br/>\brief Sets the post-solve callback for deformable volume GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable volume solver finishes.<br/>\param postSolveCallback Pointer to the callback implementation.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_userData_set")]
        public static partial void PxScene_userData_set(PxScene* @this, void* userData);

        /// <summary>*<br/>\brief Sets the post-solve callback for deformable volume GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable volume solver finishes.<br/>\param postSolveCallback Pointer to the callback implementation.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_userData_get")]
        public static partial void* PxScene_userData_get(PxScene* @this);

        /// <summary>*<br/>\brief Sets the post-solve callback for deformable volume GPU computations. Allows to schedule custom work to be done by the GPU as soon as possible after the deformable volume solver finishes.<br/>\param postSolveCallback Pointer to the callback implementation.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxScene_userData_ref")]
        public static partial void** PxScene_userData_ref(PxScene* @this);

        /// <summary>*<br/>\brief Sets the rebuild rate of the dynamic tree pruning structures.<br/><br/>\param[in] dynamicTreeRebuildRateHint Rebuild rate of the dynamic tree pruning structures.<br/><br/>\see PxSceneQueryDesc.dynamicTreeRebuildRateHint getDynamicTreeRebuildRateHint() forceRebuildDynamicTree()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_setDynamicTreeRebuildRateHint")]
        public static partial void PxSceneQuerySystemBase_setDynamicTreeRebuildRateHint(PxSceneQuerySystemBase* @this, uint dynamicTreeRebuildRateHint);

        /// <summary>*<br/>\brief Retrieves the rebuild rate of the dynamic tree pruning structures.<br/><br/>\return The rebuild rate of the dynamic tree pruning structures.<br/><br/>\see PxSceneQueryDesc.dynamicTreeRebuildRateHint setDynamicTreeRebuildRateHint() forceRebuildDynamicTree()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_getDynamicTreeRebuildRateHint")]
        public static partial uint PxSceneQuerySystemBase_getDynamicTreeRebuildRateHint(PxSceneQuerySystemBase* @this);

        /// <summary>*<br/>\brief Forces dynamic trees to be immediately rebuilt.<br/><br/>\param[in] prunerIndex	Index of pruner containing the dynamic tree to rebuild<br/><br/>\note PxScene will call this function with the PX_SCENE_PRUNER_STATIC or PX_SCENE_PRUNER_DYNAMIC value.<br/><br/>\see PxSceneQueryDesc.dynamicTreeRebuildRateHint setDynamicTreeRebuildRateHint() getDynamicTreeRebuildRateHint()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_forceRebuildDynamicTree")]
        public static partial void PxSceneQuerySystemBase_forceRebuildDynamicTree(PxSceneQuerySystemBase* @this, uint prunerIndex);

        /// <summary>*<br/>\brief Sets scene query update mode<br/><br/>\param[in] updateMode	Scene query update mode.<br/><br/>\see PxSceneQueryUpdateMode::Enum<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_setUpdateMode")]
        public static partial void PxSceneQuerySystemBase_setUpdateMode(PxSceneQuerySystemBase* @this, PxSceneQueryUpdateMode updateMode);

        /// <summary>*<br/>\brief Gets scene query update mode<br/><br/>\return Current scene query update mode.<br/><br/>\see PxSceneQueryUpdateMode::Enum<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_getUpdateMode")]
        public static partial PxSceneQueryUpdateMode PxSceneQuerySystemBase_getUpdateMode(PxSceneQuerySystemBase* @this);

        /// <summary>*<br/>\brief Retrieves the system's internal scene query timestamp, increased each time a change to the<br/>static scene query structure is performed.<br/><br/>\return scene query static timestamp<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_getStaticTimestamp")]
        public static partial uint PxSceneQuerySystemBase_getStaticTimestamp(PxSceneQuerySystemBase* @this);

        /// <summary>*<br/>\brief Flushes any changes to the scene query representation.<br/><br/>This method updates the state of the scene query representation to match changes in the scene state.<br/><br/>By default, these changes are buffered until the next query is submitted. Calling this function will not change<br/>the results from scene queries, but can be used to ensure that a query will not perform update work in the course of<br/>its execution.<br/><br/>A thread performing updates will hold a write lock on the query structure, and thus stall other querying threads. In multithread<br/>scenarios it can be useful to explicitly schedule the period where this lock may be held for a significant period, so that<br/>subsequent queries issued from multiple threads will not block.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_flushUpdates")]
        public static partial void PxSceneQuerySystemBase_flushUpdates(PxSceneQuerySystemBase* @this);

        /// <summary>*<br/>\brief Performs a raycast against objects in the scene, returns results in a PxRaycastBuffer object<br/>or via a custom user callback implementation inheriting from PxRaycastCallback.<br/><br/>\note	Touching hits are not ordered.<br/>\note	Shooting a ray from within an object leads to different results depending on the shape type. Please check the details in user guide article SceneQuery. User can ignore such objects by employing one of the provided filter mechanisms.<br/><br/>\param[in] origin		Origin of the ray.<br/>\param[in] unitDir		Normalized direction of the ray.<br/>\param[in] distance		Length of the ray. Has to be in the [0, inf) range.<br/>\param[out] hitCall		Raycast hit buffer or callback object used to report raycast hits.<br/>\param[in] hitFlags		Specifies which properties per hit should be computed and returned via the hit callback.<br/>\param[in] filterData	Filtering data passed to the filter shader.<br/>\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to be blocking.<br/>\param[in] cache		Cached hit shape (optional). Ray is tested against cached shape first. If no hit is found the ray gets queried against the scene.<br/>Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.<br/>Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.<br/>\param[in] queryFlags	Optional flags controlling the query.<br/><br/>\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.<br/><br/>\see PxRaycastCallback PxRaycastBuffer PxQueryFilterData PxQueryFilterCallback PxQueryCache PxRaycastHit PxQueryFlag PxQueryFlag::eANY_HIT PxGeometryQueryFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_raycast")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxSceneQuerySystemBase_raycast(PxSceneQuerySystemBase* @this, PxVec3* origin, PxVec3* unitDir, float distance, PxRaycastCallback* hitCall, PxHitFlag hitFlags = PxHitFlag.eDEFAULT, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD);

        /// <summary>*<br/>\brief Performs a sweep test against objects in the scene, returns results in a PxSweepBuffer object<br/>or via a custom user callback implementation inheriting from PxSweepCallback.<br/><br/>\note	Touching hits are not ordered.<br/>\note	If a shape from the scene is already overlapping with the query shape in its starting position,<br/>the hit is returned unless eASSUME_NO_INITIAL_OVERLAP was specified.<br/><br/>\param[in] geometry		Geometry of object to sweep (supported types are: box, sphere, capsule, convex core, convex mesh).<br/>\param[in] pose			Pose of the sweep object.<br/>\param[in] unitDir		Normalized direction of the sweep.<br/>\param[in] distance		Sweep distance. Needs to be in [0, inf) range and >0 if eASSUME_NO_INITIAL_OVERLAP was specified. Will be clamped to PX_MAX_SWEEP_DISTANCE.<br/>\param[out] hitCall		Sweep hit buffer or callback object used to report sweep hits.<br/>\param[in] hitFlags		Specifies which properties per hit should be computed and returned via the hit callback.<br/>\param[in] filterData	Filtering data and simple logic.<br/>\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to be blocking.<br/>\param[in] cache		Cached hit shape (optional). Sweep is performed against cached shape first. If no hit is found the sweep gets queried against the scene.<br/>Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.<br/>Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.<br/>\param[in] inflation	This parameter creates a skin around the swept geometry which increases its extents for sweeping. The sweep will register a hit as soon as the skin touches a shape, and will return the corresponding distance and normal.<br/>Note: ePRECISE_SWEEP doesn't support inflation. Therefore the sweep will be performed with zero inflation.<br/>\param[in] queryFlags	Optional flags controlling the query.<br/><br/>\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.<br/><br/>\see PxSweepCallback PxSweepBuffer PxQueryFilterData PxQueryFilterCallback PxSweepHit PxQueryCache PxGeometryQueryFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_sweep")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxSceneQuerySystemBase_sweep(PxSceneQuerySystemBase* @this, PxGeometry* geometry, PxTransform* pose, PxVec3* unitDir, float distance, PxSweepCallback* hitCall, PxHitFlag hitFlags = PxHitFlag.eDEFAULT, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, float inflation = 0f, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD);

        /// <summary>*<br/>\brief Performs an overlap test of a given geometry against objects in the scene, returns results in a PxOverlapBuffer object<br/>or via a custom user callback implementation inheriting from PxOverlapCallback.<br/><br/>\note Filtering: returning eBLOCK from user filter for overlap queries will cause a warning (see #PxQueryHitType).<br/><br/>\param[in] geometry		Geometry of object to check for overlap (supported types are: box, sphere, capsule, convex core, convex mesh).<br/>\param[in] pose			Pose of the object.<br/>\param[out] hitCall		Overlap hit buffer or callback object used to report overlap hits.<br/>\param[in] filterData	Filtering data and simple logic. See #PxQueryFilterData #PxQueryFilterCallback<br/>\param[in] filterCall	Custom filtering logic (optional). Only used if the corresponding #PxQueryFlag flags are set. If NULL, all hits are assumed to overlap.<br/>\param[in] cache		Cached hit shape (optional). Overlap is performed against cached shape first. If no hit is found the overlap gets queried against the scene.<br/>\param[in] queryFlags	Optional flags controlling the query.<br/>Note: Filtering is not executed for a cached shape if supplied; instead, if a hit is found, it is assumed to be a blocking hit.<br/>Note: Using past touching hits as cache will produce incorrect behavior since the cached hit will always be treated as blocking.<br/><br/>\return True if any touching or blocking hits were found or any hit was found in case PxQueryFlag::eANY_HIT was specified.<br/><br/>\note eBLOCK should not be returned from user filters for overlap(). Doing so will result in undefined behavior, and a warning will be issued.<br/>\note If the PxQueryFlag::eNO_BLOCK flag is set, the eBLOCK will instead be automatically converted to an eTOUCH and the warning suppressed.<br/><br/>\see PxOverlapCallback PxOverlapBuffer PxHitFlags PxQueryFilterData PxQueryFilterCallback PxGeometryQueryFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSceneQuerySystemBase_overlap")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxSceneQuerySystemBase_overlap(PxSceneQuerySystemBase* @this, PxGeometry* geometry, PxTransform* pose, PxOverlapCallback* hitCall, PxQueryFilterData* filterData = null, PxQueryFilterCallback* filterCall = null, PxQueryCache* cache = null, PxGeometryQueryFlag queryFlags = PxGeometryQueryFlag.eSIMD_GUARD);

        /// <summary>*<br/>\brief Releases the PxBase instance, please check documentation of release in derived class.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_release")]
        public static partial void PxBase_release(PxBase* @this);

        /// <summary>*<br/>\brief Returns string name of dynamic type.<br/>\return	Class name of most derived type of this object.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_getConcreteTypeName")]
        public static partial byte* PxBase_getConcreteTypeName(PxBase* @this);

        /// <summary>*<br/>\brief	Returns concrete type of object.<br/>\return	PxConcreteType::Enum of serialized object<br/><br/>\see PxConcreteType<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_getConcreteType")]
        public static partial PxType PxBase_getConcreteType(PxBase* @this);

        /// <summary>*<br/>\brief Set PxBaseFlag<br/><br/>\param[in] flag The flag to be set<br/>\param[in] value The flags new value<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_setBaseFlag")]
        public static partial void PxBase_setBaseFlag(PxBase* @this, PxBaseFlag flag, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>\brief Set PxBaseFlags<br/><br/>\param[in] inFlags The flags to be set<br/><br/>\see PxBaseFlags<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_setBaseFlags")]
        public static partial void PxBase_setBaseFlags(PxBase* @this, PxBaseFlag inFlags);

        /// <summary>*<br/>\brief Returns PxBaseFlags<br/><br/>\return	PxBaseFlags<br/><br/>\see PxBaseFlags<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_getBaseFlags")]
        public static partial PxBaseFlag PxBase_getBaseFlags(PxBase* @this);

        /// <summary>*<br/>\brief Whether the object is subordinate.<br/><br/>A class is subordinate, if it can only be instantiated in the context of another class.<br/><br/>\return	Whether the class is subordinate<br/><br/>\see PxSerialization::isSerializable<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxBase_isReleasable")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxBase_isReleasable(PxBase* @this);

        /// <summary>*<br/>\brief Decrements the reference count of the object and releases it if the new reference count is zero.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRefCounted_release")]
        public static partial void PxRefCounted_release(PxRefCounted* @this);

        /// <summary>*<br/>\brief Returns the reference count of the object.<br/><br/>At creation, the reference count of the object is 1. Every other object referencing this object increments the<br/>count by 1.	When the reference count reaches 0, and only then, the object gets destroyed automatically.<br/><br/>\return the current reference count.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRefCounted_getReferenceCount")]
        public static partial uint PxRefCounted_getReferenceCount(PxRefCounted* @this);

        /// <summary>*<br/>\brief Acquires a counted reference to this object.<br/><br/>This method increases the reference count of the object by 1. Decrement the reference count by calling release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRefCounted_acquireReference")]
        public static partial void PxRefCounted_acquireReference(PxRefCounted* @this);

        /// <summary>*<br/>\brief Deletes the actor.<br/><br/>Do not keep a reference to the deleted instance.<br/><br/>If the actor belongs to a #PxAggregate object, it is automatically removed from the aggregate.<br/><br/>\see PxBase.release(), PxAggregate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_release")]
        public static partial void PxActor_release(PxActor* @this);

        /// <summary>*<br/>\brief Retrieves the type of actor.<br/><br/>\return The actor type of the actor.<br/><br/>\see PxActorType<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getType")]
        public static partial PxActorType PxActor_getType(PxActor* @this);

        /// <summary>*<br/>\brief Retrieves the scene which this actor belongs to.<br/><br/>\return Owner Scene. NULL if not part of a scene.<br/><br/>\see PxScene<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getScene")]
        public static partial PxScene* PxActor_getScene(PxActor* @this);

        /// <summary>*<br/>\brief Sets a name string for the object that can be retrieved with getName().<br/><br/>This is for debugging and is not used by the SDK. The string is not copied by the SDK,<br/>only the pointer is stored.<br/><br/>\param[in] name String to set the objects name to.<br/><br/><b>Default:</b> NULL<br/><br/>\see getName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setName")]
        public static partial void PxActor_setName(PxActor* @this, byte* name);

        /// <summary>*<br/>\brief Retrieves the name string set with setName().<br/><br/>\return Name string associated with object.<br/><br/>\see setName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getName")]
        public static partial byte* PxActor_getName(PxActor* @this);

        /// <summary>*<br/>\brief Retrieves the axis aligned bounding box enclosing the actor.<br/><br/>\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),<br/>in PxContactModifyCallback or in contact report callbacks).<br/><br/>\param[in] inflation  Scale factor for computed world bounds. Box extents are multiplied by this value.<br/><br/>\return The actor's bounding box.<br/><br/>\see PxBounds3<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getWorldBounds")]
        public static partial PxBounds3 PxActor_getWorldBounds(PxActor* @this, float inflation = 1f);

        /// <summary>*<br/>\brief Raises or clears a particular actor flag.<br/><br/>See the list of flags #PxActorFlag<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.<br/><br/>\param[in] flag  The PxActor flag to raise(set) or clear. See #PxActorFlag.<br/>\param[in] value The boolean value to assign to the flag.<br/><br/>\see PxActorFlag getActorFlags()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setActorFlag")]
        public static partial void PxActor_setActorFlag(PxActor* @this, PxActorFlag flag, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>\brief Sets the actor flags.<br/><br/>See the list of flags #PxActorFlag<br/>\see PxActorFlag setActorFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setActorFlags")]
        public static partial void PxActor_setActorFlags(PxActor* @this, PxActorFlag inFlags);

        /// <summary>*<br/>\brief Reads the PxActor flags.<br/><br/>See the list of flags #PxActorFlag<br/><br/>\return The values of the PxActor flags.<br/><br/>\see PxActorFlag setActorFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getActorFlags")]
        public static partial PxActorFlag PxActor_getActorFlags(PxActor* @this);

        /// <summary>*<br/>\brief Assigns dynamic actors a dominance group identifier.<br/><br/>PxDominanceGroup is a 5 bit group identifier (legal range from 0 to 31).<br/><br/>The PxScene::setDominanceGroupPair() lets you set certain behaviors for pairs of dominance groups.<br/>By default every dynamic actor is created in group 0.<br/><br/><b>Default:</b> 0<br/><br/><b>Sleeping:</b> Changing the dominance group does <b>NOT</b> wake the actor up automatically.<br/><br/>\param[in] dominanceGroup The dominance group identifier. <b>Range:</b> [0..31]<br/><br/>\see getDominanceGroup() PxDominanceGroup PxScene::setDominanceGroupPair()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setDominanceGroup")]
        public static partial void PxActor_setDominanceGroup(PxActor* @this, PxDominanceGroup dominanceGroup);

        /// <summary>*<br/>\brief Retrieves the value set with setDominanceGroup().<br/><br/>\return The dominance group of this actor.<br/><br/>\see setDominanceGroup() PxDominanceGroup PxScene::setDominanceGroupPair()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getDominanceGroup")]
        public static partial PxDominanceGroup PxActor_getDominanceGroup(PxActor* @this);

        /// <summary>*<br/>\brief Sets the owner client of an actor.<br/><br/>This cannot be done once the actor has been placed into a scene.<br/><br/><b>Default:</b> PX_DEFAULT_CLIENT<br/><br/>\see PxClientID PxScene::createClient()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setOwnerClient")]
        public static partial void PxActor_setOwnerClient(PxActor* @this, PxClientID inClient);

        /// <summary>*<br/>\brief Returns the owner client that was specified at creation time.<br/><br/>This value cannot be changed once the object is placed into the scene.<br/><br/>\see PxClientID PxScene::createClient()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getOwnerClient")]
        public static partial PxClientID PxActor_getOwnerClient(PxActor* @this);

        /// <summary>*<br/>\brief Retrieves the aggregate the actor might be a part of.<br/><br/>\return The aggregate the actor is a part of, or NULL if the actor does not belong to an aggregate.<br/><br/>\see PxAggregate<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getAggregate")]
        public static partial PxAggregate* PxActor_getAggregate(PxActor* @this);

        /// <summary>*<br/>\brief Sets the environment ID for this actor.<br/><br/>The environment ID is an extra built-in filter group for the GPU broadphase. Actors will only collide with each-other if they have the<br/>same environment ID.<br/><br/>The default value is PX_INVALID_U32. Actors with this ID will collide with other actors, regardless of which environment they are a part of.<br/><br/>The environment ID must be set before adding the actor to a scene, and cannot change while the actor is in the scene.<br/><br/>If it is not PX_INVALID_U32, the environment ID must be smaller than 1<<24, i.e. the system does not support more than 1<<24 environments.<br/><br/><b>Default:</b> PX_INVALID_U32<br/><br/>\note	This is not available for CPU broadphases.<br/><br/>\param[in]	envID	 Environment ID for this actor.<br/>\return True if success.<br/><br/>\see getEnvironmentID()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_setEnviormentID")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxActor_setEnviormentID(PxActor* @this, uint envID);

        /// <summary>*<br/>\brief Returns the environment ID for this actor.<br/><br/>\return Environment ID for this actor.<br/><br/>\see setEnvironmentID()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_getEnviormentID")]
        public static partial uint PxActor_getEnviormentID(PxActor* @this);

        /// <summary>*<br/>\brief Returns the environment ID for this actor.<br/><br/>\return Environment ID for this actor.<br/><br/>\see setEnvironmentID()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_userData_set")]
        public static partial void PxActor_userData_set(PxActor* @this, void* userData);

        /// <summary>*<br/>\brief Returns the environment ID for this actor.<br/><br/>\return Environment ID for this actor.<br/><br/>\see setEnvironmentID()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_userData_get")]
        public static partial void* PxActor_userData_get(PxActor* @this);

        /// <summary>*<br/>\brief Returns the environment ID for this actor.<br/><br/>\return Environment ID for this actor.<br/><br/>\see setEnvironmentID()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxActor_userData_ref")]
        public static partial void** PxActor_userData_ref(PxActor* @this);

        /// <summary>*<br/>\brief Deletes the rigid actor object.<br/><br/>Also releases any shapes associated with the actor.<br/><br/>Releasing an actor will affect any objects that are connected to the actor (constraint shaders like joints etc.).<br/>Such connected objects will be deleted upon scene deletion, or explicitly by the user by calling release()<br/>on these objects. It is recommended to always remove all objects that reference actors before the actors<br/>themselves are removed. It is not possible to retrieve list of dead connected objects.<br/><br/><b>Sleeping:</b> This call will awaken any sleeping actors contacting the deleted actor (directly or indirectly).<br/><br/>Calls #PxActor::release() so you might want to check the documentation of that method as well.<br/><br/>\see PxActor::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_release")]
        public static partial void PxRigidActor_release(PxRigidActor* @this);

        /// <summary>*<br/>\brief Returns the internal actor index.<br/><br/>\warning	This is only defined for actors that have been added to a scene.<br/><br/>\return		The internal actor index, or 0xffffffff if the actor is not part of a scene.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getInternalActorIndex")]
        public static partial uint PxRigidActor_getInternalActorIndex(PxRigidActor* @this);

        /// <summary>*<br/>\brief Retrieves the actors world space transform.<br/><br/>The getGlobalPose() method retrieves the actor's current actor space to world space transformation.<br/><br/>\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),<br/>in PxContactModifyCallback or in contact report callbacks).<br/><br/>\note If this actor is a PxRigidDynamic or PxArticulationLink, this method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/>\return Global pose of object.<br/><br/>\see PxRigidDynamic.setGlobalPose() PxRigidStatic.setGlobalPose()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getGlobalPose")]
        public static partial PxTransform PxRigidActor_getGlobalPose(PxRigidActor* @this);

        /// <summary>*<br/>\brief Method for setting an actor's pose in the world.<br/><br/>This method instantaneously changes the actor space to world space transformation.<br/><br/>This method is mainly for dynamic rigid bodies (see #PxRigidDynamic). Calling this method on static actors is<br/>likely to result in a performance penalty, since internal optimization structures for static actors may need to be<br/>recomputed. In addition, moving static actors will not interact correctly with dynamic actors or joints.<br/><br/>To directly control an actor's position and have it correctly interact with dynamic bodies and joints, create a dynamic<br/>body with the PxRigidBodyFlag::eKINEMATIC flag, then use the setKinematicTarget() commands to define its path.<br/><br/>Even when moving dynamic actors, exercise restraint in making use of this method. Where possible, avoid:<br/><br/>\li moving actors into other actors, thus causing overlap (an invalid physical state)<br/><br/>\li moving an actor that is connected by a joint to another away from the other (thus causing joint error)<br/><br/>\note It is not allowed to use this method if the actor is part of a #PxPruningStructure that has not been<br/>added to a scene yet.<br/><br/>\note If this actor is a PxRigidDynamic or PxArticulationLink, this method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/><b>Sleeping:</b> This call wakes dynamic actors if they are sleeping and the autowake parameter is true (default).<br/><br/>\param[in] pose Transformation from the actors local frame to the global frame. <b>Range:</b> rigid body transform.<br/>\param[in] autowake whether to wake the object if it is dynamic. This parameter has no effect for static or kinematic actors. If true and the current wake counter value is smaller than #PxSceneDesc::wakeCounterResetValue it will get increased to the reset value.<br/><br/>\see getGlobalPose()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_setGlobalPose")]
        public static partial void PxRigidActor_setGlobalPose(PxRigidActor* @this, PxTransform* pose, [MarshalAs(UnmanagedType.I1)] bool autowake = true);

        /// <summary>*<br/>\brief Attach a shape to an actor<br/><br/>This call will increment the reference count of the shape.<br/><br/>\note Mass properties of dynamic rigid actors will not automatically be recomputed<br/>to reflect the new mass distribution implied by the shape. Follow this call with a call to<br/>the PhysX extensions method #PxRigidBodyExt::updateMassAndInertia() to do that.<br/><br/>Attaching a triangle mesh, heightfield or plane geometry shape configured as eSIMULATION_SHAPE is not supported for<br/>non-kinematic PxRigidDynamic instances.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.<br/><br/>\param[in] shape	the shape to attach.<br/><br/>\return True if success.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_attachShape")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxRigidActor_attachShape(PxRigidActor* @this, PxShape* shape);

        /// <summary>*<br/>\brief Detach a shape from an actor.<br/><br/>This will also decrement the reference count of the PxShape, and if the reference count is zero, will cause it to be deleted.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the actor up automatically.<br/><br/>\param[in] shape	the shape to detach.<br/>\param[in] wakeOnLostTouch Specifies whether touching objects from the previous frame should get woken up in the next frame. Only applies to PxArticulationReducedCoordinate and PxRigidActor types.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_detachShape")]
        public static partial void PxRigidActor_detachShape(PxRigidActor* @this, PxShape* shape, [MarshalAs(UnmanagedType.I1)] bool wakeOnLostTouch = true);

        /// <summary>*<br/>\brief Returns the number of shapes assigned to the actor.<br/><br/>You can use #getShapes() to retrieve the shape pointers.<br/><br/>\return Number of shapes associated with this actor.<br/><br/>\see PxShape getShapes()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getNbShapes")]
        public static partial uint PxRigidActor_getNbShapes(PxRigidActor* @this);

        /// <summary>*<br/>\brief Retrieve all the shape pointers belonging to the actor.<br/><br/>These are the shapes used by the actor for collision detection.<br/><br/>You can retrieve the number of shape pointers by calling #getNbShapes()<br/><br/>Note: Removing shapes with #PxShape::release() will invalidate the pointer of the released shape.<br/><br/>\param[out] userBuffer The buffer to store the shape pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first shape pointer to be retrieved<br/>\return Number of shape pointers written to the buffer.<br/><br/>\see PxShape getNbShapes() PxShape::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getShapes")]
        public static partial uint PxRigidActor_getShapes(PxRigidActor* @this, PxShape** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Returns the number of constraint shaders attached to the actor.<br/><br/>You can use #getConstraints() to retrieve the constraint shader pointers.<br/><br/>\return Number of constraint shaders attached to this actor.<br/><br/>\see PxConstraint getConstraints()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getNbConstraints")]
        public static partial uint PxRigidActor_getNbConstraints(PxRigidActor* @this);

        /// <summary>*<br/>\brief Retrieve all the constraint shader pointers belonging to the actor.<br/><br/>You can retrieve the number of constraint shader pointers by calling #getNbConstraints()<br/><br/>Note: Removing constraint shaders with #PxConstraint::release() will invalidate the pointer of the released constraint.<br/><br/>\param[out] userBuffer The buffer to store the constraint shader pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first constraint pointer to be retrieved<br/>\return Number of constraint shader pointers written to the buffer.<br/><br/>\see PxConstraint getNbConstraints() PxConstraint::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidActor_getConstraints")]
        public static partial uint PxRigidActor_getConstraints(PxRigidActor* @this, PxConstraint** userBuffer, uint bufferSize, uint startIndex = 0);

        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidStatic_getConcreteTypeName")]
        public static partial byte* PxRigidStatic_getConcreteTypeName(PxRigidStatic* @this);

        /// <summary>*<br/>\brief Moves kinematically controlled dynamic actors through the game world.<br/><br/>You set a dynamic actor to be kinematic using the PxRigidBodyFlag::eKINEMATIC flag<br/>with setRigidBodyFlag().<br/><br/>The move command will result in a velocity that will move the body into<br/>the desired pose. After the move is carried out during a single time step,<br/>the velocity is returned to zero. Thus, you must continuously call<br/>this in every time step for kinematic actors so that they move along a path.<br/><br/>This function simply stores the move destination until the next simulation<br/>step is processed, so consecutive calls will simply overwrite the stored target variable.<br/><br/>The motion is always fully carried out.<br/><br/>\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/><b>Sleeping:</b> This call wakes the actor if it is sleeping and will set the wake counter to #PxSceneDesc::wakeCounterResetValue.<br/><br/>\param[in] destination The desired pose for the kinematic actor, in the global frame. <b>Range:</b> rigid body transform.<br/><br/>\see getKinematicTarget() PxRigidBodyFlag setRigidBodyFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setKinematicTarget")]
        public static partial void PxRigidDynamic_setKinematicTarget(PxRigidDynamic* @this, PxTransform* destination);

        /// <summary>*<br/>\brief Get target pose of a kinematically controlled dynamic actor.<br/><br/>\param[out] target Transform to write the target pose to. Only valid if the method returns true.<br/>\return True if the actor is a kinematically controlled dynamic and the target has been set, else False.<br/><br/>\see setKinematicTarget() PxRigidBodyFlag setRigidBodyFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getKinematicTarget")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxRigidDynamic_getKinematicTarget(PxRigidDynamic* @this, PxTransform* target);

        /// <summary>*<br/>\brief Returns true if this body is sleeping.<br/><br/>When an actor does not move for a period of time, it is no longer simulated in order to save time. This state<br/>is called sleeping. However, because the object automatically wakes up when it is either touched by an awake object,<br/>or one of its properties is changed by the user, the entire sleep mechanism should be transparent to the user.<br/><br/>In general, a dynamic rigid actor is guaranteed to be awake if at least one of the following holds:<br/><br/>\li The wake counter is positive (see #setWakeCounter()).<br/>\li The linear or angular velocity is non-zero.<br/>\li A non-zero force or torque has been applied.<br/><br/>If a dynamic rigid actor is sleeping, the following state is guaranteed:<br/><br/>\li The wake counter is zero.<br/>\li The linear and angular velocity is zero.<br/>\li There is no force update pending.<br/><br/>When an actor gets inserted into a scene, it will be considered asleep if all the points above hold, else it will be treated as awake.<br/><br/>If an actor is asleep after the call to PxScene::fetchResults() returns, it is guaranteed that the pose of the actor<br/>was not changed. You can use this information to avoid updating the transforms of associated objects.<br/><br/>\note A kinematic actor is asleep unless a target pose has been set (in which case it will stay awake until two consecutive<br/>simulation steps without a target pose being set have passed). The wake counter will get set to zero or to the reset value<br/>#PxSceneDesc::wakeCounterResetValue in the case where a target pose has been set to be consistent with the definitions above.<br/><br/>\note It is invalid to use this method if the actor has not been added to a scene already.<br/><br/>\note It is not allowed to use this method while the simulation is running.<br/><br/>\return True if the actor is sleeping.<br/><br/>\see isSleeping() wakeUp() putToSleep()  getSleepThreshold()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_isSleeping")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxRigidDynamic_isSleeping(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the mass-normalized kinetic energy threshold below which an actor may go to sleep.<br/><br/>Actors whose kinetic energy divided by their mass is below this threshold will be candidates for sleeping.<br/><br/><b>Default:</b> 5e-5f * PxTolerancesScale::speed * PxTolerancesScale::speed<br/><br/>\param[in] threshold Energy below which an actor may go to sleep. <b>Range:</b> [0, PX_MAX_F32)<br/><br/>\see isSleeping() getSleepThreshold() wakeUp() putToSleep() PxTolerancesScale<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setSleepThreshold")]
        public static partial void PxRigidDynamic_setSleepThreshold(PxRigidDynamic* @this, float threshold);

        /// <summary>*<br/>\brief Returns the mass-normalized kinetic energy below which an actor may go to sleep.<br/><br/>\return The energy threshold for sleeping.<br/><br/>\see isSleeping() wakeUp() putToSleep() setSleepThreshold()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getSleepThreshold")]
        public static partial float PxRigidDynamic_getSleepThreshold(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the mass-normalized kinetic energy threshold below which an actor may participate in stabilization.<br/><br/>Actors whose kinetic energy divided by their mass is above this threshold will not participate in stabilization.<br/><br/>This value has no effect if PxSceneFlag::eENABLE_STABILIZATION was not enabled on the PxSceneDesc.<br/><br/><b>Default:</b> 1e-5f * PxTolerancesScale::speed * PxTolerancesScale::speed<br/><br/>\param[in] threshold Energy below which an actor may participate in stabilization. <b>Range:</b> [0,inf)<br/><br/>\see  getStabilizationThreshold() PxSceneFlag::eENABLE_STABILIZATION<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setStabilizationThreshold")]
        public static partial void PxRigidDynamic_setStabilizationThreshold(PxRigidDynamic* @this, float threshold);

        /// <summary>*<br/>\brief Returns the mass-normalized kinetic energy below which an actor may participate in stabilization.<br/><br/>Actors whose kinetic energy divided by their mass is above this threshold will not participate in stabilization.<br/><br/>\return The energy threshold for participating in stabilization.<br/><br/>\see setStabilizationThreshold() PxSceneFlag::eENABLE_STABILIZATION<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getStabilizationThreshold")]
        public static partial float PxRigidDynamic_getStabilizationThreshold(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the wake counter for the actor.<br/><br/>The wake counter value determines the minimum amount of time until the body can be put to sleep. Please note<br/>that a body will not be put to sleep if the energy is above the specified threshold (see #setSleepThreshold())<br/>or if other awake bodies are touching it.<br/><br/>\note Passing in a positive value will wake the actor up automatically.<br/><br/>\note It is invalid to use this method for kinematic actors since the wake counter for kinematics is defined<br/>based on whether a target pose has been set (see the comment in #isSleeping()).<br/><br/>\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/><b>Default:</b> 0.4 (which corresponds to 20 frames for a time step of 0.02)<br/><br/>\param[in] wakeCounterValue Wake counter value. <b>Range:</b> [0, PX_MAX_F32)<br/><br/>\see isSleeping() getWakeCounter()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setWakeCounter")]
        public static partial void PxRigidDynamic_setWakeCounter(PxRigidDynamic* @this, float wakeCounterValue);

        /// <summary>*<br/>\brief Returns the wake counter of the actor.<br/><br/>\note It is not allowed to use this method while the simulation is running.<br/><br/>\return The wake counter of the actor.<br/><br/>\see isSleeping() setWakeCounter()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getWakeCounter")]
        public static partial float PxRigidDynamic_getWakeCounter(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Wakes up the actor if it is sleeping.<br/><br/>The actor will get woken up and might cause other touching actors to wake up as well during the next simulation step.<br/><br/>\note This will set the wake counter of the actor to the value specified in #PxSceneDesc::wakeCounterResetValue.<br/><br/>\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/>\note It is invalid to use this method for kinematic actors since the sleep state for kinematics is defined<br/>based on whether a target pose has been set (see the comment in #isSleeping()).<br/><br/>\see isSleeping() putToSleep()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_wakeUp")]
        public static partial void PxRigidDynamic_wakeUp(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Forces the actor to sleep.<br/><br/>The actor will stay asleep during the next simulation step if not touched by another non-sleeping actor.<br/><br/>\note Any applied force will be cleared and the velocity and the wake counter of the actor will be set to 0.<br/><br/>\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/>\note It is invalid to use this method for kinematic actors since the sleep state for kinematics is defined<br/>based on whether a target pose has been set (see the comment in #isSleeping()).<br/><br/>\see isSleeping() wakeUp()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_putToSleep")]
        public static partial void PxRigidDynamic_putToSleep(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Reads the PxRigidDynamic lock flags.<br/><br/>See the list of flags #PxRigidDynamicLockFlag<br/><br/>\return The values of the PxRigidDynamic lock flags.<br/><br/>\see PxRigidDynamicLockFlag setRigidDynamicLockFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getRigidDynamicLockFlags")]
        public static partial PxRigidDynamicLockFlag PxRigidDynamic_getRigidDynamicLockFlags(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Raises or clears a particular PxRigidDynamic lock flag.<br/><br/>See the list of flags #PxRigidDynamicLockFlag<br/><br/><b>Default:</b> no flags are set<br/><br/>\param[in] flag		The PxRigidDynamicLockFlag to raise(set) or clear. See #PxRigidBodyFlag.<br/>\param[in] value	The new boolean value for the flag.<br/><br/>\see PxRigidDynamicLockFlag getRigidDynamicLockFlags()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setRigidDynamicLockFlag")]
        public static partial void PxRigidDynamic_setRigidDynamicLockFlag(PxRigidDynamic* @this, PxRigidDynamicLockFlag flag, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>\brief Set all PxRigidDynamic lock flags.<br/>\see setRigidDynamicLockFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setRigidDynamicLockFlags")]
        public static partial void PxRigidDynamic_setRigidDynamicLockFlags(PxRigidDynamic* @this, PxRigidDynamicLockFlag flags);

        /// <summary>*<br/>\brief Retrieves the actor's center-of-mass linear velocity.<br/><br/>\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),<br/>in PxContactModifyCallback or in contact report callbacks).<br/><br/>\note The linear velocity is reported with respect to the actor's center of mass and not the actor frame origin.<br/><br/>\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/>\return The actor's center-of-mass linear velocity.<br/><br/>\see PxRigidDynamic.setLinearVelocity() getAngularVelocity()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getLinearVelocity")]
        public static partial PxVec3 PxRigidDynamic_getLinearVelocity(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the actor's center-of-mass linear velocity.<br/><br/>Note that if you continuously set the velocity of an actor yourself,<br/>forces such as gravity or friction will not be able to manifest themselves, because forces directly<br/>influence only the velocity/momentum of an actor.<br/><br/><b>Default:</b> (0.0, 0.0, 0.0)<br/><br/><b>Sleeping:</b> This call wakes the actor if it is sleeping, and the autowake parameter is true (default) or the<br/>new velocity is non-zero.<br/><br/>\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/>\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/>\note The linear velocity is applied with respect to the actor's center of mass and not the actor frame origin.<br/><br/>\param[in] linVel New center-of-mass linear velocity of the actor. <b>Range:</b> velocity vector<br/>\param[in] autowake Whether to wake the object up if it is asleep. If true and the current wake counter value is<br/>smaller than #PxSceneDesc::wakeCounterResetValue it will get increased to the reset value.<br/><br/>\see getLinearVelocity() setAngularVelocity()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setLinearVelocity")]
        public static partial void PxRigidDynamic_setLinearVelocity(PxRigidDynamic* @this, PxVec3* linVel, [MarshalAs(UnmanagedType.I1)] bool autowake = true);

        /// <summary>*<br/>\brief Retrieves the angular velocity of the actor.<br/><br/>\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),<br/>in PxContactModifyCallback or in contact report callbacks).<br/><br/>\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/>\return The angular velocity of the actor.<br/><br/>\see PxRigidDynamic.setAngularVelocity() getLinearVelocity()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getAngularVelocity")]
        public static partial PxVec3 PxRigidDynamic_getAngularVelocity(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the angular velocity of the actor.<br/><br/>Note that if you continuously set the angular velocity of an actor yourself,<br/>forces such as friction will not be able to rotate the actor, because forces directly influence only the velocity/momentum.<br/><br/><b>Default:</b> (0.0, 0.0, 0.0)<br/><br/><b>Sleeping:</b> This call wakes the actor if it is sleeping, and the autowake parameter is true (default) or the<br/>new velocity is non-zero.<br/><br/>\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.<br/><br/>\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.<br/><br/>\param[in] angVel New angular velocity of actor. <b>Range:</b> angular velocity vector<br/>\param[in] autowake Whether to wake the object up if it is asleep. If true and the current wake counter value is<br/>smaller than #PxSceneDesc::wakeCounterResetValue it will get increased to the reset value.<br/><br/>\see getAngularVelocity() setLinearVelocity()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setAngularVelocity")]
        public static partial void PxRigidDynamic_setAngularVelocity(PxRigidDynamic* @this, PxVec3* angVel, [MarshalAs(UnmanagedType.I1)] bool autowake = true);

        /// <summary>*<br/>\brief Sets the solver iteration counts for the body.<br/><br/>The solver iteration count determines how accurately joints and contacts are resolved.<br/>If you are having trouble with jointed bodies oscillating and behaving erratically, then<br/>setting a higher position iteration count may improve their stability.<br/><br/>If intersecting bodies are being depenetrated too violently, increase the number of velocity<br/>iterations. More velocity iterations will drive the relative exit velocity of the intersecting<br/>objects closer to the correct value given the restitution.<br/><br/><b>Default:</b> 4 position iterations, 1 velocity iteration<br/><br/>\param[in] minPositionIters Number of position iterations the solver should perform for this body. <b>Range:</b> [1,255]<br/>\param[in] minVelocityIters Number of velocity iterations the solver should perform for this body. <b>Range:</b> [0,255]<br/><br/>\see getSolverIterationCounts()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setSolverIterationCounts")]
        public static partial void PxRigidDynamic_setSolverIterationCounts(PxRigidDynamic* @this, uint minPositionIters, uint minVelocityIters = 1);

        /// <summary>*<br/>\brief Retrieves the solver iteration counts.<br/><br/>\see setSolverIterationCounts()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getSolverIterationCounts")]
        public static partial void PxRigidDynamic_getSolverIterationCounts(PxRigidDynamic* @this, uint* minPositionIters, uint* minVelocityIters);

        /// <summary>*<br/>\brief Retrieves the force threshold for contact reports.<br/><br/>The contact report threshold is a force threshold. If the force between<br/>two actors exceeds this threshold for either of the two actors, a contact report<br/>will be generated according to the contact report threshold flags provided by<br/>the filter shader/callback.<br/>See #PxPairFlag.<br/><br/>The threshold used for a collision between a dynamic actor and the static environment is<br/>the threshold of the dynamic actor, and all contacts with static actors are summed to find<br/>the total normal force.<br/><br/><b>Default:</b> PX_MAX_F32<br/><br/>\return Force threshold for contact reports.<br/><br/>\see setContactReportThreshold PxPairFlag PxSimulationFilterShader PxSimulationFilterCallback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getContactReportThreshold")]
        public static partial float PxRigidDynamic_getContactReportThreshold(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the force threshold for contact reports.<br/><br/>See #getContactReportThreshold().<br/><br/>\param[in] threshold Force threshold for contact reports. <b>Range:</b> [0, PX_MAX_F32)<br/><br/>\see getContactReportThreshold PxPairFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_setContactReportThreshold")]
        public static partial void PxRigidDynamic_setContactReportThreshold(PxRigidDynamic* @this, float threshold);

        /// <summary>*<br/>\brief Returns the GPU rigid dynamic index.<br/><br/>\note This function only returns valid results if GPU dynamics is enabled.<br/><br/>\return The GPU index, or 0xFFFFFFFF if the actor is not inserted into a PxScene.<br/><br/>\see PxDirectGPUAPI::getRigidDynamicData(), PxDirectGPUAPI::setRigidDynamicData().<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getGPUIndex")]
        public static partial PxRigidDynamicGPUIndex PxRigidDynamic_getGPUIndex(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Returns the GPU rigid dynamic index.<br/><br/>\note This function only returns valid results if GPU dynamics is enabled.<br/><br/>\return The GPU index, or 0xFFFFFFFF if the actor is not inserted into a PxScene.<br/><br/>\see PxDirectGPUAPI::getRigidDynamicData(), PxDirectGPUAPI::setRigidDynamicData().<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxRigidDynamic_getConcreteTypeName")]
        public static partial byte* PxRigidDynamic_getConcreteTypeName(PxRigidDynamic* @this);

        /// <summary>*<br/>\brief Sets the coefficient of dynamic friction.<br/><br/>The coefficient of dynamic friction should be in [0, PX_MAX_F32). If set to greater than staticFriction, the effective value of staticFriction will be increased to match.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] coef Coefficient of dynamic friction. <b>Range:</b> [0, PX_MAX_F32)<br/><br/>\see getDynamicFriction()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setDynamicFriction")]
        public static partial void PxMaterial_setDynamicFriction(PxMaterial* @this, float coef);

        /// <summary>*<br/>\brief Retrieves the DynamicFriction value.<br/><br/>\return The coefficient of dynamic friction.<br/><br/>\see setDynamicFriction<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getDynamicFriction")]
        public static partial float PxMaterial_getDynamicFriction(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the coefficient of static friction<br/><br/>The coefficient of static friction should be in the range [0, PX_MAX_F32)<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] coef Coefficient of static friction. <b>Range:</b> [0, PX_MAX_F32)<br/><br/>\see getStaticFriction()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setStaticFriction")]
        public static partial void PxMaterial_setStaticFriction(PxMaterial* @this, float coef);

        /// <summary>*<br/>\brief Retrieves the coefficient of static friction.<br/>\return The coefficient of static friction.<br/><br/>\see setStaticFriction<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getStaticFriction")]
        public static partial float PxMaterial_getStaticFriction(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the coefficient of restitution or the spring stiffness for compliant contact<br/><br/>A coefficient of 0 makes the object bounce as little as possible, higher values up to 1.0 result in more bounce.<br/>If a negative value is provided it is interpreted as stiffness term for an implicit spring<br/>simulated at the contact site, with the spring positional error defined by<br/>the contact separation value. Higher stiffness terms produce stiffer springs that behave more like a rigid contact.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] rest Coefficient of restitution / negative spring stiffness <b>Range:</b> (-INF,1]<br/><br/>\see getRestitution()  setDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setRestitution")]
        public static partial void PxMaterial_setRestitution(PxMaterial* @this, float rest);

        /// <summary>*<br/>\brief Retrieves the coefficient of restitution.<br/><br/>See #setRestitution.<br/><br/>\return The coefficient of restitution.<br/><br/>\see setRestitution()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getRestitution")]
        public static partial float PxMaterial_getRestitution(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the coefficient of damping<br/><br/>This property only affects the simulation if compliant contact mode is enabled, i.e., a negative restitution value is set.<br/>Damping works together with spring stiffness. Spring stiffness corrects positional error while<br/>damping resists relative velocity. Setting a high damping coefficient can produce spongy contacts.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] damping Coefficient of damping. <b>Range:</b> [0,INF)<br/><br/>\see getDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setDamping")]
        public static partial void PxMaterial_setDamping(PxMaterial* @this, float damping);

        /// <summary>*<br/>\brief Retrieves the coefficient of damping.<br/><br/>See #setDamping.<br/><br/>\return The coefficient of damping.<br/><br/>\see setDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getDamping")]
        public static partial float PxMaterial_getDamping(PxMaterial* @this);

        /// <summary>*<br/>\brief Raises or clears a particular material flag.<br/><br/>See the list of flags #PxMaterialFlag<br/><br/><b>Default:</b> No flag raised.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in]	flag	The PxMaterial flag to raise(set) or clear.<br/>\param[in]	b		New state of the flag<br/><br/>\see getFlags() setFlags() PxMaterialFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setFlag")]
        public static partial void PxMaterial_setFlag(PxMaterial* @this, PxMaterialFlag flag, [MarshalAs(UnmanagedType.I1)] bool b);

        /// <summary>*<br/>\brief sets all the material flags.<br/><br/>See the list of flags #PxMaterialFlag<br/><br/><b>Default:</b> No flag raised.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in]	flags	All PxMaterial flags<br/><br/>\see getFlags() setFlag() PxMaterialFlag<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setFlags")]
        public static partial void PxMaterial_setFlags(PxMaterial* @this, PxMaterialFlag flags);

        /// <summary>*<br/>\brief Retrieves the flags. See #PxMaterialFlag.<br/><br/>\return The material flags.<br/><br/>\see PxMaterialFlag setFlags()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getFlags")]
        public static partial PxMaterialFlag PxMaterial_getFlags(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the friction combine mode.<br/><br/>See the enum ::PxCombineMode .<br/><br/><b>Default:</b> PxCombineMode::eAVERAGE<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] combMode Friction combine mode to set for this material. See #PxCombineMode.<br/><br/>\see PxCombineMode getFrictionCombineMode setStaticFriction() setDynamicFriction()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setFrictionCombineMode")]
        public static partial void PxMaterial_setFrictionCombineMode(PxMaterial* @this, PxCombineMode combMode);

        /// <summary>*<br/>\brief Retrieves the friction combine mode.<br/><br/>See #setFrictionCombineMode.<br/><br/>\return The friction combine mode for this material.<br/><br/>\see PxCombineMode setFrictionCombineMode()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getFrictionCombineMode")]
        public static partial PxCombineMode PxMaterial_getFrictionCombineMode(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the restitution combine mode.<br/><br/>See the enum ::PxCombineMode .<br/><br/><b>Default:</b> PxCombineMode::eAVERAGE<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] combMode Restitution combine mode for this material. See #PxCombineMode.<br/><br/>\see PxCombineMode getRestitutionCombineMode() setRestitution()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setRestitutionCombineMode")]
        public static partial void PxMaterial_setRestitutionCombineMode(PxMaterial* @this, PxCombineMode combMode);

        /// <summary>*<br/>\brief Retrieves the restitution combine mode.<br/><br/>See #setRestitutionCombineMode.<br/><br/>\return The coefficient of restitution combine mode for this material.<br/><br/>\see PxCombineMode setRestitutionCombineMode getRestitution()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getRestitutionCombineMode")]
        public static partial PxCombineMode PxMaterial_getRestitutionCombineMode(PxMaterial* @this);

        /// <summary>*<br/>\brief Sets the damping combine mode.<br/><br/>See the enum ::PxCombineMode .<br/><br/><b>Default:</b> PxCombineMode::eAVERAGE<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake any actors which may be affected.<br/><br/>\param[in] combMode Damping combine mode for this material. See #PxCombineMode.<br/><br/>\see PxCombineMode getDampingCombineMode() setDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_setDampingCombineMode")]
        public static partial void PxMaterial_setDampingCombineMode(PxMaterial* @this, PxCombineMode combMode);

        /// <summary>*<br/>\brief Retrieves the damping combine mode.<br/><br/>\return The damping combine mode for this material.<br/><br/>\see PxCombineMode setDampingCombineMode() getDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getDampingCombineMode")]
        public static partial PxCombineMode PxMaterial_getDampingCombineMode(PxMaterial* @this);

        /// <summary>*<br/>\brief Retrieves the damping combine mode.<br/><br/>\return The damping combine mode for this material.<br/><br/>\see PxCombineMode setDampingCombineMode() getDamping()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxMaterial_getConcreteTypeName")]
        public static partial byte* PxMaterial_getConcreteTypeName(PxMaterial* @this);

        /// <summary>*<br/>\brief Adjust the geometry of the shape.<br/><br/>\note The type of the passed in geometry must match the geometry type of the shape.<br/>\note It is not allowed to change the geometry type of a shape.<br/>\note This function does not guarantee correct/continuous behavior when objects are resting on top of old or new geometry.<br/><br/>\param[in] geometry New geometry of the shape.<br/><br/>\see PxGeometry PxGeometryType getGeometryType()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setGeometry")]
        public static partial void PxShape_setGeometry(PxShape* @this, PxGeometry* geometry);

        /// <summary>*<br/>\brief Retrieve a reference to the shape's geometry.<br/><br/>\warning The returned reference has the same lifetime as the PxShape it comes from.<br/><br/>\return	Reference to internal PxGeometry object.<br/><br/>\see PxGeometry PxGeometryType getGeometryType() setGeometry()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getGeometry")]
        public static partial PxGeometryHolder PxShape_getGeometry(PxShape* @this);

        /// <summary>*<br/>\brief Retrieves the actor which this shape is associated with.<br/><br/>\return The actor this shape is associated with, if it is an exclusive shape, else NULL<br/><br/>\see PxRigidStatic, PxRigidDynamic, PxArticulationLink<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getActor")]
        public static partial PxRigidActor* PxShape_getActor(PxShape* @this);

        /// <summary>*<br/>\brief Sets the pose of the shape in actor space, i.e. relative to the actors to which they are attached.<br/><br/>This transformation is identity by default.<br/><br/>The local pose is an attribute of the shape, and so will apply to all actors to which the shape is attached.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/><i>Note:</i> Does not automatically update the inertia properties of the owning actor (if applicable); use the<br/>PhysX extensions method #PxRigidBodyExt::updateMassAndInertia() to do this.<br/><br/><b>Default:</b> the identity transform<br/><br/>\param[in] pose	The new transform from the actor frame to the shape frame. <b>Range:</b> rigid body transform<br/><br/>\see getLocalPose()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setLocalPose")]
        public static partial void PxShape_setLocalPose(PxShape* @this, PxTransform pose);

        /// <summary>*<br/>\brief Retrieves the pose of the shape in actor space, i.e. relative to the actor they are owned by.<br/><br/>This transformation is identity by default.<br/><br/>\return Pose of shape relative to the actor's frame.<br/><br/>\see setLocalPose()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getLocalPose")]
        public static partial PxTransform PxShape_getLocalPose(PxShape* @this);

        /// <summary>*<br/>\brief Sets the user definable collision filter data.<br/><br/><b>Sleeping:</b> Does wake up the actor if the filter data change causes a formerly suppressed<br/>collision pair to be enabled.<br/><br/><b>Default:</b> (0,0,0,0)<br/><br/>\see getSimulationFilterData()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setSimulationFilterData")]
        public static partial void PxShape_setSimulationFilterData(PxShape* @this, PxFilterData data);

        /// <summary>*<br/>\brief Retrieves the shape's collision filter data.<br/><br/>\see setSimulationFilterData()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getSimulationFilterData")]
        public static partial PxFilterData PxShape_getSimulationFilterData(PxShape* @this);

        /// <summary>*<br/>\brief Sets the user definable query filter data.<br/><br/><b>Default:</b> (0,0,0,0)<br/><br/>\see getQueryFilterData()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setQueryFilterData")]
        public static partial void PxShape_setQueryFilterData(PxShape* @this, PxFilterData data);

        /// <summary>*<br/>\brief Retrieves the shape's Query filter data.<br/><br/>\see setQueryFilterData()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getQueryFilterData")]
        public static partial PxFilterData PxShape_getQueryFilterData(PxShape* @this);

        /// <summary>*<br/>\brief Assigns material(s) to the shape. Will remove existing materials from the shape.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] materials List of material pointers to assign to the shape. See #PxMaterial<br/>\param[in] materialCount The number of materials provided.<br/><br/>\see PxPhysics.createMaterial() getMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setMaterials")]
        public static partial void PxShape_setMaterials(PxShape* @this, PxMaterial** materials, ushort materialCount);

        /// <summary>*<br/>\brief Assigns surface deformable material(s) to the shape. Will remove existing materials from the shape.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] materials List of material pointers to assign to the shape. See #PxDeformableSurfaceMaterial<br/>\param[in] materialCount The number of materials provided.<br/><br/>\see PxPhysics.createDeformableSurfaceMaterial() getDeformableSurfaceMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setDeformableSurfaceMaterials")]
        public static partial void PxShape_setDeformableSurfaceMaterials(PxShape* @this, PxDeformableSurfaceMaterial** materials, ushort materialCount);

        /// <summary>*<br/>\brief Assigns deformable volume material(s) to the shape. Will remove existing materials from the shape.<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] materials List of material pointers to assign to the shape. See #PxDeformableVolumeMaterial<br/>\param[in] materialCount The number of materials provided.<br/><br/>\see PxPhysics.createDeformableVolumeMaterial() getDeformableVolumeMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setDeformableVolumeMaterials")]
        public static partial void PxShape_setDeformableVolumeMaterials(PxShape* @this, PxDeformableVolumeMaterial** materials, ushort materialCount);

        /// <summary>*<br/>\brief Returns the number of materials assigned to the shape.<br/><br/>You can use #getMaterials() to retrieve the material pointers.<br/><br/>\return Number of materials associated with this shape.<br/><br/>\see PxMaterial getMaterials()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getNbMaterials")]
        public static partial ushort PxShape_getNbMaterials(PxShape* @this);

        /// <summary>*<br/>\brief Retrieve all the material pointers associated with the shape.<br/><br/>You can retrieve the number of material pointers by calling #getNbMaterials()<br/><br/>Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().<br/><br/>\param[out] userBuffer The buffer to store the material pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first material pointer to be retrieved<br/>\return Number of material pointers written to the buffer.<br/><br/>\see PxMaterial getNbMaterials() PxMaterial::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getMaterials")]
        public static partial uint PxShape_getMaterials(PxShape* @this, PxMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Retrieve all the surface deformable material pointers associated with the shape.<br/><br/>You can retrieve the number of material pointers by calling #getNbMaterials()<br/><br/>Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().<br/><br/>\param[out] userBuffer The buffer to store the material pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first material pointer to be retrieved<br/>\return Number of material pointers written to the buffer.<br/><br/>\see PxDeformableSurfaceMaterial getNbMaterials() PxMaterial::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getDeformableSurfaceMaterials")]
        public static partial uint PxShape_getDeformableSurfaceMaterials(PxShape* @this, PxDeformableSurfaceMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Retrieve all the deformable volume material pointers associated with the shape.<br/><br/>You can retrieve the number of material pointers by calling #getNbMaterials()<br/><br/>Note: The returned data may contain invalid pointers if you release materials using #PxMaterial::release().<br/><br/>\param[out] userBuffer The buffer to store the material pointers.<br/>\param[in] bufferSize Size of provided user buffer.<br/>\param[in] startIndex Index of first material pointer to be retrieved<br/>\return Number of material pointers written to the buffer.<br/><br/>\see PxDeformableVolumeMaterial getNbMaterials() PxMaterial::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getDeformableVolumeMaterials")]
        public static partial uint PxShape_getDeformableVolumeMaterials(PxShape* @this, PxDeformableVolumeMaterial** userBuffer, uint bufferSize, uint startIndex = 0);

        /// <summary>*<br/>\brief Retrieve material from given triangle index.<br/><br/>The input index is the internal triangle index as used inside the SDK. This is the index<br/>returned to users by various SDK functions such as raycasts.<br/><br/>This function is only useful for triangle meshes or heightfields, which have per-triangle<br/>materials. For other shapes or SDF triangle meshes, the function returns the single material<br/>associated with the	shape, regardless of the index.<br/><br/>\param[in] faceIndex The internal triangle index whose material you want to retrieve.<br/>\return Material from input triangle<br/><br/>\note If faceIndex value of 0xFFFFffff is passed as an input for mesh and heightfield shapes, this function will issue a warning and return NULL.<br/>\note Scene queries set the value of PxQueryHit::faceIndex to 0xFFFFffff whenever it is undefined or does not apply.<br/><br/>\see PxMaterial getNbMaterials() PxMaterial::release()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getMaterialFromInternalFaceIndex")]
        public static partial PxBaseMaterial* PxShape_getMaterialFromInternalFaceIndex(PxShape* @this, uint faceIndex);

        /// <summary>*<br/>\brief Sets the contact offset.<br/><br/>Shapes whose distance is less than the sum of their contactOffset values will generate contacts. The contact offset must be positive and<br/>greater than the rest offset. Having a contactOffset greater than than the restOffset allows the collision detection system to<br/>predictively enforce the contact constraint even when the objects are slightly separated. This prevents jitter that would occur<br/>if the constraint were enforced only when shapes were within the rest distance.<br/><br/><b>Default:</b> 0.02f * PxTolerancesScale::length<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] contactOffset <b>Range:</b> [maximum(0,restOffset), PX_MAX_F32)<br/><br/>\see getContactOffset PxTolerancesScale setRestOffset<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setContactOffset")]
        public static partial void PxShape_setContactOffset(PxShape* @this, float contactOffset);

        /// <summary>*<br/>\brief Retrieves the contact offset.<br/><br/>\return The contact offset of the shape.<br/><br/>\see setContactOffset()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getContactOffset")]
        public static partial float PxShape_getContactOffset(PxShape* @this);

        /// <summary>*<br/>\brief Sets the rest offset.<br/><br/>Two shapes will come to rest at a distance equal to the sum of their restOffset values. If the restOffset is 0, they should converge to touching<br/>exactly.  Having a restOffset greater than zero is useful to have objects slide smoothly, so that they do not get hung up on irregularities of<br/>each others' surfaces.<br/><br/><b>Default:</b> 0.0f<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] restOffset	<b>Range:</b> (-PX_MAX_F32, contactOffset)<br/><br/>\see getRestOffset setContactOffset<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setRestOffset")]
        public static partial void PxShape_setRestOffset(PxShape* @this, float restOffset);

        /// <summary>*<br/>\brief Retrieves the rest offset.<br/><br/>\return The rest offset of the shape.<br/><br/>\see setRestOffset()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getRestOffset")]
        public static partial float PxShape_getRestOffset(PxShape* @this);

        /// <summary>*<br/>\brief Sets the density used to interact with fluids.<br/><br/>To be physically accurate, the density of a rigid body should be computed as its mass divided by its volume. To<br/>simplify tuning the interaction of fluid and rigid bodies, the density for fluid can differ from the real density. This<br/>allows to create floating bodies, even if they are supposed to sink with their mass and volume.<br/><br/><b>Default:</b> 800.0f<br/><br/>\param[in] densityForFluid	<b>Range:</b> (0, PX_MAX_F32)<br/><br/>\see getDensityForFluid<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setDensityForFLuid")]
        public static partial void PxShape_setDensityForFLuid(PxShape* @this, float densityForFluid);

        /// <summary>*<br/>\brief Retrieves the density used to interact with fluids.<br/><br/>\return The density of the body when interacting with fluid.<br/><br/>\see setDensityForFluid()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getDensityForFluid")]
        public static partial float PxShape_getDensityForFluid(PxShape* @this);

        /// <summary>*<br/>\brief Sets torsional patch radius.<br/><br/>This defines the radius of the contact patch used to apply torsional friction. If the radius is 0 (and minTorsionalPatchRadius<br/>is 0 too, see #setMinTorsionalPatchRadius), no torsional friction will be applied. If the radius is > 0, some torsional friction<br/>will be applied. This is proportional to the penetration depth so, if the shapes are separated or penetration is zero, no<br/>torsional friction will be applied. It is used to approximate rotational friction introduced by the compression of contacting surfaces.<br/><br/>\note Will only be active, if the friction patch has a single anchor point only. This is for example the case, if a contact patch<br/>has a single contact point.<br/><br/>\note Only supported in combination with solver type PxSolverType::eTGS.<br/><br/><b>Default:</b> 0.0<br/><br/>\param[in] radius	<b>Range:</b> [0, PX_MAX_F32)<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setTorsionalPatchRadius")]
        public static partial void PxShape_setTorsionalPatchRadius(PxShape* @this, float radius);

        /// <summary>*<br/>\brief Gets torsional patch radius.<br/><br/>See #setTorsionalPatchRadius for more info.<br/><br/>\return The torsional patch radius of the shape.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getTorsionalPatchRadius")]
        public static partial float PxShape_getTorsionalPatchRadius(PxShape* @this);

        /// <summary>*<br/>\brief Sets minimum torsional patch radius.<br/><br/>This defines the minimum radius of the contact patch used to apply torsional friction. If the radius is 0, the amount of torsional friction<br/>that will be applied will be entirely dependent on the value of torsionalPatchRadius.<br/><br/>If the radius is > 0, some torsional friction will be applied regardless of the value of torsionalPatchRadius or the amount of penetration.<br/><br/>\note Will only be active in certain cases, see #setTorsionalPatchRadius for details.<br/><br/><b>Default:</b> 0.0<br/><br/>\param[in] radius	<b>Range:</b> [0, PX_MAX_F32)<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setMinTorsionalPatchRadius")]
        public static partial void PxShape_setMinTorsionalPatchRadius(PxShape* @this, float radius);

        /// <summary>*<br/>\brief Gets minimum torsional patch radius.<br/><br/>See #setMinTorsionalPatchRadius for more info.<br/><br/>\return The minimum torsional patch radius of the shape.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getMinTorsionalPatchRadius")]
        public static partial float PxShape_getMinTorsionalPatchRadius(PxShape* @this);

        /// <summary>*<br/>\brief Returns the GPU shape index.<br/><br/>\note This function only returns valid results if GPU dynamics is enabled.<br/><br/>\return The GPU index, or 0xFFFFFFFF if the shape is not attached to a PxActor that is inserted into a PxScene.<br/><br/>\see PxDirectGPUAPI::evaluateSDFDistances().<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getGPUIndex")]
        public static partial PxShapeGPUIndex PxShape_getGPUIndex(PxShape* @this);

        /// <summary>*<br/>\brief Sets shape flags<br/><br/><b>Sleeping:</b> Does <b>NOT</b> wake the associated actor up automatically.<br/><br/>\param[in] flag The shape flag to enable/disable. See #PxShapeFlag.<br/>\param[in] value True to set the flag. False to clear the flag specified in flag.<br/><br/><b>Default:</b> PxShapeFlag::eVISUALIZATION | PxShapeFlag::eSIMULATION_SHAPE | PxShapeFlag::eSCENE_QUERY_SHAPE<br/><br/>\see PxShapeFlag getFlags()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setFlag")]
        public static partial void PxShape_setFlag(PxShape* @this, PxShapeFlag flag, [MarshalAs(UnmanagedType.I1)] bool value);

        /// <summary>*<br/>\brief Sets shape flags<br/><br/>\see PxShapeFlag getFlags()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setFlags")]
        public static partial void PxShape_setFlags(PxShape* @this, PxShapeFlag inFlags);

        /// <summary>*<br/>\brief Retrieves shape flags.<br/><br/>\return The values of the shape flags.<br/><br/>\see PxShapeFlag setFlag()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getFlags")]
        public static partial PxShapeFlag PxShape_getFlags(PxShape* @this);

        /// <summary>*<br/>\brief Returns true if the shape is exclusive to an actor.<br/><br/>\see PxPhysics::createShape()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_isExclusive")]
        [return: MarshalAs(UnmanagedType.I1)]
        public static partial bool PxShape_isExclusive(PxShape* @this);

        /// <summary>*<br/>\brief Sets a name string for the object that can be retrieved with #getName().<br/><br/>This is for debugging and is not used by the SDK.<br/>The string is not copied by the SDK, only the pointer is stored.<br/><br/><b>Default:</b> NULL<br/><br/>\param[in] name The name string to set the objects name to.<br/><br/>\see getName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_setName")]
        public static partial void PxShape_setName(PxShape* @this, byte* name);

        /// <summary>*<br/>\brief retrieves the name string set with setName().<br/>\return The name associated with the shape.<br/><br/>\see setName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getName")]
        public static partial byte* PxShape_getName(PxShape* @this);

        /// <summary>*<br/>\brief retrieves the name string set with setName().<br/>\return The name associated with the shape.<br/><br/>\see setName()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxShape_getConcreteTypeName")]
        public static partial byte* PxShape_getConcreteTypeName(PxShape* @this);

        /// <summary>*<br/>\brief Creates an instance of the foundation class<br/><br/>The foundation class is needed to initialize higher level SDKs. There may be only one instance per process.<br/>Calling this method after an instance has been created already will result in an error message and NULL will be<br/>returned.<br/><br/>\param version Version number we are expecting (should be #PX_PHYSICS_VERSION)<br/>\param allocator User supplied interface for allocating memory(see #PxAllocatorCallback)<br/>\param errorCallback User supplied interface for reporting errors and displaying messages(see #PxErrorCallback)<br/>\return Foundation instance on success, NULL if operation failed<br/><br/>\see PxFoundation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxCreateFoundation")]
        public static partial PxFoundation* PxCreateFoundation(PxAllocatorCallback* allocator, PxErrorCallback* errorCallback);

        /// <summary>*<br/>\brief Creates an instance of the foundation class<br/><br/>The foundation class is needed to initialize higher level SDKs. There may be only one instance per process.<br/>Calling this method after an instance has been created already will result in an error message and NULL will be<br/>returned.<br/><br/>\param version Version number we are expecting (should be #PX_PHYSICS_VERSION)<br/>\param allocator User supplied interface for allocating memory(see #PxAllocatorCallback)<br/>\param errorCallback User supplied interface for reporting errors and displaying messages(see #PxErrorCallback)<br/>\return Foundation instance on success, NULL if operation failed<br/><br/>\see PxFoundation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSetFoundationInstance")]
        public static partial void PxSetFoundationInstance(PxFoundation* foundation);

        /// <summary>*<br/>\brief Retrieves the Foundation SDK after it has been created.<br/><br/>\note The behavior of this method is undefined if the foundation instance has not been created already.<br/><br/>\see PxCreateFoundation(), PxIsFoundationValid()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetFoundation")]
        public static partial PxFoundation* PxGetFoundation();

        /// <summary>*<br/>\brief Similar to PxGetFoundation() except it handles the case if the foundation was not created already.<br/>\return Pointer to the foundation if an instance is currently available, otherwise null.<br/><br/>\see PxCreateFoundation(), PxGetFoundation()<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxIsFoundationValid")]
        public static partial PxFoundation* PxIsFoundationValid();

        /// <summary>*<br/>\brief Get the callback that will be used for all profiling.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetProfilerCallback")]
        public static partial PxProfilerCallback* PxGetProfilerCallback();

        /// <summary>*<br/>\brief Set the callback that will be used for all profiling.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxSetProfilerCallback")]
        public static partial void PxSetProfilerCallback(PxProfilerCallback* profiler);

        /// <summary>*<br/>\brief Get the allocator callback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetAllocatorCallback")]
        public static partial PxAllocatorCallback* PxGetAllocatorCallback();

        /// <summary>*<br/>\brief Get the broadcasting allocator callback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetBroadcastAllocator")]
        public static partial PxAllocatorCallback* PxGetBroadcastAllocator(bool* reportAllocationNames = null);

        /// <summary>*<br/>\brief Get the error callback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetErrorCallback")]
        public static partial PxErrorCallback* PxGetErrorCallback();

        /// <summary>*<br/>\brief Get the broadcasting error callback<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetBroadcastError")]
        public static partial PxErrorCallback* PxGetBroadcastError();

        /// <summary>*<br/>\brief Get the warn once timestamp<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetWarnOnceTimeStamp")]
        public static partial uint PxGetWarnOnceTimeStamp();

        /// <summary>*<br/>\brief Decrement the ref count of PxFoundation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxDecFoundationRefCount")]
        public static partial void PxDecFoundationRefCount();

        /// <summary>*<br/>\brief Increment the ref count of PxFoundation<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxIncFoundationRefCount")]
        public static partial void PxIncFoundationRefCount();

        /// <summary>*<br/>\brief Creates an instance of the physics SDK.<br/><br/>Creates an instance of this class. May not be a class member to avoid name mangling.<br/>Pass the constant #PX_PHYSICS_VERSION as the argument.<br/>There may be only one instance of this class per process. Calling this method after an instance<br/>has been created already will result in an error message and NULL will be returned.<br/><br/>\param version Version number we are expecting (should be #PX_PHYSICS_VERSION)<br/>\param foundation Foundation instance (see PxFoundation)<br/>\param scale values used to determine default tolerances for objects at creation time<br/>\param trackOutstandingAllocations true if you want to track memory allocations<br/>so a debugger connection partway through your physics simulation will get<br/>an accurate map of everything that has been allocated so far. This could have a memory<br/>and performance impact on your simulation hence it defaults to off.<br/>\param pvd When pvd points to a valid PxPvd instance (PhysX Visual Debugger), a connection to the specified PxPvd instance is created.<br/>If pvd is NULL no connection will be attempted.<br/>\param omniPvd When omniPvd points to a valid PxOmniPvd instance PhysX will sample its internal structures to the defined OmniPvd output streams<br/>set in the PxOmniPvd object.<br/>\return PxPhysics instance on success, NULL if operation failed<br/><br/>\see PxPhysics<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxCreatePhysics")]
        public static partial PxPhysics* PxCreatePhysics(PxFoundation* foundation, PxTolerancesScale scale, [MarshalAs(UnmanagedType.I1)] bool trackOutstandingAllocations, PxPvd* pvd);

        /// <summary>*<br/>\brief Retrieves the Physics SDK after it has been created.<br/><br/>Before using this function the user must call #PxCreatePhysics().<br/><br/>\note The behavior of this method is undefined if the Physics SDK instance has not been created already.<br/><br/></summary>
        [LibraryImport("physx_interop_x64", EntryPoint = "CPxGetPhysics")]
        public static partial PxPhysics* PxGetPhysics();

        [LibraryImport("physx_interop_x64", EntryPoint = "CCreateAllocatorCallback")]
        public static partial PxAllocatorCallback* CreateAllocatorCallback(void* pfnAllocate, void* pfnDeallocate);

        [LibraryImport("physx_interop_x64", EntryPoint = "CDestroyAllocatorCallback")]
        public static partial void DestroyAllocatorCallback(PxAllocatorCallback* ptr);

        [LibraryImport("physx_interop_x64", EntryPoint = "CCreateErrorCallback")]
        public static partial PxErrorCallback* CreateErrorCallback(void* pfnErrorCallback);

        [LibraryImport("physx_interop_x64", EntryPoint = "CDestroyErrorCallback")]
        public static partial void DestroyErrorCallback(PxErrorCallback* ptr);
    }
}
