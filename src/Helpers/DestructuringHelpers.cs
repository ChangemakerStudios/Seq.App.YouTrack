namespace Seq.App.YouTrack.Helpers
{
    using System.Collections.Generic;
    using System.Dynamic;
    using System.Linq;

    public static class DestructuringHelpers
    {
        public static object ToDynamic(this object o)
        {
            var dictionary = o as IEnumerable<KeyValuePair<string, object>>;
            if (dictionary != null)
            {
                var result = new ExpandoObject();
                var asDict = (IDictionary<string, object>)result;
                foreach (var kvp in dictionary)
                {
                    asDict.Add(kvp.Key, ToDynamic(kvp.Value));
                }
                return result;
            }

            var enumerable = o as IEnumerable<object>;
            if (enumerable != null)
                return enumerable.Select(ToDynamic).ToArray();

            return o;
        }

        public static object FromDynamic(this object o)
        {
            var dictionary = o as IEnumerable<KeyValuePair<string, object>>;
            if (dictionary != null)
                return dictionary.ToDictionary(kvp => kvp.Key, kvp => FromDynamic(kvp.Value));

            var enumerable = o as IEnumerable<object>;
            if (enumerable != null)
                return enumerable.Select(FromDynamic).ToArray();

            return o;
        }
    }
}