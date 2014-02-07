// Copyright 2014 CaptiveAire Systems
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

namespace Seq.Apps.YouTrack
{
    public partial class YouTrackExceptionPoster
    {
        /// <summary>
        ///     Gets the host.
        /// </summary>
        /// <value>
        ///     The host.
        /// </value>
        [SeqAppSetting(
            DisplayName = "Host (url)",
            HelpText = "URL of the YouTrack instance (do not include http:// or path).")]
        public string Host
        {
            get
            {
                return this.App.GetSetting<string>("Host");
            }
        }

        /// <summary>
        ///     Gets the port.
        /// </summary>
        /// <value>
        ///     The port.
        /// </value>
        [SeqAppSetting(
            DisplayName = "Port (number)",
            IsOptional = true,
            HelpText = "Default is 80. Change if the YouTrack Port is different.")]
        public int Port
        {
            get
            {
                return this.App.GetSetting<int>("Port", 80);
            }
        }

        /// <summary>
        ///     Gets a value indicating whether this object use ssl.
        /// </summary>
        /// <value>
        ///     true if use ssl, false if not.
        /// </value>
        [SeqAppSetting(
            DisplayName = "UseSSL (bool)",
            IsOptional = true,
            HelpText = "Defaults to false. Change to true if SSL (https) is required.")]
        public bool UseSSL
        {
            get
            {
                return this.App.GetSetting<bool>("UseSSL", false);
            }
        }

        /// <summary>
        ///     Gets the full pathname of the file.
        /// </summary>
        /// <value>
        ///     The full pathname of the file.
        /// </value>
        [SeqAppSetting(
            DisplayName = "Path",
            IsOptional = true,
            HelpText = "Defaults to none. Additional path on YouTrack URL.")]
        public string Path
        {
            get
            {
                return this.App.GetSetting<string>("Path", null);
            }
        }

        /// <summary>
        ///     Gets the name of the project.
        /// </summary>
        /// <value>
        ///     The name of the project.
        /// </value>
        [SeqAppSetting(
            DisplayName = "ProjectName",
            IsOptional = false,
            HelpText = "Project name to post YouTrack issue.")]
        public string ProjectName
        {
            get
            {
                return this.App.GetSetting<string>("ProjectName");
            }
        }

        /// <summary>
        ///     Gets the username.
        /// </summary>
        /// <value>
        ///     The username.
        /// </value>
        [SeqAppSetting(
            DisplayName = "Username",
            IsOptional = false,
            HelpText = "Authenticated username for YouTrack.")]
        public string Username
        {
            get
            {
                return this.App.GetSetting<string>("Username");
            }
        }

        /// <summary>
        ///     Gets the password.
        /// </summary>
        /// <value>
        ///     The password.
        /// </value>
        [SeqAppSetting(
            DisplayName = "Password",
            IsOptional = false,
            HelpText = "Authenticated username for YouTrack.")]
        public string Password
        {
            get
            {
                return this.App.GetSetting<string>("Password");
            }
        }
    }
}