// Seq.App.YouTrack - Copyright (c) 2017 CaptiveAire

using System;

namespace Seq.App.YouTrack.CreatedIssues
{
    public class CreatedIssueEvent
    {
        public CreatedIssueEvent()
        {
            this.Created = DateTime.Now;
        }

        public int Id { get; set; }

        public string SeqId { get; set; }

        public string YouTrackId { get; set; }

        public DateTime Created { get; set; }
    }
}