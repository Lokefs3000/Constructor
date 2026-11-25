#include "pch.h"
#include "bridge_types.hpp"

#include <PxPhysicsAPI.h>

#include "shared.hpp"

ASSERT_ENUM(physx::PxPairFilteringMode::eKEEP, CPxPairFilteringMode::eKEEP);
ASSERT_ENUM(physx::PxPairFilteringMode::eSUPPRESS, CPxPairFilteringMode::eSUPPRESS);
ASSERT_ENUM(physx::PxPairFilteringMode::eKILL, CPxPairFilteringMode::eKILL);
ASSERT_ENUM(physx::PxPairFilteringMode::eDEFAULT, CPxPairFilteringMode::eDEFAULT);

ASSERT_ENUM(physx::PxBroadPhaseType::eSAP, CPxBroadPhaseType::eSAP);
ASSERT_ENUM(physx::PxBroadPhaseType::eMBP, CPxBroadPhaseType::eMBP);
ASSERT_ENUM(physx::PxBroadPhaseType::eABP, CPxBroadPhaseType::eABP);
ASSERT_ENUM(physx::PxBroadPhaseType::ePABP, CPxBroadPhaseType::ePABP);
ASSERT_ENUM(physx::PxBroadPhaseType::eGPU, CPxBroadPhaseType::eGPU);
ASSERT_ENUM(physx::PxBroadPhaseType::eLAST, CPxBroadPhaseType::eLAST);

ASSERT_ENUM(physx::PxSolverType::ePGS, CPxSolverType::ePGS);
ASSERT_ENUM(physx::PxSolverType::eTGS, CPxSolverType::eTGS);

ASSERT_ENUM(physx::PxSceneFlag::eENABLE_ACTIVE_ACTORS, CPxSceneFlag::eENABLE_ACTIVE_ACTORS);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_CCD, CPxSceneFlag::eENABLE_CCD);
ASSERT_ENUM(physx::PxSceneFlag::eDISABLE_CCD_RESWEEP, CPxSceneFlag::eDISABLE_CCD_RESWEEP);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_PCM, CPxSceneFlag::eENABLE_PCM);
ASSERT_ENUM(physx::PxSceneFlag::eDISABLE_CONTACT_REPORT_BUFFER_RESIZE, CPxSceneFlag::eDISABLE_CONTACT_REPORT_BUFFER_RESIZE);
ASSERT_ENUM(physx::PxSceneFlag::eDISABLE_CONTACT_CACHE, CPxSceneFlag::eDISABLE_CONTACT_CACHE);
ASSERT_ENUM(physx::PxSceneFlag::eREQUIRE_RW_LOCK, CPxSceneFlag::eREQUIRE_RW_LOCK);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_STABILIZATION, CPxSceneFlag::eENABLE_STABILIZATION);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_AVERAGE_POINT, CPxSceneFlag::eENABLE_AVERAGE_POINT);
ASSERT_ENUM(physx::PxSceneFlag::eEXCLUDE_KINEMATICS_FROM_ACTIVE_ACTORS, CPxSceneFlag::eEXCLUDE_KINEMATICS_FROM_ACTIVE_ACTORS);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_GPU_DYNAMICS, CPxSceneFlag::eENABLE_GPU_DYNAMICS);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_ENHANCED_DETERMINISM, CPxSceneFlag::eENABLE_ENHANCED_DETERMINISM);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_FRICTION_EVERY_ITERATION, CPxSceneFlag::eENABLE_FRICTION_EVERY_ITERATION);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_EXTERNAL_FORCES_EVERY_ITERATION_TGS, CPxSceneFlag::eENABLE_EXTERNAL_FORCES_EVERY_ITERATION_TGS);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_DIRECT_GPU_API, CPxSceneFlag::eENABLE_DIRECT_GPU_API);
ASSERT_ENUM(physx::PxSceneFlag::eENABLE_BODY_ACCELERATIONS, CPxSceneFlag::eENABLE_BODY_ACCELERATIONS);
ASSERT_ENUM(physx::PxSceneFlag::eSOLVE_ARTICULATION_CONTACT_LAST, CPxSceneFlag::eSOLVE_ARTICULATION_CONTACT_LAST);
ASSERT_ENUM(physx::PxSceneFlag::eDISABLE_SLEEPING, CPxSceneFlag::eDISABLE_SLEEPING);
ASSERT_ENUM(physx::PxSceneFlag::eMUTABLE_FLAGS, CPxSceneFlag::eMUTABLE_FLAGS);

