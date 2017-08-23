using System.IO;
using System.Linq;

using LiteDB;

namespace Seq.App.YouTrack.CreatedIssues
{
    public class CreatedIssueRespository
    {
        readonly string _storagePath;

        public CreatedIssueRespository(string storagePath)
        {
            this._storagePath = storagePath;
        }

        public LiteDatabase GetDb()
        {
            return new LiteDatabase(Path.Combine(this._storagePath, "Seq.App.YouTrack.db"));
        }

        public CreatedIssueEvent Insert(CreatedIssueEvent issueEvent)
        {
            using (var db = this.GetDb())
            {
                var issues = GetCreatedIssueCollection(db);

                issues.Insert(issueEvent);

                issues.EnsureIndex(x => x.SeqId);

                return issueEvent;
            }
        }

        public CreatedIssueEvent BySeqId(string seqId)
        {
            using (var db = this.GetDb())
            {
                var issues = GetCreatedIssueCollection(db);

                return issues.Find(s => s.SeqId == seqId).FirstOrDefault();
            }
        }

        static LiteCollection<CreatedIssueEvent> GetCreatedIssueCollection(LiteDatabase db)
        {
            return db.GetCollection<CreatedIssueEvent>(typeof(CreatedIssueEvent).Name);
        }
    }
}