using System.Text;

namespace IO.Milvus.Client;

public partial class MilvusClient
{
    internal static string Combine(IDictionary<string, string> parameters)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('{');

        int index = 0;
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            stringBuilder
                .Append('"')
                .Append(parameter.Key)
                .Append("\":")
                .Append(parameter.Value);

            if (index++ != parameters.Count - 1)
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }

}
