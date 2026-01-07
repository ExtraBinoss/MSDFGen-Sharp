namespace Msdfgen
{
    /// <summary>
    /// Configuration for distance field generation.
    /// </summary>
    public struct GeneratorConfig
    {
        /// <summary>
        /// Whether to enable support for overlapping contours.
        /// </summary>
        public bool OverlapSupport;

        /// <summary>
        /// Initializes the generator configuration.
        /// </summary>
        public GeneratorConfig(bool overlapSupport = true)
        {
            OverlapSupport = overlapSupport;
        }
    }

    /// <summary>
    /// Configuration specifically for multi-channel signed distance field generation.
    /// </summary>
    public struct MSDFGeneratorConfig
    {
        /// <summary>
        /// Whether to enable support for overlapping contours.
        /// </summary>
        public bool OverlapSupport;

        /// <summary>
        /// Configuration for MSDF error correction.
        /// </summary>
        public ErrorCorrectionConfig ErrorCorrection;

        /// <summary>
        /// Initializes the MSDF generator configuration.
        /// </summary>
        public MSDFGeneratorConfig(bool overlapSupport = true, ErrorCorrectionConfig errorCorrection = default(ErrorCorrectionConfig))
        {
            OverlapSupport = overlapSupport;
            ErrorCorrection = errorCorrection;
        }
    }

    /// <summary>
    /// Configuration for MSDF error correction algorithms.
    /// </summary>
    public struct ErrorCorrectionConfig
    {
        /// <summary>
        /// Modes for distance error correction.
        /// </summary>
        public enum DistanceErrorCorrectionMode
        {
            /// <summary> Error correction is disabled. </summary>
            DISABLED,
            /// <summary> Error correction is applied to all pixels. </summary>
            INDISCRIMINATE,
            /// <summary> Error correction is only applied to pixels near edges. </summary>
            EDGE_ONLY,
            /// <summary> Error correction mode is determined automatically. </summary>
            AUTO
        }

        /// <summary>
        /// Modes for checking distance errors.
        /// </summary>
        public enum DistanceCheckMode
        {
            /// <summary> Do not check distances during error correction. </summary>
            DO_NOT_CHECK_DISTANCE,
            /// <summary> Check distances only at edges. </summary>
            CHECK_DISTANCE_AT_EDGE,
            /// <summary> Always check distances. </summary>
            CHECK_DISTANCE_ALWAYS
        }

        /// <summary>
        /// The error correction mode.
        /// </summary>
        public DistanceErrorCorrectionMode Mode;

        /// <summary>
        /// The distance check mode.
        /// </summary>
        public DistanceCheckMode DistanceCheck;

        /// <summary>
        /// The minimum ratio of deviation to trigger error correction.
        /// </summary>
        public double MinDeviationRatio;

        /// <summary>
        /// The minimum ratio of improvement required to apply a correction.
        /// </summary>
        public double MinImproveRatio;

        /// <summary>
        /// Optional buffer to store error correction metadata.
        /// </summary>
        public byte[]? Buffer; 

        /// <summary>
        /// Initializes error correction configuration with specified or default values.
        /// </summary>
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

        /// <summary>
        /// Gets the default error correction configuration.
        /// </summary>
        public static ErrorCorrectionConfig Default => new ErrorCorrectionConfig();
    }
}
