// Seq.App.YouTrack - Copyright (c) 2019 CaptiveAire

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