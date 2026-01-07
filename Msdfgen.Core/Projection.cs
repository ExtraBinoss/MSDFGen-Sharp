namespace Msdfgen
{
    public class Projection
    {
        protected Vector2 scale;
        protected Vector2 translate;

        /// <summary>
        /// Initializes a default projection (unit scale, zero translation).
        /// </summary>
        public Projection()
        {
            scale = new Vector2(1, 1);
            translate = new Vector2(0, 0);
        }

        /// <summary>
        /// Initializes a projection with specific scale and translation.
        /// </summary>
        public Projection(Vector2 scale, Vector2 translate)
        {
            this.scale = scale;
            this.translate = translate;
        }

        /// <summary>
        /// The scale factor of the projection.
        /// </summary>
        public Vector2 Scale => scale;

        /// <summary>
        /// The translation offset of the projection.
        /// </summary>
        public Vector2 Translate => translate;

        /// <summary>
        /// Projects a coordinates by adding translation and then scaling.
        /// </summary>
        public Vector2 Project(Vector2 coord)
        {
            return scale * (coord + translate);
        }

        /// <summary>
        /// Unprojects a coordinate by dividing by scale and then subtracting translation.
        /// </summary>
        public Vector2 Unproject(Vector2 coord)
        {
            return coord / scale - translate;
        }

        /// <summary>
        /// Projects a vector by scaling it.
        /// </summary>
        public Vector2 ProjectVector(Vector2 vector)
        {
            return scale * vector;
        }

        /// <summary>
        /// Unprojects a vector by dividing it by scale.
        /// </summary>
        public Vector2 UnprojectVector(Vector2 vector)
        {
            return vector / scale;
        }

        /// <summary>
        /// Projects an X coordinate.
        /// </summary>
        public double ProjectX(double x)
        {
            return scale.X * (x + translate.X);
        }

        /// <summary>
        /// Projects a Y coordinate.
        /// </summary>
        public double ProjectY(double y)
        {
            return scale.Y * (y + translate.Y);
        }

        /// <summary>
        /// Unprojects an X coordinate.
        /// </summary>
        public double UnprojectX(double x)
        {
            return x / scale.X - translate.X;
        }

        /// <summary>
        /// Unprojects a Y coordinate.
        /// </summary>
        public double UnprojectY(double y)
        {
            return y / scale.Y - translate.Y;
        }
    }
}
