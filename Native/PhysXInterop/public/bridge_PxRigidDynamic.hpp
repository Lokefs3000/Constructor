#pragma once

#include "../shared.hpp"
#include "../bridge_types.hpp"

extern "C"
{
	/**
	\brief Moves kinematically controlled dynamic actors through the game world.

	You set a dynamic actor to be kinematic using the PxRigidBodyFlag::eKINEMATIC flag
	with setRigidBodyFlag().

	The move command will result in a velocity that will move the body into
	the desired pose. After the move is carried out during a single time step,
	the velocity is returned to zero. Thus, you must continuously call
	this in every time step for kinematic actors so that they move along a path.

	This function simply stores the move destination until the next simulation
	step is processed, so consecutive calls will simply overwrite the stored target variable.

	The motion is always fully carried out.

	\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.

	<b>Sleeping:</b> This call wakes the actor if it is sleeping and will set the wake counter to #PxSceneDesc::wakeCounterResetValue.

	\param[in] destination The desired pose for the kinematic actor, in the global frame. <b>Range:</b> rigid body transform.

	\see getKinematicTarget() PxRigidBodyFlag setRigidBodyFlag()
	*/
	INTEROP_API void CPxRigidDynamic_setKinematicTarget(CPxRigidDynamic* cpp, const CPxTransform* destination);

	/**
	\brief Get target pose of a kinematically controlled dynamic actor.

	\param[out] target Transform to write the target pose to. Only valid if the method returns true.
	\return True if the actor is a kinematically controlled dynamic and the target has been set, else False.

	\see setKinematicTarget() PxRigidBodyFlag setRigidBodyFlag()
	*/
	INTEROP_API bool CPxRigidDynamic_getKinematicTarget(CPxRigidDynamic* cpp, CPxTransform* target);

	/**
	\brief Returns true if this body is sleeping.

	When an actor does not move for a period of time, it is no longer simulated in order to save time. This state
	is called sleeping. However, because the object automatically wakes up when it is either touched by an awake object,
	or one of its properties is changed by the user, the entire sleep mechanism should be transparent to the user.

	In general, a dynamic rigid actor is guaranteed to be awake if at least one of the following holds:

	\li The wake counter is positive (see #setWakeCounter()).
	\li The linear or angular velocity is non-zero.
	\li A non-zero force or torque has been applied.

	If a dynamic rigid actor is sleeping, the following state is guaranteed:

	\li The wake counter is zero.
	\li The linear and angular velocity is zero.
	\li There is no force update pending.

	When an actor gets inserted into a scene, it will be considered asleep if all the points above hold, else it will be treated as awake.

	If an actor is asleep after the call to PxScene::fetchResults() returns, it is guaranteed that the pose of the actor
	was not changed. You can use this information to avoid updating the transforms of associated objects.

	\note A kinematic actor is asleep unless a target pose has been set (in which case it will stay awake until two consecutive
	simulation steps without a target pose being set have passed). The wake counter will get set to zero or to the reset value
	#PxSceneDesc::wakeCounterResetValue in the case where a target pose has been set to be consistent with the definitions above.

	\note It is invalid to use this method if the actor has not been added to a scene already.

	\note It is not allowed to use this method while the simulation is running.

	\return True if the actor is sleeping.

	\see isSleeping() wakeUp() putToSleep()  getSleepThreshold()
	*/
	INTEROP_API bool CPxRigidDynamic_isSleeping(CPxRigidDynamic* cpp);

	/**
	\brief Sets the mass-normalized kinetic energy threshold below which an actor may go to sleep.

	Actors whose kinetic energy divided by their mass is below this threshold will be candidates for sleeping.

	<b>Default:</b> 5e-5f * PxTolerancesScale::speed * PxTolerancesScale::speed

	\param[in] threshold Energy below which an actor may go to sleep. <b>Range:</b> [0, PX_MAX_F32)

	\see isSleeping() getSleepThreshold() wakeUp() putToSleep() PxTolerancesScale
	*/
	INTEROP_API void CPxRigidDynamic_setSleepThreshold(CPxRigidDynamic* cpp, CPxReal threshold);

	/**
	\brief Returns the mass-normalized kinetic energy below which an actor may go to sleep.

	\return The energy threshold for sleeping.

	\see isSleeping() wakeUp() putToSleep() setSleepThreshold()
	*/
	INTEROP_API CPxReal CPxRigidDynamic_getSleepThreshold(CPxRigidDynamic* cpp);

	/**
	\brief Sets the mass-normalized kinetic energy threshold below which an actor may participate in stabilization.

	Actors whose kinetic energy divided by their mass is above this threshold will not participate in stabilization.

	This value has no effect if PxSceneFlag::eENABLE_STABILIZATION was not enabled on the PxSceneDesc.

	<b>Default:</b> 1e-5f * PxTolerancesScale::speed * PxTolerancesScale::speed

	\param[in] threshold Energy below which an actor may participate in stabilization. <b>Range:</b> [0,inf)

	\see  getStabilizationThreshold() PxSceneFlag::eENABLE_STABILIZATION
	*/
	INTEROP_API void CPxRigidDynamic_setStabilizationThreshold(CPxRigidDynamic* cpp, CPxReal threshold);

	/**
	\brief Returns the mass-normalized kinetic energy below which an actor may participate in stabilization.

	Actors whose kinetic energy divided by their mass is above this threshold will not participate in stabilization.

	\return The energy threshold for participating in stabilization.

	\see setStabilizationThreshold() PxSceneFlag::eENABLE_STABILIZATION
	*/
	INTEROP_API CPxReal CPxRigidDynamic_getStabilizationThreshold(CPxRigidDynamic* cpp);

	/**
	\brief Sets the wake counter for the actor.

	The wake counter value determines the minimum amount of time until the body can be put to sleep. Please note
	that a body will not be put to sleep if the energy is above the specified threshold (see #setSleepThreshold())
	or if other awake bodies are touching it.

	\note Passing in a positive value will wake the actor up automatically.

	\note It is invalid to use this method for kinematic actors since the wake counter for kinematics is defined
	based on whether a target pose has been set (see the comment in #isSleeping()).

	\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.

	<b>Default:</b> 0.4 (which corresponds to 20 frames for a time step of 0.02)

	\param[in] wakeCounterValue Wake counter value. <b>Range:</b> [0, PX_MAX_F32)

	\see isSleeping() getWakeCounter()
	*/
	INTEROP_API void CPxRigidDynamic_setWakeCounter(CPxRigidDynamic* cpp, CPxReal wakeCounterValue);

	/**
	\brief Returns the wake counter of the actor.

	\note It is not allowed to use this method while the simulation is running.

	\return The wake counter of the actor.

	\see isSleeping() setWakeCounter()
	*/
	INTEROP_API CPxReal CPxRigidDynamic_getWakeCounter(CPxRigidDynamic* cpp);

	/**
	\brief Wakes up the actor if it is sleeping.

	The actor will get woken up and might cause other touching actors to wake up as well during the next simulation step.

	\note This will set the wake counter of the actor to the value specified in #PxSceneDesc::wakeCounterResetValue.

	\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.

	\note It is invalid to use this method for kinematic actors since the sleep state for kinematics is defined
	based on whether a target pose has been set (see the comment in #isSleeping()).

	\see isSleeping() putToSleep()
	*/
	INTEROP_API void CPxRigidDynamic_wakeUp(CPxRigidDynamic* cpp);

	/**
	\brief Forces the actor to sleep.

	The actor will stay asleep during the next simulation step if not touched by another non-sleeping actor.

	\note Any applied force will be cleared and the velocity and the wake counter of the actor will be set to 0.

	\note It is invalid to use this method if the actor has not been added to a scene already or if PxActorFlag::eDISABLE_SIMULATION is set.

	\note It is invalid to use this method for kinematic actors since the sleep state for kinematics is defined
	based on whether a target pose has been set (see the comment in #isSleeping()).

	\see isSleeping() wakeUp()
	*/
	INTEROP_API void CPxRigidDynamic_putToSleep(CPxRigidDynamic* cpp);

	/**
	\brief Reads the PxRigidDynamic lock flags.

	See the list of flags #PxRigidDynamicLockFlag

	\return The values of the PxRigidDynamic lock flags.

	\see PxRigidDynamicLockFlag setRigidDynamicLockFlag()
	*/
	INTEROP_API CPxRigidDynamicLockFlag CPxRigidDynamic_getRigidDynamicLockFlags(CPxRigidDynamic* cpp);

	/**
	\brief Raises or clears a particular PxRigidDynamic lock flag.

	See the list of flags #PxRigidDynamicLockFlag

	<b>Default:</b> no flags are set

	\param[in] flag		The PxRigidDynamicLockFlag to raise(set) or clear. See #PxRigidBodyFlag.
	\param[in] value	The new boolean value for the flag.

	\see PxRigidDynamicLockFlag getRigidDynamicLockFlags()
	*/
	INTEROP_API void CPxRigidDynamic_setRigidDynamicLockFlag(CPxRigidDynamic* cpp, CPxRigidDynamicLockFlag flag, bool value);

	/**
	\brief Set all PxRigidDynamic lock flags.
	\see setRigidDynamicLockFlag()
	*/
	INTEROP_API void CPxRigidDynamic_setRigidDynamicLockFlags(CPxRigidDynamic* cpp, CPxRigidDynamicLockFlag flags);

	/**
	\brief Retrieves the actor's center-of-mass linear velocity.

	\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),
	in PxContactModifyCallback or in contact report callbacks).

	\note The linear velocity is reported with respect to the actor's center of mass and not the actor frame origin.

	\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.

	\return The actor's center-of-mass linear velocity.

	\see PxRigidDynamic.setLinearVelocity() getAngularVelocity()
	*/
	INTEROP_API CPxVec3 CPxRigidDynamic_getLinearVelocity(CPxRigidDynamic* cpp);

	/**
	\brief Sets the actor's center-of-mass linear velocity.

	Note that if you continuously set the velocity of an actor yourself,
	forces such as gravity or friction will not be able to manifest themselves, because forces directly
	influence only the velocity/momentum of an actor.

	<b>Default:</b> (0.0, 0.0, 0.0)

	<b>Sleeping:</b> This call wakes the actor if it is sleeping, and the autowake parameter is true (default) or the
	new velocity is non-zero.

	\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.

	\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.

	\note The linear velocity is applied with respect to the actor's center of mass and not the actor frame origin.

	\param[in] linVel New center-of-mass linear velocity of the actor. <b>Range:</b> velocity vector
	\param[in] autowake Whether to wake the object up if it is asleep. If true and the current wake counter value is
	smaller than #PxSceneDesc::wakeCounterResetValue it will get increased to the reset value.

	\see getLinearVelocity() setAngularVelocity()
	*/
	INTEROP_API void CPxRigidDynamic_setLinearVelocity(CPxRigidDynamic* cpp, const CPxVec3* linVel, bool autowake = true);

	/**
	\brief Retrieves the angular velocity of the actor.

	\note It is not allowed to use this method while the simulation is running (except during PxScene::collide(),
	in PxContactModifyCallback or in contact report callbacks).

	\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.

	\return The angular velocity of the actor.

	\see PxRigidDynamic.setAngularVelocity() getLinearVelocity()
	*/
	INTEROP_API CPxVec3 CPxRigidDynamic_getAngularVelocity(CPxRigidDynamic* cpp);

	/**
	\brief Sets the angular velocity of the actor.

	Note that if you continuously set the angular velocity of an actor yourself,
	forces such as friction will not be able to rotate the actor, because forces directly influence only the velocity/momentum.

	<b>Default:</b> (0.0, 0.0, 0.0)

	<b>Sleeping:</b> This call wakes the actor if it is sleeping, and the autowake parameter is true (default) or the
	new velocity is non-zero.

	\note It is invalid to use this method if PxActorFlag::eDISABLE_SIMULATION is set.

	\note This method should not be used after the direct GPU API has been enabled and initialized. See #PxDirectGPUAPI for the details.

	\param[in] angVel New angular velocity of actor. <b>Range:</b> angular velocity vector
	\param[in] autowake Whether to wake the object up if it is asleep. If true and the current wake counter value is
	smaller than #PxSceneDesc::wakeCounterResetValue it will get increased to the reset value.

	\see getAngularVelocity() setLinearVelocity()
	*/
	INTEROP_API void CPxRigidDynamic_setAngularVelocity(CPxRigidDynamic* cpp, const CPxVec3* angVel, bool autowake = true);

	/**
	\brief Sets the solver iteration counts for the body.

	The solver iteration count determines how accurately joints and contacts are resolved.
	If you are having trouble with jointed bodies oscillating and behaving erratically, then
	setting a higher position iteration count may improve their stability.

	If intersecting bodies are being depenetrated too violently, increase the number of velocity
	iterations. More velocity iterations will drive the relative exit velocity of the intersecting
	objects closer to the correct value given the restitution.

	<b>Default:</b> 4 position iterations, 1 velocity iteration

	\param[in] minPositionIters Number of position iterations the solver should perform for this body. <b>Range:</b> [1,255]
	\param[in] minVelocityIters Number of velocity iterations the solver should perform for this body. <b>Range:</b> [0,255]

	\see getSolverIterationCounts()
	*/
	INTEROP_API void CPxRigidDynamic_setSolverIterationCounts(CPxRigidDynamic* cpp, CPxU32 minPositionIters, CPxU32 minVelocityIters = 1);

	/**
	\brief Retrieves the solver iteration counts.

	\see setSolverIterationCounts()
	*/
	INTEROP_API void CPxRigidDynamic_getSolverIterationCounts(CPxRigidDynamic* cpp, CPxU32* minPositionIters, CPxU32* minVelocityIters);

	/**
	\brief Retrieves the force threshold for contact reports.

	The contact report threshold is a force threshold. If the force between
	two actors exceeds this threshold for either of the two actors, a contact report
	will be generated according to the contact report threshold flags provided by
	the filter shader/callback.
	See #PxPairFlag.

	The threshold used for a collision between a dynamic actor and the static environment is
	the threshold of the dynamic actor, and all contacts with static actors are summed to find
	the total normal force.

	<b>Default:</b> PX_MAX_F32

	\return Force threshold for contact reports.

	\see setContactReportThreshold PxPairFlag PxSimulationFilterShader PxSimulationFilterCallback
	*/
	INTEROP_API CPxReal CPxRigidDynamic_getContactReportThreshold(CPxRigidDynamic* cpp);

	/**
	\brief Sets the force threshold for contact reports.

	See #getContactReportThreshold().

	\param[in] threshold Force threshold for contact reports. <b>Range:</b> [0, PX_MAX_F32)

	\see getContactReportThreshold PxPairFlag
	*/
	INTEROP_API void CPxRigidDynamic_setContactReportThreshold(CPxRigidDynamic* cpp, CPxReal threshold);

	/**
	\brief Returns the GPU rigid dynamic index.

	\note This function only returns valid results if GPU dynamics is enabled.

	\return The GPU index, or 0xFFFFFFFF if the actor is not inserted into a PxScene.

	\see PxDirectGPUAPI::getRigidDynamicData(), PxDirectGPUAPI::setRigidDynamicData().
	*/
	INTEROP_API CPxRigidDynamicGPUIndex CPxRigidDynamic_getGPUIndex(CPxRigidDynamic* cpp);

	INTEROP_API const char* CPxRigidDynamic_getConcreteTypeName(CPxRigidDynamic* cpp);
}