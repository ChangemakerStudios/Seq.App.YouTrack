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

namespace Seq.App.YouTrack
{
    using System;
    using System.IO;

    using Seq.Apps;
    using Seq.Apps.LogEvents;

    using Veil;

    using YouTrackSharp.Infrastructure;
    using YouTrackSharp.Issues;

    /// <summary>
    /// You track issue poster.
    /// </summary>
    [SeqApp("YouTrack Issue Poster", Description = "Create a YouTrack issue from an event.")]
    public partial class YouTrackIssuePoster : Reactor, ISubscribeTo<LogEventData>
    {
        /// <summary>
        /// Issue Template Default.
        /// </summary>
        const string IssueTemplateDefault =
            "====Logged *{{Data.Level}}* Event ID *#{{Id}}*====\r\n\r\n====Exception=={{Data.Exception}}";

        /// <summary>
        /// The lazy vail engine.
        /// </summary>
        static readonly Lazy<VeilEngine> _lazyVailEngine;

        /// <summary>
        /// The compiled template.
        /// </summary>
        Action<TextWriter, Event<LogEventData>> _compiledTemplate;

        /// <summary>
        /// Initializes static members of the YouTrackIssuePoster class.
        /// </summary>
        static YouTrackIssuePoster()
        {
            _lazyVailEngine = new Lazy<VeilEngine>(() => new VeilEngine());
        }

        /// <summary>
        /// Gets the vail engine.
        /// </summary>
        /// <value>
        /// The vail engine.
        /// </value>
        static VeilEngine VailEngine
        {
            get { return _lazyVailEngine.Value; }
        }

        /// <summary>
        /// Ons the given event.
        /// </summary>
        /// <param name="event"> The event.</param>
        public void On(Event<LogEventData> @event)
        {
            var issueManagement = GetIssueManagement();
            if (issueManagement == null)
                return;

            try
            {
                dynamic issue = new Issue();

                issue.Summary = @event.Data.RenderedMessage;
                issue.Description = RenderTemplate(@event);
                issue.ProjectShortName = ProjectName;
                issue.Type = YouTrackIssueType.IsSet() ? YouTrackIssueType : "Auto-reported Exception";

                string issueNumber = issueManagement.CreateIssue(issue);

                if (!issueNumber.IsSet()) return;

                Log.Information(
                    "Issue {YouTrackIssueNumber} Created in YouTrack {IssueUrl}",
                    issueNumber,
                    string.Format("{0}/issue/{1}", GetYouTrackUrl(), issueNumber));

                issueManagement.ApplyCommand(issueNumber,
                    "comment",
                    string.Format("Posted from Seq Event Timestamp UTC: {0}", @event.TimestampUtc));
            }
            catch (Exception ex)
            {
                // failure creating issue
                Log.Error(ex, "Failure Creating Issue on YouTrack");
            }
        }

        /// <summary>
        /// Renders the template described by @event.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when one or more required arguments are null.</exception>
        /// <param name="event"> The event.</param>
        /// <returns>
        /// A string.
        /// </returns>
        string RenderTemplate(Event<LogEventData> @event)
        {
            if (@event == null)
                throw new ArgumentNullException("event");

            if (_compiledTemplate == null)
            {
                using (var templateContents = new StringReader(IssueTemplate ?? IssueTemplateDefault))
                {
                    _compiledTemplate = VailEngine.Compile<Event<LogEventData>>("handlebars", templateContents);
                }
            }

            using (var writer = new StringWriter())
            {
                _compiledTemplate(writer, @event);
                return writer.ToString();
            }
        }

        /// <summary>
        /// Gets you track URL.
        /// </summary>
        /// <returns>
        /// you track URL.
        /// </returns>
        string GetYouTrackUrl()
        {
            var builder = new UriBuilder(UseSSL ? "https" : "http", HostUrl, Port ?? 80, Path);
            return builder.ToString();
        }

        /// <summary>
        /// Gets issue management.
        /// </summary>
        /// <returns>
        /// The issue management.
        /// </returns>
        IssueManagement GetIssueManagement()
        {
            try
            {
                // have exceptions -- throw to YouTrack...
                var connection = new Connection(HostUrl, Port ?? 80, UseSSL, Path);

                if (Username.IsSet() && Password.IsSet())
                    connection.Authenticate(Username, Password);

                return new IssueManagement(connection);
            }
            catch (Exception ex)
            {
                // failure creating issue
                Log.Error(ex, "Unable to connect to YouTrack to create exception.");
            }

            return null;
        }
    }
}