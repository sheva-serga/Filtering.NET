using System.Collections;

namespace Filtering.Net.Generator.Tests.Emission;

/// <summary>
/// Reflection helpers for driving a filter class loaded through <see cref="RuntimeLoader"/>:
/// builds typed in-memory queryables of the consumer's entity types, reads their members, and
/// invokes the generated <c>ApplyFilter</c> / <c>ApplySorting</c>, materialising the results.
/// </summary>
internal static class GeneratedFilterHarness
{
    public static object BuildQueryable(Type elementType, IReadOnlyList<object> elements)
    {
        var listType = typeof(List<>).MakeGenericType(elementType);
        var typedList = Activator.CreateInstance(listType)!;
        var addMethod = listType.GetMethod("Add")!;
        foreach (var element in elements) addMethod.Invoke(typedList, [element]);
        return typeof(Queryable).GetMethods()
            .First(method => method.Name == "AsQueryable" && method.IsGenericMethod)
            .MakeGenericMethod(elementType)
            .Invoke(null, [typedList])!;
    }

    public static object CreateInstance(Type type, params (string Name, object? Value)[] properties)
    {
        var instance = Activator.CreateInstance(type)!;
        foreach (var (propertyName, propertyValue) in properties)
        {
            type.GetProperty(propertyName)!.SetValue(instance, propertyValue);
        }
        return instance;
    }

    public static T ReadMember<T>(object instance, string memberName) =>
        (T)instance.GetType().GetProperty(memberName)!.GetValue(instance)!;

    public static List<object> InvokeApplyFilter(object filterInstance, object queryable, FilterNode whereNode)
    {
        var filteredQuery = filterInstance.GetType().GetMethod("ApplyFilter")!.Invoke(filterInstance, [queryable, whereNode])!;
        return Materialize(filteredQuery);
    }

    public static List<object> InvokeApplySorting(
        object filterInstance,
        object queryable,
        IReadOnlyList<SortItem> sortItems,
        int? page = null,
        int? pageSize = null)
    {
        var applySortingMethod = filterInstance.GetType().GetMethods()
            .First(method => method.Name == "ApplySorting" && method.GetParameters().Length == 4);
        var sortedQuery = applySortingMethod.Invoke(filterInstance, [queryable, sortItems, page, pageSize])!;
        return Materialize(sortedQuery);
    }

    private static List<object> Materialize(object query) => ((IEnumerable)query).Cast<object>().ToList();
}
