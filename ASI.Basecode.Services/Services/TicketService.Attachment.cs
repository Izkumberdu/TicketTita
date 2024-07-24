using ASI.Basecode.Data.Models;
using ASI.Basecode.Services.Interfaces;
using ASI.Basecode.Services.ServiceModels;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.IO;
using System;
using System.Threading.Tasks;
using static ASI.Basecode.Services.Exceptions.TicketExceptions;
using System.Net.Sockets;
using System.Security.Claims;

namespace ASI.Basecode.Services.Services
{
    public partial class TicketService : ITicketService
    {
        /// <summary>
        /// Calls the repository to add a new attachment.
        /// </summary>
        /// <param name="attachment">The attachment</param>
        /// <param name="ticket">The ticket</param>
        public async Task AddAttachmentAsync(Attachment attachment, Ticket ticket)
        {
            attachment.Ticket = ticket;
            if (attachment.Ticket != null && ticket.Attachments.Count <= 0)
            {
                ticket.Attachments.Add(attachment);
                await _repository.AddAttachmentAsync(attachment);
            }
            if (await _repository.FindByIdAsync(ticket.TicketId) != null)
            {
                await LogActivityAsync(ticket, _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value, "Add Attachment", $"Attachment added: {attachment.Name}");
            }
        }

        /// <summary>
        /// Remove attachment by ticket identifier.
        /// </summary>
        /// <param name="id">The ticket identifier</param>
        private async Task RemoveAttachmentByTicketIdAsync(string id)
        {
            var attachment = await GetAttachmentByTicketIdAsync(id);
            if (attachment != null)
                await _repository.RemoveAttachmentAsync(attachment);
        }

        /// <summary>
        /// Helper method to create a new ticket attachment.
        /// </summary>
        /// <param name="model">The ticket</param>
        private async Task HandleAttachmentAsync(TicketViewModel model)
        {
            var allowedFileTypesString = Resources.Views.FileValidation.AllowedFileTypes;
            var allowedFileTypes = new HashSet<string>(allowedFileTypesString.Split(','), StringComparer.OrdinalIgnoreCase);
            long maxFileSize = Convert.ToInt32(Resources.Views.FileValidation.MaxFileSizeMB) * 1024 * 1024;

            if (model.File != null && model.File.Length > 0)
            {
                if (allowedFileTypes.Contains(model.File.ContentType) && model.File.Length <= maxFileSize)
                {
                    using (var stream = new MemoryStream())
                    {
                        await model.File.CopyToAsync(stream);
                        model.Attachment = new Attachment
                        {
                            AttachmentId = Guid.NewGuid().ToString(),
                            Name = model.File.FileName,
                            Content = stream.ToArray(),
                            Type = model.File.ContentType,
                            UploadedDate = DateTime.Now
                        };
                    }
                }
                else
                {
                    throw new TicketException("File type not allowed or file size exceeds the limit.", model.TicketId);
                }
            }
        }
    }
}
