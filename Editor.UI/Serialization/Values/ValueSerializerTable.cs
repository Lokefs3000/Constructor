using Editor.UI.Datatypes;
using Editor.UI.Reflection;
using Editor.UI.Serialization.Helpers;
using Editor.UI.Serialization.Values.Structs;
using Primary.Assets;
using Primary.Assets.Types;
using Primary.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Editor.UI.Serialization.Values
{
    public sealed class ValueSerializerTable
    {
        private FrozenDictionary<Type, SerializerInvoke> _serializers;
        private Dictionary<Type, DeserializeFieldSet> _enumSerializers;

        internal ValueSerializerTable()
        {
            _serializers = FrozenDictionary<Type, SerializerInvoke>.Empty;
            _enumSerializers = new Dictionary<Type, DeserializeFieldSet>();
        }

        internal void DiscoverAssembly(Assembly assembly)
        {
            long timeStart = Stopwatch.GetTimestamp();

            ConcurrentDictionary<Type, SerializerInvoke> tempDict = new ConcurrentDictionary<Type, SerializerInvoke>();

            Type[] allTypes = assembly.GetTypes();
            Parallel.ForEach(allTypes, (t) =>
            {
                if (t.IsAssignableTo(typeof(IPrivateValueSerializer)))
                {
                    ValueSerializerTargetAttribute? target = t.GetCustomAttribute<ValueSerializerTargetAttribute>();
                    if (target != null)
                    {
                        if (_serializers.ContainsKey(target.TargetType))
                            UIManager.Logger?.Warning("Duplicate serializer target type: {t}", target.TargetType);
                        else
                        {
                            tempDict.TryAdd(target.TargetType, SerializerInvoke.GenerateEmit(target.TargetType, t));
                        }
                    }
                }
            });

            _serializers = _serializers.Concat(tempDict).ToFrozenDictionary();

            UIManager.Logger?.Debug("Discovering value serializers in assembly: {asm} took: {secs:f3}s!", assembly.GetName().Name, Stopwatch.GetElapsedTime(timeStart).TotalSeconds);
        }

        internal string? Serialize<T>(T value)
        {
            throw new NotImplementedException();
        }

        internal bool Deserialize<T>(string value, out T? deserialized)
        {
            Type type = typeof(T);
            if (type.IsEnum)
            {
                return EnumGenericHelper<T>.TryParse(value, false, out deserialized);
            }
            else if (type.IsAssignableTo(typeof(IAssetDefinition)))
            {
                deserialized = (T)AssetGenericHelper<T>.LoadAsset(value);
                return Unsafe.As<IAssetDefinition>(deserialized).Status != ResourceStatus.Error;
            }
            else if (_serializers.TryGetValue(type, out SerializerInvoke invoke))
            {
                return Unsafe.As<DeserializeTyped<T>>(invoke.DeserializeTyped)(value, out deserialized);
            }

            deserialized = default;

            UIManager.Logger?.Error("No value serializer found for type: {t}", type);
            return false;
        }

        internal bool DeserializeFieldSet(Type type, string value, object fieldSetter, object fieldTarget, FieldInfo field)
        {
            if (type.IsEnum)
            {
                if (!_enumSerializers.TryGetValue(type, out DeserializeFieldSet? @delegate))
                {
                    @delegate = MethodGenerator.CreateDeserializeFieldSetEnum(type);
                    _enumSerializers.Add(type, @delegate);
                }

                return @delegate(value, fieldSetter, fieldTarget);
            }
            else if (type.IsAssignableTo(typeof(IAssetDefinition)))
            {
                IAssetDefinition asset = Unsafe.As<IAssetDefinition>(AssetManager.LoadAsset(type, value));
                if (asset.Status != ResourceStatus.Error)
                {
                    field.SetValue(fieldTarget, asset);
                    return true;
                }

                return false;
            }
            else if (_serializers.TryGetValue(type, out SerializerInvoke invoke))
            {
                return invoke.DeserializeFieldSet(value, fieldSetter, fieldTarget);
            }

            UIManager.Logger?.Error("No value serializer found for type: {t}", type);
            return false;
        }

        internal bool Deserialize(Type type, string value, out object? deserialized)
        {
            if (type.IsEnum)
            {
                return Enum.TryParse(type, value, out deserialized);
            }
            else if (type.IsAssignableTo(typeof(IAssetDefinition)))
            {
                deserialized = AssetManager.LoadAsset(type, value);
                return Unsafe.As<IAssetDefinition>(deserialized).Status != ResourceStatus.Error;
            }
            else if (_serializers.TryGetValue(type, out SerializerInvoke invoke))
            {
                return invoke.DeserializeBoxed(value, out deserialized);
            }

            deserialized = default;

            UIManager.Logger?.Error("No value serializer found for type: {t}", type);
            return false;
        }

        private readonly record struct SerializerInvoke(
            object/*DeserializeTyped<T>*/ DeserializeTyped, DeserializeFieldSet DeserializeFieldSet, DeserializeBoxed DeserializeBoxed)
        {
            public static SerializerInvoke GenerateEmit(Type t, Type serializer)
            {
                return new SerializerInvoke(
                    Delegate.CreateDelegate(typeof(DeserializeTyped<>).MakeGenericType([t]), serializer.GetMethod("Deserialize", BindingFlags.Static | BindingFlags.Public)!),
                    MethodGenerator.CreateDeserializeFieldSet(t, serializer),
                    MethodGenerator.CreateDeserializeBoxed(t, serializer));
            }
        }

        private delegate bool DeserializeTyped<T>(string value, out T result);
    }
}
