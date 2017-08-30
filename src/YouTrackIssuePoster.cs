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

using Seq.App.YouTrack.CreatedIssues;

using Serilog.Sinks.File;

namespace Seq.App.YouTrack
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using HandlebarsDotNet;

    using Seq.App.YouTrack.Helpers;
    using Seq.Apps;
    using Seq.Apps.LogEvents;

    using Serilog;
    using Serilog.Events;
    using Serilog.Formatting.Json;
    using Serilog.Parsing;

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
                if (this.IsDuplicateIssue(@event))
                {
                    return;
                }

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
                    // record as event...
                    LogIssueCreation(@event, issueNumber);

                    Log.Information(
                        "Issue {YouTrackIssueNumber} Created in YouTrack {IssueUrl}",
                        issueNumber,
                        $"{GetYouTrackUri().ToFormattedUrl()}/issue/{issueNumber}");

                    issueManagement.ApplyCommand(issueNumber, "comment", $"Posted from Seq Event Timestamp UTC: {@event.TimestampUtc}");

                    if (AttachCopyOfEventToIssue)
                    {
                        var file = GetJsonEventFile(@event, issueNumber);
                        issueManagement.AttachFileToIssue(issueNumber, file);
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // can't say I care too much...
                        }
                    }
                }
            }
            catch (System.Exception ex) when (LogError(ex, "Failure Creating Issue on YouTrack {YouTrackUrl}", GetYouTrackUri().ToFormattedUrl()))
            {
            }
        }

        void LogIssueCreation(Event<LogEventData> @event, string issueNumber)
        {
            try
            {
                var repo = new CreatedIssueRespository(App.StoragePath);
                var createdIssueEvent = new CreatedIssueEvent() { SeqId = @event.Id, YouTrackId = issueNumber };
                repo.Insert(createdIssueEvent);
            }
            catch (Exception e) when (LogError(e, "Failure Inserting Issue Creation Record"))
            {
            }
        }

        bool IsDuplicateIssue(Event<LogEventData> @event)
        {
            if (AllowDuplicateIssueCreation)
            {
                return false;
            }

            var repo = new CreatedIssueRespository(App.StoragePath);
            var existingIssue = repo.BySeqId(@event.Id);

            if (existingIssue != null)
            {
                // event has already been posted
                Log.Warning(
                    "Issue {YouTrackIssueNumber} Already Exists in YouTrack {IssueUrl} -- Duplicate Not Created",
                    existingIssue.YouTrackId,
                    $"{this.GetYouTrackUri().ToFormattedUrl()}/issue/{existingIssue.YouTrackId}");

                return true;
            }

            return false;
        }

        string GetJsonEventFile(Event<LogEventData> evt, string issueNumber)
        {
            var parser = new MessageTemplateParser();
            var properties =
                (evt.Data.Properties ?? new Dictionary<string, object>()).Select(
                    kvp => CreateProperty(kvp.Key, kvp.Value));

            var logEvent = new LogEvent(
                evt.Data.LocalTimestamp,
                (Serilog.Events.LogEventLevel)Enum.Parse(typeof(Serilog.Events.LogEventLevel), evt.Data.Level.ToString()),
                evt.Data.Exception != null ? new WrappedException(evt.Data.Exception) : null,
                parser.Parse(evt.Data.MessageTemplate),
                properties);

            string logFilePath = Path.Combine(App.StoragePath, string.Format($"SeqAppYouTrack-{issueNumber}.json"));
            using (var jsonSink = new FileSink(logFilePath, new JsonFormatter(), null))
            {
                var logger =
                    new LoggerConfiguration().WriteTo.Sink(jsonSink, Serilog.Events.LogEventLevel.Verbose)
                        .CreateLogger();
                logger.Write(logEvent);
            }

            return logFilePath;
        }

        LogEventProperty CreateProperty(string name, object value) => new LogEventProperty(name, CreatePropertyValue(value));

        LogEventPropertyValue CreatePropertyValue(object value)
        {
            if (value is IDictionary<string, object> d)
            {
                d.TryGetValue("$typeTag", out var tt);
                return new StructureValue(
                    d.Where(kvp => kvp.Key != "$typeTag").Select(kvp => CreateProperty(kvp.Key, kvp.Value)),
                    tt as string);
            }

            if (value is IDictionary dd)
            {
                return new DictionaryValue(dd.Keys
                    .Cast<object>()
                    .Select(k => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
                        (ScalarValue)CreatePropertyValue(k),
                        CreatePropertyValue(dd[k]))));
            }

            if (value == null || value is string || !(value is IEnumerable))
            {
                return new ScalarValue(value);
            }

            var enumerable = (IEnumerable)value;
            return new SequenceValue(enumerable.Cast<object>().Select(CreatePropertyValue));
        }

        object GetPayload(Event<LogEventData> @event)
        {
            IDictionary<string, object> properties =
                (@event.Data.Properties ?? new Dictionary<string, object>()).ToDynamic() as IDictionary<string, object>;

            var serverUri = string.IsNullOrWhiteSpace(this.SeqBaseUri) ? Host.ListenUris.FirstOrDefault() : this.SeqBaseUri;

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
                { "$ServerUri", serverUri },
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
            catch (UriFormatException ex) when (LogError(ex, "Failure Connecting to YouTrack: Invalid Url Format"))
            {
                // logged in when
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
            catch (System.Exception ex) when (LogError(ex, "Failure Connecting to YouTrack"))
            {
                // failure connecting to YT
            }

            return null;
        }

        /// <summary>
        /// Logs the error
        /// </summary>
        /// <param name="ex">The ex.</param>
        /// <param name="message">The message.</param>
        /// <returns></returns>
        bool LogError(Exception ex, string message, params object[] propertyValues)
        {
            Log.Error(ex, message, propertyValues);
            return true;
        }
    }
}
