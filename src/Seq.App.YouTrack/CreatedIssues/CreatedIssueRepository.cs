// Seq.App.YouTrack - Copyright (c) 2019 CaptiveAire

using System;
using System.IO;
using System.Linq;

using JsonFlatFileDataStore;

namespace Seq.App.YouTrack.CreatedIssues
{
    public class CreatedIssueRepository : IDisposable
    {
        readonly DataStore _dataStore;

        public CreatedIssueRepository(string storagePath)
        {
            this._dataStore = new DataStore(Path.Combine(storagePath, "Seq-App-YouTrack.json"));
        }

        public void Dispose()
        {
            this._dataStore?.Dispose();
        }

        public CreatedIssueEvent Insert(CreatedIssueEvent issueEvent)
        {
            var issues = GetCreatedIssueCollection();

            issues.InsertOne(issueEvent);

            // cleanup
            CleanupOldIssues();

            return issueEvent;
        }

        void CleanupOldIssues()
        {
            var issues = GetCreatedIssueCollection();

            issues.DeleteMany(s => s.Created < DateTime.Now.AddMonths(-1));
        }

        public CreatedIssueEvent BySeqId(string seqId)
        {
            var issues = GetCreatedIssueCollection();

            return issues.AsQueryable().FirstOrDefault(s => s.SeqId.Equals(seqId, StringComparison.OrdinalIgnoreCase));
        }

        IDocumentCollection<CreatedIssueEvent> GetCreatedIssueCollection()
        {
            return this._dataStore.GetCollection<CreatedIssueEvent>();
        }
    }
}