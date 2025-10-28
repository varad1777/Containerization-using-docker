

using System.ComponentModel.DataAnnotations;

namespace MyApp.Domain.Entities
{
    public class Asset
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();


        [Required]
        public string Name { get; set; }


        [Required]
        public string Description { get; set; }


        // adding the owner in you application 
        public string UserId { get; set; } // for indentifytin the user 
        public ApplicationUser User { get; set; } //  for easy navigation 

        public List<Signal> Signals { get; set; } = new();
    }
}

