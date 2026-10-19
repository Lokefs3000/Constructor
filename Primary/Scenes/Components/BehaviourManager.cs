using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Primary.Collections;
using Primary.Common;
using Primary.Components;
using Primary.Components.Behaviour;

namespace Primary.Scenes.Components
{
    internal sealed class BehaviourManager
    {
        private readonly ArrayPool<EntityBehaviour> _behaviourArrayPool;

        private readonly Dictionary<Type, BehaviourFeatures> _featureDict;
        private readonly Dictionary<SceneEntity, RentedList<EntityBehaviour>> _behaviours;

        internal BehaviourManager()
        {
            _behaviourArrayPool = ArrayPool<EntityBehaviour>.Create();

            _featureDict = new Dictionary<Type, BehaviourFeatures>();
            _behaviours = new Dictionary<SceneEntity, RentedList<EntityBehaviour>>();
        }

        internal void RegisterBehaviour(Type type)
        {
            ref BehaviourFeatures features = ref CollectionsMarshal.GetValueRefOrAddDefault(_featureDict, type, out bool exists);
            if (!exists)
            {
                features = BehaviourFeatures.None;

                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (!features.HasFlags(BehaviourFeatures.Update) && interfaceType == typeof(IUpdateListener))
                    {
                        features = BehaviourFeatures.Update;
                    }
                    else if (!features.HasFlags(BehaviourFeatures.TransformUpdate) && interfaceType == typeof(ITransformUpdateListener))
                    {
                        features = BehaviourFeatures.TransformUpdate;
                    }
                }
            }
        }

        internal void SetupBehaviour(SceneEntity entity, EntityBehaviour behaviour)
        {
            ref BehaviourFeatures features = ref CollectionsMarshal.GetValueRefOrNullRef(_featureDict, behaviour.GetType());
            if (Unsafe.IsNullRef(in features))
            {
                EngLog.Scene.Error("Expected entity behaviour '{t}' to have been already registerer", behaviour.GetType());

                RegisterBehaviour(behaviour.GetType());
                features = ref CollectionsMarshal.GetValueRefOrNullRef(_featureDict, behaviour.GetType());
            }

            ref RentedList<EntityBehaviour> behaviourList = ref CollectionsMarshal.GetValueRefOrAddDefault(_behaviours, entity, out bool exists);
            if (!exists)
            {
                behaviourList = new RentedList<EntityBehaviour>(_behaviourArrayPool)
                {
                    behaviour
                };
            }
            else
            {
                behaviourList.Add(behaviour);
            }

            behaviour.SetBehaviourParams(entity, features);
            behaviour.OnEarlyInit();
        }

        internal void DestroyBehaviour(SceneEntity entity, EntityBehaviour behaviour)
        {
            ref RentedList<EntityBehaviour> behaviourList = ref CollectionsMarshal.GetValueRefOrNullRef(_behaviours, entity);
            if (!Unsafe.IsNullRef(in behaviourList))
            {
                if (behaviourList.Remove(behaviour))
                {
                    behaviour.OnDestroy();

                    if (behaviourList.IsEmpty)
                    {
                        behaviourList.Dispose();
                        _behaviours.Remove(entity);
                    }
                }
            }
        }

        internal void DestroyEntity(SceneEntity entity)
        {
            ref RentedList<EntityBehaviour> behaviourList = ref CollectionsMarshal.GetValueRefOrNullRef(_behaviours, entity);
            if (!Unsafe.IsNullRef(in behaviourList))
            {
                foreach (EntityBehaviour behaviour in behaviourList)
                {
                    behaviour.OnDestroy();
                }

                behaviourList.Dispose();
                _behaviours.Remove(entity);
            }
        }
    }
}
