using System.Linq.Expressions;
using System.Text.Json;

namespace Filtering.Net;

// TProperty is what the accessor returns: TColumn, or Nullable<TColumn> when nullableSupport is set.
internal sealed class ColumnFilterProperty<TEntity, TProperty, TColumn> : FilterProperty<TEntity>
{
    private readonly FilterPropertyConfiguration<TColumn> _configuration;
    private readonly Expression<Func<TEntity, TProperty>> _accessor;
    private readonly INullableColumnSupport? _nullableSupport;
    private readonly Dictionary<string, BoundOperator> _boundOperatorsByName;
    private readonly string[] _operatorNames;

    public ColumnFilterProperty(
        FilterPropertyConfiguration<TColumn> configuration,
        Expression<Func<TEntity, TProperty>> accessor,
        INullableColumnSupport? nullableSupport)
        : base(
            configuration.Field,
            configuration.Alias,
            configuration.Sortable,
            configuration.DefaultSortDirection,
            configuration.ProfileName,
            configuration.IsLifted)
    {
        _configuration = configuration;
        _accessor = accessor;
        _nullableSupport = nullableSupport;

        var operatorBinding = new OperatorBinding<TColumn>(accessor, nullableSupport, configuration.Interception, configuration.Field);
        _boundOperatorsByName = new Dictionary<string, BoundOperator>(StringComparer.OrdinalIgnoreCase);
        _operatorNames = new string[configuration.EffectiveOperators.Count];
        var typedValueOperators = new List<string>();
        for (var operatorIndex = 0; operatorIndex < configuration.EffectiveOperators.Count; operatorIndex++)
        {
            var filterOperator = configuration.EffectiveOperators[operatorIndex];
            if (_boundOperatorsByName.ContainsKey(filterOperator.Name))
                throw new FilterConfigurationException($"Property '{configuration.Field}' declares operator '{filterOperator.Name}' more than once.");
            _boundOperatorsByName.Add(filterOperator.Name, filterOperator.Bind(operatorBinding));
            _operatorNames[operatorIndex] = filterOperator.Name;
            if (filterOperator.RequiresSerializerOptions) typedValueOperators.Add(filterOperator.Name);
        }
        TypedValueOperators = typedValueOperators;
    }

    public override IReadOnlyCollection<string> Operators => _operatorNames;

    internal override IReadOnlyList<string> TypedValueOperators { get; }

    internal override void ValidateLeaf(FilterLeaf leaf, string path, List<FilterValidationError> errors, JsonSerializerOptions? serializerOptions)
    {
        if (_boundOperatorsByName.TryGetValue(leaf.Operator, out var boundOperator))
        {
            boundOperator.Validate(leaf, path, errors, serializerOptions);
            return;
        }
        LeafValidation.AddOperatorError(errors, leaf, path, Field);
    }

    internal override Expression<Func<TEntity, bool>> BuildPredicate(FilterLeaf leaf, JsonSerializerOptions? serializerOptions)
    {
        if (!_boundOperatorsByName.TryGetValue(leaf.Operator, out var boundOperator))
            throw new FilterDispatchException($"Unknown operator '{leaf.Operator}' for '{Field}' (validation should have caught this).");
        return Expression.Lambda<Func<TEntity, bool>>(boundOperator.BuildBody(leaf, serializerOptions), _accessor.Parameters[0]);
    }

    internal override IOrderedQueryable<TEntity> ApplySort(IQueryable<TEntity> query, IOrderedQueryable<TEntity>? orderedQuery, SortDir direction) =>
        (direction, orderedQuery) switch
        {
            (SortDir.Asc, null) => query.OrderBy(_accessor),
            (SortDir.Desc, null) => query.OrderByDescending(_accessor),
            (SortDir.Asc, not null) => orderedQuery.ThenBy(_accessor),
            (SortDir.Desc, not null) => orderedQuery.ThenByDescending(_accessor),
            _ => throw new FilterDispatchException($"Unreachable sort direction '{direction}' for property '{Field}' (validation should have caught this).")
        };

    public override FilterProperty<TParent> LiftThrough<TParent>(
        Expression<Func<TParent, TEntity>> navigation,
        string prefix,
        bool disableSorting)
    {
        if (navigation is null) throw new ArgumentNullException(nameof(navigation));
        if (string.IsNullOrWhiteSpace(prefix))
            throw new FilterConfigurationException($"Lifting '{Field}' through a navigation requires a non-empty prefix.");

        // The CLR path follows the navigation members; the alias follows the prefix. They coincide unless a custom prefix was given.
        var liftedField = NavigationPath(navigation.Body, prefix) + "." + Field;
        var liftedAlias = prefix + "." + (Alias ?? Field);

        var liftedConfiguration = _configuration with
        {
            Field = liftedField,
            Alias = string.Equals(liftedAlias, liftedField, StringComparison.OrdinalIgnoreCase) ? null : liftedAlias,
            Sortable = Sortable && !disableSorting,
            IsLifted = true,
        };
        return new ColumnFilterProperty<TParent, TProperty, TColumn>(
            liftedConfiguration,
            ExpressionSplicer.Compose(navigation, _accessor),
            _nullableSupport);
    }

    // Every hop of the navigation belongs in the CLR path, so two nestings that end in the same member stay distinct.
    private static string NavigationPath(Expression navigationBody, string prefix)
    {
        var memberNames = new List<string>();
        var currentExpression = navigationBody;
        while (currentExpression is MemberExpression memberExpression)
        {
            memberNames.Add(memberExpression.Member.Name);
            currentExpression = memberExpression.Expression!;
        }
        if (memberNames.Count == 0 || currentExpression is not ParameterExpression) return prefix;

        memberNames.Reverse();
        return string.Join(".", memberNames);
    }
}
