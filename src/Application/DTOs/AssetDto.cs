
using System.ComponentModel.DataAnnotations;


namespace MyApp.Application.DTOs
{
    public class AssetDto
    {
        [Required(ErrorMessage = "Asset Name is Required...")]
        [Length(3, 15, ErrorMessage = "Asset Name must be between {1} and {2} character.")]
        public string Name { get; set; }


        [Required(ErrorMessage = "Asset Description is Required...")]
        [Length(3, 100, ErrorMessage = "Asset Description Name must be between {1} and {2} character.")]
        public string Description { get; set; }


        public List<SignalDto> Signals { get; set; } = new();
    }
}