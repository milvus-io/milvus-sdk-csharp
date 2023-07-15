using System.Text;

namespace IO.Milvus.Utils;

internal static class ParameterUtils
{
    internal static string Combine(this IDictionary<string, string> parameters)
    {
        StringBuilder stringBuilder = new();
        stringBuilder.Append('{');

        int index = 0;
        foreach (KeyValuePair<string, string> parameter in parameters)
        {
            stringBuilder.Append('"').Append(parameter.Key).Append('"').Append(':').Append(parameter.Value);

            if (index++ != (parameters.Count - 1))
            {
                stringBuilder.Append(", ");
            }
        }

        stringBuilder.Append('}');
        return stringBuilder.ToString();
    }
}
