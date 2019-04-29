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

using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Seq.App.YouTrack.Helpers
{
    public static class StringExtensions
    {
        public static bool IsSet(this string str) => !string.IsNullOrWhiteSpace(str);

        public static bool IsNotSet(this string str) => string.IsNullOrWhiteSpace(str);

        public static async Task<Stream> ToStream(this string contents)
        {
            if (contents == null)
                throw new ArgumentNullException(nameof(contents));

            var ms = new MemoryStream();

            using (var streamWriter = new StreamWriter(ms, Encoding.UTF8, 4096, leaveOpen: true))
            {
                await streamWriter.WriteAsync(contents);
                await streamWriter.FlushAsync();
            }

            ms.Position = 0;

            return ms;
        }
    }
}
