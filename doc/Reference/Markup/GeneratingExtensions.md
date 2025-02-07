---
uid: Reference.Markup.GeneratingExtensions
---
# Generating Extensions

[**Uno.Extensions.Markup.Generator**](https://www.nuget.org/packages/Uno.Extensions.Markup.Generators) is a source generator that will scan one or more assemblies for you to create the C# Markup Extensions for you to use. Once you have added the Source Generator to a given project it will scan that project and automatically generate extensions for the types that are found.

## Using the Generator for 3rd Party Libraries

To generate extensions for another assembly (i.e. from a NuGet dependency) you can add the `GenerateMarkupForAssembly` attribute to the assembly with a specified reference type from the assembly to scan.

```cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Generator;

[assembly: GenerateMarkupForAssembly(typeof(FrameworkElement))]
```

> [!TIP]
> If you do not add the reference to the Generator NuGet this attribute will be ignored and no source will be generated.

## Next Steps

Learn more about:

- [Generating C# Extensions for your libraries](xref:Reference.Markup.GeneratingExtensions)
