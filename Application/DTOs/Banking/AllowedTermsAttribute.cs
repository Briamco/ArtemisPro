using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Application.DTOs.Banking
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class AllowedTermsAttribute : ValidationAttribute
    {
        private static readonly int[] AllowedTerms = new[] { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 };

        public AllowedTermsAttribute()
        {
            ErrorMessage = "El plazo seleccionado no es válido. Los plazos permitidos son: 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 meses.";
        }

        public override bool IsValid(object value)
        {
            if (value == null) return true; // Required attribute handles null check
            if (int.TryParse(value.ToString(), out int term))
            {
                return AllowedTerms.Contains(term);
            }
            return false;
        }
    }
}
