// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Diagnostics.Tracing;

namespace osu.Framework.Statistics
{
    // https://medium.com/criteo-labs/c-in-process-clr-event-listeners-with-net-core-2-2-ef4075c14e87
    internal sealed class DotNetRuntimeListener : EventListener
    {
        private const int gc_keyword = 0x0000001;

        private const string statistics_grouping = "GC";

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Microsoft-Windows-DotNETRuntime")
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)gc_keyword);
        }

        protected override void OnEventWritten(EventWrittenEventArgs data)
        {
            switch ((EventType)data.EventId)
            {
                case EventType.GCStart_V1 when data.Payload != null:
                    // https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcstart_v1_event
                    GlobalStatistics.Get<int>(statistics_grouping, $"Collections Gen{data.Payload[1]}").Value++;
                    break;

                case EventType.GCHeapStats_V1 when data.Payload != null:
                    // https://docs.microsoft.com/en-us/dotnet/framework/performance/garbage-collection-etw-events#gcheapstats_v1_event
                    for (int i = 0; i <= 6; i += 2)
                        addStatistic<ulong>($"Size Gen{i / 2}", data.Payload[i]);

                    addStatistic<ulong>("Finalization queue length", data.Payload[9]);
                    addStatistic<uint>("Pinned objects", data.Payload[10]);
                    break;
            }
        }

        private void addStatistic<T>(string name, object data)
            => GlobalStatistics.Get<T>(statistics_grouping, name).Value = (T)data;

        private enum EventType
        {
            GCStart_V1 = 1,
            GCHeapStats_V1 = 4,
        }
    }
}
