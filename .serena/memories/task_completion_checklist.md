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

Follow **[Conventional Commits 1.0.0](https://www.conventionalcommits.org/)** specification.

See `.github/copilot-instructions.md` → **Commit Message Standards** section for:
- Required and recommended types
- Codebase-specific scopes
- Breaking change conventions
- Examples

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
