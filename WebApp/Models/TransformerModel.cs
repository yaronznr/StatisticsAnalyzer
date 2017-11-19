using System.ComponentModel.DataAnnotations;

namespace WebApp.Models
{
    public class TransformerModel
    {
        public TransformerModel() { }

        public TransformerModel(TransformerModel transformerModel)
        {
            TransformerId = transformerModel.TransformerId;
            Action = transformerModel.Action;
            ColumnName = transformerModel.ColumnName;
        }

        [Key]
        public string TransformerId { get; set; }

        [Required]
        public string Action { get; set; }

        [Required]
        public string ColumnName { get; set; }
    }
}