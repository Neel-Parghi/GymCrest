namespace GymCrest.Domain.Entities
{
    public class Address : BaseEntity
    {
        public string Address1 {  get; set; } = string.Empty;
       
        public string? Address2 {  get; set; } = string.Empty;
        
        public string City {  get; set; } = string.Empty;
        
        public string State {  get; set; } = string.Empty;
        
        public string Country {  get; set; } = string.Empty;
        
        public string PostalCode {  get; set; } = string.Empty;
    }
}
