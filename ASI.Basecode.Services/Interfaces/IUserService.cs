using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASI.Basecode.Services.Interfaces
{
    public interface IUserService
    {
        IEnumerable<UserViewModel> RetrieveAll();
        void Add(UserViewModel model);
        UserViewModel RetrieveUser(String UserId);
        void Update(UserViewModel model);
        void Delete(String UserId);
        IEnumerable<Role> GetRoles();
        IEnumerable<UserViewModel> FilterUsers(string sortOrder, string currentFilter, string searchString, string roleFilter);
        int CountFilteredUsers(IEnumerable<UserViewModel> users);
        IEnumerable<UserViewModel> PaginateUsers(IEnumerable<UserViewModel> users, int pageSize, int pageNumber);

        bool IsSupportAgent(string userId);
        public bool IsAgentInTeam(string userId);
    }
}
