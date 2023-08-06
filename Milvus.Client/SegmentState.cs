namespace Milvus.Client;

#pragma warning disable CS1591 // Missing documentation for the below

public enum SegmentState
{
    None = Grpc.SegmentState.None,
    NotExist = Grpc.SegmentState.NotExist,
    Growing = Grpc.SegmentState.Growing,
    Sealed = Grpc.SegmentState.Sealed,
    Flushed = Grpc.SegmentState.Flushed,
    Flushing = Grpc.SegmentState.Flushing,
    Dropped = Grpc.SegmentState.Dropped,
    Importing = Grpc.SegmentState.Importing,
}
