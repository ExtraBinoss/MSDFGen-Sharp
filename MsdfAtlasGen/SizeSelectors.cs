using System;

namespace MsdfAtlasGen
{
    public interface ISizeSelector
    {
        bool GetDimensions(out int width, out int height);
        void Next();
        void Previous();
    }

    public class SquareSizeSelector : ISizeSelector
    {
        private int _lowerBound;
        private int _upperBound;
        private int _current;
        private readonly int _multiple;

        public SquareSizeSelector(int minArea = 0, int multiple = 1)
        {
            _multiple = multiple;
            _lowerBound = 0;
            _upperBound = -1;
            if (minArea > 0)
                _lowerBound = (int)(Math.Sqrt(minArea - 1)) / _multiple + 1;
            UpdateCurrent();
        }

        private void UpdateCurrent()
        {
            if (_upperBound < 0)
                _current = 5 * _lowerBound / 4 + 16 / _multiple + 1;
            else
                _current = _lowerBound + (_upperBound - _lowerBound) / 2;
        }

        public bool GetDimensions(out int width, out int height)
        {
            width = _multiple * _current;
            height = _multiple * _current;
            return _lowerBound < _upperBound || _upperBound < 0;
        }

        public void Next()
        {
            _lowerBound = _current + 1;
            UpdateCurrent();
        }

        public void Previous()
        {
            _upperBound = _current;
            UpdateCurrent();
        }
    }

    public class SquarePowerOfTwoSizeSelector : ISizeSelector
    {
        private int _side;

        public SquarePowerOfTwoSizeSelector(int minArea = 0)
        {
            _side = 1;
            while (_side * _side < minArea)
                _side <<= 1;
        }

        public bool GetDimensions(out int width, out int height)
        {
            width = _side;
            height = _side;
            return _side > 0;
        }

        public void Next()
        {
            _side <<= 1;
        }

        public void Previous()
        {
            _side = 0;
        }
    }

    public class PowerOfTwoSizeSelector : ISizeSelector
    {
        private int _w, _h;

        public PowerOfTwoSizeSelector(int minArea = 0)
        {
            _w = 1;
            _h = 1;
            while (_w * _h < minArea)
                Next();
        }

        public bool GetDimensions(out int width, out int height)
        {
            width = _w;
            height = _h;
            return _w > 0 && _h > 0;
        }

        public void Next()
        {
            if (_w == _h)
                _w <<= 1;
            else
                _h = _w;
        }

        public void Previous()
        {
            _w = 0;
            _h = 0;
        }
    }
}
