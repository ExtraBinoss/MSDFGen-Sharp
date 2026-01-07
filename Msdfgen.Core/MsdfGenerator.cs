using System;

namespace Msdfgen
{
    public static class MsdfGenerator
    {
        /// <summary>
        /// Generates a distance field of the specified type.
        /// </summary>
        private static void GenerateDistanceField<TCombiner>(Bitmap<float> output, Shape shape, SDFTransformation transformation)
            where TCombiner : class
        {
            // Create Combiner via reflection
            TCombiner contourCombiner = (TCombiner)Activator.CreateInstance(typeof(TCombiner), shape)!;
            
            ShapeDistanceFinder<TCombiner> distanceFinder = new ShapeDistanceFinder<TCombiner>(shape, contourCombiner);
            
            int width = output.Width;
            int height = output.Height;
            int xDirection = 1;
            
            for (int y = 0; y < height; ++y)
            {
                int x = xDirection < 0 ? width - 1 : 0;
                for (int col = 0; col < width; ++col)
                {
                    Vector2 p = transformation.Unproject(new Vector2(x + 0.5, y + 0.5));
                    dynamic distance = distanceFinder.Distance(p);
                    
                    // Convert distance to pixel and write
                    SetPixel(output, x, y, distance, transformation.DistanceMapping);
                    
                    x += xDirection;
                }
                xDirection = -xDirection;
            }
        }
        
        /// <summary>
        /// Sets a pixel in a single-channel or grayscale distance field.
        /// </summary>
        private static void SetPixel(Bitmap<float> output, int x, int y, double distance, DistanceMapping mapping)
        {
            float val = (float)mapping.Map(distance);
            output[x, y, 0] = val;
            if (output.Channels >= 3)
            {
                output[x, y, 1] = val; 
                output[x, y, 2] = val;
            }
        }

        /// <summary>
        /// Sets a pixel in a multi-channel distance field (MSDF).
        /// </summary>
        private static void SetPixel(Bitmap<float> output, int x, int y, MultiDistance distance, DistanceMapping mapping)
        {
            output[x, y, 0] = (float)mapping.Map(distance.R);
            output[x, y, 1] = (float)mapping.Map(distance.G);
            output[x, y, 2] = (float)mapping.Map(distance.B);
        }

        /// <summary>
        /// Sets a pixel in a multi-channel and true distance field (MTSDF).
        /// </summary>
        private static void SetPixel(Bitmap<float> output, int x, int y, MultiAndTrueDistance distance, DistanceMapping mapping)
        {
            output[x, y, 0] = (float)mapping.Map(distance.R);
            output[x, y, 1] = (float)mapping.Map(distance.G);
            output[x, y, 2] = (float)mapping.Map(distance.B);
            output[x, y, 3] = (float)mapping.Map(distance.A);
        }

        /// <summary>
        /// Generates a conventional single-channel signed distance field.
        /// </summary>
        public static void GenerateSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, GeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<TrueDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<TrueDistanceSelector>>(output, shape, transformation);
        }

        /// <summary>
        /// Generates a pseudo-signed distance field.
        /// </summary>
        public static void GeneratePSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, GeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<PerpendicularDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<PerpendicularDistanceSelector>>(output, shape, transformation);
        }

        /// <summary>
        /// Generates a multi-channel signed distance field (MSDF).
        /// </summary>
        public static void GenerateMSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiDistanceSelector>>(output, shape, transformation);
            
            MSDFErrorCorrection.Correct(output, shape, transformation, config.ErrorCorrection);
        }
        
        /// <summary>
        /// Generates a multi-channel and true signed distance field (MTSDF).
        /// </summary>
        public static void GenerateMTSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiAndTrueDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiAndTrueDistanceSelector>>(output, shape, transformation);
            
            MSDFErrorCorrection.Correct(output, shape, transformation, config.ErrorCorrection);
        }

        /// <summary>
        /// Generates a conventional single-channel signed distance field using a projection and range.
        /// </summary>
        public static void GenerateSDF(Bitmap<float> output, Shape shape, Projection projection, Range range, GeneratorConfig config)
        {
            GenerateSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), config);
        }
        
        /// <summary>
        /// Generates a multi-channel signed distance field (MSDF) using a projection and range.
        /// </summary>
         public static void GenerateMSDF(Bitmap<float> output, Shape shape, Projection projection, Range range, MSDFGeneratorConfig config)
        {
            GenerateMSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), config);
        }
    }
}
