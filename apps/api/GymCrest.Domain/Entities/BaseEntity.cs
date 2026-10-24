namespace GymCrest.Domain.Entities
{
    public class BaseEntity
    {
        public Guid Id { get; set; }

        public DateTime CreatedOn { get; set; }

        public Guid CreatedBy {  get; set; }

        public DateTime? ModifiedOn { get; set; }

        public Guid? ModifiedBy {  get; set; }
    }
}
