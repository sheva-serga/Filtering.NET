using System.Text.Json;

namespace Filtering.Net;

/// <summary>Configures one <see cref="FilterProperty{TEntity}"/>. Obtain it from <see cref="FilterProperty"/>.</summary>
public sealed class FilterPropertyBuilder<TEntity, TColumn>
{
    private readonly string _field;
    private readonly string _profileName;
    private readonly IReadOnlyList<FilterOperator<TColumn>> _profileOperators;
    private readonly Func<FilterPropertyConfiguration<TColumn>, FilterProperty<TEntity>> _propertyFactory;

    private string? _alias;
    private bool _sortable;
    private SortDir _defaultSortDirection = SortDir.Asc;
    private string[]? _onlyOperators;
    private string[]? _exceptOperators;
    private Func<InterceptContext, TColumn, TColumn>? _scalarInterceptor;
    private Func<InterceptContext, TColumn[], TColumn[]>? _arrayInterceptor;
    private Func<InterceptContext, JsonElement, TColumn>? _rawInterceptor;

    internal FilterPropertyBuilder(
        string field,
        string profileName,
        IReadOnlyList<FilterOperator<TColumn>> profileOperators,
        Func<FilterPropertyConfiguration<TColumn>, FilterProperty<TEntity>> propertyFactory)
    {
        if (string.IsNullOrWhiteSpace(field))
            throw new FilterConfigurationException("A filter property field must be a non-empty string.");
        _field = field;
        _profileName = profileName;
        _profileOperators = profileOperators;
        _propertyFactory = propertyFactory;
    }

    /// <summary>Accepts <paramref name="alias"/> as a second wire key for the property.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> Alias(string alias)
    {
        _alias = alias;
        return this;
    }

    /// <summary>Allows the property in sort lists.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> Sortable(SortDir defaultDirection = SortDir.Asc)
    {
        _sortable = true;
        _defaultSortDirection = defaultDirection;
        return this;
    }

    /// <summary>Restricts the property to the named operators.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> Only(params string[] operatorNames)
    {
        _onlyOperators = operatorNames ?? throw new ArgumentNullException(nameof(operatorNames));
        return this;
    }

    /// <summary>Removes the named operators from the property.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> Except(params string[] operatorNames)
    {
        _exceptOperators = operatorNames ?? throw new ArgumentNullException(nameof(operatorNames));
        return this;
    }

    /// <summary>Transforms every parsed scalar value before the predicate is built.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> Intercept(Func<InterceptContext, TColumn, TColumn> interceptor)
    {
        _scalarInterceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        return this;
    }

    /// <summary>Transforms every parsed array value before the predicate is built.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> InterceptArray(Func<InterceptContext, TColumn[], TColumn[]> interceptor)
    {
        _arrayInterceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        return this;
    }

    /// <summary>Replaces scalar value parsing: the interceptor receives the raw JSON element and returns the typed value.</summary>
    public FilterPropertyBuilder<TEntity, TColumn> InterceptRaw(Func<InterceptContext, JsonElement, TColumn> interceptor)
    {
        _rawInterceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));
        return this;
    }

    /// <summary>Builds the property. Unknown names in <c>Only</c> / <c>Except</c> throw <see cref="FilterConfigurationException"/>.</summary>
    public FilterProperty<TEntity> Build()
    {
        var effectiveOperators = ResolveEffectiveOperators();
        var interception = new PropertyInterception<TColumn>(_scalarInterceptor, _arrayInterceptor, _rawInterceptor);
        return _propertyFactory(new FilterPropertyConfiguration<TColumn>(
            _field, _alias, _sortable, _defaultSortDirection, _profileName, effectiveOperators, interception, IsLifted: false));
    }

    private IReadOnlyList<FilterOperator<TColumn>> ResolveEffectiveOperators()
    {
        var onlySet = ToValidatedSet(_onlyOperators, "Only");
        var exceptSet = ToValidatedSet(_exceptOperators, "Except");
        var effectiveOperators = new List<FilterOperator<TColumn>>();
        foreach (var profileOperator in _profileOperators)
        {
            if (onlySet is not null && !onlySet.Contains(profileOperator.Name)) continue;
            if (exceptSet is not null && exceptSet.Contains(profileOperator.Name)) continue;
            effectiveOperators.Add(profileOperator);
        }
        return effectiveOperators;
    }

    private HashSet<string>? ToValidatedSet(string[]? operatorNames, string optionName)
    {
        if (operatorNames is null) return null;
        var operatorNameSet = new HashSet<string>(operatorNames, StringComparer.OrdinalIgnoreCase);
        foreach (var operatorName in operatorNameSet)
        {
            var isKnown = false;
            foreach (var profileOperator in _profileOperators)
            {
                if (string.Equals(profileOperator.Name, operatorName, StringComparison.OrdinalIgnoreCase))
                {
                    isKnown = true;
                    break;
                }
            }
            if (!isKnown)
                throw new FilterConfigurationException(
                    $"{optionName} on '{_field}' names operator '{operatorName}', which profile '{_profileName}' does not declare.");
        }
        return operatorNameSet;
    }
}

internal sealed record FilterPropertyConfiguration<TColumn>(
    string Field,
    string? Alias,
    bool Sortable,
    SortDir DefaultSortDirection,
    string ProfileName,
    IReadOnlyList<FilterOperator<TColumn>> EffectiveOperators,
    PropertyInterception<TColumn> Interception,
    bool IsLifted);
