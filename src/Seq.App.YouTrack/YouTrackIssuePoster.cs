﻿// Seq.App.YouTrack - Copyright (c) 2019 CaptiveAire

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using HandlebarsDotNet;

using Seq.App.YouTrack.CreatedIssues;
using Seq.App.YouTrack.Helpers;
using Seq.Apps;
using Seq.Apps.LogEvents;

using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;
using Serilog.Sinks.File;

using YouTrackSharp;
using YouTrackSharp.Issues;

using LogEventLevel = Serilog.Events.LogEventLevel;

namespace Seq.App.YouTrack
{
    /// <summary>
    /// You track issue poster.
    /// </summary>
    [SeqApp("YouTrack Issue Poster", Description = "Create a YouTrack issue from an event.")]
    public partial class YouTrackIssuePoster : SeqApp, IDisposable, ISubscribeToAsync<LogEventData>
    {
        readonly Lazy<Func<object, string>> _bodyTemplate;
        readonly Lazy<Func<object, string>> _projectIdTemplate;
        readonly Lazy<Func<object, string>> _summaryTemplate;

        CreatedIssueRepository _repository;

        static YouTrackIssuePoster()
        {
            Handlebars.RegisterHelper("pretty", TemplateHelper.PrettyPrint);
        }

        protected override void OnAttached()
        {
            // create the LiteDb;
            this._repository = new CreatedIssueRepository(this.App.StoragePath);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public YouTrackIssuePoster()
        {
            _summaryTemplate = new Lazy<Func<object, string>>(
                () => Handlebars.Compile(IssueSummaryTemplate.IsSet() ? IssueSummaryTemplate : Resources.DefaultIssueSummaryTemplate));

            _bodyTemplate = new Lazy<Func<object, string>>(
                () => Handlebars.Compile(IssueBodyTemplate.IsSet() ? IssueBodyTemplate : Resources.DefaultIssueBodyTemplate));

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
                        "Issue {YouTrackIssueNumber} Created in YouTrack {IssueUrl}",
                        issueNumber,
                        $"{GetYouTrackUri().ToFormattedUrl()}/issue/{issueNumber}");

                    await issueManagement.ApplyCommand(
                            issueNumber,
                            "comment",
                            $"Posted from Seq Event Timestamp UTC: {@event.TimestampUtc}")
                        .ConfigureAwait(false);

                    if (AttachCopyOfEventToIssue)
                    {
                        var file = GetJsonEventFile(@event, issueNumber);

                        using (var jsonFileStream = file.ToStream())

                        {
                            await issueManagement.AttachFileToIssue(issueNumber, $"{issueNumber}.json", jsonFileStream)
                                .ConfigureAwait(false);
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
                var repo = new CreatedIssueRepository(App.StoragePath);
                var createdIssueEvent = new CreatedIssueEvent { SeqId = @event.Id, YouTrackId = issueNumber };
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

            var existingIssue = this._repository.BySeqId(@event.Id);

            // not duplicate
            if (existingIssue == null)
                return false;

            // event has already been posted
            this.Log.Warning(
                "Issue {YouTrackIssueNumber} Already Exists in YouTrack {IssueUrl} -- Duplicate Not Created",
                existingIssue.YouTrackId,
                $"{this.GetYouTrackUri().ToFormattedUrl()}/issue/{existingIssue.YouTrackId}");

            return true;

        }

        string GetJsonEventFile(Event<LogEventData> evt, string issueNumber)
        {
            var parser = new MessageTemplateParser();
            var properties = (evt.Data.Properties ?? new Dictionary<string, object>()).Select(kvp => CreateProperty(kvp.Key, kvp.Value));

            var logEvent = new LogEvent(
                evt.Data.LocalTimestamp,
                (LogEventLevel)Enum.Parse(typeof(LogEventLevel), evt.Data.Level.ToString()),
                evt.Data.Exception != null ? new WrappedException(evt.Data.Exception) : null,
                parser.Parse(evt.Data.MessageTemplate),
                properties);

            string logFilePath = Path.Combine(App.StoragePath, string.Format($"SeqAppYouTrack-{issueNumber}.json"));

            using (var jsonSink = new FileSink(logFilePath, new JsonFormatter(), null))
            {
                var logger = new LoggerConfiguration().WriteTo.Sink(jsonSink, LogEventLevel.Verbose).CreateLogger();

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
                    { "$Message", @event.Data.RenderedMessage },
                    { "$Exception", @event.Data.Exception },
                    { "$Properties", properties },
                    { "$EventType", "$" + @event.EventType.ToString("X8") },
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

                if (this.Username.IsNotSet() || this.Password.IsNotSet())
                {
                    throw new Exception("Username and password are required if a bearer token is not set");
                }

                return new UsernamePasswordConnection(uri.ToFormattedUrl(), this.Username, this.Password);
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

        public void Dispose()
        {
            this._repository?.Dispose();
        }
    }
}