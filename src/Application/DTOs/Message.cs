using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.DTOs
{
    using System;

    namespace MyApp.Messaging
    {
        // AvgRequestMessage — published by API to avg_requests
        public record AvgRequestMessage
        {
            public Guid RequestId { get; init; } = Guid.NewGuid();
            public Guid AssetId { get; init; }
            public string ColumnName { get; init; } = default!;
            public string? UserId { get; init; }
            public string? UserName { get; init; }
            public DateTime EnqueuedAtUtc { get; init; } = DateTime.UtcNow;
        }

        // AvgResultMessage — published by Worker to avg_results
        public record AvgResultMessage
        {
            public Guid RequestId { get; init; }
            public Guid AssetId { get; init; }
            public string ColumnName { get; init; } = default!;
            public double Average { get; init; }
            public string? Error { get; init; }
            public string? UserId { get; init; }
            public DateTime CompletedAtUtc { get; init; } = DateTime.UtcNow;
        }

        // QueueFullException used by publisher
        public class QueueFullException : Exception
        {
            public QueueFullException() : base("The message queue is full. Please try again later.") { }
            public QueueFullException(string message) : base(message) { }
            public QueueFullException(string message, Exception inner) : base(message, inner) { }
        }
    }

}
