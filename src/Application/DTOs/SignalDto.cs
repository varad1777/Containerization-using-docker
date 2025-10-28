using System.ComponentModel.DataAnnotations;

namespace MyApp.Application.DTOs
{
    public class SignalDto
    {

        public int Id { get; set; } // 0 for create

        [Required(ErrorMessage = "Signal Name is Required...")]
        [Length(3, 15, ErrorMessage = "Signal Name must be between {1} and {2} character.")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Signal Description is Required...")]
        [Length(3, 150, ErrorMessage = "Signal Description must be between {1} and {2} character.")]
        public string Description { get; set; }
        [Range(1, 100)]
        public double Strength { get; set; }
        public Guid AssetId { get; set; }

    }
}