# Solution Template

_A template used to create a new DDD-enabled solution from scratch._

## Instructions

- Pull this repository, to ensure that you have the latest version.
- Make a copy of the ToDoBoundedContextName directory.
- Rename the copied directory and the sln file.
- In Notepad++, perform the following "replace in files" operations (Ctrl+Shift+F), within the solution dir, on `*.*`:
	- `__ToDoBoundedContextName__` (e.g. `OrderProcessing`)
	- `__ToDoAreaName__` (e.g. `MyCompany.MyDepartment`)
- In the Api's launchSettings.json, complete the TODOs regarding the HTTP ports (for example, by typing Get-Random in PowerShell and taking 5 digits.)
- Complete the TODOs in the Api's Startup.cs.
- Complete the TODOs in the readme file.
- From Notepad++, "find in files" the text `__ToDo`, and handle any TODOs that were missed. (You can ignore any .pdb files, which may not have been updated yet.)

### Adding an Application

Keep in mind that, by design, the Bounded Context uses its Application layer to form a single logical application. However, reality demands that certain things be split up, such as APIs vs. schedulers vs. portals, internal vs. external APIs, etc. To that end, the physical application projects expose subsets of the Application layer's use cases.

To add another physical application project (e.g. ExternalApi, JobScheduler, Portal), do the following:

- In a temporary directory, create a copy of the solution template's Api project.
- In the temporary copy, rename the Api project directory and csproj file to use the new application's name.
- In the temporary copy, use Notepad++ to find all files containing the word "Api" (whole word, case-sensitive). _Manually_ replace the occurrences that make sense.
- Perform the [instructions](#instructions) above on the temporary directory.
- From the temporary copy, move the project directory into the solution directory.
- In Visual Studio, add the project to the solution.
