using System;
using System.Diagnostics;

namespace EF_Split_Projector.Helpers
{
    public static class Logging
    {
        public static bool Enabled { get { return EFSplitProjectorSection.Diagnostics != EFSplitProjectorSection.DiagnosticType.Disabled; } }

        internal static void Start(string label)
        {
            if(Enabled)
            {
                _current = new Timer(_current, label);
            }
        }

        internal static void Stop()
        {
            if(Enabled && _current != null)
            {
                _current = _current.Stop();
            }
        }

        internal static T Stop<T>(T t)
        {
            Stop();
            return t;
        }

        private static Timer _current;

        private class Timer
        {
            private readonly string _label;
            private readonly string _indent;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
            private readonly Timer _parent;
            private TimeSpan _childTime = new TimeSpan(0);
            private bool _headerWritten;

            internal Timer(Timer parent, string label)
            {
                if((_parent = parent) != null)
                {
                    _parent.WriteHeader();
                }
                _label = string.Format("{0}{1}", _indent = _parent == null ? "" : _parent._indent + " ", label);
            }

            internal Timer Stop(bool writeOutput = true)
            {
                _stopwatch.Stop();
                var elapsed = _stopwatch.Elapsed;
                
                if(writeOutput)
                {
                    if(_childTime.Ticks != 0)
                    {
                        var percent = ((1.0 - (elapsed.Ticks == 0 ? 0.0 : (double)_childTime.Ticks / elapsed.Ticks)) * 100.0);
                        Console.WriteLine("{0}: %{1} x [{2}]", _label, percent.ToString("0.00"), elapsed);
                    }
                    else
                    {
                        Console.WriteLine("{0}: [{1}]", _label, elapsed);
                    }

                    if(_parent != null)
                    {
                        _parent._childTime += elapsed;
                    }
                }
                
                return _parent;
            }

            private void WriteHeader()
            {
                if(!_headerWritten)
                {
                    Console.WriteLine(_label);
                    _headerWritten = true;
                }
            }
        }
    }
}