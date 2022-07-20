using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using IO.Milvus.Exception;
using IO.Milvus.Grpc;
using IO.Milvus.Param;
using IO.Milvus.Param.Alias;
using IO.Milvus.Param.Collection;
using IO.Milvus.Param.Control;
using IO.Milvus.Param.Credential;
using IO.Milvus.Param.Dml;
using IO.Milvus.Param.Index;
using IO.Milvus.Param.Partition;
using IO.Milvus.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IO.Milvus.Client
{
    public abstract class AbstractMilvusGrpcClient : IMilvusClient
    {
        #region Fields
        protected GrpcChannel channel;
        protected MilvusService.MilvusServiceClient client;
        protected CallOptions defaultCallOptions;
        private TimeSpan? defaultTimeOut;
        #endregion

        #region Public Methods
        public bool ClientIsReady()
        {
#if NET461_OR_GREATER
            return true;
#else
            return channel.State != ConnectivityState.Shutdown;
#endif
        }

        public void Close()
        {
            channel.ShutdownAsync().Wait();
            channel.Dispose();
        }

        public async Task CloseAsync()
        {
            await channel.ShutdownAsync();
            channel.Dispose();
        }

        public IMilvusClient WithTimeout(TimeSpan timeSpan)
        {
            defaultTimeOut = timeSpan;

            return this;
        }
        #endregion

        #region Private Methods
        private List<Grpc.KeyValuePair> AssembleKvPair(Dictionary<string,string> sourceDic)
        {
            var result = new List<Grpc.KeyValuePair>();
            if (sourceDic.IsNotEmpty())
            {
                foreach (var kv in sourceDic)
                {
                    result.Add(new Grpc.KeyValuePair()
                    {
                        Key = kv.Key,
                        Value = kv.Value
                    });
                }
            }
            return result;
        }

        private CallOptions WithInternalOptions()
        {
            if (defaultTimeOut != null)
            {
                var options = defaultCallOptions;
                options.WithDeadline(DateTime.UtcNow.AddSeconds(defaultTimeOut.Value.TotalSeconds));
            }
            return defaultCallOptions;
        }

        private R<T> FailedStatus<T>(string requestName, IO.Milvus.Grpc.Status status)
        {
            var reason = status.Reason;
            if (string.IsNullOrEmpty(reason))
            {
                reason = $"error code: {status.ErrorCode}";
            }
            //logError(requestName + " failed:\n{}", reason);
            return R<T>.Failed(status.ErrorCode, reason);
        }
        #endregion

        #region Api Methods

        #region Collection
        ///<inheritdoc/>
        public R<ShowCollectionsResponse> ShowCollections(ShowCollectionsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<ShowCollectionsResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new ShowCollectionsRequest()
                {
                    Type = requestParam.ShowType
                };
                requestParam.CollectionNames.AddRange(requestParam.CollectionNames);

                var response = client.ShowCollections(request,callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<ShowCollectionsResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<ShowCollectionsResponse>(nameof(ShowCollectionsRequest),response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<ShowCollectionsResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> CreateCollection(CreateCollectionParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var schema = new CollectionSchema()
                {
                    Name = requestParam.CollectionName,
                    Description = requestParam.Description,
                };

                long fieldID = 0;
                foreach (var fieldType in requestParam.FieldTypes)
                {
                    var field = new FieldSchema()
                    {
                        FieldID = fieldID++,
                        Name = fieldType.Name,
                        IsPrimaryKey = fieldType.IsPrimaryKey,
                        Description = fieldType.Description,
                        DataType = fieldType.DataType,
                        AutoID = fieldType.IsAutoID,
                    };

                    var typeParamsList = AssembleKvPair(fieldType.TypeParams);

                    foreach (var item in typeParamsList)
                    {
                        field.TypeParams.Add(item);
                    }

                    schema.Fields.Add(field);
                }
                
                var request = new CreateCollectionRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    ShardsNum = requestParam.ShardsNum,
                    Schema = schema.ToByteString()
                };

                var response = client.CreateCollection(request,callOptions ?? WithInternalOptions());

                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(CreateCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> DropCollection(DropCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return DropCollection(requestParam.CollectionName, callOptions);
        }

        ///<inheritdoc/>
        public R<RpcStatus> DropCollection(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));

                var request = new DropCollectionRequest()
                {
                    CollectionName = collectionName,
                };

                var response = client.DropCollection(request,callOptions ?? WithInternalOptions());

                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(DropCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<bool> HasCollection(HasCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return HasCollection(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<bool> HasCollection(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<bool>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));

                var hasCollectionRequest = new HasCollectionRequest()
                {
                    CollectionName = collectionName,
                };

                var response = client.HasCollection(hasCollectionRequest);

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<bool>.Sucess(response.Value);
                }
                else
                {
                    return FailedStatus<bool>(nameof(HasCollectionRequest), response.Status);
                }
            }
            catch (System.Exception ex)
            {
                return R<bool>.Failed(ex);
            }
        }

        ///<inheritdoc/>
        public R<DescribeCollectionResponse> DescribeCollection(DescribeCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return DescribeCollection(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<DescribeCollectionResponse> DescribeCollection(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<DescribeCollectionResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new DescribeCollectionRequest()
                {
                    CollectionName = collectionName
                };
                var response = client.DescribeCollection(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<DescribeCollectionResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<DescribeCollectionResponse>(
                        nameof(DescribeCollectionRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<DescribeCollectionResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetCollectionStatisticsResponse> GetCollectionStatistics(
            GetCollectionStatisticsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetCollectionStatisticsResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                if (requestParam.IsFlushCollection)
                {
                    var flushResponse = Flush(FlushParam.Create(requestParam.CollectionName));
                    if (flushResponse.Status != Param.Status.Success)
                    {
                        return R<GetCollectionStatisticsResponse>.Failed((ErrorCode)flushResponse.Status, flushResponse.Exception?.Message);
                    }
                }

                var request = new GetCollectionStatisticsRequest()
                {
                    CollectionName = requestParam.CollectionName,
                };

                var response = client.GetCollectionStatistics(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetCollectionStatisticsResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetCollectionStatisticsResponse>(nameof(GetCollectionStatisticsRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
               return R<GetCollectionStatisticsResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> LoadCollection(LoadCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return LoadCollection(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<RpcStatus> LoadCollection(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {

                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new LoadCollectionRequest()
                {
                    CollectionName = collectionName
                };

                var response = client.LoadCollection(request,callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<RpcStatus>> LoadCollectionAsync(LoadCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return await LoadCollectionAsync(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public async Task<R<RpcStatus>> LoadCollectionAsync(string collectionName,CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new LoadCollectionRequest()
                {
                    CollectionName = collectionName
                };

                var response = await client.LoadCollectionAsync(request,callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> ReleaseCollection(ReleaseCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return ReleaseCollection(requestParam.CollectionName, callOptions);
        }

        ///<inheritdoc/>
        public R<RpcStatus> ReleaseCollection(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new ReleaseCollectionRequest()
                {
                    CollectionName = collectionName
                };

                var response = client.ReleaseCollection(request,callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<RpcStatus>> ReleaseCollectionAsync(ReleaseCollectionParam requestParam, CallOptions? callOptions = null)
        {
            return await ReleaseCollectionAsync(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public async Task<R<RpcStatus>> ReleaseCollectionAsync(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new ReleaseCollectionRequest()
                {
                    CollectionName = collectionName
                };

                var response = await client.ReleaseCollectionAsync(request,callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest), response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }
        #endregion

        #region Partition
        ///<inheritdoc/>
        public R<RpcStatus> CreatePartition(CreatePartitionParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new CreatePartitionRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName,
                };

                var response = client.CreatePartition(request,callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> DropPartition(DropPartitionParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new DropPartitionRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName,
                };

                var response = client.DropPartition(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<bool> HasPartition(HasPartitionParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<bool>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new HasPartitionRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName,
                };

                var response = client.HasPartition(request, callOptions ?? WithInternalOptions());
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<bool>.Sucess(response.Value);
                }
                else
                {
                    return FailedStatus<bool>(nameof(LoadCollectionRequest),response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<bool>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> LoadPartitions(LoadPartitionsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new LoadPartitionsRequest()
                {
                    CollectionName = requestParam.CollectionName,
                };
                request.PartitionNames.AddRange(requestParam.PartitionNames);

                var response = client.LoadPartitions(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),new Grpc.Status() 
                    { ErrorCode = response.ErrorCode,
                        Reason = response.Reason});
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> ReleasePartitions(ReleasePartitionsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new ReleasePartitionsRequest()
                {
                    CollectionName = requestParam.CollectionName,
                };
                request.PartitionNames.AddRange(requestParam.PartitionNames);

                var response = client.ReleasePartitions(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest), new Grpc.Status()
                    {
                        ErrorCode = response.ErrorCode,
                        Reason = response.Reason
                    });
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetPartitionStatisticsResponse> GetPartitionStatistics(GetPartitionStatisticsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetPartitionStatisticsResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                if (requestParam.IsFulshCollection)
                {
                    var flushResponse = Flush(FlushParam.Create(requestParam.CollectionName));
                    if (flushResponse.Status != Param.Status.Success)
                    {
                        return R<GetPartitionStatisticsResponse>.Failed((ErrorCode)flushResponse.Status,flushResponse.Exception);
                    }
                }
                
                var request = new GetPartitionStatisticsRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName
                };

                var response = client.GetPartitionStatistics(request, callOptions ?? WithInternalOptions());
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetPartitionStatisticsResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetPartitionStatisticsResponse>(nameof(GetPartitionStatisticsRequest),response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetPartitionStatisticsResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<ShowPartitionsResponse> ShowPartitions(ShowPartitionsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<ShowPartitionsResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new ShowPartitionsRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Type = requestParam.ShowType,
                };
                request.PartitionNames.Add(requestParam.PartitionNames);

                var response = client.ShowPartitions(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<ShowPartitionsResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<ShowPartitionsResponse>(
                        nameof(DescribeCollectionRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<ShowPartitionsResponse>.Failed(e);
            }
        }
        #endregion

        #region Alias
        ///<inheritdoc/>
        public R<RpcStatus> CreateAlias(CreateAliasParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new CreateAliasRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Alias = requestParam.Alias,                    
                };

                var response = client.CreateAlias(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> AlterAlias(AlterAliasParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new AlterAliasRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Alias = requestParam.Alias,
                };

                var response = client.AlterAlias(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> DropAlias(DropAliasParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new DropAliasRequest()
                {                    
                    Alias = requestParam.Alias,
                };

                var response = client.DropAlias(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }
        #endregion

        #region Data
        ///<inheritdoc/>
        public R<MutationResult> Insert(InsertParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<MutationResult>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new InsertRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName,
                    NumRows = requestParam.RowCount,
                };
                request.FieldsData.AddRange(requestParam.Fields.Select(p => p.ToGrpcFieldData()));

                var response = client.Insert(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<MutationResult>.Sucess(response);
                }
                else
                {
                    return FailedStatus<MutationResult>(
                        nameof(MutationResult),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<MutationResult>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<MutationResult>> InsertAsync(InsertParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<MutationResult>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new InsertRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    PartitionName = requestParam.PartitionName,
                };
                request.FieldsData.AddRange(requestParam.Fields.Select(p => p.ToGrpcFieldData()));

                var response = await client.InsertAsync(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<MutationResult>.Sucess(response);
                }
                else
                {
                    return FailedStatus<MutationResult>(
                        nameof(MutationResult),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<MutationResult>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<MutationResult> Delete(DeleteParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<MutationResult>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new DeleteRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Expr = requestParam.Expr,
                    PartitionName = requestParam.PartitionName,
                };

                var response = client.Delete(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<MutationResult>.Sucess(response);
                }
                else
                {
                    return FailedStatus<MutationResult>(
                        nameof(DeleteRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<MutationResult>.Failed(e);
            }
        }

        public R<GetCompactionStateResponse> GetCompactionState(GetCompactionStateParam requestParam, CallOptions? callOptions = null)
        {
            return GetCompactionState(requestParam.CompactionID);
        }

        ///<inheritdoc/>
        public R<GetCompactionStateResponse> GetCompactionState(long compactionID, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetCompactionStateResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                var request = new GetCompactionStateRequest()
                {
                    CompactionID = compactionID
                };

                var response = client.GetCompactionState(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetCompactionStateResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetCompactionStateResponse>(
                        nameof(GetCompactionStateRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetCompactionStateResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetCompactionPlansResponse> GetCompactionStateWithPlans(GetCompactionPlansParam requestParam, CallOptions? callOptions = null)
        {
            return GetCompactionStateWithPlans(requestParam.CompactionID, callOptions);
        }

        ///<inheritdoc/>
        public R<GetCompactionPlansResponse> GetCompactionStateWithPlans(long compactionID, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetCompactionPlansResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                var request = new GetCompactionPlansRequest()
                {
                    CompactionID = compactionID
                };

                var response = client.GetCompactionStateWithPlans(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetCompactionPlansResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetCompactionPlansResponse>(
                        nameof(GetCompactionStateRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetCompactionPlansResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<FlushResponse> Flush(FlushParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<FlushResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                var request = new FlushRequest();
                request.CollectionNames.AddRange(requestParam.CollectionNames);

                var response = client.Flush(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<FlushResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<FlushResponse>(nameof(FlushRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<FlushResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<FlushResponse>> FlushAsync(FlushParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<FlushResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                var request = new FlushRequest();
                request.CollectionNames.AddRange(requestParam.CollectionNames);

                var response = await client.FlushAsync(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<FlushResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<FlushResponse>(nameof(FlushRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<FlushResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetFlushStateResponse> GetFlushState(GetFlushStateParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetFlushStateResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                var request = new GetFlushStateRequest();
                request.SegmentIDs.AddRange(requestParam.SegmentIDs);

                var response = client.GetFlushState(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetFlushStateResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetFlushStateResponse>(nameof(GetFlushStateRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetFlushStateResponse>.Failed(e);
            }
        }
        #endregion

        #region Index
        ///<inheritdoc/>
        public R<RpcStatus> CreateIndex(CreateIndexParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new CreateIndexRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    FieldName = requestParam.FieldName,
                };
                request.ExtraParams.AddRange(requestParam.ExtraDic.Select(p => new Grpc.KeyValuePair()
                {
                    Key = p.Key,
                    Value = p.Value
                }));

                var response = client.CreateIndex(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> DropIndex(DropIndexParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new DropIndexRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    IndexName = requestParam.IndexName,
                };

                var response = client.DropIndex(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<DescribeIndexResponse> DescribeIndex(DescribeIndexParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<DescribeIndexResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                var request = new DescribeIndexRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    IndexName = requestParam.IndexName,
                };

                var response = client.DescribeIndex(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<DescribeIndexResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<DescribeIndexResponse>(nameof(DescribeIndexRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<DescribeIndexResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetIndexBuildProgressResponse> GetIndexBuildProgress(GetIndexBuildProgressParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetIndexBuildProgressResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();

                var request = new GetIndexBuildProgressRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    IndexName = requestParam.IndexName,
                };

                var response = client.GetIndexBuildProgress(request);
                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetIndexBuildProgressResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetIndexBuildProgressResponse>(nameof(GetIndexBuildProgressRequest), response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetIndexBuildProgressResponse>.Failed(e);
            }
        }
        #endregion

        #region Search and Query
        ///<inheritdoc/>
        public R<QueryResults> Query(QueryParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<QueryResults>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new QueryRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Expr = requestParam.Expr,
                    GuaranteeTimestamp = requestParam.GuaranteeTimestamp,
                    TravelTimestamp = requestParam.TravelTimestamp,                    
                };

                request.OutputFields.AddRange(requestParam.OutFields);
                request.PartitionNames.AddRange(requestParam.PartitionNames);

                var response = client.Query(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<QueryResults>.Sucess(response);
                }
                else
                {
                    return FailedStatus<QueryResults>(
                        nameof(QueryRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<QueryResults>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<QueryResults>> QueryAsync(QueryParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<QueryResults>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new QueryRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    Expr = requestParam.Expr,
                    GuaranteeTimestamp = requestParam.GuaranteeTimestamp,
                    TravelTimestamp = requestParam.TravelTimestamp,
                };

                request.OutputFields.AddRange(requestParam.OutFields);
                request.PartitionNames.AddRange(requestParam.PartitionNames);

                var response = await client.QueryAsync(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<QueryResults>.Sucess(response);
                }
                else
                {
                    return FailedStatus<QueryResults>(
                        nameof(QueryRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<QueryResults>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<SearchResults> Search<TVector>(SearchParam<TVector> requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<SearchResults>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new SearchRequest()
                {
                    CollectionName = requestParam.CollectionName,
                    
                    GuaranteeTimestamp = requestParam.GuaranteeTimestamp,
                    TravelTimestamp = requestParam.TravelTimestamp,
                };

                request.PartitionNames.AddRange(requestParam.PartitionNames);
                request.OutputFields.AddRange(requestParam.OutFields);

                var response = client.Search(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<SearchResults>.Sucess(response);
                }
                else
                {
                    return FailedStatus<SearchResults>(
                        nameof(QueryRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<SearchResults>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public async Task<R<SearchResults>> SearchAsync<TVector>(SearchParam<TVector> requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<SearchResults>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new SearchRequest()
                {
                    CollectionName = requestParam.CollectionName,

                    GuaranteeTimestamp = requestParam.GuaranteeTimestamp,
                    TravelTimestamp = requestParam.TravelTimestamp,
                };

                request.PartitionNames.AddRange(requestParam.PartitionNames);
                request.OutputFields.AddRange(requestParam.OutFields);

                var response = await client.SearchAsync(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<SearchResults>.Sucess(response);
                }
                else
                {
                    return FailedStatus<SearchResults>(
                        nameof(QueryRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<SearchResults>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<CalcDistanceResults> CalcDistance(CalcDistanceParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<CalcDistanceResults>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new CalcDistanceRequest();
                //TODO add cal param
                //request.OpLeft.DataArray = new VectorField()
                //{
                //    FloatVector =
                //};

                var response = client.CalcDistance(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<CalcDistanceResults>.Sucess(response);
                }
                else
                {
                    return FailedStatus<CalcDistanceResults>(
                        nameof(CalcDistanceRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<CalcDistanceResults>.Failed(e);
            }
        }
        #endregion

        #region Credential
        [Obsolete]
        public R<RpcStatus> CreateCredential(CreateCredentialParam requestParam, CallOptions? callOptions = null)
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public R<RpcStatus> DeleteCredential(DeleteCredentialParam requestParam, CallOptions? callOptions = null)
        {
            throw new NotImplementedException();
        }

        [Obsolete]
        public R<RpcStatus> UpdateCredential(UpdateCredentialParam requestParam, CallOptions? callOptions = null)
        {
            throw new NotImplementedException();
        }
        #endregion

        #region Others
        ///<inheritdoc/>
        public R<GetMetricsResponse> GetMetrics(GetMetricsParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetMetricsResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                var request = new GetMetricsRequest()
                {
                    Request = requestParam.Request
                };

                var response = client.GetMetrics(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetMetricsResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetMetricsResponse>(
                        nameof(GetCompactionStateRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetMetricsResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetPersistentSegmentInfoResponse> GetPersistentSegmentInfo(GetPersistentSegmentInfoParam requestParam, CallOptions? callOptions = null)
        {
            return GetPersistentSegmentInfo(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<GetPersistentSegmentInfoResponse> GetPersistentSegmentInfo(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetPersistentSegmentInfoResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new GetPersistentSegmentInfoRequest()
                {
                    CollectionName = collectionName
                };

                var response = client.GetPersistentSegmentInfo(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetPersistentSegmentInfoResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetPersistentSegmentInfoResponse>(
                        nameof(GetPersistentSegmentInfoRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetPersistentSegmentInfoResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<GetQuerySegmentInfoResponse> GetQuerySegmentInfo(GetQuerySegmentInfoParam requestParam, CallOptions? callOptions = null)
        {
            return GetQuerySegmentInfo(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<GetQuerySegmentInfoResponse> GetQuerySegmentInfo(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<GetQuerySegmentInfoResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var request = new GetQuerySegmentInfoRequest()
                {
                    CollectionName = collectionName
                };

                var response = client.GetQuerySegmentInfo(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<GetQuerySegmentInfoResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<GetQuerySegmentInfoResponse>(
                        nameof(GetQuerySegmentInfoRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<GetQuerySegmentInfoResponse>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<RpcStatus> LoadBalance(LoadBalanceParam requestParam, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<RpcStatus>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                requestParam.Check();
                var request = new LoadBalanceRequest()
                {
                    SrcNodeID = requestParam.SrcNodeID,
                };
                request.SealedSegmentIDs.AddRange(requestParam.SegmentIDs);
                request.DstNodeIDs.AddRange(requestParam.DestNodeIDs);

                var response = client.LoadBalance(request, callOptions ?? WithInternalOptions());
                if (response.ErrorCode == ErrorCode.Success)
                {
                    return R<RpcStatus>.Sucess(new RpcStatus(RpcStatus.SUCCESS_MSG));
                }
                else
                {
                    return FailedStatus<RpcStatus>(nameof(LoadCollectionRequest),
                        response);
                }
            }
            catch (System.Exception e)
            {
                return R<RpcStatus>.Failed(e);
            }
        }

        ///<inheritdoc/>
        public R<ManualCompactionResponse> ManualCompaction(ManualCompactionParam requestParam, CallOptions? callOptions = null)
        {
            return ManualCompaction(requestParam.CollectionName,callOptions);
        }

        ///<inheritdoc/>
        public R<ManualCompactionResponse> ManualCompaction(string collectionName, CallOptions? callOptions = null)
        {
            if (!ClientIsReady())
            {
                return R<ManualCompactionResponse>.Failed(new ClientNotConnectedException("Client rpc channel is not ready"));
            }

            try
            {
                ParamUtils.CheckNullEmptyString(collectionName, nameof(collectionName));
                var describleCollectionResponse = DescribeCollection(collectionName);
                if (describleCollectionResponse.Status != Param.Status.Success)
                {
                    return R<ManualCompactionResponse>.Failed((ErrorCode)describleCollectionResponse.Status, describleCollectionResponse.Exception);
                }

                var request = new ManualCompactionRequest()
                {
                    CollectionID = describleCollectionResponse.Data.CollectionID,
                };

                var response = client.ManualCompaction(request, callOptions ?? WithInternalOptions());

                if (response.Status.ErrorCode == ErrorCode.Success)
                {
                    return R<ManualCompactionResponse>.Sucess(response);
                }
                else
                {
                    return FailedStatus<ManualCompactionResponse>(
                        nameof(ManualCompactionRequest),
                        response.Status);
                }
            }
            catch (System.Exception e)
            {
                return R<ManualCompactionResponse>.Failed(e);
            }
        }

        public R<GetIndexStateResponse> getIndexState(GetIndexStateParam requestParam, CallOptions? callOptions = null)
        {
            throw new NotImplementedException();
        }
        #endregion

        #endregion

    }
}