ASSERT_ENUM(physx::PxPruningStructureType::eNONE, CPxPruningStructureType::eNONE);
ASSERT_ENUM(physx::PxPruningStructureType::eDYNAMIC_AABB_TREE, CPxPruningStructureType::eDYNAMIC_AABB_TREE);
ASSERT_ENUM(physx::PxPruningStructureType::eSTATIC_AABB_TREE, CPxPruningStructureType::eSTATIC_AABB_TREE);
ASSERT_ENUM(physx::PxPruningStructureType::eLAST, CPxPruningStructureType::eLAST);

ASSERT_ENUM(physx::PxBVHBuildStrategy::eFAST, CPxBVHBuildStrategy::eFAST);
ASSERT_ENUM(physx::PxBVHBuildStrategy::eDEFAULT, CPxBVHBuildStrategy::eDEFAULT);
ASSERT_ENUM(physx::PxBVHBuildStrategy::eSAH, CPxBVHBuildStrategy::eSAH);
ASSERT_ENUM(physx::PxBVHBuildStrategy::eLAST, CPxBVHBuildStrategy::eLAST);

ASSERT_ENUM(physx::PxSceneQueryUpdateMode::eBUILD_ENABLED_COMMIT_ENABLED, CPxSceneQueryUpdateMode::eBUILD_ENABLED_COMMIT_ENABLED);
ASSERT_ENUM(physx::PxSceneQueryUpdateMode::eBUILD_ENABLED_COMMIT_DISABLED, CPxSceneQueryUpdateMode::eBUILD_ENABLED_COMMIT_DISABLED);
ASSERT_ENUM(physx::PxSceneQueryUpdateMode::eBUILD_DISABLED_COMMIT_DISABLED, CPxSceneQueryUpdateMode::eBUILD_DISABLED_COMMIT_DISABLED);

ASSERT_ENUM(physx::PxDynamicTreeSecondaryPruner::eNONE, CPxDynamicTreeSecondaryPruner::eNONE);
ASSERT_ENUM(physx::PxDynamicTreeSecondaryPruner::eBUCKET, CPxDynamicTreeSecondaryPruner::eBUCKET);
ASSERT_ENUM(physx::PxDynamicTreeSecondaryPruner::eINCREMENTAL, CPxDynamicTreeSecondaryPruner::eINCREMENTAL);
ASSERT_ENUM(physx::PxDynamicTreeSecondaryPruner::eBVH, CPxDynamicTreeSecondaryPruner::eBVH);
ASSERT_ENUM(physx::PxDynamicTreeSecondaryPruner::eLAST, CPxDynamicTreeSecondaryPruner::eLAST);

ASSERT_ENUM(physx::PxShapeFlag::eSIMULATION_SHAPE, CPxShapeFlag::eSIMULATION_SHAPE);
ASSERT_ENUM(physx::PxShapeFlag::eSCENE_QUERY_SHAPE, CPxShapeFlag::eSCENE_QUERY_SHAPE);
ASSERT_ENUM(physx::PxShapeFlag::eTRIGGER_SHAPE, CPxShapeFlag::eTRIGGER_SHAPE);
ASSERT_ENUM(physx::PxShapeFlag::eVISUALIZATION, CPxShapeFlag::eVISUALIZATION);

ASSERT_ENUM(physx::PxGeometryType::eSPHERE, CPxGeometryType::eSPHERE);
ASSERT_ENUM(physx::PxGeometryType::ePLANE, CPxGeometryType::ePLANE);
ASSERT_ENUM(physx::PxGeometryType::eCAPSULE, CPxGeometryType::eCAPSULE);
ASSERT_ENUM(physx::PxGeometryType::eBOX, CPxGeometryType::eBOX);
ASSERT_ENUM(physx::PxGeometryType::eCONVEXCORE, CPxGeometryType::eCONVEXCORE);
ASSERT_ENUM(physx::PxGeometryType::eCONVEXMESH, CPxGeometryType::eCONVEXMESH);
ASSERT_ENUM(physx::PxGeometryType::ePARTICLESYSTEM, CPxGeometryType::ePARTICLESYSTEM);
ASSERT_ENUM(physx::PxGeometryType::eTETRAHEDRONMESH, CPxGeometryType::eTETRAHEDRONMESH);
ASSERT_ENUM(physx::PxGeometryType::eTRIANGLEMESH, CPxGeometryType::eTRIANGLEMESH);
ASSERT_ENUM(physx::PxGeometryType::eCUSTOM, CPxGeometryType::eCUSTOM);
ASSERT_ENUM(physx::PxGeometryType::eGEOMETRY_COUNT, CPxGeometryType::eGEOMETRY_COUNT);

