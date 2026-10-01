using System.Collections;

using AwesomeAssertions;

namespace Filtering.Net.Generator.Tests.Emission;

public class PropertyMapSortingRuntimeTests
{
    private const string ConsumerSource = """
        using Filtering.Net;
        namespace Sample;

        public sealed class Person
        {
            public int Id { get; set; }
            public string FirstName { get; set; } = "";
            public string LastName { get; set; } = "";
        }

        public sealed class Team
        {
            public int Id { get; set; }
            public Person Leader { get; set; } = new();
        }

        [GenerateFilter<Person>]
        [Map(nameof(Person.Id), Sortable = true)]
        public partial class PersonFilter
        {
            [PropertyMap("FullName", Alias = "name", Sortable = true, DefaultSortDirection = SortDir.Desc)]
            private static FilterRule<Person, string> MapFullName(FilterRuleBuilder<Person, string> builder) =>
                builder.For(person => person.FirstName + " " + person.LastName)
                       .Operator<string>("eq", (string column, string value) => column == value);
        }

        [GenerateFilter<Team>]
        [MapNested(nameof(Team.Leader))]
        public partial class TeamFilter;
        """;

    [Fact]
    public void Schema_AliasedSortableRule_ExposesAliasAndSortability()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var filter = Activator.CreateInstance(assembly.GetType("Sample.PersonFilter")!)!;

        // Act
        var schema = filter.GetType().GetProperty("Schema")!.GetValue(filter)!;
        var properties = ((IEnumerable)schema.GetType().GetProperty("Properties")!.GetValue(schema)!).Cast<object>();
        var fullNameProperty = properties.Single(property => (string)ReadMember(property, "Field") == "FullName");

        // Assert
        ReadMember(fullNameProperty, "Alias").Should().Be("name");
        ReadMember(fullNameProperty, "Sortable").Should().Be(true);
        ReadMember(fullNameProperty, "DefaultSortDirection").Should().Be(SortDir.Desc);
    }

    [Fact]
    public void ApplySorting_AliasWithoutDirection_SortsByComputedValueDescending()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var personType = assembly.GetType("Sample.Person")!;
        var filterType = assembly.GetType("Sample.PersonFilter")!;
        var filter = Activator.CreateInstance(filterType)!;
        var people = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(personType))!;
        people.Add(CreatePerson(personType, 1, "Ann", "Adams"));
        people.Add(CreatePerson(personType, 2, "Cid", "Clark"));
        people.Add(CreatePerson(personType, 3, "Bea", "Brown"));
        var applySorting = filterType.GetMethod(
            "ApplySorting",
            [typeof(IQueryable<>).MakeGenericType(personType), typeof(IReadOnlyList<SortItem>), typeof(int?), typeof(int?)])!;
        IReadOnlyList<SortItem> sortItems = [new SortItem("name")];

        // Act
        var sortedPeople = (IEnumerable)applySorting.Invoke(filter, [Queryable.AsQueryable(people), sortItems, null, null])!;

        // Assert
        sortedPeople.Cast<object>().Select(person => (string)ReadMember(person, "FirstName"))
            .Should().Equal("Cid", "Bea", "Ann");
    }

    [Fact]
    public void ValidateSort_RuleLiftedThroughMapNested_AcceptsPrefixedAlias()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var filterType = assembly.GetType("Sample.TeamFilter")!;
        var filter = Activator.CreateInstance(filterType)!;
        var validateSort = filterType.GetMethods()
            .First(method => method.Name == "Validate" && method.GetParameters().Length == 1
                && method.GetParameters()[0].ParameterType == typeof(IReadOnlyList<SortItem>));
        IReadOnlyList<SortItem> sortItems = [new SortItem("Leader.name", SortDir.Asc)];

        // Act
        var validationResult = (FilterValidationResult)validateSort.Invoke(filter, [sortItems])!;

        // Assert
        validationResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ApplySorting_LiftedAliasWithoutDirection_SortsByComputedValueDescending()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var personType = assembly.GetType("Sample.Person")!;
        var teamType = assembly.GetType("Sample.Team")!;
        var filterType = assembly.GetType("Sample.TeamFilter")!;
        var filter = Activator.CreateInstance(filterType)!;
        var teams = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(teamType))!;
        teams.Add(CreateTeam(teamType, 1, CreatePerson(personType, 1, "Ann", "Adams")));
        teams.Add(CreateTeam(teamType, 2, CreatePerson(personType, 2, "Cid", "Clark")));
        teams.Add(CreateTeam(teamType, 3, CreatePerson(personType, 3, "Bea", "Brown")));
        var applySorting = filterType.GetMethod(
            "ApplySorting",
            [typeof(IQueryable<>).MakeGenericType(teamType), typeof(IReadOnlyList<SortItem>), typeof(int?), typeof(int?)])!;
        IReadOnlyList<SortItem> sortItems = [new SortItem("Leader.name")];

        // Act
        var sortedTeams = (IEnumerable)applySorting.Invoke(filter, [Queryable.AsQueryable(teams), sortItems, null, null])!;

        // Assert
        sortedTeams.Cast<object>().Select(team => (int)ReadMember(team, "Id"))
            .Should().Equal(2, 3, 1);
    }

    private static object CreateTeam(Type teamType, int id, object leader)
    {
        var team = Activator.CreateInstance(teamType)!;
        teamType.GetProperty("Id")!.SetValue(team, id);
        teamType.GetProperty("Leader")!.SetValue(team, leader);
        return team;
    }

    private static object CreatePerson(Type personType, int id, string firstName, string lastName)
    {
        var person = Activator.CreateInstance(personType)!;
        personType.GetProperty("Id")!.SetValue(person, id);
        personType.GetProperty("FirstName")!.SetValue(person, firstName);
        personType.GetProperty("LastName")!.SetValue(person, lastName);
        return person;
    }

    private static object ReadMember(object instance, string memberName) =>
        instance.GetType().GetProperty(memberName)!.GetValue(instance)!;
}
