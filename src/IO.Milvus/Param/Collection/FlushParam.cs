using IO.Milvus.Exception;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;

namespace IO.Milvus.Param.Collection
{
    public class FlushParam
    {
        public static FlushParam Create(IEnumerable<string> collectionNames)
        {
            var param = new FlushParam();
            if (collectionNames != null)
            {
                foreach (var collectionName in collectionNames)
                {
                    if (!param.CollectionNames.Contains(collectionName))
                    {
                        param.CollectionNames.Add(collectionName);
                    }
                }
            }
            param.Check();

            return param;
        }

        public static FlushParam Create(string collectionName)
        {
            var param = new FlushParam();
            ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));

            if (!param.CollectionNames.Contains(collectionName))
            {
                param.CollectionNames.Add(collectionName);
            }
            param.Check();

            return param;
        }

        public List<string> CollectionNames { get; } = new List<string>();

        [Obsolete("Useless")]
        public bool SyncFlush { get; set; } = true;

        [Obsolete("Useless")]
        public TimeSpan SyncFlushWaitingInterval { get; set; } = TimeSpan.FromMilliseconds(500);

        [Obsolete("Useless")]
        public TimeSpan SyncFlushWaitingTimeout { get; set; } = TimeSpan.FromSeconds(60);

        internal void Check()
        {
            if (CollectionNames.IsEmpty())
            {
                throw new ParamException("CollectionNames can not be empty");
            }

            CollectionNames.ForEach(n => ParamUtils.CheckNullEmptyString(n, "Collection name"));

            if (SyncFlush)
            {
                if (SyncFlushWaitingInterval.Milliseconds > Constant.MAX_WAITING_FLUSHING_INTERVAL)
                {
                    throw new ParamException($"Sync flush waiting interval cannot be larger than {Constant.MAX_WAITING_FLUSHING_INTERVAL}  milliseconds");
                }

                else if (SyncFlushWaitingTimeout.Seconds > Constant.MAX_WAITING_FLUSHING_TIMEOUT)
                {
                    throw new ParamException($"Sync flush waiting timeout cannot be larger than {Constant.MAX_WAITING_FLUSHING_TIMEOUT} seconds");
                }
            }
        }

        public override string ToString()
        {
            return $"{nameof(FlushParam)}{{{nameof(CollectionNames)}=\'{CollectionNames}\', {nameof(SyncFlush)}=\'{SyncFlush}\', {nameof(SyncFlushWaitingInterval)}=\'{SyncFlushWaitingInterval}\', {nameof(SyncFlushWaitingTimeout)}=\'{SyncFlushWaitingTimeout}\', }}";
        }
    }
}
