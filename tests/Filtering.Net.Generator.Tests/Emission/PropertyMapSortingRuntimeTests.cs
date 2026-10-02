using System.Collections;
using System.Text.Json;

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
        var schema = GeneratedFilterHarness.ReadMember<object>(filter, "Schema");
        var properties = GeneratedFilterHarness.ReadMember<IEnumerable>(schema, "Properties").Cast<object>();
        var fullNameProperty = properties.Single(property => GeneratedFilterHarness.ReadMember<string>(property, "Field") == "FullName");

        // Assert
        GeneratedFilterHarness.ReadMember<string>(fullNameProperty, "Alias").Should().Be("name");
        GeneratedFilterHarness.ReadMember<bool>(fullNameProperty, "Sortable").Should().BeTrue();
        GeneratedFilterHarness.ReadMember<SortDir>(fullNameProperty, "DefaultSortDirection").Should().Be(SortDir.Desc);
    }

    [Fact]
    public void ApplySorting_AliasWithoutDirection_SortsByComputedValueDescending()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var personType = assembly.GetType("Sample.Person")!;
        var filter = Activator.CreateInstance(assembly.GetType("Sample.PersonFilter")!)!;
        var people = GeneratedFilterHarness.BuildQueryable(personType,
        [
            GeneratedFilterHarness.CreateInstance(personType, ("Id", 1), ("FirstName", "Ann"), ("LastName", "Adams")),
            GeneratedFilterHarness.CreateInstance(personType, ("Id", 2), ("FirstName", "Cid"), ("LastName", "Clark")),
            GeneratedFilterHarness.CreateInstance(personType, ("Id", 3), ("FirstName", "Bea"), ("LastName", "Brown")),
        ]);

        // Act
        var sortedPeople = GeneratedFilterHarness.InvokeApplySorting(filter, people, [new SortItem("name")]);

        // Assert
        sortedPeople.Select(person => GeneratedFilterHarness.ReadMember<string>(person, "FirstName"))
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
        var filter = Activator.CreateInstance(assembly.GetType("Sample.TeamFilter")!)!;
        var teams = GeneratedFilterHarness.BuildQueryable(teamType,
        [
            CreateTeam(teamType, 1, GeneratedFilterHarness.CreateInstance(personType, ("Id", 1), ("FirstName", "Ann"), ("LastName", "Adams"))),
            CreateTeam(teamType, 2, GeneratedFilterHarness.CreateInstance(personType, ("Id", 2), ("FirstName", "Cid"), ("LastName", "Clark"))),
            CreateTeam(teamType, 3, GeneratedFilterHarness.CreateInstance(personType, ("Id", 3), ("FirstName", "Bea"), ("LastName", "Brown"))),
        ]);

        // Act
        var sortedTeams = GeneratedFilterHarness.InvokeApplySorting(filter, teams, [new SortItem("Leader.name")]);

        // Assert
        sortedTeams.Select(team => GeneratedFilterHarness.ReadMember<int>(team, "Id"))
            .Should().Equal(2, 3, 1);
    }

    [Fact]
    public void ApplyFilter_AliasLeaf_FiltersByComputedValue()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var personType = assembly.GetType("Sample.Person")!;
        var filter = Activator.CreateInstance(assembly.GetType("Sample.PersonFilter")!)!;
        var people = GeneratedFilterHarness.BuildQueryable(personType,
        [
            GeneratedFilterHarness.CreateInstance(personType, ("Id", 1), ("FirstName", "Ann"), ("LastName", "Adams")),
            GeneratedFilterHarness.CreateInstance(personType, ("Id", 2), ("FirstName", "Cid"), ("LastName", "Clark")),
        ]);
        var leaf = new FilterLeaf("name", "eq", JsonDocument.Parse("\"Cid Clark\"").RootElement);

        // Act
        var matchedPeople = GeneratedFilterHarness.InvokeApplyFilter(filter, people, leaf);

        // Assert
        matchedPeople.Select(person => GeneratedFilterHarness.ReadMember<int>(person, "Id")).Should().Equal(2);
    }

    [Fact]
    public void ApplyFilter_LiftedAliasLeaf_FiltersByComputedValue()
    {
        // Arrange
        var assembly = RuntimeLoader.LoadGeneratedAssembly(ConsumerSource);
        var personType = assembly.GetType("Sample.Person")!;
        var teamType = assembly.GetType("Sample.Team")!;
        var filter = Activator.CreateInstance(assembly.GetType("Sample.TeamFilter")!)!;
        var teams = GeneratedFilterHarness.BuildQueryable(teamType,
        [
            CreateTeam(teamType, 1, GeneratedFilterHarness.CreateInstance(personType, ("Id", 1), ("FirstName", "Ann"), ("LastName", "Adams"))),
            CreateTeam(teamType, 2, GeneratedFilterHarness.CreateInstance(personType, ("Id", 2), ("FirstName", "Cid"), ("LastName", "Clark"))),
        ]);
        var leaf = new FilterLeaf("Leader.name", "eq", JsonDocument.Parse("\"Cid Clark\"").RootElement);

        // Act
        var matchedTeams = GeneratedFilterHarness.InvokeApplyFilter(filter, teams, leaf);

        // Assert
        matchedTeams.Select(team => GeneratedFilterHarness.ReadMember<int>(team, "Id")).Should().Equal(2);
    }

    private static object CreateTeam(Type teamType, int id, object leader) =>
        GeneratedFilterHarness.CreateInstance(teamType, ("Id", id), ("Leader", leader));
}
