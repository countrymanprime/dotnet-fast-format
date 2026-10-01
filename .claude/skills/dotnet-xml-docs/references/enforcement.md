# Enforcing documentation in the build

Every snippet here was checked against .NET SDK 10.0.101 with a class library and an xUnit
test project. Before adding any package, check nuget.org for the current version. The versions
shown were current in September 2026.

## Layer 1: compiler (always)

`Directory.Build.props` at the repo root:

```xml
<Project>
  <PropertyGroup>
    <!-- Emit the .xml doc file (IntelliSense, NuGet, DocFX) and turn on doc diagnostics.
         Side benefit: IDE0005 (unnecessary usings) is only reported on build when this is on. -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!-- Structural doc errors: malformed XML, stale/duplicate <param>, unresolved or ambiguous
         cref, misplaced ///. Silent on correct docs. In an existing repo, fix the current hits
         first (see "Rolling out" below) or this breaks the build. -->
    <WarningsAsErrors>$(WarningsAsErrors);CS0419;CS1570;CS1571;CS1572;CS1573;CS1574;CS1580;CS1584;CS1587;CS1711;CS1712;CS1734;CS1735</WarningsAsErrors>
  </PropertyGroup>
</Project>
```

| ID | Meaning |
| --- | --- |
| CS1570 | Badly formed XML |
| CS1571 | Duplicate `<param>` |
| CS1572 | `<param>` for a parameter that doesn't exist (the classic "renamed and forgot the docs" case) |
| CS1573 | A parameter has no `<param>`, while other parameters do |
| CS1574, CS1580, CS1584, CS0419 | `cref` is unresolved, invalid, malformed, or ambiguous |
| CS1587 | `///` on something that can't be documented |
| CS1711, CS1712 | `<typeparam>` mismatch |
| CS1734, CS1735 | `paramref` / `typeparamref` to something that doesn't exist |
| CS1591 | Missing doc on a publicly visible member. **Handled separately; see below.** |

`Directory.Build.targets` at the repo root. It has to be the `.targets` file, because the test
SDK sets `IsTestProject` after `Directory.Build.props` is evaluated:

```xml
<Project>
  <!-- Missing-docs warnings only matter for shipped/public API. -->
  <PropertyGroup Condition="'$(IsTestProject)' == 'true' or '$(IsPackable)' == 'false'">
    <NoWarn>$(NoWarn);CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

**Application repos.** Web apps, workers and console apps usually shouldn't require docs on
every public controller or handler. Widen the condition, for example to
`'$(IsTestProject)' == 'true' or '$(OutputType)' != 'Library'`, so that only the shared class
libraries are required to have docs. The structural errors still apply everywhere.

**Rolling out in an existing repo.** Don't add the `WarningsAsErrors` line first. Count, fix,
then promote.

Count unique warnings. `--no-incremental` matters, because an up-to-date build prints nothing.
Piped MSBuild output also repeats each warning, so de-duplicate:

```bash
count() { dotnet build -c Release --no-incremental 2>&1 | grep -oE "[^ ]+\([0-9,]+\): warning ($1)" | sort -u | wc -l; }
count 'CS0419|CS157[0-4]|CS158[04]|CS1587|CS171[12]|CS173[45]'   # structural
count 'CS1591'                                                  # missing docs
```

1. **Structural IDs:** with only `GenerateDocumentationFile` on, fix every structural hit. They
   are real defects: stale `<param>` names, dead `cref`s. Then add the `WarningsAsErrors` line.
   If the backlog is too big for one change, promote the IDs per project, in each csproj, as each
   project is cleaned.
2. **CS1591:** leave it as a warning, and backfill one project at a time using the
   `dotnet-xml-docs` writing rules. Don't generate filler docs just to clear the warning.
3. **When a project reaches zero CS1591,** promote it to an error for that project by adding
   `<WarningsAsErrors>$(WarningsAsErrors);CS1591</WarningsAsErrors>` to its csproj. Move it into
   `Directory.Build.props` once every project is clean.

**Never** turn on `TreatWarningsAsErrors` for the whole repo as part of documentation work
unless the user asks. It escalates every analyzer warning, not just the ones for docs.

## Layer 2: content quality (optional, StyleCop documentation rules only)

The compiler can't tell "Gets the value." on a settable property from a correct summary.
StyleCop's SA16xx rules can. Add the package without adopting all of StyleCop's layout and
spacing rules:

```xml
<!-- Directory.Build.props -->
<ItemGroup>
  <PackageReference Include="StyleCop.Analyzers" Version="1.1.118" PrivateAssets="all" />
