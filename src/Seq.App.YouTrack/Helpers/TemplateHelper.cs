﻿// Copyright 2014-2017 CaptiveAire Systems
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
    using System.IO;
    using System.Linq;

    using HandlebarsDotNet;

    using Newtonsoft.Json;

    /// <summary>
    /// Adapted from Nicholas Blumhardt's Seq.App.EmailPlus:
    // https://github.com/continuousit/seq-apps/tree/master/src/Seq.App.EmailPlus
    /// </summary>
    public static class TemplateHelper
    {
        public static void PrettyPrint(EncodedTextWriter output, Context context, Arguments arguments)
        {
            var value = arguments.FirstOrDefault();

            switch (value)
            {
                case null:
                    output.WriteSafeString("null");
                    break;

                case IEnumerable<object> _:
                case IEnumerable<KeyValuePair<string, object>> _:
                    output.WriteSafeString(JsonConvert.SerializeObject(value.FromDynamic()));
                    break;

                default:
                    output.WriteSafeString(value.ToString());
                    break;
            }
        }
    }
}