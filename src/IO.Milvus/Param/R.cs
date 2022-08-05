using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Milvus.Param
{
    public class R<T>
    {
        public System.Exception Exception { get; set; }

        public Status Status { get; set; }

        public T Data { get; set; }

        public static R<T> Failed(System.Exception exception)
        {
            var r = new R<T>()
            {
                Exception = exception
            };

            if (exception is MilvusException milvusEx)
            {
                r.Status = milvusEx.Status;
            }
            else
            {
                r.Status = Status.Unknown;
            }

            return r;
        }

        public static R<T> Failed(ErrorCode errorCode, string msg)
        {
            var r = new R<T>();
            //TODO:Check if it is right
            r.Status = (Status)errorCode;
            r.Exception = new System.Exception(msg);
            return r;
        }

        public static R<T> Failed(ErrorCode errorCode,System.Exception exception)
        {
            return new R<T>()
            {
                //TODO:Check if it is right
                Status = (Status)errorCode,
                Exception = exception
            };
        }

        public static R<T> Failed(Status status, System.Exception exception)
        {
            return new R<T>()
            {
                Status = status ,
                Exception = exception
            };
        }

        public static R<T> Sucess(T data)
        {
            return new R<T>()
            {
                Status = Status.Success,
                Data = data
            };
        }

        public override string ToString()
        {
            if (Exception != null)
            {
                return $"R{{exception={Exception.Message}, status={Status}, data={Data}}}";
            }
            else
            {
                return $"R{{ status={Status}, data={Data}}}";
            }
        }
    }

    /// <summary>
    /// Represents server and client side status code
    /// </summary>
    public enum Status
    {
        // Server side error
        Success = 0,
        UnexpectedError = 1,
        ConnectFailed = 2,
        PermissionDenied = 3,
        CollectionNotExists = 4,
        IllegalArgument = 5,
        IllegalDimension = 7,
        IllegalIndexType = 8,
        IllegalCollectionName = 9,
        IllegalTOPK = 10,
        IllegalRowRecord = 11,
        IllegalVectorID = 12,
        IllegalSearchResult = 13,
        FileNotFound = 14,
        MetaFailed = 15,
        CacheFailed = 16,
        CannotCreateFolder = 17,
        CannotCreateFile = 18,
        CannotDeleteFolder = 19,
        CannotDeleteFile = 20,
        BuildIndexError = 21,
        IllegalNLIST = 22,
        IllegalMetricType = 23,
        OutOfMemory = 24,
        IndexNotExist = 25,
        EmptyCollection = 26,

        // internal error code.
        DDRequestRace = 1000,

        // Client side error
        RpcError = -1,
        ClientNotConnected = -2,
        Unknown = -3,
        VersionMismatch = -4,
        ParamError = -5,
        IllegalResponse = -6
    }
}
