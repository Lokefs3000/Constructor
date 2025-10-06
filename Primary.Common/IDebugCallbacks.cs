namespace Primary.Common
{
    public interface IDebugCallbacks
    {
        public void BeginSection(string name);
        public void EndSection();

        public void ShowStatistic(DebugStatisticType type, string text, ulong value);
        public void ShowStatistic(DebugStatisticType type, string text, float value);
        public void ShowStatistic(DebugStatisticType type, string text, double value);
        public void ShowStatistic(DebugStatisticType type, string text, int value);
    }

    public enum DebugStatisticType : byte
    {
        Plain = 0,
        Percentage,
        Time,
        DataSize
    }
}
