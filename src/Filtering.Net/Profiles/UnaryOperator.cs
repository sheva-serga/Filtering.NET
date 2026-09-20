using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

internal sealed class UnaryOperator<TColumn>(string name, LambdaExpression predicate) : FilterOperator<TColumn>(name)
{
    internal override bool RequiresSerializerOptions => false;

    internal override BoundOperator Bind(OperatorBinding<TColumn> binding)
    {
        var columnParameter = predicate.Parameters[0];
        var accessorBody = binding.Accessor.Body;

        Expression splicedBody;
        if (columnParameter.Type == accessorBody.Type)
        {
            splicedBody = ExpressionSplicer.ReplaceParameter(predicate.Body, columnParameter, accessorBody);
        }
        else if (binding.NullableSupport is not null)
        {
            splicedBody = new NullableColumnLifter(columnParameter, accessorBody, valueParameter: null, binding.NullableSupport)
                .Lift(predicate.Body);
        }
        else
        {
            // Predicate over TColumn? applied to a non-nullable column, e.g. isNull on int: always false, as C# evaluates it.
            splicedBody = ExpressionSplicer.ReplaceParameter(
                predicate.Body, columnParameter, Expression.Convert(accessorBody, columnParameter.Type));
        }
        return new BoundUnaryOperator(splicedBody);
    }

    private sealed class BoundUnaryOperator(Expression splicedBody) : BoundOperator
    {
        public override void Validate(FilterLeaf leaf, string path, List<FilterValidationError> errors, FilterValueContext valueContext)
        {
            if (leaf.Value.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
            {
                LeafValidation.AddNoValueError(errors, leaf, path);
            }
        }

        public override Expression BuildBody(FilterLeaf leaf, FilterValueContext valueContext) => splicedBody;
    }
}
