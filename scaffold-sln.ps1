$ProjectName = "wsl-squad"
$DotnetVersion = "10.0"
$SdkVersion = "10.0.402"

# 1. Initialisation Git et création du .gitignore officiel .NET
git init
dotnet new gitignore

# 2. Création du fichier global.json
dotnet new globaljson --sdk-version $SdkVersion --roll-forward latestFeature

# 3. Création de la solution
dotnet new sln -n $ProjectName

# 4. Création des projets applicatifs
dotnet new classlib -n "$ProjectName.Core" -o "src/$ProjectName.Core" -f "net$DotnetVersion"
dotnet new classlib -n "$ProjectName.Presentation" -o "src/$ProjectName.Presentation" -f "net$DotnetVersion"
dotnet new wpf -n "$ProjectName.UI.Wpf" -o "src/$ProjectName.UI.Wpf" -f "net$DotnetVersion"
dotnet new console -n "$ProjectName.UI.Cli" -o "src/$ProjectName.UI.Cli" -f "net$DotnetVersion"

# 5. Création du projet de tests xUnit
dotnet new xunit -n "$ProjectName.Tests" -o "tests/$ProjectName.Tests" -f "net$DotnetVersion"

# 6. Ajout des projets à la solution
dotnet sln add "src/$ProjectName.Core/$ProjectName.Core.csproj"
dotnet sln add "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj"
dotnet sln add "src/$ProjectName.UI.Wpf/$ProjectName.UI.Wpf.csproj"
dotnet sln add "src/$ProjectName.UI.Cli/$ProjectName.UI.Cli.csproj"
dotnet sln add "tests/$ProjectName.Tests/$ProjectName.Tests.csproj"

# 7. Configuration des dépendances inter-projets
dotnet add "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj" reference "src/$ProjectName.Core/$ProjectName.Core.csproj"
dotnet add "src/$ProjectName.UI.Wpf/$ProjectName.UI.Wpf.csproj" reference "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj"
dotnet add "src/$ProjectName.UI.Cli/$ProjectName.UI.Cli.csproj" reference "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj"
dotnet add "tests/$ProjectName.Tests/$ProjectName.Tests.csproj" reference "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj"

# 8. Ajout des packages NuGet indispensables
dotnet add "src/$ProjectName.Presentation/$ProjectName.Presentation.csproj" package CommunityToolkit.Mvvm
dotnet add "tests/$ProjectName.Tests/$ProjectName.Tests.csproj" package Moq
dotnet add "tests/$ProjectName.Tests/$ProjectName.Tests.csproj" package FluentAssertions

# 9. Création de Directory.Build.props
@'
<Project>
  <PropertyGroup>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisMode>Recommended</AnalysisMode>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
    <TreatWarningsAsErrors Condition="'$(Configuration)' == 'Release'">true</TreatWarningsAsErrors>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="SonarAnalyzer.CSharp" Version="9.*">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
'@ | Set-Content -Path "Directory.Build.props" -Encoding UTF8

# 10. Création de .editorconfig
@'
root = true

[*]
indent_style = space
indent_size = 4
end_of_line = lf
charset = utf-8
trim_trailing_whitespace = true
insert_final_newline = true

[*.{xml,xaml}]
indent_size = 2

[*.cs]
dotnet_analyzer_diagnostic.severity = warning
csharp_style_namespace_declarations = file_scoped:warning
dotnet_sort_system_directives_first = true
csharp_using_directive_placement = outside_namespace:warning
dotnet_style_require_accessibility_modifiers = always:warning

dotnet_naming_rule.interface_should_be_prefixed_with_i.severity = warning
dotnet_naming_rule.interface_should_be_prefixed_with_i.symbols = interface_symbols
dotnet_naming_rule.interface_should_be_prefixed_with_i.style = prefix_i_style
dotnet_naming_symbols.interface_symbols.applicable_kinds = interface
dotnet_naming_style.prefix_i_style.required_prefix = I
dotnet_naming_style.prefix_i_style.capitalization = pascal_case

dotnet_naming_rule.private_fields_should_be_prefixed.severity = warning
dotnet_naming_rule.private_fields_should_be_prefixed.symbols = private_field_symbols
dotnet_naming_rule.private_fields_should_be_prefixed.style = prefix_underscore_style
dotnet_naming_symbols.private_field_symbols.applicable_kinds = field
dotnet_naming_symbols.private_field_symbols.applicable_accessibilities = private
dotnet_naming_style.prefix_underscore_style.required_prefix = _
dotnet_naming_style.prefix_underscore_style.capitalization = camel_case

dotnet_naming_rule.types_and_methods_should_be_pascal_case.severity = warning
dotnet_naming_rule.types_and_methods_should_be_pascal_case.symbols = types_and_methods_symbols
dotnet_naming_rule.types_and_methods_should_be_pascal_case.style = pascal_case_style
dotnet_naming_symbols.types_and_methods_symbols.applicable_kinds = class, struct, enum, property, method
dotnet_naming_style.pascal_case_style.capitalization = pascal_case
'@ | Set-Content -Path ".editorconfig" -Encoding UTF8

# 11. Premier commit Git sur la branche main
git branch -M main
git add .
git commit -m "chore: initial solution setup"