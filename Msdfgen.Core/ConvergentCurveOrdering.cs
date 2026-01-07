using System;

namespace Msdfgen
{
    /// <summary>
    /// Provides utility methods to determine the winding order or orientation of curves at a corner.
    /// </summary>
    internal static class ConvergentCurveOrdering
    {
        /// <summary>
        /// Simplifies a curve that is degenerate (e.g., control points are at the same location as endpoints).
        /// </summary>
        private static void SimplifyDegenerateCurve(Vector2[] controlPoints, ref int order)
        {
            if (order == 3 && (controlPoints[1] == controlPoints[0] || controlPoints[1] == controlPoints[3]) && (controlPoints[2] == controlPoints[0] || controlPoints[2] == controlPoints[3]))
            {
                controlPoints[1] = controlPoints[3];
                order = 1;
            }
            if (order == 2 && (controlPoints[1] == controlPoints[0] || controlPoints[1] == controlPoints[2]))
            {
                controlPoints[1] = controlPoints[2];
                order = 1;
            }
            if (order == 1 && controlPoints[0] == controlPoints[1])
                order = 0;
        }

        /// <summary>
        /// Determines the orientation relationship between two segments meeting at a corner.
        /// </summary>
        private static int Ordering(Vector2[] p, int cornerIndex, int controlPointsBefore, int controlPointsAfter) {
            if (!(controlPointsBefore > 0 && controlPointsAfter > 0))
                return 0;
            
            // wrappers for pointer arithmetic
            Vector2 Get(int offset) => p[cornerIndex + offset];

            Vector2 a1, a2, a3 = new Vector2(), b1, b2, b3 = new Vector2();
            a1 = Get(-1) - Get(0);
            b1 = Get(1) - Get(0);

            if (controlPointsBefore >= 2)
                a2 = Get(-2) - Get(-1) - a1;
            else a2 = new Vector2(); // Init

            if (controlPointsAfter >= 2)
                b2 = Get(2) - Get(1) - b1;
            else b2 = new Vector2();

            if (controlPointsBefore >= 3)
            {
                a3 = Get(-3) - Get(-2) - (Get(-2) - Get(-1)) - a2;
                a2 *= 3;
            }
            if (controlPointsAfter >= 3)
            {
                b3 = Get(3) - Get(2) - (Get(2) - Get(1)) - b2;
                b2 *= 3;
            }

            a1 *= controlPointsBefore;
            b1 *= controlPointsAfter;

            // Non-degenerate case
            if (a1.X != 0 || a1.Y != 0)
            {
                bool b1Valid = b1.X != 0 || b1.Y != 0;
                if (b1Valid) {
                     double @as = a1.Length();
                     double bs = b1.Length();
                     // Third derivative
                     double d3 = @as * Vector2.CrossProduct(a1, b2) + bs * Vector2.CrossProduct(a2, b1);
                     if (d3 != 0) return Arithmetics.Sign(d3);
                     // Fourth derivative
                     double d4 = @as * @as * Vector2.CrossProduct(a1, b3) + @as * bs * Vector2.CrossProduct(a2, b2) + bs * bs * Vector2.CrossProduct(a3, b1);
                     if (d4 != 0) return Arithmetics.Sign(d4);
                     // Fifth derivative
                     double d5 = @as * Vector2.CrossProduct(a2, b3) + bs * Vector2.CrossProduct(a3, b2);
                     if (d5 != 0) return Arithmetics.Sign(d5);
                     // Sixth derivative
                     return Arithmetics.Sign(Vector2.CrossProduct(a3, b3));
                }
            }

            // Degenerate checks...
            int s = 1;
            bool a1Valid = a1.X != 0 || a1.Y != 0;
            bool b1Valid2 = b1.X != 0 || b1.Y != 0;

            if (a1Valid && !b1Valid2) { 
                b1 = a1;
                
                Vector2 temp = b2;
                a1 = b2; 
                b2 = a2;
                a2 = temp; 
                
                temp = b3;
                a1 = b3; 
                b3 = a3;
                a3 = temp;
                
                s = -1;
                
                 a1Valid = a1.X != 0 || a1.Y != 0;
                 b1Valid2 = b1.X != 0 || b1.Y != 0;
            }
            
            if (b1Valid2) { 
                 double d;
                 d = Vector2.CrossProduct(a3, b1);
                 if (d != 0) return s * Arithmetics.Sign(d);
                 
                 d = Vector2.CrossProduct(a2, b2);
                 if (d != 0) return s * Arithmetics.Sign(d);

                 d = Vector2.CrossProduct(a3, b2);
                 if (d != 0) return s * Arithmetics.Sign(d);

                 d = Vector2.CrossProduct(a2, b3);
                 if (d != 0) return s * Arithmetics.Sign(d);
                 
                 return s * Arithmetics.Sign(Vector2.CrossProduct(a3, b3));
            }
            
            // Both degenerate
            {
                 double d = Math.Sqrt(a2.Length()) * Vector2.CrossProduct(a2, b3) + Math.Sqrt(b2.Length()) * Vector2.CrossProduct(a3, b2);
                 if (d != 0) return Arithmetics.Sign(d);
                 return Arithmetics.Sign(Vector2.CrossProduct(a3, b3));
            }
        }

        /// <summary>
        /// Determines the convergent curve ordering for two edge segments meeting at a corner.
        /// </summary>
        /// <param name="a">The incoming edge segment.</param>
        /// <param name="b">The outgoing edge segment.</param>
        /// <returns>An integer representing the ordering/orientation sign.</returns>
        public static int Find(EdgeSegment a, EdgeSegment b)
        {
            Vector2[] controlPoints = new Vector2[12];
            int cornerIdx = 4;
            int aCpTmpIdx = 8;
            
            int aOrder = a.Type;
            int bOrder = b.Type;
            
            if (!(aOrder >= 1 && aOrder <= 3 && bOrder >= 1 && bOrder <= 3))
                return 0;
            
            for (int i = 0; i <= aOrder; ++i)
                controlPoints[aCpTmpIdx + i] = a.ControlPoints[i];
            for (int i = 0; i <= bOrder; ++i)
                controlPoints[cornerIdx + i] = b.ControlPoints[i];
                
            if (controlPoints[aCpTmpIdx + aOrder] != controlPoints[cornerIdx])
                return 0;
            
            SimplifyDegenerateCurveInPlace(controlPoints, aCpTmpIdx, ref aOrder);
            SimplifyDegenerateCurveInPlace(controlPoints, cornerIdx, ref bOrder);
            
            for (int i = 0; i < aOrder; ++i)
                controlPoints[cornerIdx - aOrder + i] = controlPoints[aCpTmpIdx + i];
                
            return Ordering(controlPoints, cornerIdx, aOrder, bOrder);
        }

        /// <summary>
        /// Simplifies a degenerate curve in-place within a larger control point array.
        /// </summary>
        private static void SimplifyDegenerateCurveInPlace(Vector2[] p, int offset, ref int order)
        {
             if (order == 3 && (p[offset+1] == p[offset] || p[offset+1] == p[offset+3]) && (p[offset+2] == p[offset] || p[offset+2] == p[offset+3])) {
                 p[offset+1] = p[offset+3];
                 order = 1;
             }
             if (order == 2 && (p[offset+1] == p[offset] || p[offset+1] == p[offset+2])) {
                 p[offset+1] = p[offset+2];
                 order = 1;
             }
             if (order == 1 && p[offset] == p[offset+1])
                 order = 0;
        }
    }
}
