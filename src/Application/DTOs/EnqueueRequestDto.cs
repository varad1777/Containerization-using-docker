using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Application.DTOs
{
    public class EnqueueRequest
    {
        public string ColumnName { get; set; }
        public string? userId { get; set; }
        public string? userName { get; set; }
        public Guid AssetId { get; set; }
    }

    public class EnqueueRequestDto
    {
        public string ColumnName { get; set; } = string.Empty;
        public Guid AssetId { get; set; }
    }
}
