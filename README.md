# Solution Template

A solution template for Domain-Driven Design (DDD) on Clean Architecture.

## Instructions

To create a new solution using the template, first clone or pull this repository, ensuring that you have the latest version. Then perform the following commands using PowerShell or another terminal _in the repository's `ToDoBoundedContextName` directory_:

```PowerShell
# Ensure that the template can be freshly installed by removing any prior version
dotnet new uninstall .

# Install the template
dotnet new install .

# Create a new solution using the template
dotnet new CleanDdd -o ToDoTargetPathIncludingSolutionDir -n ToDoSolutionName -ar ToDoCompanyName.ToDoDepartmentName -e "TODO: A short summary of the Bounded Context, for the readme."
```

### Adding an Application

Keep in mind that, by design, the Bounded Context uses its Application layer to form a single logical application. However, reality demands that certain things be split up, such as APIs vs. background apps vs. portals, internal vs. external APIs, etc. To that end, the physical application projects expose subsets of the Application layer's use cases.

To add another physical application project (e.g. ExternalApi, BackgroundApp, Portal), do the following:

- Duplicate the Api project directory, renaming both the directy and the csproj file.
- In the new directory, delete bin, obj, and *.user.
- In the new directory, use Notepad++ to find all files containing the word "Api" (whole word, case-sensitive). _Manually_ replace the occurrences that make sense.
- In the new directory, in launchSettings.json, increment the last digit of each port number until it is unique.
- In Visual Studio, add the new project to the solution.
