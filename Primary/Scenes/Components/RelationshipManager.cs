using Arch.Core;
using Arch.Core.Extensions;
using Primary.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Primary.Scenes.Components
{
    public sealed class RelationshipManager
    {
        internal RelationshipManager()
        {
            
        }

        internal bool SetParent(Entity entity, Entity newParent)
        {
            //ref EntityRelationship relationship = ref entity.TryGetRef<EntityRelationship>(out _);
            //Debug.Assert(!Unsafe.IsNullRef(in relationship));
            //
            //if (relationship.Parent != Entity.Null)
            //{
            //    RemoveFromParent(ref relationship);
            //}
            //
            //relationship.Parent = newParent;
            //
            //if (newParent != Entity.Null)
            //{
            //    AddToParent(ref relationship);
            //}

            ref EntityRelationships relationships = ref entity.TryGetRef<EntityRelationships>(out _);
            Debug.Assert(!Unsafe.IsNullRef(in relationships));

            if (relationships.Parent == newParent)
                return false;

            if (relationships.Parent != Entity.Null)
            {
                ref EntityRelationships parentRelationships = ref relationships.Parent.TryGetRef<EntityRelationships>(out _);
                Debug.Assert(!Unsafe.IsNullRef(in parentRelationships));

                parentRelationships.Children.Remove(entity);
            }

            if (newParent != Entity.Null)
            {
                ref EntityRelationships parentRelationships = ref newParent.TryGetRef<EntityRelationships>(out _);
                Debug.Assert(!Unsafe.IsNullRef(in parentRelationships));

                parentRelationships.Children.Add(entity);
            }

            relationships.Parent = newParent;
            return true;
        }

        //private void RemoveFromParent(ref EntityRelationship relationship)
        //{
        //    ref EntityRelationship parentRelationship = ref relationship.Parent.TryGetRef<EntityRelationship>(out _);
        //    Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //    if (relationship.Prev == Entity.Null) // is first in the child list
        //    {
        //        parentRelationship.First = relationship.Next;
        //
        //        if (relationship.Next != Entity.Null)
        //        {
        //            ref EntityRelationship childRelationship = ref relationship.Next.TryGetRef<EntityRelationship>(out _);
        //            Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //            childRelationship.Prev = Entity.Null;
        //        }
        //    }
        //    else if (relationship.Next == Entity.Null) // is last in the child list
        //    {
        //        ref EntityRelationship childRelationship = ref relationship.Prev.TryGetRef<EntityRelationship>(out _);
        //        Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //        childRelationship.Next = Entity.Null;
        //    }
        //    else // is in the middle of the child list
        //    {
        //        {
        //            ref EntityRelationship childRelationship = ref relationship.Prev.TryGetRef<EntityRelationship>(out _);
        //            Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //            childRelationship.Next = relationship.Next;
        //        }
        //
        //        {
        //            ref EntityRelationship childRelationship = ref relationship.Next.TryGetRef<EntityRelationship>(out _);
        //            Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //            childRelationship.Prev = relationship.Prev;
        //        }
        //    }
        //
        //    --parentRelationship.ChildCount;
        //}

        //private void AddToParent(ref EntityRelationship relationship)
        //{
        //    ref EntityRelationship parentRelationship = ref relationship.Parent.TryGetRef<EntityRelationship>(out _);
        //    Debug.Assert(!Unsafe.IsNullRef(in relationship));
        //
        //    Entity lastEntity = parentRelationship.
        //
        //    ++parentRelationship.ChildCount;
        //}
    }

    [Component, DontSerializeComponent]
    public struct EntityRelationship
    {
        public Entity Parent;
        public Entity Prev;
        public Entity Next;

        public int ChildCount;
        public Entity First;
    }
}
