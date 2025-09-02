using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace EventTicket
{
    public class Customer
    {

        [Required]
        [DisplayName("Email")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string AttendeeEmail { get; set; }

        [Required(ErrorMessage = "You must provide a phone number")]
        [DisplayName("Contact Number")]
        [DataType(DataType.PhoneNumber)]
        [RegularExpression(@"^\(?([0-9]{3})\)?[-. ]?([0-9]{3})[-. ]?([0-9]{4})$", ErrorMessage = "Not a valid phone number")]
        public string AttendeeSms {get;set;}

        [Required]
        [DisplayName("Name")]
        [StringLength(100)]
        public string AttendeeName {get; set; }

        [Required]
        [Range(1,10,ErrorMessage = "Please enter number of tickets required")]
        public int NumberofTickets {get; set;}  =1;
        
        public int EventId {get; set;}  =1;
        
        public string TicketCode { get; set; }="";

        public byte[] QRCode {get; set;} 

        
    }
}