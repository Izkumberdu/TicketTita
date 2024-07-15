using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Exceptions
{
    public class TicketExceptions
    {
        public class InvalidFileException : Exception
        {
            public string Id { get; }
            public InvalidFileException(string message, string id) : base(message) 
            {
                Id = id;
            }
        }

        public class NoChangesException : Exception
        {
            public string Id { get; }
            public NoChangesException(string message) : base(message) { }
            public NoChangesException(string message, string id) : base(message)
            {
                Id = id;
            }
        }

        public class DuplicateTicketException : Exception
        {
            public DuplicateTicketException(string message) : base(message) { }
        }
    }
}
