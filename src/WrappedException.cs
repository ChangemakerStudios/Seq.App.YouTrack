namespace Seq.App.YouTrack
{
    using System;

    class WrappedException : Exception
    {
        readonly string _asString;

        public WrappedException(string asString)
        {
            _asString = asString;
        }

        public override string ToString()
        {
            return _asString;
        }
    }
}