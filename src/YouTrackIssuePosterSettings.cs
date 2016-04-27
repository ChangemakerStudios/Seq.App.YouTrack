// Copyright 2014-2016 CaptiveAire Systems
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

using Seq.Apps;

namespace Seq.App.YouTrack
{
    /// <summary>
    ///     you track issue poster settings
    /// </summary>
    public partial class YouTrackIssuePoster
    {
        /// <summary>
        ///     Gets or sets the host.
        /// </summary>
        /// <value>
        ///     The host.
        /// </value>
        [SeqAppSetting(DisplayName = "YouTrack Url",
            HelpText = "Complete Uri of the YouTrack instance including http and path (e.g. https://mytrack.domain.com/path)")]
        public string YouTrackUri { get; set; }

        /// <summary>
        /// Gets or sets the Base Url for Seq.
        /// </summary>
        [SeqAppSetting(DisplayName = "Seq Base Url (Optional)", IsOptional = true, HelpText = "Override the Base Url for the permalink generation")]
        public string SeqBaseUri { get; set; }

        /// <summary>
        ///     Gets or sets the name of the project.
        /// </summary>
        /// <value>
        ///     The name of the project.
        /// </value>
        [SeqAppSetting(DisplayName = "Project ID", IsOptional = false, HelpText = "Project ID (short name) to post YouTrack issue.")]
        public string ProjectId { get; set; }

        /// <summary>
        /// Gets or sets the issue summary template.
        /// </summary>
        /// <value>
        /// The issue summary template.
        /// </value>
        [SeqAppSetting(DisplayName = "Issue Summary Template", IsOptional = true, InputType = SettingInputType.LongText,
            HelpText =
                "The template to use when generating issue body, using Handlebars.NET syntax. Leave this blank to use "
                + "the default template that includes the message.")]
        public string IssueSummaryTemplate { get; set; }

        /// <summary>
        ///     Gets or sets the issue body template.
        /// </summary>
        /// <value>
        ///     The issue template.
        /// </value>
        [SeqAppSetting(DisplayName = "Issue Body Template", IsOptional = true, InputType = SettingInputType.LongText,
            HelpText =
                "The template to use when generating issue body, using Handlebars.NET syntax. Leave this blank to use "
                + "the default template that includes the message and properties.")]
        public string IssueBodyTemplate { get; set; }

        /// <summary>
        ///     Gets or sets the type of you track.
        /// </summary>
        /// <value>
        ///     The type of you track.
        /// </value>
        [SeqAppSetting(DisplayName = "YouTrack Issue Type", IsOptional = true,
            HelpText = @"Type of issue to create. Default is ""Auto-reported Exception"".", InputType = SettingInputType.Text)]
        public string YouTrackIssueType { get; set; }

        /// <summary>
        ///     Gets or sets the username.
        /// </summary>
        /// <value>
        ///     The username.
        /// </value>
        [SeqAppSetting(DisplayName = "Username", IsOptional = false, HelpText = "Authenticated username for YouTrack.")]
        public string Username { get; set; }

        /// <summary>
        ///     Gets or sets the password.
        /// </summary>
        /// <value>
        ///     The password.
        /// </value>
        [SeqAppSetting(DisplayName = "Password", IsOptional = false, HelpText = "Authenticated username for YouTrack.",
            InputType = SettingInputType.Password)]
        public string Password { get; set; }


        /// <summary>
        /// Should attach a copy of the event to the issue.
        /// </summary>
        [SeqAppSetting(DisplayName = "Attach Event?", IsOptional = true, HelpText = "Attaches a copy of the log event to the issue.",
            InputType = SettingInputType.Checkbox)]
        public bool AttachCopyOfEventToIssue { get; set; }
    }
}