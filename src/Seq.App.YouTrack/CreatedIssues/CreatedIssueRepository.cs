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