ASSERT_ENUM(physx::PxConvexMeshGeometryFlag::eTIGHT_BOUNDS, CPxConvexMeshGeometryFlag::eTIGHT_BOUNDS);

ASSERT_ENUM(physx::PxMeshGeometryFlag::eTIGHT_BOUNDS, CPxMeshGeometryFlag::eTIGHT_BOUNDS);
ASSERT_ENUM(physx::PxMeshGeometryFlag::eDOUBLE_SIDED, CPxMeshGeometryFlag::eDOUBLE_SIDED);

ASSERT_ENUM(physx::PxConstraintFlag::eBROKEN, CPxConstraintFlag::eBROKEN);
ASSERT_ENUM(physx::PxConstraintFlag::eCOLLISION_ENABLED, CPxConstraintFlag::eCOLLISION_ENABLED);
ASSERT_ENUM(physx::PxConstraintFlag::eVISUALIZATION, CPxConstraintFlag::eVISUALIZATION);
ASSERT_ENUM(physx::PxConstraintFlag::eDRIVE_LIMITS_ARE_FORCES, CPxConstraintFlag::eDRIVE_LIMITS_ARE_FORCES);
ASSERT_ENUM(physx::PxConstraintFlag::eIMPROVED_SLERP, CPxConstraintFlag::eIMPROVED_SLERP);
ASSERT_ENUM(physx::PxConstraintFlag::eDISABLE_PREPROCESSING, CPxConstraintFlag::eDISABLE_PREPROCESSING);
ASSERT_ENUM(physx::PxConstraintFlag::eENABLE_EXTENDED_LIMITS, CPxConstraintFlag::eENABLE_EXTENDED_LIMITS);
ASSERT_ENUM(physx::PxConstraintFlag::eGPU_COMPATIBLE, CPxConstraintFlag::eGPU_COMPATIBLE);
ASSERT_ENUM(physx::PxConstraintFlag::eALWAYS_UPDATE, CPxConstraintFlag::eALWAYS_UPDATE);
ASSERT_ENUM(physx::PxConstraintFlag::eDISABLE_CONSTRAINT, CPxConstraintFlag::eDISABLE_CONSTRAINT);

ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eVERTEX, CPxDeformableAttachmentTargetType::eVERTEX);
ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eTRIANGLE, CPxDeformableAttachmentTargetType::eTRIANGLE);
ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eTETRAHEDRON, CPxDeformableAttachmentTargetType::eTETRAHEDRON);
ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eRIGID, CPxDeformableAttachmentTargetType::eRIGID);
ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eWORLD, CPxDeformableAttachmentTargetType::eWORLD);
ASSERT_ENUM(physx::PxDeformableAttachmentTargetType::eUNDEFINED, CPxDeformableAttachmentTargetType::eUNDEFINED);

ASSERT_ENUM(physx::PxDeletionEventFlag::eUSER_RELEASE, CPxDeletionEventFlag::eUSER_RELEASE);
ASSERT_ENUM(physx::PxDeletionEventFlag::eMEMORY_RELEASE, CPxDeletionEventFlag::eMEMORY_RELEASE);

ASSERT_ENUM(physx::PxActorTypeFlag::eRIGID_STATIC, CPxActorTypeFlag::eRIGID_STATIC);
ASSERT_ENUM(physx::PxActorTypeFlag::eRIGID_DYNAMIC, CPxActorTypeFlag::eRIGID_DYNAMIC);

ASSERT_ENUM(physx::PxContactPairHeaderFlag::eREMOVED_ACTOR_0, CPxContactPairHeaderFlag::eREMOVED_ACTOR_0);
ASSERT_ENUM(physx::PxContactPairHeaderFlag::eREMOVED_ACTOR_1, CPxContactPairHeaderFlag::eREMOVED_ACTOR_1);

