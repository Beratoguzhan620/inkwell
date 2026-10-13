using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace BlogApp.MVC.Infrastructure
{
    /// <summary>
    /// Gives the standard DataAnnotations attributes Turkish default messages, so view models
    /// only need a <see cref="DisplayAttribute"/> instead of repeating every message.
    /// </summary>
    public class TurkishValidationMetadataProvider : IValidationMetadataProvider
    {
        public void CreateValidationMetadata(ValidationMetadataProviderContext context)
        {
            foreach (var attribute in context.ValidationMetadata.ValidatorMetadata.OfType<ValidationAttribute>())
            {
                if (!string.IsNullOrEmpty(attribute.ErrorMessage) || attribute.ErrorMessageResourceName is not null)
                {
                    continue;
                }

                var message = attribute switch
                {
                    RequiredAttribute => "{0} alanı zorunludur.",
                    EmailAddressAttribute => "Geçerli bir e-posta adresi girin.",
                    MinLengthAttribute => "{0} en az {1} karakter olmalıdır.",
                    MaxLengthAttribute => "{0} en fazla {1} karakter olabilir.",
                    StringLengthAttribute => "{0} en fazla {1} karakter olabilir.",
                    RangeAttribute => "{0} {1} ile {2} arasında olmalıdır.",
                    CompareAttribute => "{0} ile {1} aynı olmalıdır.",
                    _ => null,
                };

                if (message is not null)
                {
                    attribute.ErrorMessage = message;
                }
            }
        }
    }
}
