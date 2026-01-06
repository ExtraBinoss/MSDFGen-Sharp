namespace Msdfgen
{
    public struct GeneratorConfig
    {
        public bool OverlapSupport;

        public GeneratorConfig(bool overlapSupport = true)
        {
            OverlapSupport = overlapSupport;
        }
    }

    public struct MSDFGeneratorConfig
    {
        public bool OverlapSupport;
        public ErrorCorrectionConfig ErrorCorrection;

        public MSDFGeneratorConfig(bool overlapSupport = true, ErrorCorrectionConfig errorCorrection = default(ErrorCorrectionConfig))
        {
            OverlapSupport = overlapSupport;
            ErrorCorrection = errorCorrection;
        }
    }

    public struct ErrorCorrectionConfig
    {
        public enum DistanceErrorCorrectionMode
        {
            DISABLED,
            INDISCRIMINATE,
            EDGE_ONLY,
            AUTO
        }

        public enum DistanceCheckMode
        {
            DO_NOT_CHECK_DISTANCE,
            CHECK_DISTANCE_AT_EDGE,
            CHECK_DISTANCE_ALWAYS
        }

        public DistanceErrorCorrectionMode Mode;
        public DistanceCheckMode DistanceCheck;
        public double MinDeviationRatio;
        public double MinImproveRatio;
        public byte[]? Buffer; 

        public ErrorCorrectionConfig(
            DistanceErrorCorrectionMode mode = DistanceErrorCorrectionMode.EDGE_ONLY,
            DistanceCheckMode distanceCheck = DistanceCheckMode.CHECK_DISTANCE_AT_EDGE,
            double minDeviationRatio = 1.11111111111111111,
            double minImproveRatio = 1.11111111111111111,
            byte[]? buffer = null)
        {
            Mode = mode;
            DistanceCheck = distanceCheck;
            MinDeviationRatio = minDeviationRatio;
            MinImproveRatio = minImproveRatio;
            Buffer = buffer;
        }

        public static ErrorCorrectionConfig Default => new ErrorCorrectionConfig();
    }
}