ASSERT_ENUM(physx::PxContactPairFlag::eREMOVED_SHAPE_0, CPxContactPairFlag::eREMOVED_SHAPE_0);
ASSERT_ENUM(physx::PxContactPairFlag::eREMOVED_SHAPE_1, CPxContactPairFlag::eREMOVED_SHAPE_1);
ASSERT_ENUM(physx::PxContactPairFlag::eACTOR_PAIR_HAS_FIRST_TOUCH, CPxContactPairFlag::eACTOR_PAIR_HAS_FIRST_TOUCH);
ASSERT_ENUM(physx::PxContactPairFlag::eACTOR_PAIR_LOST_TOUCH, CPxContactPairFlag::eACTOR_PAIR_LOST_TOUCH);
ASSERT_ENUM(physx::PxContactPairFlag::eINTERNAL_HAS_IMPULSES, CPxContactPairFlag::eINTERNAL_HAS_IMPULSES);
ASSERT_ENUM(physx::PxContactPairFlag::eINTERNAL_CONTACTS_ARE_FLIPPED, CPxContactPairFlag::eINTERNAL_CONTACTS_ARE_FLIPPED);

ASSERT_ENUM(physx::PxPairFlag::eSOLVE_CONTACT, CPxPairFlag::eSOLVE_CONTACT);
ASSERT_ENUM(physx::PxPairFlag::eMODIFY_CONTACTS, CPxPairFlag::eMODIFY_CONTACTS);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_TOUCH_FOUND, CPxPairFlag::eNOTIFY_TOUCH_FOUND);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_TOUCH_PERSISTS, CPxPairFlag::eNOTIFY_TOUCH_PERSISTS);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_TOUCH_LOST, CPxPairFlag::eNOTIFY_TOUCH_LOST);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_TOUCH_CCD, CPxPairFlag::eNOTIFY_TOUCH_CCD);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_THRESHOLD_FORCE_FOUND, CPxPairFlag::eNOTIFY_THRESHOLD_FORCE_FOUND);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_THRESHOLD_FORCE_PERSISTS, CPxPairFlag::eNOTIFY_THRESHOLD_FORCE_PERSISTS);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_THRESHOLD_FORCE_LOST, CPxPairFlag::eNOTIFY_THRESHOLD_FORCE_LOST);
ASSERT_ENUM(physx::PxPairFlag::eNOTIFY_CONTACT_POINTS, CPxPairFlag::eNOTIFY_CONTACT_POINTS);
ASSERT_ENUM(physx::PxPairFlag::eDETECT_DISCRETE_CONTACT, CPxPairFlag::eDETECT_DISCRETE_CONTACT);
ASSERT_ENUM(physx::PxPairFlag::eDETECT_CCD_CONTACT, CPxPairFlag::eDETECT_CCD_CONTACT);
ASSERT_ENUM(physx::PxPairFlag::ePRE_SOLVER_VELOCITY, CPxPairFlag::ePRE_SOLVER_VELOCITY);
ASSERT_ENUM(physx::PxPairFlag::ePOST_SOLVER_VELOCITY, CPxPairFlag::ePOST_SOLVER_VELOCITY);
ASSERT_ENUM(physx::PxPairFlag::eCONTACT_EVENT_POSE, CPxPairFlag::eCONTACT_EVENT_POSE);
ASSERT_ENUM(physx::PxPairFlag::eNEXT_FREE, CPxPairFlag::eNEXT_FREE);
ASSERT_ENUM(physx::PxPairFlag::eCONTACT_DEFAULT, CPxPairFlag::eCONTACT_DEFAULT);
ASSERT_ENUM(physx::PxPairFlag::eTRIGGER_DEFAULT, CPxPairFlag::eTRIGGER_DEFAULT);

