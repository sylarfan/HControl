using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalconControl
{
    [EventSource(Name = "System.Buffers.ArrayPoolEventSource")]
    internal sealed class ArrayPoolEventSource : EventSource
    {
        internal static readonly ArrayPoolEventSource Log = new ArrayPoolEventSource();

        [Event(1, Level = EventLevel.Verbose)]
        internal unsafe void BufferRented(int bufferId, int bufferSize, int poolId, int bucketId)
        {
            EventSource.EventData* data = stackalloc EventSource.EventData[4];
            data[0] = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bufferId)
            };
            *(data + 1) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bufferSize)
            };
            *(data + 2) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&poolId)
            };
            *(data + 3) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bucketId)
            };
            this.WriteEventCore(1, 4, data);
        }

        [Event(2, Level = EventLevel.Informational)]
        internal unsafe void BufferAllocated(int bufferId, int bufferSize, int poolId, int bucketId, ArrayPoolEventSource.BufferAllocatedReason reason)
        {
            EventSource.EventData* data = stackalloc EventSource.EventData[5];
            data[0] = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bufferId)
            };
            *(data + 1) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bufferSize)
            };
            *(data + 2) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&poolId)
            };
            *(data + 3) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&bucketId)
            };
            *(data + 4) = new EventSource.EventData()
            {
                Size = 4,
                DataPointer = (IntPtr)((void*)&reason)
            };
            this.WriteEventCore(2, 5, data);
        }

        [Event(3, Level = EventLevel.Verbose)]
        internal void BufferReturned(int bufferId, int bufferSize, int poolId)
        {
            this.WriteEvent(3, bufferId, bufferSize, poolId);
        }

        internal enum BufferAllocatedReason
        {
            Pooled,
            OverMaximumSize,
            PoolExhausted,
        }
    }
}
