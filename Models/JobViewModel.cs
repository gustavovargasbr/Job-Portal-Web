using System.ComponentModel.DataAnnotations;

namespace Job_Portal_Web.Models
{
    public class JobViewModel
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "Please select a company")]
        public int CompanyId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal Salary { get; set; }
        public string? CompanyName { get; set; }
        public List<CompanyViewModel> Companies { get; set; } 
    }


}
