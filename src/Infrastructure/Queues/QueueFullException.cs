using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace MyApp.Infrastructure.Queues
{
    public class QueueFullException : Exception
    {
        public QueueFullException() { }
        public QueueFullException(string message) : base(message) { }
        public QueueFullException(string message, Exception inner) : base(message, inner) { }
    }
}