ASSERT_ENUM(physx::PxVisualizationParameter::eSCALE, CPxVisualizationParameter::eSCALE);
ASSERT_ENUM(physx::PxVisualizationParameter::eWORLD_AXES, CPxVisualizationParameter::eWORLD_AXES);
ASSERT_ENUM(physx::PxVisualizationParameter::eBODY_AXES, CPxVisualizationParameter::eBODY_AXES);
ASSERT_ENUM(physx::PxVisualizationParameter::eBODY_MASS_AXES, CPxVisualizationParameter::eBODY_MASS_AXES);
ASSERT_ENUM(physx::PxVisualizationParameter::eBODY_LIN_VELOCITY, CPxVisualizationParameter::eBODY_LIN_VELOCITY);
ASSERT_ENUM(physx::PxVisualizationParameter::eBODY_ANG_VELOCITY, CPxVisualizationParameter::eBODY_ANG_VELOCITY);
ASSERT_ENUM(physx::PxVisualizationParameter::eCONTACT_POINT, CPxVisualizationParameter::eCONTACT_POINT);
ASSERT_ENUM(physx::PxVisualizationParameter::eCONTACT_NORMAL, CPxVisualizationParameter::eCONTACT_NORMAL);
ASSERT_ENUM(physx::PxVisualizationParameter::eCONTACT_ERROR, CPxVisualizationParameter::eCONTACT_ERROR);
ASSERT_ENUM(physx::PxVisualizationParameter::eCONTACT_IMPULSE, CPxVisualizationParameter::eCONTACT_IMPULSE);
ASSERT_ENUM(physx::PxVisualizationParameter::eFRICTION_POINT, CPxVisualizationParameter::eFRICTION_POINT);
ASSERT_ENUM(physx::PxVisualizationParameter::eFRICTION_NORMAL, CPxVisualizationParameter::eFRICTION_NORMAL);
ASSERT_ENUM(physx::PxVisualizationParameter::eFRICTION_IMPULSE, CPxVisualizationParameter::eFRICTION_IMPULSE);
ASSERT_ENUM(physx::PxVisualizationParameter::eACTOR_AXES, CPxVisualizationParameter::eACTOR_AXES);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_AABBS, CPxVisualizationParameter::eCOLLISION_AABBS);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_SHAPES, CPxVisualizationParameter::eCOLLISION_SHAPES);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_AXES, CPxVisualizationParameter::eCOLLISION_AXES);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_COMPOUNDS, CPxVisualizationParameter::eCOLLISION_COMPOUNDS);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_FNORMALS, CPxVisualizationParameter::eCOLLISION_FNORMALS);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_EDGES, CPxVisualizationParameter::eCOLLISION_EDGES);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_STATIC, CPxVisualizationParameter::eCOLLISION_STATIC);
ASSERT_ENUM(physx::PxVisualizationParameter::eCOLLISION_DYNAMIC, CPxVisualizationParameter::eCOLLISION_DYNAMIC);
ASSERT_ENUM(physx::PxVisualizationParameter::eJOINT_LOCAL_FRAMES, CPxVisualizationParameter::eJOINT_LOCAL_FRAMES);
ASSERT_ENUM(physx::PxVisualizationParameter::eJOINT_LIMITS, CPxVisualizationParameter::eJOINT_LIMITS);
ASSERT_ENUM(physx::PxVisualizationParameter::eCULL_BOX, CPxVisualizationParameter::eCULL_BOX);
ASSERT_ENUM(physx::PxVisualizationParameter::eMBP_REGIONS, CPxVisualizationParameter::eMBP_REGIONS);
ASSERT_ENUM(physx::PxVisualizationParameter::eSIMULATION_MESH, CPxVisualizationParameter::eSIMULATION_MESH);
ASSERT_ENUM(physx::PxVisualizationParameter::eSDF, CPxVisualizationParameter::eSDF);
ASSERT_ENUM(physx::PxVisualizationParameter::eNUM_VALUES, CPxVisualizationParameter::eNUM_VALUES);
ASSERT_ENUM(physx::PxVisualizationParameter::eFORCE_DWORD, CPxVisualizationParameter::eFORCE_DWORD);

ASSERT_ENUM(physx::PxSimulationStatistics::eDISCRETE_CONTACT_PAIRS, CRbPairStatsType::eDISCRETE_CONTACT_PAIRS);
ASSERT_ENUM(physx::PxSimulationStatistics::eCCD_PAIRS, CRbPairStatsType::eCCD_PAIRS);
ASSERT_ENUM(physx::PxSimulationStatistics::eMODIFIED_CONTACT_PAIRS, CRbPairStatsType::eMODIFIED_CONTACT_PAIRS);
ASSERT_ENUM(physx::PxSimulationStatistics::eTRIGGER_PAIRS, CRbPairStatsType::eTRIGGER_PAIRS);

