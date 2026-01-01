# Task Completion Checklist

When completing a task in this project, follow these steps to ensure code quality and maintainability:

## 1. Code Quality Checks

### Verify Code Style
- Ensure all code follows the `.editorconfig` conventions
- Check that namespace declarations use file-scoped syntax
- Verify PascalCase naming for types, properties, and methods
- Ensure interfaces start with "I" prefix
- Confirm proper indentation (4 spaces for C#, XAML)

### Verify Documentation
- Add or update XML documentation comments for public APIs
- Use `<summary>`, `<param>`, `<returns>`, and `<remarks>` tags appropriately
- Document any complex logic or non-obvious behavior

### Check Nullable Annotations
- Ensure proper use of nullable reference types (`?`)
- Avoid nullable warnings
- Use null-forgiving operator (`!`) only when necessary and safe

## 2. Build Verification

### Build the Solution
```powershell
dotnet build MatroskaBatchFlow.sln
```

Ensure there are no build errors or warnings.

### Build Specific Configurations
If changes affect specific platforms:
```powershell
# Desktop
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj /property:TargetFramework=net10.0-desktop

# Windows
dotnet build src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj /property:TargetFramework=net10.0-windows10.0.19041
```

## 3. Testing

### Run All Tests
```powershell
dotnet test
```

Ensure all tests pass.

### Run Affected Tests
If you modified specific areas:
```powershell
# Core tests
dotnet test tests/MatroskaBatchFlow.Core/MatroskaBatchFlow.Core.Tests.csproj

# Uno tests
dotnet test tests/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.Tests.csproj
```

### Add New Tests
- Write unit tests for new functionality
- Follow the AAA pattern (Arrange, Act, Assert)
- Use descriptive test names: `MethodName_Scenario_ExpectedResult`
- Use test builders for complex test data
- Mock dependencies with NSubstitute

## 4. Code Review Checks

### Self-Review Checklist
- [ ] Code follows established patterns in the project
- [ ] No hardcoded values that should be configurable
- [ ] Error handling is appropriate
- [ ] Resources are properly disposed (using statements, IDisposable)
- [ ] No obvious performance issues
- [ ] XAML bindings are correct (if UI changes)
- [ ] Thread safety considered for concurrent operations
- [ ] No unnecessary dependencies added

### Security Considerations
- [ ] No sensitive data in code or logs
- [ ] Input validation for user-provided data
- [ ] File paths are validated and sanitized
- [ ] External process execution is secure

## 5. Integration Testing (if applicable)

### Test GUI Changes
If you modified the Uno UI:
1. Run the application:
   ```powershell
   dotnet run --project src/MatroskaBatchFlow.Uno/MatroskaBatchFlow.Uno.csproj -f net10.0-desktop
   ```
2. Manually verify the UI changes work as expected
3. Test on different window sizes (responsive design)
4. Verify dark/light theme compatibility
5. Check accessibility (keyboard navigation, screen readers)

### Test Core Functionality
If you modified the Core library:
1. Test with sample MKV files
2. Verify MediaInfo integration works
3. Verify mkvpropedit integration works
4. Test batch processing with multiple files

## 6. Documentation Updates

### Update Project Documentation
- Update README.md if features changed
- Update inline code comments
- Consider if new memory files are needed for Serena or if they need to be updated

### Update Configuration
- Update `appsettings.json` if new settings were added
- Update `Directory.Packages.props` if packages were added/updated

## 7. Version Control

### Git Operations
```powershell
# Check what changed
git status
git diff

# Stage changes
git add .

# Commit following Conventional Commits specification
# Use PowerShell here-string for clean multi-line messages
git commit -m @"
<type>[optional scope]: <description>

[optional body with bullet points]

[optional footer(s)]
"@
```

### Commit Message Format
Follow the **[Conventional Commits 1.0.0](https://www.conventionalcommits.org/)** specification.

**Structure:**
```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**Required Elements:**
- **type**: Noun describing the change (e.g., `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`)
  - `feat`: New feature (correlates with MINOR in SemVer)
  - `fix`: Bug fix (correlates with PATCH in SemVer)
- **description**: Short summary immediately following `: ` (imperative mood, e.g., "add feature" not "adds feature")

**Optional Elements:**
- **scope**: Section of codebase in parentheses (e.g., `core`, `uno`, `cli`, `tests`)
- **!**: Appended after type/scope to indicate breaking change
- **body**: Additional context, begins one blank line after description, free-form
  - *Project preference:* Use bullet points when listing multiple changes (not required, but preferred for clarity)
- **footer(s)**: Metadata following git trailer format
  - `Refs: #123` - References issue(s) without closing it
  - `Closes: #123` (or `Fixes:`, `Resolves:`) - References and automatically closes issue(s) on GitHub
  - Other footers: `See-also: #456`

**Breaking Changes:**
MUST be indicated by either (or both):
- Appending `!` immediately before `:` (e.g., `feat!:` or `feat(api)!:`)
- Footer with `BREAKING CHANGE: <description>` (MUST be uppercase)

Breaking changes correlate with MAJOR in SemVer and can be part of any commit type.

**Examples:**

Commit with description only:
```
docs: correct spelling of CHANGELOG
```

Commit with scope:
```
feat(lang): add Polish language
```

Commit with body:
```
fix: prevent racing of requests

Introduce a request id and a reference to latest request. Dismiss
incoming responses other than from latest request.
```

Commit with bulleted body (project preference):
```
feat(cli): add recursive folder processing

- Process nested folders in batch jobs
- Preserve original file timestamps
- Skip hidden/system files automatically
```

Breaking change with `!`:
```
feat(api)!: send an email to the customer when a product is shipped
```

Breaking change with footer:
```
feat: allow provided config object to extend other configs

BREAKING CHANGE: `extends` key in config file is now used for extending other config files
```

Multiple footers:
```
fix: prevent racing of requests

Introduce a request id and a reference to latest request. Dismiss
incoming responses other than from latest request.

Refs: #123
```

Closing an issue:
```
fix(core): handle empty MKV metadata gracefully

- Return default values for missing track info
- Log warning instead of throwing exception
- Add unit tests for edge cases

Refs: #38
Closes: #42
```

Revert commit:
```
revert: let us never again speak of the noodle incident

Refs: 676104e, a215868
```

## 8. Clean Up

### Remove Debugging Code
- Remove `Console.WriteLine` or debug logging
- Remove commented-out code
- Remove unused imports (if any)

### Verify Build Artifacts
```powershell
# Clean and rebuild to ensure no stale artifacts
dotnet clean
dotnet build
```

## 9. Performance Considerations (if applicable)

- Profile code if performance-critical changes were made
- Ensure no memory leaks (especially with event handlers)
- Check for unnecessary allocations
- Verify efficient LINQ usage

## 10. Final Checks

### Pre-Commit Checklist
- [ ] Code compiles without warnings
- [ ] All tests pass
- [ ] Code follows style guidelines
- [ ] Documentation is updated
- [ ] No debug code remains
- [ ] Changes are backwards compatible (or breaking changes are documented)
- [ ] UI changes tested manually (if applicable)
- [ ] Git commit message is descriptive

### Ready for Review
Once all checks pass, the task is complete and ready for code review (if applicable).
