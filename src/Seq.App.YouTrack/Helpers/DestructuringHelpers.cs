// Copyright 2014-2019 CaptiveAire Systems
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

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
