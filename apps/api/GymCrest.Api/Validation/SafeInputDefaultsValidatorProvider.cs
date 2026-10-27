using System.Collections;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GymCrest.Api.Validation
{
    public class SafeInputDefaultsValidatorProvider : IMetadataBasedModelValidatorProvider
    {
        public const int MaxTextLength = 500;
        public const int MaxListItems = 200;
        public const double MaxNumber = 1_000_000_000;

        public bool HasValidators(Type modelType, IList<object> attributes) => Classify(modelType) != Kind.None;

        public void CreateValidators(ModelValidatorProviderContext context)
        {
            IReadOnlyList<object> attributes = context.ValidatorMetadata;
            bool hasOwnLength = attributes.Any(a => a is MaxLengthAttribute or StringLengthAttribute);
            bool hasOwnRange = attributes.Any(a => a is RangeAttribute);

            IModelValidator? validator = Classify(context.ModelMetadata.ModelType) switch
            {
                Kind.Text when !hasOwnLength => new TextLengthValidator(),
                Kind.List when !hasOwnLength => new ListSizeValidator(),
                Kind.Number when !hasOwnRange => new NumberRangeValidator(),
                Kind.Enum => new DefinedEnumValidator(),
                _ => null
            };

            if (validator != null)
            {
                context.Results.Add(new ValidatorItem { Validator = validator, IsReusable = true });
            }
        }

        private enum Kind { None, Text, List, Number, Enum }

        private static Kind Classify(Type modelType)
        {
            Type type = Nullable.GetUnderlyingType(modelType) ?? modelType;

            if (type == typeof(string)) return Kind.Text;
            if (type.IsEnum) return Kind.Enum;
            if (type == typeof(int) || type == typeof(long) || type == typeof(short) ||
                type == typeof(decimal) || type == typeof(double) || type == typeof(float)) return Kind.Number;
            if (type != typeof(byte[]) && typeof(IEnumerable).IsAssignableFrom(type)) return Kind.List;

            return Kind.None;
        }

        private static IEnumerable<ModelValidationResult> Fail(string message) => [new ModelValidationResult(string.Empty, message)];

        private static string FieldName(ModelValidationContext context) =>
            context.ModelMetadata.GetDisplayName() ?? "Value";

        private sealed class TextLengthValidator : IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context) =>
                context.Model is string text && text.Length > MaxTextLength
                    ? Fail($"{FieldName(context)} must be {MaxTextLength} characters or fewer.")
                    : [];
        }

        private sealed class ListSizeValidator : IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context) =>
                context.Model is ICollection list && list.Count > MaxListItems
                    ? Fail($"{FieldName(context)} can have at most {MaxListItems} items.")
                    : [];
        }

        private sealed class NumberRangeValidator : IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                if (context.Model == null) return [];

                double value = Convert.ToDouble(context.Model);
                if (value < 0) return Fail($"{FieldName(context)} cannot be negative.");
                if (value > MaxNumber) return Fail($"{FieldName(context)} is too large.");
                return [];
            }
        }

        private sealed class DefinedEnumValidator : IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context) =>
                context.Model != null && !Enum.IsDefined(context.Model.GetType(), context.Model)
                    ? Fail($"{FieldName(context)} has an invalid value.")
                    : [];
        }
    }
}
