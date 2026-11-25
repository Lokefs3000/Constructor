#pragma once


#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Creates an instance of the physics SDK.
	
	Creates an instance of this class. May not be a class member to avoid name mangling.
	Pass the constant #PX_PHYSICS_VERSION as the argument.
	There may be only one instance of this class per process. Calling this method after an instance
	has been created already will result in an error message and NULL will be returned.
	
	\param version Version number we are expecting (should be #PX_PHYSICS_VERSION)
	\param foundation Foundation instance (see PxFoundation)
	\param scale values used to determine default tolerances for objects at creation time
	\param trackOutstandingAllocations true if you want to track memory allocations
				so a debugger connection partway through your physics simulation will get
				an accurate map of everything that has been allocated so far. This could have a memory
				and performance impact on your simulation hence it defaults to off.
	\param pvd When pvd points to a valid PxPvd instance (PhysX Visual Debugger), a connection to the specified PxPvd instance is created.
				If pvd is NULL no connection will be attempted.
	\param omniPvd When omniPvd points to a valid PxOmniPvd instance PhysX will sample its internal structures to the defined OmniPvd output streams
				set in the PxOmniPvd object.
	\return PxPhysics instance on success, NULL if operation failed
	
	\see PxPhysics
	*/
	INTEROP_API CPxPhysics* CPxCreatePhysics(CPxFoundation* foundation, CPxTolerancesScale scale, bool trackOutstandingAllocations, CPxPvd* pvd);

	/**
	\brief Destroys the instance it is called on.

	Use this release method to destroy an instance of this class. Be sure
	to not keep a reference to this object after calling release.
	Avoid release calls while a scene is simulating (in between simulate() and fetchResults() calls).

	Note that this must be called once for each prior call to PxCreatePhysics, as
	there is a reference counter. Also note that you mustn't destroy the PxFoundation instance (holding the allocator, error callback etc.)
	until after the reference count reaches 0 and the SDK is actually removed.

	Releasing an SDK will also release any objects created through it (scenes, triangle meshes, convex meshes, heightfields, shapes etc.),
	provided the user hasn't already done so.

	\note Releasing the PxPhysics instance is a prerequisite to releasing the PxFoundation instance.

	\see PxCreatePhysics() PxFoundation
	*/
	INTEROP_API void CPxPhysics_release(CPxPhysics* cpp);

	/**
	\brief Creates an aggregate with the specified maximum size and filtering hint.

	The previous API used "bool enableSelfCollision" which should now silently evaluates
	to a PxAggregateType::eGENERIC aggregate with its self-collision bit.

	Use PxAggregateType::eSTATIC or PxAggregateType::eKINEMATIC for aggregates that will
	only contain static or kinematic actors. This provides faster filtering when used in
	combination with PxPairFilteringMode.

	\param	[in] maxActor		The maximum number of actors that may be placed in the aggregate.
	\param	[in] maxShape		The maximum number of shapes that may be placed in the aggregate.
	\param	[in] filterHint		The aggregate's filtering hint.
	\return The new aggregate.

	\see PxAggregate PxAggregateFilterHint PxAggregateType PxPairFilteringMode
	*/
	INTEROP_API CPxAggregate* CPxPhysics_createAggregate(CPxPhysics* cpp, CPxU32 maxActor, CPxU32 maxShape, CPxAggregateFilterHint filterHint);

	/**
	\brief Return the number of aggregates that currently exist.

	\return Number of aggregates.
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbAggregates(CPxPhysics* cpp);

	/**
	\brief Creates a triangle mesh object.

	This can then be instanced into #PxShape objects.

	\param	[in] stream	The triangle mesh stream.
	\return The new triangle mesh.

	\see PxTriangleMesh PxMeshPreprocessingFlag PxTriangleMesh.release() PxInputStream PxTriangleMeshFlag
	*/
	INTEROP_API CPxTriangleMesh* CPxPhysics_createTriangleMesh(CPxPhysics* cpp, CPxInputStream* stream);

	/**
	\brief Return the number of triangle meshes that currently exist.

	\return Number of triangle meshes.

	\see getTriangleMeshes()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbTriangleMeshes(CPxPhysics* cpp);

	/**
	\brief Writes the array of triangle mesh pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the triangle meshes in the array is not specified.

	\param	[out] userBuffer	The buffer to receive triangle mesh pointers.
	\param	[in]  bufferSize	The number of triangle mesh pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first mesh pointer to be retrieved.
	\return The number of triangle mesh pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbTriangleMeshes() PxTriangleMesh
	*/
	INTEROP_API CPxU32 CPxPhysics_getTriangleMeshes(CPxPhysics* cpp, CPxTriangleMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a tetrahedron mesh object.

	This can then be instanced into #PxShape objects.

	\param[in] stream The tetrahedron mesh stream.
	\return The new tetrahedron mesh.

	\see PxTetrahedronMesh PxMeshPreprocessingFlag PxTetrahedronMesh.release() PxInputStream PxTriangleMeshFlag
	*/
	INTEROP_API CPxTetrahedronMesh* CPxPhysics_createTetrahedronMesh(CPxPhysics* cpp, CPxInputStream* stream);

	/**
	\brief Return the number of tetrahedron meshes that currently exist.

	\return Number of tetrahedron meshes.

	\see getTetrahedronMeshes()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbTetrahedronMeshes(CPxPhysics* cpp);

	/**
	\brief Writes the array of tetrahedron mesh pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the tetrahedron meshes in the array is not specified.

	\param[out] userBuffer The buffer to receive tetrahedron mesh pointers.
	\param[in] bufferSize The number of tetrahedron mesh pointers which can be stored in the buffer.
	\param[in] startIndex Index of first mesh pointer to be retrieved.
	\return The number of tetrahedron mesh pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbTetrahedronMeshes() PxTetrahedronMesh
	*/
	INTEROP_API CPxU32 CPxPhysics_getTetrahedronMeshes(CPxPhysics* cpp, CPxTetrahedronMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a heightfield object from previously cooked stream.
	
	This can then be instanced into #PxShape objects.
	
	\param	[in] stream	The heightfield mesh stream.
	\return The new heightfield.
	
	\see PxHeightField PxHeightField.release() PxInputStream
	*/
	INTEROP_API CPxHeightField* CPxPhysics_createHeightField(CPxPhysics* cpp, CPxInputStream* stream);

	/**
	\brief Return the number of heightfields that currently exist.

	\return Number of heightfields.

	\see getHeightFields()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbHeightFields(CPxPhysics* cpp);

	/**
	\brief Writes the array of heightfield pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the heightfields in the array is not specified.

	\param	[out] userBuffer	The buffer to receive heightfield pointers.
	\param	[in]  bufferSize	The number of heightfield pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first heightfield pointer to be retrieved.
	\return The number of heightfield pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbHeightFields() PxHeightField
	*/
	INTEROP_API CPxU32 CPxPhysics_getHeightFields(CPxPhysics* cpp, CPxHeightField** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a convex mesh object.

	This can then be instanced into #PxShape objects.

	\param	[in] stream	The stream to load the convex mesh from.
	\return The new convex mesh.

	\see PxConvexMesh PxConvexMesh.release() PxInputStream createTriangleMesh() PxConvexMeshGeometry PxShape
	*/
	INTEROP_API CPxConvexMesh* CPxPhysics_createConvexMesh(CPxPhysics* cpp, CPxInputStream* stream);
	
	/**
	\brief Return the number of convex meshes that currently exist.

	\return Number of convex meshes.

	\see getConvexMeshes()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbConvexMeshes(CPxPhysics* cpp);

	/**
	\brief Writes the array of convex mesh pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the convex meshes in the array is not specified.

	\param	[out] userBuffer	The buffer to receive convex mesh pointers.
	\param	[in]  bufferSize	The number of convex mesh pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first convex mesh pointer to be retrieved.
	\return The number of convex mesh pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbConvexMeshes() PxConvexMesh
	*/
	INTEROP_API CPxU32 CPxPhysics_getConvexMeshes(CPxPhysics* cpp, CPxConvexMesh** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a deformable volume mesh object.

	\param[in] stream The deformable volume mesh stream.
	\return The new deformable volume mesh.

	\see createTetrahedronMesh
	*/
	INTEROP_API CPxDeformableVolumeMesh* CPxPhysics_createDeformableVolumeMesh(CPxPhysics* cpp, CPxInputStream* stream);

	/**
	\brief Creates a bounding volume hierarchy.

	\param	[in] stream	The stream to load the BVH from.
	\return The new BVH.

	\see PxBVH PxInputStream
	*/
	INTEROP_API CPxBVH* CPxPhysics_createBVH(CPxPhysics* cpp, CPxInputStream* stream);
	
	/**
	\brief Return the number of bounding volume hierarchies that currently exist.

	\return Number of bounding volume hierarchies.

	\see PxBVH getBVHs()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbBVHs(CPxPhysics* cpp);

	/**
	\brief Writes the array of bounding volume hierarchy pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the BVHs in the array is not specified.

	\param	[out] userBuffer	The buffer to receive BVH pointers.
	\param	[in]  bufferSize	The number of BVH pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first BVH pointer to be retrieved.
	\return The number of BVH pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbBVHs() PxBVH
	*/
	INTEROP_API CPxU32 CPxPhysics_getBVHs(CPxPhysics* cpp, CPxBVH** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a scene.

	\note Every scene uses a Thread Local Storage slot. This imposes a platform specific limit on the
	number of scenes that can be created.

	\param	[in] sceneDesc	Scene descriptor. See #PxSceneDesc
	\return The new scene object.

	\see PxScene PxScene.release() PxSceneDesc
	*/
	INTEROP_API CPxScene* CPxPhysics_createScene(CPxPhysics* cpp, CPxSceneDesc sceneDesc);
	
	/**
	\brief Gets number of created scenes.

	\return The number of scenes created.

	\see getScenes()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbScenes(CPxPhysics* cpp);

	/**
	\brief Writes the array of scene pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the scene pointers in the array is not specified.

	\param	[out] userBuffer	The buffer to receive scene pointers.
	\param	[in]  bufferSize	The number of scene pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first scene pointer to be retrieved.
	\return The number of scene pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbScenes() PxScene
	*/
	INTEROP_API CPxU32 CPxPhysics_getScenes(CPxPhysics* cpp, CPxScene** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a static rigid actor with the specified pose and all other fields initialized
	to their default values.

	\param	[in] pose	The initial pose of the actor. Must be a valid transform.

	\see PxRigidStatic
	*/
	INTEROP_API CPxRigidStatic* CPxPhysics_createRigidStatic(CPxPhysics* cpp, CPxTransform pose);
	
	/**
	\brief Creates a dynamic rigid actor with the specified pose and all other fields initialized
	to their default values.

	\param	[in] pose	The initial pose of the actor. Must be a valid transform.

	\see PxRigidDynamic
	*/
	INTEROP_API CPxRigidDynamic* CPxPhysics_createRigidDynamic(CPxPhysics* cpp, CPxTransform pose);
	
	/**
	\brief Creates a pruning structure from actors.

	\note Every provided actor needs at least one shape with the eSCENE_QUERY_SHAPE flag set.
	\note Both static and dynamic actors can be provided.
	\note It is not allowed to pass in actors which are already part of a scene.
	\note Articulation links cannot be provided.

	\param	[in] actors		Array of actors to add to the pruning structure. Must be non NULL.
	\param	[in] nbActors	Number of actors in the array. Must be >0.
	\return Pruning structure created from given actors, or NULL if any of the actors did not comply with the above requirements.
	\see PxActor PxPruningStructure
	*/
	INTEROP_API CPxPruningStructure* CPxPhysics_createPruningStructure(CPxPhysics* cpp, CPxRigidActor** actors, CPxU32 nbActors);
	
	/**
	\brief Creates a shape which may be attached to multiple actors

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] material		The material for the shape
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_material(CPxPhysics* cpp, CPxGeometry* geometry, CPxMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags);
	
	/**
	\brief Creates a shape which may be attached to exactly one deformable volume actor

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] material		The material for the shape
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_deformableVolume(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableVolumeMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags);
	
	/**
	\brief Creates a shape which may be attached to exactly one deformable surface actor

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] material		The material for the shape
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or maybe be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_deformableSurface(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableSurfaceMaterial* material, bool isExclusive, CPxShapeFlag shapeFlags);
	
	/**
	\brief Creates a shape which may be attached to multiple actors

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] materials		The materials for the shape
	\param	[in] materialCount	The number of materials
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor
	\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_materials(CPxPhysics* cpp, CPxGeometry* geometry, CPxMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags);

	/**
	\brief Creates a shape which may be attached to multiple actors

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] materials		The materials for the shape
	\param	[in] materialCount	The number of materials
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor
	\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_deformableVolumes(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableVolumeMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags);
	
	/**
	\brief Creates a shape which may be attached to multiple actors

	The shape will be created with a reference count of 1.

	\param	[in] geometry		The geometry for the shape
	\param	[in] materials		The materials for the shape
	\param	[in] materialCount	The number of materials
	\param	[in] isExclusive	Whether this shape is exclusive to a single actor or may be shared
	\param	[in] shapeFlags		The PxShapeFlags to be set
	\return The shape

	\note Shared shapes are not mutable when they are attached to an actor
	\note Shapes created from *SDF* triangle-mesh geometries do not support more than one material.

	\see PxShape
	*/
	INTEROP_API CPxShape* CPxPhysics_createShape_deformableSurfaces(CPxPhysics* cpp, CPxGeometry* geometry, CPxDeformableSurfaceMaterial** materials, CPxU16 materialCount, bool isExclusive, CPxShapeFlag shapeFlags);
	
	/**
	\brief Return the number of shapes that currently exist.

	\return Number of shapes.

	\see getShapes()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbShapes(CPxPhysics* cpp);

	/**
	\brief Writes the array of shape pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the shapes in the array is not specified.

	\param	[out] userBuffer	The buffer to receive shape pointers.
	\param	[in]  bufferSize	The number of shape pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first shape pointer to be retrieved
	\return The number of shape pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbShapes() PxShape
	*/
	INTEROP_API CPxU32 CPxPhysics_getShapes(CPxPhysics* cpp, CPxShape** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a constraint shader.

	\note A constraint shader will get added automatically to the scene the two linked actors belong to. Either, but not both, of actor0 and actor1 may
	be NULL to denote attachment to the world.

	\param	[in] actor0		The first actor
	\param	[in] actor1		The second actor
	\param	[in] connector	The connector object, which the SDK uses to communicate with the infrastructure for the constraint
	\param	[in] shaders	The shader functions for the constraint
	\param	[in] dataSize	The size of the data block for the shader

	\return The new constraint shader.

	\see PxConstraint
	*/
	INTEROP_API CPxConstraint* CPxPhysics_createConstraint(CPxPhysics* cpp, CPxRigidActor* actor0, CPxRigidActor* actor1, CPxConstraintConnector* connector, CPxConstraintShaderTable shaders, CPxU32 dataSize);
	
	/**
	\brief Return the number of constraints that currently exist.

	\return Number of constraints.
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbConstraints(CPxPhysics* cpp);

	/**
	\brief Writes the constraints that currently exist into a user buffer.

	\param[out] userBuffer The buffer to receive constraint pointers.
	\param[in] bufferSize The number of constraint pointers which can be stored in the buffer.
	\param[in] startIndex Index of first constraint pointer to be retrieved.
	\return Number of constraint pointers written to the buffer.

	\see getNbConstraints() PxConstraint
	*/
	INTEROP_API CPxU32 CPxPhysics_getConstraints(CPxPhysics* cpp, CPxConstraint** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a reduced-coordinate articulation with all fields initialized to their default values.

	\return the new articulation

	\see PxArticulationReducedCoordinate
	*/
	INTEROP_API CPxArticulationReducedCoordinate* CPxPhysics_createArticulationReducedCoordinate(CPxPhysics* cpp);

	/**
	\brief Return the number of articulations that currently exist.

	\return Number of articulations.
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbArticulations(CPxPhysics* cpp);

	/**
	\brief Creates an attachment between two actors, based on the provided PxDeformableAttachmentData. At least one of the actors must be a deformable.

	An attachment is a collection of one or more positional constraints between a point on one actor and a point on another actor.
	Attachments between two rigid objects are not permitted, use joints instead.

	\note The attachment is only active when both actors are added to the same scene or one of the actors is NULL.

	\param[in] data Attachment data.
	\return The PxDeformableAttachment created if successful, NULL otherwise.
	\see PxDeformableAttachmentData, PxDeformableAttachment
	*/
	INTEROP_API CPxDeformableAttachment* CPxPhysics_createDeformableAttachment(CPxPhysics* cpp, CPxDeformableAttachmentData data);
	
	/**
	\brief Creates an element-level collision filter between two actors, based on the provided PxDeformableElementFilterData. At least one of the actors must be a deformable.

	Element filters define how parts of deformable actors are excluded from collisions.
	They are usually added to avoid conflicting attachment and contact constraints.

	\note The element filter is only active when both actors are added to the same scene or one of the actors is NULL.

	\param[in] data Element filter data.
	\return The PxDeformableElementFilter created if successful, NULL otherwise.
	\see PxDeformableElementFilterData, PxDeformableElementFilter
	*/
	INTEROP_API CPxDeformableElementFilter* CPxPhysics_createDeformableElementFilter(CPxPhysics* cpp, CPxDeformableElementFilterData data);
	
	/**
	\brief Creates a deformable surface with all fields initialized to their default values.

	\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.
	\return the new deformable surface

	\see PxDeformableSurface
	*/
	INTEROP_API CPxDeformableSurface* CPxPhysics_createDeformableSurface(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager);
	
	/**
	\brief Creates a FEM-based deformable volume with all fields initialized to their default values.

	\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.
	\return the new deformable volume

	\see PxDeformableVolume
	*/
	INTEROP_API CPxDeformableVolume* CPxPhysics_createDeformableVolume(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager);
	
	/**
	\brief Creates a particle system with a position-based dynamics (PBD) solver.

	A PBD particle system can be used to simulate particle systems with fluid and granular particles. It also allows simulating cloth using
	mass-spring constraints and rigid bodies by shape matching the bodies with particles.

	In order to accelerate neighborhood finding for particle-particle interactions (e.g.: for fluid density constraints) a regular grid is used.
	This grid is built every time step but may provide inaccurate neighborhood information during the solver iterations. The neighborhood scale
	parameter can be used to configure the grid such that it provides a more conservative neighborhood at the cost of run-time performance.
	The grid cell width is defined as 2*particleContactOffset*neighborhoodScale. \see PxParticleSystem::setParticleContactOffset.
	The neighborhood scale parameter should typically not be much larger than 1.

	The maxNeighborhood defines how many particles fit into the neighborhood, at the cost of memory.

	Both maxNeighborhood and neighborhoodScale should be set as low as possible for performance, but high enough to not cause any behavioral degredation.

	\param[in] cudaContextManager The PxCudaContextManager this instance is tied to.
	\param[in] maxNeighborhood The maximum number of particles considered in neighborhood-based particle interaction calculations.
	\param[in] neighborhoodScale Scale applied to the particle contact offset used to define the particle neighborhood size spatially. <b>Range:</b> (1.0, PX_MAX_F32)
	\return the new particle system

	\see PxPBDParticleSystem
	*/
	INTEROP_API CPxPBDParticleSystem* CPxPhysics_createPBDParticleSystem(CPxPhysics* cpp, CPxCudaContextManager* cudaContextManager, CPxU32 maxNeighborhood, CPxReal neighborhoodScale);
	
	/**
	\brief Create particle buffer to simulate fluid/granular material.

	\param[in] maxParticles The maximum number of particles in this buffer.
	\param[in] cudaContextManager The PxCudaContextManager this buffer is tied to.
	\return PxParticleBuffer instance

	\see PxParticleBuffer
	*/
	INTEROP_API CPxParticleBuffer* CPxPhysics_createParticleBuffer(CPxPhysics* cpp, CPxU32 maxParticles, CPxCudaContextManager* cudaContextManager);
	
	/**
	\brief Create a particle buffer for fluid dynamics with diffuse particles. Diffuse particles are used to simulate fluid effects
	such as foam, spray and bubbles.

	\param[in] maxParticles The maximum number of particles in this buffer.
	\param[in] maxDiffuseParticles The max number of diffuse particles in this buffer.
	\param[in] cudaContextManager The PxCudaContextManager this buffer is tied to.
	\return PxParticleAndDiffuseBuffer instance

	\see PxParticleAndDiffuseBuffer, PxDiffuseParticleParams
	*/
	INTEROP_API CPxParticleAndDiffuseBuffer* CPxPhysics_createParticleAndDiffuseBuffer(CPxPhysics* cpp, CPxU32 maxParticles, CPxU32 maxDiffuseParticles, CPxCudaContextManager* cudaContextManager);
	
	/**
	\brief Creates a new rigid body material with certain default properties.

	\return The new rigid body material.

	\param	[in] staticFriction		The coefficient of static friction
	\param	[in] dynamicFriction	The coefficient of dynamic friction
	\param	[in] restitution		The coefficient of restitution (if in range [0,1]) or the spring stiffness for compliant contact (if in range (-PX_MAX_REAL, 0))

	\see PxMaterial
	*/
	INTEROP_API CPxMaterial* CPxPhysics_createMaterial(CPxPhysics* cpp, CPxReal staticFriction, CPxReal dynamicFriction, CPxReal restitution);
	
	/**
	\brief Return the number of rigid body materials that currently exist.

	\return Number of rigid body materials.

	\see getMaterials()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbMaterials(CPxPhysics* cpp);

	/**
	\brief Writes the array of rigid body material pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the materials in the array is not specified.

	\param	[out] userBuffer	The buffer to receive material pointers.
	\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first material pointer to be retrieved.
	\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbMaterials() PxMaterial
	*/
	INTEROP_API CPxU32 CPxPhysics_getMaterials(CPxPhysics* cpp, CPxMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a new surface deformable material with certain default properties.

	\return The new surface deformable material.

	\param	[in] youngs					The young's modulus
	\param	[in] poissons				The poissons's ratio
	\param	[in] dynamicFriction		The dynamic friction coefficient
	\param	[in] thickness				The thickness of the surface
	\param	[in] bendingStiffness		The bending stiffness of the surface
	\param	[in] elasticityDamping		The damping of the surface with respect to stretch and shear
	\param	[in] bendingDamping			The damping of the surface with respect to bending

	\see PxDeformableSurfaceMaterial
	*/
	INTEROP_API CPxDeformableSurfaceMaterial* CPxPhysics_createDeformableSurfaceMaterial(CPxPhysics* cpp, CPxReal youngs, CPxReal poissons, CPxReal dynamicFriction, CPxReal thickness, CPxReal bendingStiffness, CPxReal elasticityDamping, CPxReal bendingDamping);
	
	/**
	\brief Return the number of deformable surface materials that currently exist.

	\return Number of deformable surface materials.

	\see getDeformableSurfaceMaterials()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbDeformableSurfaceMaterials(CPxPhysics* cpp);

	/**
	\brief Writes the array of deformable surface material pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the materials in the array is not specified.

	\param	[out] userBuffer	The buffer to receive material pointers.
	\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first material pointer to be retrieved.
	\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbDeformableSurfaceMaterials() PxDeformableSurfaceMaterial
	*/
	INTEROP_API CPxU32 CPxPhysics_getDeformableSurfaceMaterials(CPxPhysics* cpp, CPxDeformableSurfaceMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a new deformable volume material with certain default properties.

	\return The new deformable volume material.

	\param	[in] youngs					The young's modulus
	\param	[in] poissons				The poissons's ratio
	\param	[in] dynamicFriction		The dynamic friction coefficient
	\param	[in] elasticityDamping		The elasticity damping

	\see PxDeformableVolumeMaterial
	*/
	INTEROP_API CPxDeformableVolumeMaterial* CPxPhysics_createDeformableVolumeMaterial(CPxPhysics* cpp, CPxReal youngs, CPxReal poissons, CPxReal dynamicFriction, CPxReal elasticityDamping);
	
	/**
	\brief Return the number of deformable volume materials that currently exist.

	\return Number of materials.

	\see getDeformableVolumeMaterials()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbDeformableVolumeMaterials(CPxPhysics* cpp);

	/**
	\brief Writes the array of deformable volume material pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the materials in the array is not specified.

	\param	[out] userBuffer	The buffer to receive material pointers.
	\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first material pointer to be retrieved.
	\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbDeformableVolumeMaterials() PxDeformableVolumeMaterial
	*/
	INTEROP_API CPxU32 CPxPhysics_getDeformableVolumeMaterials(CPxPhysics* cpp, CPxDeformableVolumeMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Creates a new PBD material with certain default properties.

	\param	[in]  friction				The friction parameter
	\param	[in]  damping				The velocity damping parameter
	\param	[in]  adhesion				The adhesion parameter
	\param	[in]  viscosity				The viscosity parameter
	\param	[in]  vorticityConfinement	The vorticity confinement coefficient
	\param	[in]  surfaceTension		The surface tension coefficient
	\param	[in]  cohesion				The cohesion parameter
	\param	[in]  lift					The lift parameter
	\param	[in]  drag					The drag parameter
	\param	[in]  cflCoefficient		The Courant-Friedrichs-Lewy(cfl) coefficient
	\param	[in]  gravityScale			The gravity scale
	\return The new PBD material.

	\see PxPBDMaterial
	*/
	INTEROP_API CPxPBDMaterial* CPxPhysics_createPDBMaterial(CPxPhysics* cpp, CPxReal friction, CPxReal damping, CPxReal adhesion, CPxReal viscosity, CPxReal vorticityConfinement, CPxReal surfaceTension, CPxReal cohesion, CPxReal lift, CPxReal drag, CPxReal cflCoefficient, CPxReal gravityScale);
	
	/**
	\brief Return the number of PBD materials that currently exist.

	\return Number of PBD materials.

	\see getPBDMaterials()
	*/
	INTEROP_API CPxU32 CPxPhysics_getNbPBDMaterials(CPxPhysics* cpp);

	/**
	\brief Writes the array of PBD material pointers to a user buffer.

	Returns the number of pointers written.

	The ordering of the materials in the array is not specified.

	\param	[out] userBuffer	The buffer to receive material pointers.
	\param	[in]  bufferSize	The number of material pointers which can be stored in the buffer.
	\param	[in]  startIndex	Index of first material pointer to be retrieved.
	\return The number of material pointers written to userBuffer, this should be less or equal to bufferSize.

	\see getNbPBDMaterials() PxPBDMaterial
	*/
	INTEROP_API CPxU32 CPxPhysics_getPBDMaterials(CPxPhysics* cpp, CPxPBDMaterial** userBuffer, CPxU32 bufferSize, CPxU32 startIndex = 0);

	/**
	\brief Register a deletion listener. Listeners will be called whenever an object is deleted.

	It is illegal to register or unregister a deletion listener while deletions are being processed.

	\note By default a registered listener will receive events from all objects. Set the restrictedObjectSet parameter to true on registration and use #registerDeletionListenerObjects to restrict the received events to specific objects.

	\note The deletion events are only supported on core PhysX objects. In general, objects in extension modules do not provide this functionality, however, in the case of PxJoint objects, the underlying PxConstraint will send the events.

	\param	[in] observer				Observer object to send notifications to.
	\param	[in] deletionEvents			The deletion event types to get notified of.
	\param	[in] restrictedObjectSet	If false, the deletion listener will get events from all objects, else the objects to receive events from have to be specified explicitly through #registerDeletionListenerObjects.

	\see PxDeletionListener unregisterDeletionListener
	*/
	INTEROP_API void CPxPhysics_registerDeletionListener(CPxPhysics* cpp, CPxDeletionListener* observer, CPxDeletionEventFlag deletionEvents, bool restrictedObjectSet);
	
	/**
	\brief Unregister a deletion listener.

	It is illegal to register or unregister a deletion listener while deletions are being processed.

	\param	[in] observer	Observer object to stop sending notifications to.

	\see PxDeletionListener registerDeletionListener
	*/
	INTEROP_API void CPxPhysics_unregisterDeletionListener(CPxPhysics* cpp, CPxDeletionListener* observer);

	/**
	\brief Register specific objects for deletion events.

	This method allows for a deletion listener to limit deletion events to specific objects only.

	\note It is illegal to register or unregister objects while deletions are being processed.

	\note The deletion listener has to be registered through #registerDeletionListener() and configured to support restricted object sets prior to this method being used.

	\param	[in] observer			Observer object to send notifications to.
	\param	[in] observables		List of objects for which to receive deletion events. Only PhysX core objects are supported. In the case of PxJoint objects, the underlying PxConstraint can be used to get the events.
	\param	[in] observableCount	Size of the observables list.

	\see PxDeletionListener unregisterDeletionListenerObjects
	*/
	INTEROP_API void CPxPhysics_registerDeletionListenerObjects(CPxPhysics* cpp, CPxDeletionListener* observer, CPxBase** observables, CPxU32 observableCount);

	/**
	\brief Unregister specific objects for deletion events.

	This method allows to clear previously registered objects for a deletion listener (see #registerDeletionListenerObjects()).

	\note It is illegal to register or unregister objects while deletions are being processed.

	\note The deletion listener has to be registered through #registerDeletionListener() and configured to support restricted object sets prior to this method being used.

	\param	[in] observer			Observer object to stop sending notifications to.
	\param	[in] observables		List of objects for which to not receive deletion events anymore.
	\param	[in] observableCount	Size of the observables list.

	\see PxDeletionListener registerDeletionListenerObjects
	*/
	INTEROP_API void CPxPhysics_unregisterDeletionListenerObjects(CPxPhysics* cpp, CPxDeletionListener* observer, CPxBase** observables, CPxU32 observableCount);

	/**
	\brief Retrieves the Physics SDK after it has been created.
	
	Before using this function the user must call #PxCreatePhysics().
	
	\note The behavior of this method is undefined if the Physics SDK instance has not been created already.
	*/
	INTEROP_API CPxPhysics* CPxGetPhysics();
}