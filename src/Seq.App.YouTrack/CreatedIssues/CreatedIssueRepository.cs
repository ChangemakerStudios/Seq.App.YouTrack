// Seq.App.YouTrack - Copyright (c) 2019 CaptiveAire

using System;
using System.IO;
using System.Linq;

using LiteDB;

namespace Seq.App.YouTrack.CreatedIssues
{
    public class CreatedIssueRepository : IDisposable
    {
        readonly LiteDatabase _liteDb;

        public CreatedIssueRepository(string storagePath)
        {
            this._liteDb = new LiteDatabase(Path.Combine(storagePath, "Seq.App.YouTrack.db"));
        }

        public void Dispose()
        {
            this._liteDb?.Dispose();
        }

        public CreatedIssueEvent Insert(CreatedIssueEvent issueEvent)
        {
            var issues = GetCreatedIssueCollection();

            issues.Insert(issueEvent);

            issues.EnsureIndex(x => x.SeqId);

            return issueEvent;
        }

        public CreatedIssueEvent BySeqId(string seqId)
        {
            var issues = GetCreatedIssueCollection();

            return issues.Find(s => s.SeqId == seqId).FirstOrDefault();
        }

        LiteCollection<CreatedIssueEvent> GetCreatedIssueCollection()
        {
            return this._liteDb.GetCollection<CreatedIssueEvent>(typeof(CreatedIssueEvent).Name);
        }
    }
}