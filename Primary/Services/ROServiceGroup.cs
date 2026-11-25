using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Primary.Services
{
    public readonly record struct ROServiceGroup : IEquatable<ROServiceGroup>, IEquatable<ServiceGroup>
    {
        private readonly ServiceGroup _group;

        public ROServiceGroup(ServiceGroup group)
        {
            _group = group;
        }

        public T GetService<T>() where T : class, IService => _group.GetService<T>();
        public T? FindService<T>() where T : class, IService => _group.FindService<T>();
        public bool TryGetService<T>([NotNullWhen(true)] out T? service) where T : class, IService => _group.TryGetService(out service);

        public bool Equals(ServiceGroup? other) => _group.Equals(other);

        public override int GetHashCode() => _group.GetHashCode();
        public override string? ToString() => _group.ToString();

        public bool IsNull => _group == null;

        public static implicit operator ROServiceGroup(ServiceGroup group) => new ROServiceGroup(group);
    }
}
