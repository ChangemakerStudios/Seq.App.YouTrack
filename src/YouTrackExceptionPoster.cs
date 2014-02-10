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
        /// Ons the given event.
        /// </summary>
        /// <param name="event"> The event.</param>
        public void On(Event<LogEventData> @event)
        {
            if (!@event.Data.Exception.IsSet())
            {
                this.Log.Information("Cannot send event that does not have exception data.");
                return;
            }

            var issueManagement = this.GetIssueManagement();

            if (issueManagement == null)
            {
                return;
            }

            try
            {
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

                if (issueNumber.IsSet())
                {
                    this.Log.Information("Issue #{0} Posted to YouTrack", issueNumber);

                    issueManagement.ApplyCommand(issueNumber, "comment", string.Format("Posted from Seq. Event Timestamp UTC: {0}", @event.TimestampUtc));
                }
            }
            catch (System.Exception ex)
            {
                // failure creating issue
                this.Log.Error(ex, "Can't Create Issue on YouTrack.");
            }
        }

        private IssueManagement GetIssueManagement()
        {
            try
            {
                // have exceptions -- throw to YouTrack...
                var connection = new Connection(this.Host, this.Port ?? 80, this.UseSSL, this.Path);

                if (this.Username.IsSet() && this.Password.IsSet())
                {
                    connection.Authenticate(this.Username, this.Password);
                }

                return new IssueManagement(connection);
            }
            catch (System.Exception ex)
            {
                // failure creating issue
                this.Log.Error(ex, "Unable to connect to YouTrack to create exception.");
            }

            return null;
        }
    }
}
