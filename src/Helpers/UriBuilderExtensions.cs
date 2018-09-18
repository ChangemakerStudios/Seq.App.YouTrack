// Copyright 2014-2017 CaptiveAire Systems
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
    using System;

    public static class UriBuilderExtensions
    {
        public static string GetPathOrEmptyAsNull(this UriBuilder uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            return uri.Path == @"/" ? null : uri.Path;
        }

        public static bool IsSSL(this UriBuilder uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            return uri.Scheme == "https";
        }

        public static string ToFormattedUrl(this UriBuilder uri)
        {
            if (uri == null)
                throw new ArgumentNullException(nameof(uri));

            string url;

            if (uri.Uri.IsDefaultPort && uri.Port != -1)
            {
                uri.Port = -1;
                url = uri.ToString();
            }
            else
            {
                url = uri.ToString();
            }

            if (url.EndsWith("/"))
            {
                return url.Substring(0, url.Length - 1);
            }

            return url;
        }
    }
}
