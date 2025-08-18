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

- **Visual Studio 2022** (recommended) or Visual Studio 2019/2022
- **.NET 8.0 SDK** or later
- **T4 Template Support** - Ensure the "Text Template Transformation" workload is installed in Visual Studio

## Project Structure

```
src/
├── Linq2GraphQL.Generator/           # Main code generation project
│   ├── Templates/                     # T4 template files
│   │   ├── Client/                    # Client generation templates
│   │   ├── Class/                     # Class generation templates
│   │   ├── Interface/                 # Interface generation templates
│   │   ├── Methods/                   # Method generation templates
│   │   └── Enum/                      # Enum generation templates
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
- **`.cs`** - Preprocessed T4 templates (auto-generated, contains the actual `TransformText()` method)

### Template Development Workflow

#### 1. Modifying T4 Templates

When modifying `.tt` files:

1. **Edit the `.tt` file** with your changes
2. **Manually regenerate the `.cs` file** using Visual Studio's custom tool
3. **Build the project** to ensure compilation
4. **Test the generation** by running the client generator

#### 2. Manual Template Regeneration

**⚠️ IMPORTANT: After modifying any `.tt` file, you MUST manually regenerate the corresponding `.cs` file.**

**In Visual Studio 2022:**

1. Right-click on the `.tt` file in Solution Explorer
2. Select **"Run Custom Tool"**
3. This will regenerate the `.cs` file with your changes
4. Verify the `.cs` file contains your updated template logic

**Alternative method:**
1. Right-click on the `.tt` file
2. Select **"Properties"**
3. Set **"Custom Tool"** to `TextTemplatingFilePreprocessor`
4. Set **"Custom Tool Namespace"** to your desired namespace
5. Save the file to trigger regeneration

#### 3. Template File Dependencies

Each T4 template requires:

- **`.tt` file** - Contains the template logic and output format
- **`.tt.cs` file** - Provides the partial class with constructor parameters and helper methods
- **`.cs` file** - Auto-generated preprocessed template (regenerated from `.tt`)

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
Edit .tt file → Run Custom Tool → Build Project → Test Generation → Repeat
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
1. Ensure you've run the **"Run Custom Tool"** on the `.tt` file
2. Check that the `.cs` file was updated with your changes
3. Clean and rebuild the project
4. Verify the T4 preprocessor is working in Visual Studio

#### Missing TransformText Method

**Problem:** Compilation error "does not contain a definition for 'TransformText'".

**Solution:**
1. The `.cs` file is missing or outdated
2. Run **"Run Custom Tool"** on the corresponding `.tt` file
3. Ensure the `.tt.cs` file exists and has the correct partial class definition

#### Template Variables Not Available

**Problem:** Template variables like `namespaceName` or `name` are undefined.

**Solution:**
1. Check the `.tt.cs` file has the correct constructor parameters
2. Verify the partial class has `readonly` fields for all template variables
3. Ensure the `ClientGenerator.cs` passes the correct parameters when instantiating templates

### Debugging Tips

1. **Check the `.cs` file content** - It should contain your template logic in the `TransformText()` method
2. **Verify template compilation** - Build errors often indicate template syntax issues
3. **Use Visual Studio's T4 debugging** - Set breakpoints in the generated `.cs` files
4. **Check build output** - Look for T4-related error messages

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
2. **Ignore generated `.cs` files** - Add `**/*.cs` to `.gitignore` for auto-generated files
3. **Document template changes** - Include clear commit messages for template modifications
4. **Review generated output** - Verify that template changes produce the expected results

## Getting Help

- **Check existing templates** - Review similar templates for examples
- **T4 documentation** - Microsoft's T4 documentation provides comprehensive guidance
- **Project issues** - Search existing GitHub issues for similar problems
- **Community support** - Reach out to the project maintainers or community

---

**Note:** T4 template development requires careful attention to the regeneration workflow. Always remember to run the custom tool after modifying `.tt` files to ensure your changes are applied to the generated code.
