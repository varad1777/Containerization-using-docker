
using System.ComponentModel.DataAnnotations;


namespace MyApp.Application.DTOs
{
    public class AssetDto
    {
        [Required(ErrorMessage = "Asset Name is Required...")]
        [Length(3, 15, ErrorMessage = "Asset Name must be between {1} and {2} character.")]
        [RegularExpression(@"^[a-zA-Z0-9_ ]+$", ErrorMessage = "Asset Name cannot contain special characters.")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Asset Description is Required...")]
        [Length(3, 150, ErrorMessage = "Asset Description Name must be between {1} and {2} character.")]
        public string Description { get; set; }


        public List<SignalDto> Signals { get; set; } = new();
    }
}