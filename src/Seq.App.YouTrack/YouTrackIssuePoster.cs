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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using HandlebarsDotNet;

using Newtonsoft.Json;

using Seq.App.YouTrack.CreatedIssues;
using Seq.App.YouTrack.Helpers;
using Seq.App.YouTrack.Resources;
using Seq.Apps;
using Seq.Apps.LogEvents;

using Serilog;
using Serilog.Debugging;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;

using YouTrackSharp;
using YouTrackSharp.Issues;

using LogEventLevel = Serilog.Events.LogEventLevel;

namespace Seq.App.YouTrack
{
    /// <summary>
    /// You track issue poster.
    /// </summary>
    [SeqApp("YouTrack Issue Poster", Description = "Create a YouTrack issue from an event.")]
    public partial class YouTrackIssuePoster : SeqApp, ISubscribeToAsync<LogEventData>
    {
        readonly Lazy<Func<object, string>> _bodyTemplate;
        readonly Lazy<Func<object, string>> _projectIdTemplate;
        readonly Lazy<Func<object, string>> _summaryTemplate;

        static YouTrackIssuePoster()
        {
            Handlebars.RegisterHelper("pretty", TemplateHelper.PrettyPrint);
        }

        CreatedIssueRepository GetIssueRepositoryInstance()
        {
            return new CreatedIssueRepository(this.App.StoragePath);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public YouTrackIssuePoster()
        {
            _summaryTemplate = new Lazy<Func<object, string>>(
                () => Handlebars.Compile(IssueSummaryTemplate.IsSet() ? IssueSummaryTemplate : TemplateResources.DefaultIssueSummaryTemplate));

            _bodyTemplate = new Lazy<Func<object, string>>(
                () => Handlebars.Compile(IssueBodyTemplate.IsSet() ? IssueBodyTemplate : TemplateResources.DefaultIssueBodyTemplate));

            _projectIdTemplate = new Lazy<Func<object, string>>(() => Handlebars.Compile(ProjectId));
        }

        public async Task OnAsync(Event<LogEventData> @event)
        {
            var connection = GetConnection();
            if (connection == null) return;

            try
            {
                if (this.IsDuplicateIssue(@event))
                {
                    return;
                }

                var issueManagement = connection.CreateIssuesService();
                var issue = new Issue();
                var payload = GetPayload(@event);

                var projectId = this._projectIdTemplate.Value(payload);

                issue.Summary = this._summaryTemplate.Value(payload);
                issue.Description = this._bodyTemplate.Value(payload);
                issue.AsDynamic().Type = this.YouTrackIssueType.IsSet() ? this.YouTrackIssueType : "Auto-reported Exception";

                if (projectId.IsNotSet())
                {
                    Log.Error("Failure posting to YouTrack: Project Short Name (ID) is empty");
                    return;
                }

                string issueNumber = await issueManagement.CreateIssue(projectId, issue).ConfigureAwait(false);

                if (issueNumber.IsSet())
                {
                    // record as event...
                    LogIssueCreation(@event, issueNumber);

                    Log.Information(
                        "Issue {YouTrackIssueNumber} Created in YouTrack {IssueUrl} Based on {SeqId}",
                        issueNumber,
                        $"{GetYouTrackUri().ToFormattedUrl()}/issue/{issueNumber}",
                        @event.Id);

                    await issueManagement.ApplyCommand(
                            issueNumber,
                            "comment",
                            $"Posted from Seq Event Timestamp UTC: {@event.TimestampUtc}")
                        .ConfigureAwait(false);

                    if (AttachCopyOfEventToIssue)
                    {
                        var file = GetJsonEventFile(@event, issueNumber);

                        var jsonFileData = File.ReadAllText(file, Encoding.UTF8);

                        using (var fileStream = await jsonFileData.ToStream().ConfigureAwait(false))
                        {
                            await issueManagement.AttachFileToIssue(issueNumber, $"{issueNumber}.json", fileStream).ConfigureAwait(false);
                        }

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
            catch (Exception ex) when (LogError(ex, "Failure Creating Issue on YouTrack {YouTrackUrl}", GetYouTrackUri().ToFormattedUrl()))
            {
            }
        }

        void LogIssueCreation(Event<LogEventData> @event, string issueNumber)
        {
            try
            {
                using (var repository = GetIssueRepositoryInstance())
                {
                    var createdIssueEvent = new CreatedIssueEvent { SeqId = @event.Id, YouTrackId = issueNumber };
                    repository.Insert(createdIssueEvent);
                }
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

            CreatedIssueEvent existingIssue;

            using (var repository = GetIssueRepositoryInstance())
            {
                existingIssue = repository.BySeqId(@event.Id);
            }

            // not duplicate
            if (existingIssue == null)
                return false;

            // event has already been posted
            this.Log.Warning(
                "Issue {YouTrackIssueNumber} already exists for {SeqId} in YouTrack {IssueUrl}",
                existingIssue.YouTrackId,
                $"{this.GetYouTrackUri().ToFormattedUrl()}/issue/{existingIssue.YouTrackId}",
                @event.Id);

            return true;
        }

        string GetJsonEventFile(Event<LogEventData> evt, string issueNumber)
        {
            var parser = new MessageTemplateParser();
            var properties =
                (evt.Data.Properties ?? new Dictionary<string, object>()).Select(
                    kvp => CreateProperty(kvp.Key, kvp.Value));

            var logEvent = new LogEvent(
                evt.Data.LocalTimestamp,
                (LogEventLevel)Enum.Parse(typeof(LogEventLevel), evt.Data.Level.ToString()),
                evt.Data.Exception != null ? new WrappedException(evt.Data.Exception) : null,
                parser.Parse(evt.Data.MessageTemplate),
                properties);

            string logFilePath = Path.Combine(App.StoragePath, string.Format($"SeqAppYouTrack-{issueNumber}.json"));

            var json = JsonConvert.SerializeObject(logEvent, Formatting.Indented);

            File.WriteAllText(logFilePath, json, Encoding.UTF8);

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
                return new DictionaryValue(
                    dd.Keys.Cast<object>()
                        .Select(
                            k => new KeyValuePair<ScalarValue, LogEventPropertyValue>(
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

            var payload = new Dictionary<string, object>
                {
                    { "$Id", @event.Id },
                    { "$UtcTimestamp", @event.TimestampUtc },
                    { "$LocalTimestamp", @event.Data.LocalTimestamp },
                    { "$Level", @event.Data.Level },
                    { "$MessageTemplate", @event.Data.MessageTemplate },
                    { "$Message", HttpUtility.HtmlDecode(@event.Data.RenderedMessage) },
                    { "$Exception", @event.Data.Exception },
                    { "$Properties", properties },
                    { "$EventType", $"${@event.EventType:X8}" },
                    { "$Instance", this.Host.InstanceName },
                    { "$ServerUri", Host.BaseUri },
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
                if (this.BearerToken.IsSet())
                {
                    return new BearerTokenConnection(uri.ToFormattedUrl(), this.BearerToken);
                }

                throw new Exception("Must provided permanent/bearer token authentication token");
            }
            catch (Exception ex) when (LogError(ex, "Failure Connecting to YouTrack"))
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
