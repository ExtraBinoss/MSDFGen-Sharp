namespace Msdfgen
{
    public class Projection
    {
        protected Vector2 scale;
        protected Vector2 translate;

        public Projection()
        {
            scale = new Vector2(1, 1);
            translate = new Vector2(0, 0);
        }

        public Projection(Vector2 scale, Vector2 translate)
        {
            this.scale = scale;
            this.translate = translate;
        }

        public Vector2 Scale => scale;
        public Vector2 Translate => translate;

        public Vector2 Project(Vector2 coord)
        {
            return scale * (coord + translate);
        }

        public Vector2 Unproject(Vector2 coord)
        {
            return coord / scale - translate;
        }

        public Vector2 ProjectVector(Vector2 vector)
        {
            return scale * vector;
        }

        public Vector2 UnprojectVector(Vector2 vector)
        {
            return vector / scale;
        }

        public double ProjectX(double x)
        {
            return scale.X * (x + translate.X);
        }

        public double ProjectY(double y)
        {
            return scale.Y * (y + translate.Y);
        }

        public double UnprojectX(double x)
        {
            return x / scale.X - translate.X;
        }

        public double UnprojectY(double y)
        {
            return y / scale.Y - translate.Y;
        }
    }
}
