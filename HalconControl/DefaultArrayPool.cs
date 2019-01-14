using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HalconControl
{
    internal sealed class DefaultArrayPool<T> : ArrayPool<T>
    {
        private const int DefaultMaxArrayLength = 1048576;
        private const int DefaultMaxNumberOfArraysPerBucket = 50;
        private static T[] s_emptyArray;
        private readonly DefaultArrayPool<T>.Bucket[] _buckets;

        internal DefaultArrayPool()
          : this(1048576, 50)
        {
        }

        internal DefaultArrayPool(int maxArrayLength, int maxArraysPerBucket)
        {
            if (maxArrayLength <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxArrayLength));
            if (maxArraysPerBucket <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxArraysPerBucket));
            if (maxArrayLength > 1073741824)
                maxArrayLength = 1073741824;
            else if (maxArrayLength < 16)
                maxArrayLength = 16;
            int id = this.Id;
            DefaultArrayPool<T>.Bucket[] bucketArray = new DefaultArrayPool<T>.Bucket[Utilities.SelectBucketIndex(maxArrayLength) + 1];
            for (int binIndex = 0; binIndex < bucketArray.Length; ++binIndex)
                bucketArray[binIndex] = new DefaultArrayPool<T>.Bucket(Utilities.GetMaxSizeForBucket(binIndex), maxArraysPerBucket, id);
            this._buckets = bucketArray;
        }

        private int Id
        {
            get
            {
                return this.GetHashCode();
            }
        }

        public override T[] Rent(int minimumLength)
        {
            if (minimumLength < 0)
                throw new ArgumentOutOfRangeException(nameof(minimumLength));
            if (minimumLength == 0)
                return DefaultArrayPool<T>.s_emptyArray ?? (DefaultArrayPool<T>.s_emptyArray = new T[0]);
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            int index1 = Utilities.SelectBucketIndex(minimumLength);
            T[] objArray1;
            if (index1 < this._buckets.Length)
            {
                int index2 = index1;
                do
                {
                    T[] objArray2 = this._buckets[index2].Rent();
                    if (objArray2 != null)
                    {
                        if (log.IsEnabled())
                            log.BufferRented(objArray2.GetHashCode(), objArray2.Length, this.Id, this._buckets[index2].Id);
                        return objArray2;
                    }
                }
                while (++index2 < this._buckets.Length && index2 != index1 + 2);
                objArray1 = new T[this._buckets[index1]._bufferLength];
            }
            else
                objArray1 = new T[minimumLength];
            if (log.IsEnabled())
            {
                int hashCode = objArray1.GetHashCode();
                int bucketId = -1;
                log.BufferRented(hashCode, objArray1.Length, this.Id, bucketId);
                log.BufferAllocated(hashCode, objArray1.Length, this.Id, bucketId, index1 >= this._buckets.Length ? ArrayPoolEventSource.BufferAllocatedReason.OverMaximumSize : ArrayPoolEventSource.BufferAllocatedReason.PoolExhausted);
            }
            return objArray1;
        }

        public override void Return(T[] array, bool clearArray = false)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (array.Length == 0)
                return;
            int index = Utilities.SelectBucketIndex(array.Length);
            if (index < this._buckets.Length)
            {
                if (clearArray)
                    Array.Clear((Array)array, 0, array.Length);
                this._buckets[index].Return(array);
            }
            ArrayPoolEventSource log = ArrayPoolEventSource.Log;
            if (!log.IsEnabled())
                return;
            log.BufferReturned(array.GetHashCode(), array.Length, this.Id);
        }

        private sealed class Bucket
        {
            internal readonly int _bufferLength;
            private readonly T[][] _buffers;
            private readonly int _poolId;
            private SpinLock _lock;
            private int _index;

            internal Bucket(int bufferLength, int numberOfBuffers, int poolId)
            {
                this._lock = new SpinLock(Debugger.IsAttached);
                this._buffers = new T[numberOfBuffers][];
                this._bufferLength = bufferLength;
                this._poolId = poolId;
            }

            internal int Id
            {
                get
                {
                    return this.GetHashCode();
                }
            }

            internal T[] Rent()
            {
                T[][] buffers = this._buffers;
                T[] objArray = (T[])null;
                bool lockTaken = false;
                bool flag = false;
                try
                {
                    this._lock.Enter(ref lockTaken);
                    if (this._index < buffers.Length)
                    {
                        objArray = buffers[this._index];
                        buffers[this._index++] = (T[])null;
                        flag = objArray == null;
                    }
                }
                finally
                {
                    if (lockTaken)
                        this._lock.Exit(false);
                }
                if (flag)
                {
                    objArray = new T[this._bufferLength];
                    ArrayPoolEventSource log = ArrayPoolEventSource.Log;
                    if (log.IsEnabled())
                        log.BufferAllocated(objArray.GetHashCode(), this._bufferLength, this._poolId, this.Id, ArrayPoolEventSource.BufferAllocatedReason.Pooled);
                }
                return objArray;
            }

            internal void Return(T[] array)
            {
                if (array.Length != this._bufferLength)
                    throw new ArgumentException("The buffer is not associated with this pool and may not be returned to it.", nameof(array));
                bool lockTaken = false;
                try
                {
                    this._lock.Enter(ref lockTaken);
                    if (this._index == 0)
                        return;
                    this._buffers[--this._index] = array;
                }
                finally
                {
                    if (lockTaken)
                        this._lock.Exit(false);
                }
            }
        }
    }
}
