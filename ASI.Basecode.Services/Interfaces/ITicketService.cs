﻿using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System.Collections.Generic;
using static ASI.Basecode.Resources.Constants.Enums;

namespace ASI.Basecode.Services.Interfaces
{
    public interface ITicketService
    {
        IEnumerable<TicketViewModel> GetAll();
        void Add(TicketViewModel ticket);
        void Update(TicketViewModel ticket);
        void Delete(string id);
        TicketViewModel GetTicketById(string id);
        IEnumerable<CategoryType> GetCategoryTypes();
        IEnumerable<PriorityType> GetPriorityTypes();
        IEnumerable<StatusType> GetStatusTypes();
    }
}