</ItemGroup>
```

```ini
# .editorconfig: keep only StyleCop's documentation rules
[*.cs]
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.SpacingRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.ReadabilityRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.OrderingRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.NamingRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.MaintainabilityRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.LayoutRules.severity = none
dotnet_analyzer_diagnostic.category-StyleCop.CSharp.SpecialRules.severity = none
# CS1591 already covers "is documented"; file headers are noise for most repos.
dotnet_diagnostic.SA1600.severity = none
dotnet_diagnostic.SA1633.severity = none
```

If the repo already uses StyleCop, **don't** add these lines, because they would switch off its
existing rules. Just make sure the SA16xx rules aren't suppressed.

Useful rules left on by that config:

| Rule | Flags |
| --- | --- |
| SA1604, SA1606 | Missing or empty `<summary>` |
| SA1608 | Summary still has the default placeholder text |
| SA1611, SA1614, SA1615, SA1616 | Missing or empty `<param>` / `<returns>` |
| SA1618 | Missing `<typeparam>` |
| SA1623 | Property summary doesn't match the accessors ("Gets" on a get/set property) |
| SA1625 | Copy-pasted documentation text |
| SA1629 | Doc text doesn't end with a period |
| SA1642 | Constructor summary doesn't start with the standard phrase |

StyleCop.Analyzers 1.1.118 is the latest **stable** release. It's old but still works. The
1.2.0 betas add newer C# syntax support; use one if 1.1.118 gives false positives on new
language features.

## Layer 3: public API surface (libraries)

Microsoft.CodeAnalysis.PublicApiAnalyzers makes every public API change visible as a diff line,
and that line is where reviewers check its docs. Add it to **shipped library projects only**,
in their csproj:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.CodeAnalysis.PublicApiAnalyzers" Version="5.6.0" PrivateAssets="all" />
</ItemGroup>
```

Create two files next to the csproj. The analyzer picks them up automatically. Each must start
with the `#nullable enable` line:

```text
PublicAPI.Shipped.txt     -> "#nullable enable"
PublicAPI.Unshipped.txt   -> "#nullable enable"
```

- **RS0016** fires for public symbols not listed in either file, and **RS0017** for listed symbols
  that no longer exist.
- **To record new API without an IDE:**
  `dotnet format analyzers path/to/Project.csproj --diagnostics RS0016 --severity info`
  This appends every missing symbol to `PublicAPI.Unshipped.txt`.
- **At release:** move the lines from Unshipped to Shipped.
- **A line removed from Shipped is a breaking change.** Call it out in the CHANGELOG.

## Layer 4: doc output for consumers (libraries)

- **`<inheritdoc/>` is not expanded by the compiler.** The raw tag goes into the `.xml` file, and
  some consumers show nothing. `SauceControl.InheritDoc` (2.0.2) expands it after the build.
  It only runs in non-Debug configurations, so check `bin/Release/**/*.xml`, not Debug.

  ```xml
  <PackageReference Include="SauceControl.InheritDoc" Version="2.0.2" PrivateAssets="all" />
  ```

- **The package includes the `.xml` file automatically** when `GenerateDocumentationFile` is on.
- **If you publish an API site with DocFX**, build it with `--warningsAsErrors`. That fails on
  unresolved xrefs (see the `dotnet-repo-docs` skill).

## Verifying the setup

```bash
# Unique doc-related diagnostics, grouped by ID (--no-incremental: an up-to-date build prints nothing).
dotnet build -c Release --no-incremental 2>&1 \
  | grep -oE '[^ ]+\([0-9,]+\): (warning|error) (CS15[0-9]{2}|CS17[0-9]{2}|CS0419|SA16[0-9]{2}|RS001[67])' \
  | sort -u | grep -oE '(CS|SA|RS)[0-9]+$' | sort | uniq -c
```

Expected result:

- no structural CS errors
- a known CS1591 count, trending toward zero
- no RS0016 once `PublicAPI.Unshipped.txt` is updated
