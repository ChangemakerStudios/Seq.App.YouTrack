namespace Seq.App.YouTrack.Helpers
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    public static class DestructuringHelpers
    {
        public static object ToDynamic(this object o)
        {
            switch (o)
            {
                case IEnumerable<KeyValuePair<string, object>> dictionary:
                    var result = new ExpandoObject();
                    var asDict = (IDictionary<string, object>)result;
                    foreach (var kvp in dictionary)
                    {
                        asDict.Add(kvp.Key, ToDynamic(kvp.Value));
                    }
                    return result;

                case IEnumerable<object> enumerable:
                    return enumerable.Select(ToDynamic).ToArray();
            }

            return o;
        }

        public static object FromDynamic(this object o)
        {
            switch (o)
            {
                case IEnumerable<KeyValuePair<string, object>> dictionary:
                    return dictionary.ToDictionary(kvp => kvp.Key, kvp => FromDynamic(kvp.Value));

                case IEnumerable<object> enumerable:
                    return enumerable.Select(FromDynamic).ToArray();
            }

            return o;
        }
    }
}