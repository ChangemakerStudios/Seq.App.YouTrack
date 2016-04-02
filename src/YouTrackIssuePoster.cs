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

namespace Seq.App.YouTrack
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    using HandlebarsDotNet;

    using Seq.App.YouTrack.Helpers;
    using Seq.Apps;
    using Seq.Apps.LogEvents;

    using YouTrackSharp.Infrastructure;
    using YouTrackSharp.Issues;

    /// <summary>
    /// You track issue poster.
    /// </summary>
    [SeqApp("YouTrack Issue Poster", Description = "Create a YouTrack issue from an event.")]
    public partial class YouTrackIssuePoster : Reactor, ISubscribeTo<LogEventData>
    {
        static YouTrackIssuePoster()
        {
            Handlebars.RegisterHelper("pretty", TemplateHelper.PrettyPrint);
        }

        readonly Lazy<Func<object, string>> _summaryTemplate;
        readonly Lazy<Func<object, string>> _bodyTemplate;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public YouTrackIssuePoster()
        {
            _summaryTemplate =
                new Lazy<Func<object, string>>(
                    () =>
                    Handlebars.Compile(IssueSummaryTemplate.IsSet() ? IssueSummaryTemplate : Resources.DefaultIssueSummaryTemplate));


            _bodyTemplate =
                new Lazy<Func<object, string>>(
                    () =>
                    Handlebars.Compile(IssueBodyTemplate.IsSet() ? IssueBodyTemplate : Resources.DefaultIssueBodyTemplate));
        }

        /// <summary>
        /// Ons the given event.
        /// </summary>
        /// <param name="event"> The event.</param>
        public void On(Event<LogEventData> @event)
        {
            var connection = GetConnection();
            if (connection == null) return;

            try
            {
                var issueManagement = new IssueManagement(connection);

                dynamic issue = new Issue();

                var payload = GetPayload(@event);
                issue.Summary = this._summaryTemplate.Value(payload);
                issue.Description = this._bodyTemplate.Value(payload);
                issue.ProjectShortName = this.ProjectId;
                issue.Type = this.YouTrackIssueType.IsSet() ? this.YouTrackIssueType : "Auto-reported Exception";

                string issueNumber = issueManagement.CreateIssue(issue);

                if (issueNumber.IsSet())
                {
                    this.Log.Information(
                        "Issue {YouTrackIssueNumber} Created in YouTrack {IssueUrl}",
                        issueNumber,
                        $"{GetYouTrackUri().ToFormattedUrl()}/issue/{issueNumber}");

                    issueManagement.ApplyCommand(issueNumber, "comment", $"Posted from Seq Event Timestamp UTC: {@event.TimestampUtc}");
                }
            }
            catch (System.Exception ex)
            {
                // failure creating issue
                this.Log.Error(ex, "Failure Creating Issue on YouTrack {YouTrackUrl}", GetYouTrackUri().ToFormattedUrl());
            }
        }

        object GetPayload(Event<LogEventData> @event)
        {
            IDictionary<string, object> properties =
                (@event.Data.Properties ?? new Dictionary<string, object>()).ToDynamic() as IDictionary<string, object>;

            var payload = new Dictionary<string, object>
            {
                { "$Id", @event.Id },
                { "$UtcTimestamp", @event.TimestampUtc },
                { "$LocalTimestamp", @event.Data.LocalTimestamp },
                { "$Level", @event.Data.Level },
                { "$MessageTemplate", @event.Data.MessageTemplate },
                { "$Message", @event.Data.RenderedMessage },
                { "$Exception", @event.Data.Exception },
                { "$Properties", properties },
                { "$EventType", "$" + @event.EventType.ToString("X8") },
                { "$Instance", this.Host.InstanceName },
                { "$ServerUri", this.Host.ListenUris.FirstOrDefault() },
                { "$YouTrackProjectId", this.ProjectId }
            }.ToDynamic() as IDictionary<string, object>;
            
            foreach (var property in properties)
            {
                payload[property.Key] = property.Value;
            }

            return payload;
        }

        /// <summary>
        /// Gets you track URL.
        /// </summary>
        /// <returns>
        /// you track URL.
        /// </returns>
        UriBuilder GetYouTrackUri()
        {
            try
            {
                return new UriBuilder(this.YouTrackUri);
            }
            catch (UriFormatException ex)
            {
                Log.Error(ex, "Failure Connecting to YouTrack: Invalid Url Format");
            }   

            return null;
        }

        Connection GetConnection()
        {
            var uri = GetYouTrackUri();
            if (uri == null) return null;

            try
            {
                // have exceptions -- throw to YouTrack...
                var connection = new Connection(
                    uri.Host,
                    uri.Port,
                    uri.IsSSL(),
                    uri.GetPathOrEmptyAsNull());

                if (this.Username.IsSet() && this.Password.IsSet())
                    connection.Authenticate(this.Username, this.Password);

                return connection;
            }
            catch (System.Exception ex)
            {
                // failure connecting to YT
                Log.Error(ex, "Failure Connecting to YouTrack");
            }

            return null;
        }
    }
}