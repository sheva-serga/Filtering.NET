using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Filtering.Net;

internal sealed class ValueOperator<TColumn, TValue>(
    string name,
    Expression<Func<TColumn, TValue, bool>> predicate,
    TryParseValue<TValue>? parser) : FilterOperator<TColumn>(name)
{
    // No parser means the value is a typed value deserialized through the definition's resolver.
    internal override bool RequiresSerializerOptions => parser is null;

    internal override BoundOperator Bind(OperatorBinding<TColumn> binding)
    {
        var columnParameter = predicate.Parameters[0];
        var valueParameter = predicate.Parameters[1];

        if (binding.NullableSupport is null)
        {
            var splicedBody = ExpressionSplicer.ReplaceParameter(predicate.Body, columnParameter, binding.Accessor.Body);
            return new BoundValueOperator(Name, parser, binding, splicedBody, valueParameter, nullableSupportForArrayValue: null);
        }

        var lifter = new NullableColumnLifter(columnParameter, binding.Accessor.Body, valueParameter, binding.NullableSupport);
        var liftedBody = lifter.Lift(predicate.Body);
        return lifter.NullableArrayValueParameter is { } nullableArrayParameter
            ? new BoundValueOperator(Name, parser, binding, liftedBody, nullableArrayParameter, binding.NullableSupport)
            : new BoundValueOperator(Name, parser, binding, liftedBody, valueParameter, nullableSupportForArrayValue: null);
    }

    private sealed class BoundValueOperator : BoundOperator
    {
        private readonly string _operatorName;
        private readonly TryParseValue<TValue>? _parser;
        private readonly string _propertyPath;
        private readonly Expression _splicedBody;
        private readonly ParameterExpression _valueParameter;
        private readonly INullableColumnSupport? _nullableSupportForArrayValue;
        private readonly Func<InterceptContext, TValue, TValue>? _valueInterceptor;
        private readonly Func<InterceptContext, JsonElement, TValue>? _rawInterceptor;

        public BoundValueOperator(
            string operatorName,
            TryParseValue<TValue>? parser,
            OperatorBinding<TColumn> binding,
            Expression splicedBody,
            ParameterExpression valueParameter,
            INullableColumnSupport? nullableSupportForArrayValue)
        {
            _operatorName = operatorName;
            _parser = parser;
            _propertyPath = binding.PropertyPath;
            _splicedBody = splicedBody;
            _valueParameter = valueParameter;
            _nullableSupportForArrayValue = nullableSupportForArrayValue;

            // Interceptors apply to parser-extracted values only. The casts succeed exactly when
            // TValue is TColumn (scalar, raw) or TColumn[] (array), because the delegate types then coincide.
            if (parser is not null)
            {
                _valueInterceptor =
                    binding.Interception.ScalarInterceptor as Func<InterceptContext, TValue, TValue>
                    ?? binding.Interception.ArrayInterceptor as Func<InterceptContext, TValue, TValue>;
                _rawInterceptor = binding.Interception.RawInterceptor as Func<InterceptContext, JsonElement, TValue>;
            }
        }

        public override void Validate(FilterLeaf leaf, string path, List<FilterValidationError> errors, JsonSerializerOptions? serializerOptions)
        {
            try
            {
                if (!TryExtract(leaf, serializerOptions, out _, out var extractionError))
                {
                    LeafValidation.AddTypeError(errors, leaf, path, extractionError);
                }
            }
            catch (FilterValidationException interceptorRejection)
            {
                var interceptorMessage = interceptorRejection.Result.Errors.Count > 0
                    ? interceptorRejection.Result.Errors[0].Message
                    : interceptorRejection.Message;
                LeafValidation.AddInterceptorError(errors, leaf, path, interceptorMessage);
            }
        }

        public override Expression BuildBody(FilterLeaf leaf, JsonSerializerOptions? serializerOptions)
        {
            if (!TryExtract(leaf, serializerOptions, out var typedValue, out var extractionError, out var extractionFailure))
            {
                if (_parser is null)
                {
                    throw new FilterDispatchException(
                        $"Apply-time value extraction failed for custom operator '{_operatorName}': {extractionError}",
                        extractionFailure!);
                }
                var extractionKind = typeof(TValue).IsArray ? "array" : "value";
                throw new FilterDispatchException($"Apply-time {extractionKind} extraction failed: {extractionError}");
            }

            Expression valueExpression;
            if (_nullableSupportForArrayValue is not null)
            {
                if (typedValue is null)
                    throw new FilterDispatchException(
                        $"Operator '{_operatorName}' on '{_propertyPath}' produced a null array value (validation should have caught this).");
                valueExpression = _nullableSupportForArrayValue.CreateNullableArrayExpression(typedValue);
            }
            else
            {
                valueExpression = FilterValueHolder<TValue>.AsExpression(typedValue);
            }
            return ExpressionSplicer.ReplaceParameter(_splicedBody, _valueParameter, valueExpression);
        }

        private bool TryExtract(FilterLeaf leaf, JsonSerializerOptions? serializerOptions, out TValue typedValue, out string extractionError) =>
            TryExtract(leaf, serializerOptions, out typedValue, out extractionError, out _);

        private bool TryExtract(
            FilterLeaf leaf,
            JsonSerializerOptions? serializerOptions,
            out TValue typedValue,
            out string extractionError,
            out Exception? extractionFailure)
        {
            extractionFailure = null;
            if (_parser is null)
            {
                return TryDeserialize(leaf, serializerOptions, out typedValue, out extractionError, out extractionFailure);
            }

            var interceptContext = new InterceptContext(_propertyPath, leaf.Field, leaf.Operator);
            if (_rawInterceptor is not null)
            {
                typedValue = _rawInterceptor(interceptContext, leaf.Value);
                extractionError = string.Empty;
                return true;
            }

            if (!_parser(leaf.Value, out typedValue, out extractionError)) return false;
            if (_valueInterceptor is not null)
            {
                typedValue = _valueInterceptor(interceptContext, typedValue);
            }
            return true;
        }

        private bool TryDeserialize(
            FilterLeaf leaf,
            JsonSerializerOptions? serializerOptions,
            out TValue typedValue,
            out string extractionError,
            out Exception? extractionFailure)
        {
            var resolvedSerializerOptions = serializerOptions
                ?? throw new FilterConfigurationException(
                    $"Operator '{_operatorName}' on '{_propertyPath}' takes a typed value, but the filter definition was built without JsonSerializerOptions.");

            // System.Text.Json turns a JSON null into a null TValue without complaining, which would only
            // surface as a dereference at query time. Every parser-based extractor rejects Null, so this does too.
            if (leaf.Value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
            {
                typedValue = default!;
                extractionError = $"Expected a JSON value for operator '{_operatorName}', got {leaf.Value.ValueKind}.";
                extractionFailure = null;
                return false;
            }

            try
            {
                var typeInfo = (JsonTypeInfo<TValue>)resolvedSerializerOptions.GetTypeInfo(typeof(TValue));
                typedValue = leaf.Value.Deserialize(typeInfo)!;
                extractionError = string.Empty;
                extractionFailure = null;
                return true;
            }
            catch (Exception deserializationFailure)
                when (deserializationFailure is JsonException
                   || deserializationFailure is NotSupportedException
                   || deserializationFailure is InvalidOperationException
                   || deserializationFailure is InvalidCastException)
            {
                typedValue = default!;
                extractionError = deserializationFailure.Message;
                extractionFailure = deserializationFailure;
                return false;
            }
        }
    }
}
