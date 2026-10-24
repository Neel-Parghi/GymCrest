using GymCrest.Domain.Enums;
using GymCrest.Domain.Interface;
using System.ComponentModel.DataAnnotations.Schema;

namespace GymCrest.Domain.Entities
{
    public class AnnouncementTemplate : BaseEntity, IBranchScoped
    {
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }

        public string Name { get; set; } = string.Empty;
        public TemplateType Type { get; set; } = TemplateType.Custom;
        
        public string TitleTemplate { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        [ForeignKey(nameof(GymId))]
        public Gym Gym { get; set; } = null!;

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }
    }
}
