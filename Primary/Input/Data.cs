namespace Primary.Input
{
    public enum CompositeFavoritsm : byte
    {
        None = 0,

        Left,
        Right,
        Up,
        Down,
        Forward,
        Backward,

        Positive = Left,
        Negative = Right,
    }

    public enum CompositeMode : byte
    {
        Analog = 0,
        Digital,
        DigitalNormalized
    }
}
