# Developer Guide

This document provides comprehensive guidance for developers working on the Linq2GraphQL.Client project, particularly when modifying T4 templates and the code generation system.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Project Structure](#project-structure)
- [T4 Template Development](#t4-template-development)
- [Code Generation Workflow](#code-generation-workflow)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Prerequisites

- **.NET 10.0 SDK** or later
- **Any editor** - Visual Studio, Rider, VS Code or plain `dotnet build` all work;
  T4 preprocessing runs as part of the build, so no IDE-specific tooling is required
- **`dotnet tool restore`** once per clone, to fetch the pinned `dotnet-t4` CLI tool
  (the build does this for you)

## Project Structure

```
src/
├── Linq2GraphQL.Generator/           # Main code generation project
│   ├── Templates/                     # T4 template files
│   │   ├── Client/                    # Client generation templates
│   │   ├── Class/                     # Class generation templates
│   │   ├── Interface/                 # Interface generation templates
│   │   ├── Methods/                   # Method generation templates
│   │   ├── Enum/                      # Enum generation templates
│   │   └── Scalars/                   # Custom scalar templates
│   ├── GraphQLSchema/                 # Schema parsing and processing
│   └── ClientGenerator.cs             # Main generation orchestration
└── Linq2GraphQL.Client/              # Core client library
```

## T4 Template Development

### Understanding T4 Templates

This project uses **T4 (Text Template Transformation Toolkit)** for code generation. T4 templates are `.tt` files that generate C# source code based on GraphQL schema information.

#### Template File Types

- **`.tt`** - Source T4 template files (human-editable)
- **`.tt.cs`** - Partial class definitions for template variables and helper methods
- **`.g.cs`** - Preprocessed T4 templates (contains the actual `TransformText()` method).
  **Generated at build time, gitignored, never checked in.**

### Template Development Workflow

#### 1. Modifying T4 Templates

When modifying `.tt` files:

1. **Edit the `.tt` file** with your changes
2. **Build the project** - the `.g.cs` file is regenerated automatically
3. **Test the generation** by running the client generator

That is the whole loop. There is no manual regeneration step, and no way to build
stale template logic: compile errors in a template point straight back at the
`.tt` file and line number.

#### 2. How Build-Time Preprocessing Works

`Linq2GraphQL.Generator.csproj` preprocesses every `Templates\**\*.tt` into a
sibling `<Template>.g.cs` before compiling, using the
[dotnet-t4](https://www.nuget.org/packages/dotnet-t4) CLI tool pinned in
`.config/dotnet-tools.json`:

| Target | Does |
| --- | --- |
| `RestoreT4Tool` | Runs `dotnet tool restore` (once per project file change) |
| `PreprocessT4Templates` | Runs `dotnet t4 --class=<ns>.<Name> --out=<Name>.g.cs <Name>.tt` per template, incrementally |
| `IncludeT4Output` | Adds the `.g.cs` files to `Compile` before `BeforeCompile` |

New template folders need one line in the `T4Template` item group so the class
namespace can be derived:

```xml
<T4Template Include="Templates\MyFolder\*.tt" TemplateNamespace="Templates.MyFolder"/>
```

To preprocess a single template by hand (rarely needed - the build does it):

```powershell
dotnet tool restore
cd src/Linq2GraphQL.Generator
dotnet t4 --class="Linq2GraphQL.Generator.Templates.Enum.EnumTemplate" --out="Templates/Enum/EnumTemplate.g.cs" "Templates/Enum/EnumTemplate.tt"
```

Visual Studio's *Run Custom Tool* is **no longer used** and the `.tt` files
deliberately carry no `Generator`/`LastGenOutput` metadata - the build owns
generation on every platform, IDE or CI.

#### 3. Template File Dependencies

Each T4 template requires:

- **`.tt` file** - Contains the template logic and output format
- **`.tt.cs` file** - Provides the partial class with constructor parameters and helper methods
- **`.g.cs` file** - Preprocessed template, produced by the build (never edited or committed)

### Template Syntax

#### Basic T4 Directives

```t4
<#@ template language="C#" #>
<#@ assembly name="System.Core" #>
<#@ import namespace="System.Linq" #>
```

#### Template Expressions

```t4
<#= variableName #>                    <!-- Output variable value -->
<# if (condition) { #>                 <!-- Conditional blocks -->
    // C# code here
<# } #>
<# foreach (var item in collection) { #> <!-- Loops -->
    // Process each item
<# } #>
```

#### Helper Methods

Define helper methods in the `.tt.cs` file:

```csharp
public partial class TemplateName
{
    private readonly string variableName;
    
    public TemplateName(string variableName)
    {
        this.variableName = variableName;
    }
    
    private string HelperMethod()
    {
        return "Helper logic here";
    }
}
```

## Code Generation Workflow

### 1. Development Cycle

```
Edit .tt file → Build Project → Test Generation → Repeat
```

### 2. Testing Changes

After modifying templates:

1. **Build the project** to ensure no compilation errors
2. **Run the client generator** to test template output
3. **Verify generated code** matches your expectations
4. **Test the generated client** in a sample application

### 3. Command Line Generation

```bash
# Build the generator project
dotnet build src/Linq2GraphQL.Generator

# Generate a client
dotnet run --project src/Linq2GraphQL.Generator -- <endpoint> [options]
```

## Troubleshooting

### Common Issues

#### T4 Templates Not Regenerating

**Problem:** Changes to `.tt` files not reflected in generated output.

**Solution:**
1. Confirm the template folder has a `T4Template` entry in
   `Linq2GraphQL.Generator.csproj` - a template outside those globs is never preprocessed
2. Check the timestamp of the sibling `.g.cs` file; delete it and rebuild to force regeneration
3. Run `dotnet build src/Linq2GraphQL.Generator -v:n` and look for the `dotnet t4` command lines

#### `dotnet t4` Not Found

**Problem:** Build fails with "Cannot find command 'dotnet t4'".

**Solution:** Run `dotnet tool restore` from the repository root - the tool is a local
tool pinned in `.config/dotnet-tools.json`. The build normally does this for you.

#### Missing TransformText Method

**Problem:** Compilation error "does not contain a definition for 'TransformText'".

**Solution:**
1. The `.g.cs` file was not produced - see *T4 Templates Not Regenerating* above
2. Ensure the `.tt.cs` file exists, and that its namespace and class name match the
   `--class` value the build derives (`$(RootNamespace).<TemplateNamespace>.<Filename>`)

#### Template Variables Not Available

**Problem:** Template variables like `namespaceName` or `name` are undefined.

**Solution:**
1. Check the `.tt.cs` file has the correct constructor parameters
2. Verify the partial class has `readonly` fields for all template variables
3. Ensure the `ClientGenerator.cs` passes the correct parameters when instantiating templates

### Debugging Tips

1. **Read the compile error location** - errors inside template code are reported against
   the `.tt` file and line, thanks to the `#line` pragmas in the generated `.g.cs`
2. **Inspect the `.g.cs` file** - it sits next to the `.tt` and contains the generated
   `TransformText()` method
3. **Set breakpoints in the `.g.cs` file** to step through template execution
4. **Check build output** - `dotnet t4` failures surface as `Exec` task errors

## Best Practices

### Template Design

1. **Keep templates focused** - Each template should handle one specific aspect of code generation
2. **Use helper methods** - Move complex logic to the `.tt.cs` file
3. **Maintain readability** - Use clear variable names and consistent formatting
4. **Handle edge cases** - Always check for null values and empty collections

### Code Organization

1. **Separate concerns** - Keep template logic separate from business logic
2. **Use partial classes** - Leverage C# partial classes for template organization
3. **Consistent naming** - Follow the project's naming conventions
4. **Documentation** - Include XML comments in generated code

### Testing

1. **Test with various schemas** - Ensure templates work with different GraphQL schemas
2. **Validate generated code** - Check that generated code compiles and works correctly
3. **Regression testing** - Ensure changes don't break existing functionality
4. **Integration testing** - Test the complete generation pipeline

### Version Control

1. **Commit `.tt` and `.tt.cs` files** - These are source files
2. **Never commit `.g.cs` files** - they are build output and are gitignored
3. **Document template changes** - Include clear commit messages for template modifications
4. **Review generated output** - Verify that template changes produce the expected results

## Getting Help

- **Check existing templates** - Review similar templates for examples
- **T4 documentation** - Microsoft's T4 documentation provides comprehensive guidance
- **Project issues** - Search existing GitHub issues for similar problems
- **Community support** - Reach out to the project maintainers or community

---

**Note:** T4 template development requires careful attention to the regeneration workflow. Always remember to run the custom tool after modifying `.tt` files to ensure your changes are applied to the generated code.
