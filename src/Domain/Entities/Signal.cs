

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyApp.Domain.Entities
{
    public class Signal
    {
        [Key]
        public int Id { get; set; }


        [Required]
        public string Name { get; set; }


        [Required]
        public string Description { get; set; }

        [Range(1, 100)]
        public double Strength { get; set; }

        // Foreign key
        public Guid AssetId { get; set; }
        [JsonIgnore]
        public Asset Asset { get; set; }

        // here The AssetID is the forign key 
        // and the Asset is basically use for the navigation 
        // like if we do not give the navigatin proporty then , 
        // first we need to get the id from the AssetId and then get the Asset by using this id 
        // but we include the navigation property, then we will get the Asset also by using (include ->(Linq))

    }
}

// here Asset we use for ,only the Navigation 
// so while converting the object into the json like while sending the data from the APi
// we dont have to send Asset in this 
// otherwise it will give the recursion and error 
// thats why we use the justIgnore attribute over there 