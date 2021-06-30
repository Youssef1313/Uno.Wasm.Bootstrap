using ConsoleTables;
using HtmlAgilityPack;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Uno.VersionChecker
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var webSiteUrl = args.FirstOrDefault()
#if DEBUG
				?? "https://nuget.info/";
#else
				;
#endif

			if(string.IsNullOrEmpty(webSiteUrl))
			{
				var module = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
				Console.WriteLine($"Usage: {module} [web site url]");
				return 100;
			}

			Console.WriteLine($"Checking website at address {webSiteUrl}");

			Uri siteUri;

			try
			{
				siteUri = new Uri(webSiteUrl);
			}
			catch(UriFormatException)
			{
				siteUri = new Uri($"https://{webSiteUrl}");
			}

			var web = new HtmlWeb();
			var doc = await web.LoadFromWebAsync(siteUri, default, default, default);

			Console.WriteLine("Identifiying the Uno.UI application");

			var unoConfigPath = doc.DocumentNode
				.SelectNodes("//script")
				.Select(scriptElement => scriptElement.GetAttributeValue("src", ""))
				.Where(src => !string.IsNullOrWhiteSpace(src))
				.Select(src => new Uri(src, UriKind.RelativeOrAbsolute))
				.Where(uri => !uri.IsAbsoluteUri)
				.Select(uri => new Uri(siteUri, uri))
				.FirstOrDefault(uri => uri.GetLeftPart(UriPartial.Path).EndsWith("uno-config.js", StringComparison.OrdinalIgnoreCase));

			if(unoConfigPath is null)
			{
				Console.WriteLine("No Uno.UI application found.");
				return 2;
			}

			Console.WriteLine($"Application found, configuration at url {unoConfigPath}");

			using var httpClient = new HttpClient();

			async Task<(Uri assembliesPath, string? mainAssembly, string[]? assemblies)> GetConfig(Uri uri)
			{
				using var stream = await httpClient.GetStreamAsync(uri);
				using var reader = new StreamReader(stream);

				string? managePath = default;
				string? packagePath = default;
				string? mainAssembly = default;
				string[]? assemblies = default;

				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if(string.IsNullOrWhiteSpace(line))
					{
						continue;
					}

					var parts = line.Split(new[] { '=' }, 2);
					if(parts.Length != 2)
					{
						continue;
					}

					var field = parts[0].Trim().ToLowerInvariant();
					var value = parts[1].Trim().TrimEnd(';');

					switch (field)
					{
						case "config.uno_remote_managedpath":
							managePath = JsonSerializer.Deserialize<string>(value);
							break;
						case "config.uno_app_base":
							packagePath = JsonSerializer.Deserialize<string>(value);
							break;
						case "config.assemblies_with_size":
							assemblies = JsonSerializer.Deserialize<Dictionary<string, long>>(value)?.Keys.ToArray();
							break;
						case "config.uno_main":
							mainAssembly = JsonSerializer.Deserialize<string>(value)?.Split(']', 2)[0].TrimStart('[');
							break;
					}

					if(managePath is { } && packagePath is { } && mainAssembly is { } && assemblies is { })
					{
						break;
					}
				}

				var assembliesPath = new Uri(new Uri(siteUri, packagePath + "/"), managePath + "/");

				return (assembliesPath, mainAssembly, assemblies);
			}

			async Task<(string name, string version, string fileVersion, string targetFramework)> GetAssemblyDetails(Uri uri)
			{
				using var httpStream = await httpClient.GetStreamAsync(uri);

				var stream = new MemoryStream();
				await httpStream.CopyToAsync(stream);
				stream.Position = 0;

				var assembly = AssemblyDefinition.ReadAssembly(stream);

				var attributes = assembly.CustomAttributes.ToArray();

				string name = assembly.Name.Name;
				string version = assembly.Name.Version.ToString();
				string? fileVersion = "";
				string? targetFramework = "";

				foreach(var attribute in attributes)
				{
					switch(attribute.AttributeType.Name)
					{
						case "AssemblyInformationalVersionAttribute":
							version = attribute.ConstructorArguments[0].Value?.ToString() ?? "";
							break;
						case "AssemblyFileVersionAttribute":
							fileVersion = attribute.ConstructorArguments[0].Value?.ToString() ?? "";
							break;
						case "TargetFrameworkAttribute":
							targetFramework = attribute.ConstructorArguments[0].Value?.ToString() ?? "";
							break;
					}
				}

				if(attributes.Length == 0)
				{
					targetFramework = "WASM AOT";
				}

				return (name, version, fileVersion, targetFramework);
			}

			try
			{
				var config = await GetConfig(unoConfigPath);

				Console.WriteLine($"Starting assembly is {config.mainAssembly}");

				if(config.assemblies is null || config.assemblies is {Length: 0 })
				{
					Console.WriteLine("No assemblies found");
					return 1;
				}

				Console.WriteLine($"{config.assemblies?.Length ?? 0} assemblies found. Reading metadata...");

				var tasks = config.assemblies
					.Select(a => GetAssemblyDetails(new Uri(config.assembliesPath, a)))
					.ToArray();

				var assemblyDetails = (await Task.WhenAll(tasks))
					.OrderBy(d => d.name);

				var table = new ConsoleTable("Name", "Version", "File Version", "Framework");

				foreach(var assemblyDetail in assemblyDetails)
				{
					table.AddRow(assemblyDetail.name, assemblyDetail.version, assemblyDetail.fileVersion, assemblyDetail.targetFramework);
				}

				table.Write(Format.Minimal);

				var uno = assemblyDetails.FirstOrDefault(d => d.name.Equals("Uno.UI"));
				if(uno is { })
				{
					Console.WriteLine($"Uno.UI version is {uno.version}");
				}
				else
				{
					Console.WriteLine("Unable to identify the version of Uno on this application.");
				}

				return 0;
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Unable to read uno config: {ex}");
				return -1;
			}
		}
	}
}
