using System.Reflection;
using System.Runtime.CompilerServices;

using FluentAssertions;

using NetArchTest.Rules;

using WslSquad.Core.Repositories;

namespace WslSquad.Core.Tests.Architecture;

public class NamingConventionTests
{
    [Fact]
    public void Constructor_parameters_are_named_after_their_interface()
    {
        Assembly assembly = typeof(IInstanceRepository).Assembly;
        string[] suffixes = ["Repository", "Service", "Mapper", "Catalog"];
        List<string> violations = [];
        int inspected = 0;

        foreach (Type type in Types.InAssembly(assembly).GetTypes())
        {
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            foreach (ConstructorInfo constructor in type.GetConstructors(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (ParameterInfo parameter in constructor.GetParameters())
                {
                    Type parameterType = parameter.ParameterType;

                    if (!parameterType.IsInterface ||
                        !parameterType.Name.StartsWith('I') ||
                        !Array.Exists(
                            suffixes,
                            suffix => parameterType.Name.EndsWith(suffix, StringComparison.Ordinal)))
                    {
                        continue;
                    }

                    inspected++;
                    string cleanName = parameterType.Name[1..];
                    string expected = $"{char.ToLowerInvariant(cleanName[0])}{cleanName[1..]}";

                    if (!string.Equals(parameter.Name, expected, StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"{type.Name}({parameterType.Name} {parameter.Name}) should be '{expected}'");
                    }
                }
            }
        }

        inspected.Should().BeGreaterThan(0, "the scan must find constructor parameters");
        violations.Should().BeEmpty(
            "constructor parameters holding an interface must be named after that interface without the I prefix");
    }

    [Fact]
    public void Interface_fields_are_named_after_their_interface()
    {
        Assembly assembly = typeof(IInstanceRepository).Assembly;
        List<string> violations = [];
        int inspected = 0;

        foreach (Type type in Types.InAssembly(assembly).GetTypes())
        {
            if (type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
            {
                continue;
            }

            foreach (FieldInfo field in type.GetFields(
                         BindingFlags.NonPublic | BindingFlags.Instance))
            {
                Type fieldType = field.FieldType;

                if (!fieldType.IsInterface ||
                    fieldType.IsGenericType ||
                    !fieldType.Name.StartsWith('I') ||
                    field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false))
                {
                    continue;
                }

                inspected++;
                string suffix = fieldType.Name[1..];
                string expected = $"_{char.ToLowerInvariant(suffix[0])}{suffix[1..]}";

                if (!string.Equals(field.Name, expected, StringComparison.Ordinal))
                {
                    violations.Add($"{type.Name}.{field.Name} ({fieldType.Name}) should be {expected}");
                }
            }
        }

        inspected.Should().BeGreaterThan(0, "the scan must find the repository and service fields");
        violations.Should().BeEmpty(
            "private fields holding an interface must be named after that interface without the I prefix");
    }
}
