using AutoMapper;
using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace ASI.Basecode.WebApp
{
    // AutoMapper configuration
    internal partial class StartupConfigurer
    {
        /// <summary>
        /// Configure auto mapper
        /// </summary>
        private void ConfigureAutoMapper()
        {
            var mapperConfiguration = new MapperConfiguration(config =>
            {
                config.AddProfile(new AutoMapperProfileConfiguration());
            });

            this._services.AddSingleton<IMapper>(sp => mapperConfiguration.CreateMapper());
        }

        private class AutoMapperProfileConfiguration : Profile
        {
            public AutoMapperProfileConfiguration()
            {
                CreateMap<UserViewModel, User>();
                CreateMap<User, UserViewModel>();
                CreateMap<TicketViewModel, Ticket>();
                CreateMap<Ticket, TicketViewModel>()
                        .ForMember(dest => dest.Attachment, opt => opt.MapFrom(src => src.Attachments.FirstOrDefault()))
                        .ForMember(dest => dest.Agent, opt => opt.MapFrom(src => src.TicketAssignment != null
                            ? src.TicketAssignment.Team.TeamMembers.FirstOrDefault(tm => tm.User.RoleId == "Support Agent").User
                            : null))
                        .ForMember(dest => dest.TicketAssignment, opt => opt.MapFrom(src => src.TicketAssignment))
                        .ForMember(dest => dest.Feedback, opt => opt.MapFrom(src => src.Feedback));
                CreateMap<Feedback, FeedbackViewModel>();
                CreateMap<FeedbackViewModel, Feedback>();
                CreateMap<Team, TeamViewModel>();
                CreateMap<TeamViewModel, Team>();
                CreateMap<KnowledgeBaseViewModel, KnowledgeBaseArticle>();
                CreateMap<KnowledgeBaseArticle, KnowledgeBaseViewModel>();
                CreateMap<IEnumerable<KnowledgeBaseViewModel>, IEnumerable<KnowledgeBaseArticle>>();
            }
        }
    }
}
