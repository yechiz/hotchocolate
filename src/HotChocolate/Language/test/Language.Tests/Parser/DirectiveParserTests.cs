using System.Linq;
using System.Text;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language;

public class DirectiveParserTests
{
    [Fact]
    public void ParseUniqueDirective()
    {
        // arrange
        var text = "directive @skip(if: Boolean!) " +
            "on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        DocumentNode document = parser.Parse();

        // assert
        DirectiveDefinitionNode directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.False(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void ParseRepeatableDirective()
    {
        // arrange
        var text = "directive @skip(if: Boolean!) repeatable " +
            "on FIELD | FRAGMENT_SPREAD | INLINE_FRAGMENT";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        DocumentNode document = parser.Parse();

        // assert
        DirectiveDefinitionNode directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.True(directiveDefinition.IsRepeatable);
    }

    [Fact]
    public void ParseDescription()
    {
        // arrange
        var text = @"
            """"""
            Description
            """"""
            directive @foo(bar:String!) on FIELD_DEFINITION
            ";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        DocumentNode document = parser.Parse();

        // assert
        DirectiveDefinitionNode directiveDefinition = document.Definitions
            .OfType<DirectiveDefinitionNode>().FirstOrDefault();
        Assert.NotNull(directiveDefinition);
        Assert.Equal("Description", directiveDefinition.Description!.Value);
    }

    [Fact]
    public void DirectiveOrderIsSignificant()
    {
        // arrange
        var text = "type Query { field: String @a @b @c }";
        var parser = new Utf8GraphQLParser(Encoding.UTF8.GetBytes(text));

        // assert
        DocumentNode document = parser.Parse();

        // assert
        ObjectTypeDefinitionNode type = document.Definitions
            .OfType<ObjectTypeDefinitionNode>().FirstOrDefault();
        Assert.Collection(type.Fields.Single().Directives,
            t => Assert.Equal("a", t.Name.Value),
            t => Assert.Equal("b", t.Name.Value),
            t => Assert.Equal("c", t.Name.Value));
    }

    [Fact]
    public void ParseQueryDirective()
    {
        // arrange
        var text = @"
                query ($var: Boolean) @onQuery {
                    field
                }
            ";

        // act
        DocumentNode document = Utf8GraphQLParser.Parse(
            Encoding.UTF8.GetBytes(text));

        // assert
        document.MatchSnapshot();
    }
}
