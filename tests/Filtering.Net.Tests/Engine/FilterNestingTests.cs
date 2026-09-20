using System.Text.Json;

using AwesomeAssertions;

using Xunit;

using static Filtering.Net.Tests.Engine.EngineTestData;

namespace Filtering.Net.Tests.Engine;

public class FilterNestingTests
{
    private sealed class Employee
    {
        public string Name { get; set; } = "";

        public Employee? Manager { get; set; }

        public Team? Team { get; set; }
    }

    private sealed class Team
    {
        public string Title { get; set; } = "";

        public Employee? Lead { get; set; }
    }

    // Mirrors what the generator emits: every schema factory takes the nesting context it was entered with.
    private static FilterSchema<Employee> EmployeeSchema(FilterNestingContext nestingContext, int managerMaxDepth, int teamLeadMaxDepth) =>
        new FilterSchemaBuilder<Employee>(new FilterSettings())
            .Add(FilterProperty.Map<Employee, string>("Name", employee => employee.Name, StringFilter.Profile).Build())
            .AddNested(nestingContext, "EmployeeFilter.MapManager", managerMaxDepth,
                nestedContext => EmployeeSchema(nestedContext, managerMaxDepth, teamLeadMaxDepth),
                employee => employee.Manager!, "Manager")
            .AddNested(nestingContext, "EmployeeFilter.MapTeam", maxDepth: 0,
                nestedContext => TeamSchema(nestedContext, managerMaxDepth, teamLeadMaxDepth),
                employee => employee.Team!, "Team")
            .Build();

    private static FilterSchema<Team> TeamSchema(FilterNestingContext nestingContext, int managerMaxDepth, int teamLeadMaxDepth) =>
        new FilterSchemaBuilder<Team>(new FilterSettings())
            .Add(FilterProperty.Map<Team, string>("Title", team => team.Title, StringFilter.Profile).Build())
            .AddNested(nestingContext, "TeamFilter.MapLead", teamLeadMaxDepth,
                nestedContext => EmployeeSchema(nestedContext, managerMaxDepth, teamLeadMaxDepth),
                team => team.Lead!, "Lead")
            .Build();

    [Fact]
    public void AddNested_SelfReferenceWithMaxDepthTwo_ExposesTwoLevelsAndStops()
    {
        // Act
        var schema = new FilterSchemaBuilder<Employee>(new FilterSettings())
            .Add(FilterProperty.Map<Employee, string>("Name", employee => employee.Name, StringFilter.Profile).Build())
            .AddNested(FilterNestingContext.Root, "EmployeeFilter.MapManager", maxDepth: 2,
                nestedContext => SelfReferencingSchema(nestedContext),
                employee => employee.Manager!, "Manager")
            .Build();

        // Assert
        schema.Properties.Select(property => property.Field)
            .Should().Equal("Name", "Manager.Name", "Manager.Manager.Name");
    }

    private static FilterSchema<Employee> SelfReferencingSchema(FilterNestingContext nestingContext) =>
        new FilterSchemaBuilder<Employee>(new FilterSettings())
            .Add(FilterProperty.Map<Employee, string>("Name", employee => employee.Name, StringFilter.Profile).Build())
            .AddNested(nestingContext, "EmployeeFilter.MapManager", maxDepth: 2, SelfReferencingSchema, employee => employee.Manager!, "Manager")
            .Build();

    [Fact]
    public void AddNested_IndirectCycleBoundedOnOneEdge_CutsTheCycleAtThatEdge()
    {
        // Act
        var schema = EmployeeSchema(FilterNestingContext.Root, managerMaxDepth: 1, teamLeadMaxDepth: 1);

        // Assert
        schema.Properties.Select(property => property.Field).Should().BeEquivalentTo(
            "Name",
            "Manager.Name",
            "Manager.Team.Title",
            "Manager.Team.Lead.Name",
            "Manager.Team.Lead.Team.Title",
            "Team.Title",
            "Team.Lead.Name",
            "Team.Lead.Manager.Name",
            "Team.Lead.Manager.Team.Title",
            "Team.Lead.Team.Title");
    }

    [Fact]
    public void AddNested_LiftedSelfReference_FiltersThroughTheChain()
    {
        // Arrange
        var director = new Employee { Name = "Dana" };
        var manager = new Employee { Name = "Mia", Manager = director };
        var employees = new[]
        {
            new Employee { Name = "Eli", Manager = manager },
            new Employee { Name = "Noa", Manager = new Employee { Name = "Max", Manager = new Employee { Name = "Zed" } } },
        }.AsQueryable();
        var definition = new FilterDefinition<Employee>(SelfReferencingSchema(FilterNestingContext.Root));
        var leaf = new FilterLeaf("manager.manager.name", "eq", JsonDocument.Parse("\"Dana\"").RootElement);

        // Act
        var matchedNames = definition.ApplyFilter(employees, leaf).Select(employee => employee.Name).ToList();
        var beyondDepth = definition.Validate(new FilterLeaf("manager.manager.manager.name", "eq", JsonDocument.Parse("\"x\"").RootElement));

        // Assert
        matchedNames.Should().Equal("Eli");
        beyondDepth.Errors.Should().ContainSingle(error => error.Code == FilterValidationCode.UnknownField);
    }

    [Fact]
    public void AddNested_CycleWithNoBoundedEdge_ThrowsConfigurationExceptionNamingTheChain()
    {
        // Act
        var buildUnboundedCycle = () => EmployeeSchema(FilterNestingContext.Root, managerMaxDepth: 0, teamLeadMaxDepth: 0);

        // Assert
        buildUnboundedCycle.Should().Throw<FilterConfigurationException>()
            .WithMessage("*EmployeeFilter.MapManager*MaxDepth*");
    }

    [Fact]
    public void AddNested_NegativeMaxDepth_ThrowsConfigurationException()
    {
        // Act
        var enterWithNegativeDepth = () => new FilterSchemaBuilder<Person>(new FilterSettings())
            .AddNested(FilterNestingContext.Root, "PersonFilter.MapDepartment", maxDepth: -1,
                _ => new FilterSchemaBuilder<Department>(new FilterSettings()).Build(),
                person => person.Department, "Department");

        // Assert
        enterWithNegativeDepth.Should().Throw<FilterConfigurationException>();
    }
}
