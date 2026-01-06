using System;

namespace Msdfgen
{
    public static class MsdfGenerator
    {
        private static void GenerateDistanceField<TCombiner>(Bitmap<float> output, Shape shape, SDFTransformation transformation)
            where TCombiner : class
        {
            // output.reorient(shape.getYAxisOrientation()); // Not implemented in Bitmap yet
            
            // Create Combiner via reflection
            TCombiner contourCombiner = (TCombiner)Activator.CreateInstance(typeof(TCombiner), shape);
            
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

        private static void SetPixel(Bitmap<float> output, int x, int y, MultiDistance distance, DistanceMapping mapping)
        {
            output[x, y, 0] = (float)mapping.Map(distance.R);
            output[x, y, 1] = (float)mapping.Map(distance.G);
            output[x, y, 2] = (float)mapping.Map(distance.B);
        }

        private static void SetPixel(Bitmap<float> output, int x, int y, MultiAndTrueDistance distance, DistanceMapping mapping)
        {
            output[x, y, 0] = (float)mapping.Map(distance.R);
            output[x, y, 1] = (float)mapping.Map(distance.G);
            output[x, y, 2] = (float)mapping.Map(distance.B);
            output[x, y, 3] = (float)mapping.Map(distance.A);
        }

        public static void GenerateSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, GeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<TrueDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<TrueDistanceSelector>>(output, shape, transformation);
        }

        public static void GeneratePSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, GeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<PerpendicularDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<PerpendicularDistanceSelector>>(output, shape, transformation);
        }

        public static void GenerateMSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiDistanceSelector>>(output, shape, transformation);
            
            MSDFErrorCorrection.Correct(output, shape, transformation, config.ErrorCorrection);
        }
        
        public static void GenerateMTSDF(Bitmap<float> output, Shape shape, SDFTransformation transformation, MSDFGeneratorConfig config)
        {
            if (config.OverlapSupport)
                GenerateDistanceField<OverlappingContourCombiner<MultiAndTrueDistanceSelector>>(output, shape, transformation);
            else
                GenerateDistanceField<SimpleContourCombiner<MultiAndTrueDistanceSelector>>(output, shape, transformation);
            
            MSDFErrorCorrection.Correct(output, shape, transformation, config.ErrorCorrection);
        }

        // Overloads with Range, Scale, Translate
        public static void GenerateSDF(Bitmap<float> output, Shape shape, Projection projection, Range range, GeneratorConfig config)
        {
            GenerateSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), config);
        }
        
         public static void GenerateMSDF(Bitmap<float> output, Shape shape, Projection projection, Range range, MSDFGeneratorConfig config)
        {
            GenerateMSDF(output, shape, new SDFTransformation(projection, new DistanceMapping(range)), config);
        }
    }
}
