using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Exceptions
{
    public class TeamExceptions
    {
        public class TeamNameAlreadyExistsException : Exception
        {
            public string Id { get; set; }
            public TeamNameAlreadyExistsException(string message) : base(message) { }
            public TeamNameAlreadyExistsException(string message, string id) : base(message) 
            {
                Id = id;
            }
        }

        public class TeamHasUnresolvedTicketsException : Exception
        {
            public string Id { get; set; }
            public TeamHasUnresolvedTicketsException(string message) : base(message) { }
            public TeamHasUnresolvedTicketsException(string message, string id) : base(message)
            {
                Id = id;
            }
        }

        public class TeamHasMembersException : Exception
        {
            public string Id { get; set; }
            public TeamHasMembersException(string message) : base(message) { }
            public TeamHasMembersException(string message, string id) : base(message)
            {
                Id = id;
            }
        }

        public class NoAgentSelectedException : Exception
        {
            public string Id { get; set; }
            public NoAgentSelectedException(string message) : base(message) { }
            public NoAgentSelectedException(string message, string id) : base(message)
            {
                Id = id;
            }
        }
    }
}