ASSERT_ENUM(physx::PxErrorCode::eNO_ERROR, CPxErrorCode::eNO_ERROR);
ASSERT_ENUM(physx::PxErrorCode::eDEBUG_INFO, CPxErrorCode::eDEBUG_INFO);
ASSERT_ENUM(physx::PxErrorCode::eDEBUG_WARNING, CPxErrorCode::eDEBUG_WARNING);
ASSERT_ENUM(physx::PxErrorCode::eINVALID_PARAMETER, CPxErrorCode::eINVALID_PARAMETER);
ASSERT_ENUM(physx::PxErrorCode::eINVALID_OPERATION, CPxErrorCode::eINVALID_OPERATION);
ASSERT_ENUM(physx::PxErrorCode::eOUT_OF_MEMORY, CPxErrorCode::eOUT_OF_MEMORY);
ASSERT_ENUM(physx::PxErrorCode::eINTERNAL_ERROR, CPxErrorCode::eINTERNAL_ERROR);
ASSERT_ENUM(physx::PxErrorCode::eABORT, CPxErrorCode::eABORT);
ASSERT_ENUM(physx::PxErrorCode::ePERF_WARNING, CPxErrorCode::ePERF_WARNING);
ASSERT_ENUM(physx::PxErrorCode::eMASK_ALL, CPxErrorCode::eMASK_ALL);

ASSERT_ENUM(physx::PxBaseFlag::eOWNS_MEMORY, CPxBaseFlag::eOWNS_MEMORY);
ASSERT_ENUM(physx::PxBaseFlag::eIS_RELEASABLE, CPxBaseFlag::eIS_RELEASABLE);

ASSERT_ENUM(physx::PxActorFlag::eVISUALIZATION, CPxActorFlag::eVISUALIZATION);
ASSERT_ENUM(physx::PxActorFlag::eDISABLE_GRAVITY, CPxActorFlag::eDISABLE_GRAVITY);
ASSERT_ENUM(physx::PxActorFlag::eSEND_SLEEP_NOTIFIES, CPxActorFlag::eSEND_SLEEP_NOTIFIES);
ASSERT_ENUM(physx::PxActorFlag::eDISABLE_SIMULATION, CPxActorFlag::eDISABLE_SIMULATION);

ASSERT_ENUM(physx::PxActorType::eRIGID_STATIC, CPxActorType::eRIGID_STATIC);
ASSERT_ENUM(physx::PxActorType::eRIGID_DYNAMIC, CPxActorType::eRIGID_DYNAMIC);
ASSERT_ENUM(physx::PxActorType::eARTICULATION_LINK, CPxActorType::eARTICULATION_LINK);
ASSERT_ENUM(physx::PxActorType::eDEFORMABLE_SURFACE, CPxActorType::eDEFORMABLE_SURFACE);
ASSERT_ENUM(physx::PxActorType::eDEFORMABLE_VOLUME, CPxActorType::eDEFORMABLE_VOLUME);
ASSERT_ENUM(physx::PxActorType::ePBD_PARTICLESYSTEM, CPxActorType::ePBD_PARTICLESYSTEM);
ASSERT_ENUM(physx::PxActorType::eACTOR_COUNT, CPxActorType::eACTOR_COUNT);
ASSERT_ENUM(physx::PxActorType::eACTOR_FORCE_DWORD, CPxActorType::eACTOR_FORCE_DWORD);

ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_LINEAR_X, CPxRigidDynamicLockFlag::eLOCK_LINEAR_X);
ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_LINEAR_Y, CPxRigidDynamicLockFlag::eLOCK_LINEAR_Y);
ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_LINEAR_Z, CPxRigidDynamicLockFlag::eLOCK_LINEAR_Z);
ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_ANGULAR_X, CPxRigidDynamicLockFlag::eLOCK_ANGULAR_X);
ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_ANGULAR_Y, CPxRigidDynamicLockFlag::eLOCK_ANGULAR_Y);
ASSERT_ENUM(physx::PxRigidDynamicLockFlag::eLOCK_ANGULAR_Z, CPxRigidDynamicLockFlag::eLOCK_ANGULAR_Z);

