using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using StatisticsAnalyzerCore.DataExplore;

namespace StatisticsAnalyzerCore.Modeling
{
        public enum FieldType
    {
        String,
        //Decimal, (Currently decimal can always be a string)
        StringOrDecimal,
        Date
    }

    public class ModelVariable
    {
        public static FieldType ToFieldType(Type type)
        {
            if (type == typeof(string))
            {
                return FieldType.String;                
            }

            if (type == typeof(double) || type == typeof(float) || type == typeof(int))
            {
                return FieldType.StringOrDecimal;
            }

            return FieldType.Date;
        }

        public ModelVariable()
        {
        }

        public ModelVariable(ModelVariable variable)
        {
            ModelVariableId = variable.ModelVariableId;
            Name = variable.Name;
            Type = variable.Type;
            ValueCount = variable.ValueCount;
            Average = variable.Average;
            Std = variable.Std;
        }

        [Key]
        public int ModelVariableId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public FieldType Type { get; set; }

        [Required]
        public int ValueCount { get; set; }

        [Required]
        public double Average { get; set; }

        [Required]
        public double Std { get; set; }
    }

    public class MixedModel
    {
        public MixedModel()
        {
        }

        public MixedModel(MixedModel mixedModel)
        {
            ModelId = mixedModel.ModelId;
            Formula = mixedModel.Formula;
            ModelInterpert = mixedModel.ModelInterpert;
            Data = mixedModel.Data;
            RowCount = mixedModel.RowCount;
            TableAnalysis = mixedModel.TableAnalysis;
            FileName = mixedModel.FileName;
            Variables = new List<ModelVariable>();
            foreach (ModelVariable variable in mixedModel.Variables)
            {
                Variables.Add(new ModelVariable(variable));
            }
        }

        [Key]
        public int ModelId { get; set; }

        [Required]
        public string Formula { get; set; }

        [Required]
        public string FileName { get; set; }

        [Required]
        public string ModelInterpert { get; set; }

        [Required]
        public string ModelIntent { get; set; }

        [Required]
        public string Data { get; set; }

        [Required]
        public List<ModelVariable> Variables { get; set; }

        [Required]
        public long RowCount { get; set; }

        [Required]
        public TableAnalysis TableAnalysis { get; set; }

        public ModelAnalysis ModelAnalysis { get; set; }
    }
}
