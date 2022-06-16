# Solution Template

A solution template for Domain-Driven Design (DDD) on Clean Architecture.

## Instructions

To create a new solution using the template, first clone or pull this repository, ensuring that you have the latest version. Then perform the following commands using PowerShell or another terminal _in the repository's `ToDoBoundedContextName` directory_:

```PowerShell
# Ensure that the template can be freshly installed by removing any prior version
dotnet new -u .

# Install the template
dotnet new -i .

# Create a new solution using the template
dotnet new CleanDdd -o ToDoTargetPathIncludingSolutionDir -n ToDoSolutionName -ar ToDoCompanyName.ToDoDepartmentName -e "TODO: A short summary of the Bounded Context, for the readme."
```

### Adding an Application

Keep in mind that, by design, the Bounded Context uses its Application layer to form a single logical application. However, reality demands that certain things be split up, such as APIs vs. background apps vs. portals, internal vs. external APIs, etc. To that end, the physical application projects expose subsets of the Application layer's use cases.

To add another physical application project (e.g. ExternalApi, BackgroundApp, Portal), do the following:

- In a temporary directory, create a copy of the solution template's Api project.
- In the temporary copy, rename the Api project directory and csproj file to use the new application's name.
- In the temporary copy, use Notepad++ to find all files containing the word "Api" (whole word, case-sensitive). _Manually_ replace the occurrences that make sense.
- Perform the [instructions](#instructions) above on the temporary directory.
- From the temporary copy, move the project directory into the solution directory.
- In Visual Studio, add the project to the solution.
