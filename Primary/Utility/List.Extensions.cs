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
    }
}
