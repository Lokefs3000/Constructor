namespace Primary.Utility
{
    public static class ListExtensions
    {
        public static bool RemoveWhere<T>(this List<T> self, Predicate<T> predicate)
        {
            int idx = self.FindIndex(predicate);
            if (idx != -1)
            {
                self.RemoveAt(idx);
                return true;
            }

            return false;
        }

        public static bool RemoveWhere<T>(this List<T> self, Predicate<T> predicate, out T? value)
        {
            int idx = self.FindIndex(predicate);
            if (idx != -1)
            {
                value = self[idx];
                self.RemoveAt(idx);
                return true;
            }

            value = default;
            return false;
        }

        public static bool AddUnique<T>(this List<T> self, T item)
        {
            if (!self.Contains(item))
            {
                self.Add(item);
                return true;
            }

            return false;
        }

        public static int IndexOf<T>(this IReadOnlyList<T> self, T item)
        {
            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < self.Count; ++i)
            {
                if (comparer.Equals(self[i], item))
                    return i;
            }

            return -1;
        }
    }
}
