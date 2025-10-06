namespace Primary
{
    public static class Constants
    {
        public const float rLightBufferShrinkPercentage = 3.0f;
        public const float rLightBufferFragmentationPercentage = 0.45f;
        public const uint rLightBufferMinimumSize = 8;

        public const float rShadowImportanceHighBias = 3.0f;
        public const float rShadowImportanceMediumBias = 2.0f;
        public const float rShadowImportanceLowBias = 1.0f;
        public const short rShadowResolutionIncrements = 64;
        public const short rShadowMinimumResolution = 64;
        public const short rShadowImportanceHighResolution = 512;
        public const float rShadowMaximumDistance = 96.0f;
        public const short rShadowMapResolution = 2048;
        public const uint rShadowBufferMinimumSize = 8;
        public const float rShadowBufferShrinkPercentage = 3.0f;
        public const uint rShadowCubemapBufferMinimumSize = 8;
        public const float rShadowCubemapBufferShrinkPercentage = 3.0f;

        public const int rFlagListStartSize = 32;

        public const int rRPBufferManagerHistorySize = 12;
        public const float rRPBufferManagerHistoryTime = 0.2f;
        public const int rRPBufferManagerMinimumSize = 64; //bytes
        public const int rRPBufferManagerMaximumSize = 1024; //bytes
    }
}
