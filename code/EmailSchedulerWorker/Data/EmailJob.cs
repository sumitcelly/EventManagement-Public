using System;

namespace EmailSchedulerWorker.Data
{
    public class EmailJob
    {
        public int Id { get; set; }
        public string Recipient { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
        public DateTime SendAtUtc { get; set; }
        public string Status { get; set; } = "pending"; // pending, queued, sent
    }
}
