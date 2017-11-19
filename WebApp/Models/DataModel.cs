using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class GroupingIndexValue
    {
        [Required]
        public List<Tuple<string, string>> GroupingVariable { get; set; }

        [Required]
        public string Value { get; set; }
    }

    public class DataModel
    {
        [Required]
        public List<GroupingIndexValue> ValueList { get; set; }
    }
}
