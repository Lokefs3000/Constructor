using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

        internal void UnregisterBehavioursFrom(Assembly assembly)
        {
            using RentedList<Type> typesToRemove = new RentedList<Type>();
            foreach (var (type, _) in _featureDict)
            {
                if (type.Assembly == assembly)
                {
                    typesToRemove.Add(type);
                }
            }

            if (!typesToRemove.IsEmpty)
            {
                foreach (Type type in typesToRemove)
                {
                    _featureDict.Remove(type);
                }
            }
        }

        internal void DestroyBehavioursFrom(Assembly assembly)
        {
            List<(SceneEntity, EntityBehaviour)> behavioursToRemove = new List<(SceneEntity, EntityBehaviour)>();

            foreach (var (entity, list) in _behaviours)
            {
                for (int i = 0; i < list.Count; ++i)
                {
                    EntityBehaviour behaviour = list[i];
                    if (behaviour.GetType().Assembly == assembly)
                    {
                        behavioursToRemove.Add((entity, behaviour));
                    }
                }
            }

            if (behavioursToRemove.Count > 0)
            {
                foreach ((SceneEntity entity, EntityBehaviour behaviour) in behavioursToRemove)
                {
                    entity.RemoveComponent(behaviour.GetType());
                }
            }
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
                EngLog.Scene.Error("Expected entity behaviour '{t}' to have been already registered", behaviour.GetType());

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
                for (int i = 0; i < behaviourList.Count; ++i)
                {
                    EntityBehaviour behaviourOnEntity = behaviourList[i];
                    if (object.ReferenceEquals(behaviourOnEntity, behaviour))
                    {
                        behaviourList.RemoveAt(i);
                        behaviour.OnDestroy();

                        if (behaviourList.IsEmpty)
                        {
                            behaviourList.Dispose();
                            _behaviours.Remove(entity);
                        }

                        break;
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
