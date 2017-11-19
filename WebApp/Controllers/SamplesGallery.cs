using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;

namespace WebApp.Controllers
{
    public class Sample
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }
    }

    public class SamplesGallery
    {
        [Required]
        public List<Sample> Samples { get; set; }
    }
}
