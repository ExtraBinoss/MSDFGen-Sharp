using System;

namespace Msdfgen
{
    internal static class ConvergentCurveOrdering
    {
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

        public static int Find(Vector2[] corner, int controlPointsBefore, int controlPointsAfter) {
             // Accessing relative to corner pointer in C++: corner[-1] is controlPointsBefore corner.
             // Here we pass the full array and an index? Or just assume the array layout is specifically prepared.
             // The C++ code prepares a `controlPoints` array of size 12. `corner` points to `controlPoints + 4`.
             // So `corner[-1]` is `controlPoints[3]`, `corner[0]` is `controlPoints[4]`.
             // We can pass the array and offset.
             // But the signature here is just the logic.
             // Let's adapt the C# signature to take the central index.
             
             // However, to keep it simple, I will replicate the C++ Call structure:
             // It builds a 12-element array and passes logic to a core function.
             // I will duplicate that behavior inside the public method.
             throw new NotImplementedException("Helper method requires specific memory layout logic. See public method below.");
        }

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
            if (a1)
            {
                // C++ operator bool() checks if x!=0 or y!=0.
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
             // Degenerate curve after corner (control point after corner equals corner)
            int s = 1;
            bool a1Valid = a1.X != 0 || a1.Y != 0;
            bool b1Valid2 = b1.X != 0 || b1.Y != 0;

            if (a1Valid && !b1Valid2) { // a1 && !b1
                // Swap aN <-> bN and handle in if (b1)
                b1 = a1;
                a1 = b2; b2 = a2; a2 = a1; // wait, swap a1/b2?
                // C++:
                // b1 = a1;
                // a1 = b2, b2 = a2, a2 = a1; // This logic swaps a1 with b2, then b2 with a2, then a2 with a1... wait.
                // comma operator has sequence points.
                // a1 = b2; -> a1 takes b2's value.
                // b2 = a2; -> b2 takes a2's value.
                // a2 = a1; -> a2 takes NEW a1's value (which is old b2).
                // Effectively: swap(b2, a2) then assignment? No.
                // Let's trace C++: a1 = b2, b2 = a2, a2 = a1;
                // temp = b2; b2 = a2; a2 = temp; a1 = temp; (Wait, a2=a1 means a2 gets b2).
                // Correct swap of (a2, b2) should be standard swap. 
                // But a1 is assigned b2 first.
                // The intent is likely swapping the sides A and B properties.
                
                // Let's use simple semantic swap.
                // a1 (the tangent) is valid. b1 is 0.
                // We pretend we are looking at B side having valid tangent, and flip result.
                Vector2 tmp;
                tmp = a1; a1 = b1; b1 = tmp; // b1 now valid, a1 zero.
                tmp = a2; a2 = b2; b2 = tmp;
                tmp = a3; a3 = b3; b3 = tmp;
                
                // Correct pointer mapping from original code:
                // b1 = a1; (b1 gets A's 1st deriv)
                // a1 = b2; (a1 gets B's 2nd deriv - wait, B's 2nd is b2).
                // b2 = a2; (b2 gets A's 2nd deriv)
                // a2 = a1; (a2 gets B's 2nd deriv which was in a1).
                // Wait, C++ `a1 = b2, b2 = a2, a2 = a1` is standard swap of `a2` and `b2` IF `a1` was used as temp.
                // Yes, `a1` is overwritten by `b2`. Then `b2` takes `a2`. Then `a2` takes `a1` (old `b2`).
                // So `a2` and `b2` are swapped. `a1` ends up with old `b2`.
                // BUT `b1` got old `a1`.
                // So `a1` and `b1` are NOT swapped. `b1` gets old `a1`. `a1` gets old `b2`.
                // This seems specific to the degenerate handling logic where derivatives shift orders.
                
                // Retrying detailed logic:
                // Original: 
                // b1 = a1;
                // a1 = b2, b2 = a2, a2 = a1;
                // a1 = b3, b3 = a3, a3 = a1;
                
                // Interpreting:
                // b1_new = a1_old
                // a1_new = b2_old
                // b2_new = a2_old
                // a2_new = a1_new (which is b2_old)
                
                // This essentially shifts:
                // Side B (degenerate):
                //   b1 becomes a1 (first valid deriv from A)
                //   b2 becomes a2
                //   b3 becomes a3
                // Side A (non-degenerate side acting as degenerate now??):
                //   a1 becomes b2
                //   a2 becomes b2
                //   a3 becomes b3
                // Wait, if !b1 (b is degenerate), then b1 is 0.
                // We are swapping roles of A and B but B is degenerate.
                // Actually, let's look at the logic block for `if (b1) { // !a1` below.
                // It checks derivatives of mixed cross products.
                
                // Let's implement EXACTLY what C++ does code-wise.
                
                b1 = a1;
                
                Vector2 temp = b2;
                a1 = b2; // a1 gets old b2
                b2 = a2;
                a2 = temp; // a2 gets old b2.
                // So a1 and a2 get old b2. b2 gets old a2.
                
                temp = b3;
                a1 = b3; 
                b3 = a3;
                a3 = temp;
                
                s = -1;
                
                // Update validity
                 a1Valid = a1.X != 0 || a1.Y != 0;
                 b1Valid2 = b1.X != 0 || b1.Y != 0;
            }
            
            if (b1Valid2) { // !a1
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

        public static int Find(EdgeSegment a, EdgeSegment b)
        {
            Vector2[] controlPoints = new Vector2[12];
            // corner is at index 4 (0-based)
            int cornerIdx = 4;
            // aCpTmp is at index 8
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
            
            // Simplify degenerate logic operating on subarrays
            // We need to pass subarray reference or indices.
            // Let's implement SimplifyDegenerateCurve receiving the array and offset.
            SimplifyDegenerateCurveInPlace(controlPoints, aCpTmpIdx, ref aOrder);
            SimplifyDegenerateCurveInPlace(controlPoints, cornerIdx, ref bOrder);
            
            for (int i = 0; i < aOrder; ++i)
                controlPoints[cornerIdx - aOrder + i] = controlPoints[aCpTmpIdx + i];
                
            return Ordering(controlPoints, cornerIdx, aOrder, bOrder);
        }

        private static void SimplifyDegenerateCurveInPlace(Vector2[] p, int offset, ref int order)
        {
             // Mapping: p[offset] is controlPoints[0]
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
