using System;
using System.Collections.Generic;
using System.Text;

namespace IO.Milvus;

/// <summary>
/// Query parameter
/// </summary>
public class QueryParameter
{
    public string CollectionName { get; }

    public string Expr { get; }

    public IList<string> OutFields { get; }

    public long GuaranteeTimestamp { get; }
}
