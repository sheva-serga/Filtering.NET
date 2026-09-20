namespace Filtering.Net;

/// <summary>The runtime form of a filter profile: a named, immutable set of operators for columns of type <typeparamref name="TColumn"/>.</summary>
public sealed class FilterProfile<TColumn>
{
    private readonly Dictionary<string, FilterOperator<TColumn>> _operatorsByName;

    private FilterProfile(string name, Dictionary<string, FilterOperator<TColumn>> operatorsByName)
    {
        Name = name;
        _operatorsByName = operatorsByName;
    }

    /// <summary>The profile name used in configuration error messages.</summary>
    public string Name { get; }

    /// <summary>Operators keyed by name, case-insensitively, in declaration order.</summary>
    public IReadOnlyDictionary<string, FilterOperator<TColumn>> Operators => _operatorsByName;

    /// <summary>Creates a standalone profile. Two operators with the same name throw <see cref="FilterConfigurationException"/>.</summary>
    public static FilterProfile<TColumn> Create(string name, params FilterOperator<TColumn>[] operators)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new FilterConfigurationException("A filter profile name must be a non-empty string.");
        if (operators is null) throw new ArgumentNullException(nameof(operators));

        var operatorsByName = new Dictionary<string, FilterOperator<TColumn>>(StringComparer.OrdinalIgnoreCase);
        AddOperators(name, operatorsByName, operators);
        return new FilterProfile<TColumn>(name, operatorsByName);
    }

    /// <summary>Creates a profile that inherits every operator of this one and adds more. Re-declaring an inherited name throws <see cref="FilterConfigurationException"/>.</summary>
    public FilterProfile<TColumn> Extend(string name, params FilterOperator<TColumn>[] addedOperators)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new FilterConfigurationException("A filter profile name must be a non-empty string.");
        if (addedOperators is null) throw new ArgumentNullException(nameof(addedOperators));

        var operatorsByName = new Dictionary<string, FilterOperator<TColumn>>(_operatorsByName, StringComparer.OrdinalIgnoreCase);
        AddOperators(name, operatorsByName, addedOperators);
        return new FilterProfile<TColumn>(name, operatorsByName);
    }

    private static void AddOperators(
        string profileName,
        Dictionary<string, FilterOperator<TColumn>> operatorsByName,
        FilterOperator<TColumn>[] operators)
    {
        foreach (var filterOperator in operators)
        {
            if (filterOperator is null)
                throw new FilterConfigurationException($"Profile '{profileName}' was given a null operator.");
            if (operatorsByName.ContainsKey(filterOperator.Name))
                throw new FilterConfigurationException($"Profile '{profileName}' declares operator '{filterOperator.Name}' more than once.");
            operatorsByName.Add(filterOperator.Name, filterOperator);
        }
    }
}
