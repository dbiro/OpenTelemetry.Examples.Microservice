using System;
using System.Diagnostics;

namespace Utils.Tracing
{
    public static class NexogenActivitySource
    {
        private static readonly Lazy<ActivitySource> defaultSourceFactory = new Lazy<ActivitySource>(() => new ActivitySource(System.Reflection.Assembly.GetEntryAssembly().GetName().Name, "2.1.3"), isThreadSafe: true);
        public static ActivitySource Default => defaultSourceFactory.Value;        
    }
}
