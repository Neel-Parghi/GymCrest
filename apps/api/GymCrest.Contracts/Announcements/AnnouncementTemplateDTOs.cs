namespace GymCrest.Contracts.Announcements
{
    public class AnnouncementTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; } // 0 = Custom, 1 = Inactivity, 2 = ExpiredMembership
        public string TitleTemplate { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class AnnouncementTemplateResponse
    {
        public Guid Id { get; set; }
        public Guid GymId { get; set; }
        public Guid? BranchId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Type { get; set; }
        public string TitleTemplate { get; set; } = string.Empty;
        public string MessageTemplate { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
    }
}
