using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Celeste64.HookGen.Tests;

public class IncrementalHookGeneratorTests
{
	private const string SourceClassText = @"
namespace TestNamespace
{
	public class Player
	{
	    public float X {{ get; set; }}
	    public float Y {{ get; set; }}
	    public float Z {{ get; set; }}

		public void Jump()
		{
			Console.WriteLine(""Jump"");	
		}
	}
}

namespace Celeste64 
{
	public class Player
	{
	    public float X {{ get; set; }}
	    public float Y {{ get; set; }}
	    public float Z {{ get; set; }}

		public static void Jump()
		{
			Console.WriteLine(""Jump"");	
		}
	}

	public class Badeline
	{
	    public float X {{ get; set; }}
	    public float Y {{ get; set; }}
	    public float Z {{ get; set; }}

		public int? Spawn(float timer,int frames)
		{
			Console.WriteLine(""Spawn"");	
		}
	}

	public static class Test
	{
		static Test() 
		{
			Console.WriteLine(""static"");
		}
	}
}
";

	private const string ExpectedGeneratedClassText = @"// <auto-generated/>

using System;
using System.Collections.Generic;

namespace TestNamespace;

partial class Vector3
{
    public IEnumerable<string> Report()
    {
        yield return $""X:{this.X}"";
        yield return $""Y:{this.Y}"";
        yield return $""Z:{this.Z}"";
    }
}
";
	
	public static void GenerateOrigDelegate()
	{
		// Create an instance of the source generator.
		var generator = new IncrementalHookGenerator();

		// Source generators should be tested using 'GeneratorDriver'.
		var driver = CSharpGeneratorDriver.Create(generator);

		// We need to create a compilation with the required source code.
		var compilation = CSharpCompilation.Create(nameof(SampleSourceGeneratorTests),
			new[] { CSharpSyntaxTree.ParseText(SourceClassText) },
			new[]
			{
				// To support 'System.Attribute' inheritance, add reference to 'System.Private.CoreLib'.
				MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
			});

		// Run generators and retrieve all results.
		var runResult = driver.RunGenerators(compilation).GetRunResult();

		// All generated files can be found in 'RunResults.GeneratedTrees'.
		// var generatedFileSyntax = runResult.GeneratedTrees.Single(t => t.FilePath.EndsWith("Vector3.g.cs"));
		foreach (var runResultGeneratedTree in runResult.GeneratedTrees)
		{
			Console.WriteLine($"Generated {runResultGeneratedTree.FilePath}: {runResultGeneratedTree.GetText().ToString()}");
		}
		
		// Complex generators should be tested using text comparison.
		// Assert.Equal(ExpectedGeneratedClassText, generatedFileSyntax.GetText().ToString(),
		// 	ignoreLineEndingDifferences: true);
	}
}