ASSERT_ENUM(physx::PxMaterialFlag::eDISABLE_FRICTION, CPxMaterialFlag::eDISABLE_FRICTION);
ASSERT_ENUM(physx::PxMaterialFlag::eDISABLE_STRONG_FRICTION, CPxMaterialFlag::eDISABLE_STRONG_FRICTION);
ASSERT_ENUM(physx::PxMaterialFlag::eCOMPLIANT_ACCELERATION_SPRING, CPxMaterialFlag::eCOMPLIANT_ACCELERATION_SPRING);

ASSERT_ENUM(physx::PxCombineMode::eAVERAGE, CPxCombineMode::eAVERAGE);
ASSERT_ENUM(physx::PxCombineMode::eMIN, CPxCombineMode::eMIN);
ASSERT_ENUM(physx::PxCombineMode::eMULTIPLY, CPxCombineMode::eMULTIPLY);
ASSERT_ENUM(physx::PxCombineMode::eMAX, CPxCombineMode::eMAX);
ASSERT_ENUM(physx::PxCombineMode::eN_VALUES, CPxCombineMode::eN_VALUES);
ASSERT_ENUM(physx::PxCombineMode::ePAD_32, CPxCombineMode::ePAD_32);

ASSERT_ENUM(physx::PxHitFlag::ePOSITION, CPxHitFlag::ePOSITION);
ASSERT_ENUM(physx::PxHitFlag::eNORMAL, CPxHitFlag::eNORMAL);
ASSERT_ENUM(physx::PxHitFlag::eUV, CPxHitFlag::eUV);
ASSERT_ENUM(physx::PxHitFlag::eASSUME_NO_INITIAL_OVERLAP, CPxHitFlag::eASSUME_NO_INITIAL_OVERLAP);
ASSERT_ENUM(physx::PxHitFlag::eANY_HIT, CPxHitFlag::eANY_HIT);
ASSERT_ENUM(physx::PxHitFlag::eMESH_MULTIPLE, CPxHitFlag::eMESH_MULTIPLE);
ASSERT_ENUM(physx::PxHitFlag::eMESH_BOTH_SIDES, CPxHitFlag::eMESH_BOTH_SIDES);
ASSERT_ENUM(physx::PxHitFlag::ePRECISE_SWEEP, CPxHitFlag::ePRECISE_SWEEP);
ASSERT_ENUM(physx::PxHitFlag::eMTD, CPxHitFlag::eMTD);
ASSERT_ENUM(physx::PxHitFlag::eFACE_INDEX, CPxHitFlag::eFACE_INDEX);
ASSERT_ENUM(physx::PxHitFlag::eDEFAULT, CPxHitFlag::eDEFAULT);
ASSERT_ENUM(physx::PxHitFlag::eMODIFIABLE_FLAGS, CPxHitFlag::eMODIFIABLE_FLAGS);

ASSERT_ENUM(physx::PxGeometryQueryFlag::eSIMD_GUARD, CPxGeometryQueryFlag::eSIMD_GUARD);
ASSERT_ENUM(physx::PxGeometryQueryFlag::eDEFAULT, CPxGeometryQueryFlag::eDEFAULT);

ASSERT_ENUM(physx::PxQueryFlag::eSTATIC, CPxQueryFlag::eSTATIC);
ASSERT_ENUM(physx::PxQueryFlag::eDYNAMIC, CPxQueryFlag::eDYNAMIC);
ASSERT_ENUM(physx::PxQueryFlag::ePREFILTER, CPxQueryFlag::ePREFILTER);
ASSERT_ENUM(physx::PxQueryFlag::ePOSTFILTER, CPxQueryFlag::ePOSTFILTER);
ASSERT_ENUM(physx::PxQueryFlag::eANY_HIT, CPxQueryFlag::eANY_HIT);
ASSERT_ENUM(physx::PxQueryFlag::eNO_BLOCK, CPxQueryFlag::eNO_BLOCK);
ASSERT_ENUM(physx::PxQueryFlag::eBATCH_QUERY_LEGACY_BEHAVIOUR, CPxQueryFlag::eBATCH_QUERY_LEGACY_BEHAVIOUR);
ASSERT_ENUM(physx::PxQueryFlag::eDISABLE_HARDCODED_FILTER, CPxQueryFlag::eDISABLE_HARDCODED_FILTER);
ASSERT_ENUM(physx::PxQueryFlag::eRESERVED, CPxQueryFlag::eRESERVED);
