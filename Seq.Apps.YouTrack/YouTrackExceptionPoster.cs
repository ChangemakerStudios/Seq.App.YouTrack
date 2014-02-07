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
    using Seq.Apps.LogEvents;

    using YouTrackSharp.Infrastructure;
    using YouTrackSharp.Issues;

    /// <summary>
    /// You track exception poster.
    /// </summary>
    [SeqApp("YouTrack Exception Poster", Description = "Create a YouTrack issue from an exception event.")]
    public partial class YouTrackExceptionPoster : Reactor, ISubscribeTo<LogEventData>
    {
        /// <summary>
        /// true if this object is initialized.
        /// </summary>
        private bool _isInitialized = false;

        /// <summary>
        /// Ons the given event.
        /// </summary>
        /// <param name="event"> The event.</param>
        public void On(Event<LogEventData> @event)
        {
            this.EnsureInitalized();

            if (string.IsNullOrWhiteSpace(@event.Data.Exception))
            {
                this.Log.Information("Cannot send event that does not have exception data.");
                return;
            }

            try
            {
                // have exceptions -- throw to YouTrack...
                var connection = new Connection(this.Host, this.Port, this.UseSSL, this.Path);

                if (this.Username.IsSet() && this.Password.IsSet())
                {
                    connection.Authenticate(this.Username, this.Password);
                }

                var issueManagement = new IssueManagement(connection);

                dynamic issue = new Issue();

                issue.Summary = @event.Data.RenderedMessage;
                issue.Description = string.Format(
                    "Logged {0} Exception Event Id #{1}\r\nException:\r\n{2}",
                    @event.Data.Level,
                    @event.Id,
                    @event.Data.Exception);

                issue.ProjectShortName = this.ProjectName;
                issue.Type = "Auto-reported Exception";

                string issueNumber = issueManagement.CreateIssue(issue);

                issueManagement.ApplyCommand(issueNumber, "add tag Seq", "");

                this.Log.Information("Issue #{0} Posted to YouTrack", issueNumber);
            }
            catch (System.Exception ex)
            {
                // failure creating issue
                this.Log.Error(ex, "Unable to connect to YouTrack to create exception.");
            }
        }


        /// <summary>
        /// Ensures that initalized.
        /// </summary>
        private void EnsureInitalized()
        {
            if (!this._isInitialized)
            {
                this._isInitialized = true;
            }
        }
    }
}
