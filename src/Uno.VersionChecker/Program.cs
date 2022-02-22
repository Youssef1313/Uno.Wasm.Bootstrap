using Colorful;
using ConsoleTables;
using HtmlAgilityPack;
using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Console = Colorful.Console;

namespace Uno.VersionChecker
{
	class Program
	{
		static async Task<int> Main(string[] args)
		{
			var version = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
			Console.WriteLineFormatted("Uno Version Checker v{0}.", Color.Gray, new Formatter(version, Color.Aqua));
			var webSiteUrl = args.FirstOrDefault()
#if DEBUG
				?? "https://nuget.info/";
#else
				;
#endif

			if(string.IsNullOrEmpty(webSiteUrl))
			{
				var module = global::System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name;
				Console.WriteLine($"Usage: {module} [application hostname or url]", Color.Yellow);
				Console.WriteLine($"Default scheme is https if not specified.");
				return 100;
			}

			if(!webSiteUrl.EndsWith('/'))
			{
				webSiteUrl += '/';
			}

			Console.WriteLineFormatted("Checking website at address {0}.", Color.Gray, new Formatter(webSiteUrl, Color.Aqua));

			Uri siteUri;

			try
			{
				siteUri = new Uri(webSiteUrl);

                if (siteUri.Scheme != Uri.UriSchemeHttp && siteUri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new UriFormatException();
                }
            }
			catch(UriFormatException)
			{
				siteUri = new Uri($"https://{webSiteUrl}");
			}

			var web = new HtmlWeb();
			var doc = await web.LoadFromWebAsync(siteUri, default, default, default);

			Console.WriteLine("Trying to find Uno bootstrapper configuration...", Color.Gray);

			var unoConfigPath = doc.DocumentNode
				.SelectNodes("//script")
				.Select(scriptElement => scriptElement.GetAttributeValue("src", ""))
				.Where(src => !string.IsNullOrWhiteSpace(src))
				.Select(src => new Uri(src, UriKind.RelativeOrAbsolute))
				.Where(uri => !uri.IsAbsoluteUri)
				.Select(uri => new Uri(siteUri, uri))
				.FirstOrDefault(uri => uri.GetLeftPart(UriPartial.Path).EndsWith("uno-config.js", StringComparison.OrdinalIgnoreCase));

            if (unoConfigPath is null)
            {
                using var http = new HttpClient();
                var embeddedjs = new Uri(siteUri, "embedded.js");
                var embeddedResponse = await http.GetAsync(embeddedjs);
                if (embeddedResponse.IsSuccessStatusCode)
                {
                    var content = await embeddedResponse.Content.ReadAsStringAsync(default);
                    if (Regex.Match(content, @"const\spackage\s?=\s?""(?<package>package_\w+)"";") is { Success : true } match)
                    {
                        var package = match.Groups["package"].Value + "/uno-config.js";
                        unoConfigPath = new Uri(siteUri, package);
                    }
                }
            }

			if(unoConfigPath is null)
			{
				Console.WriteLine("No Uno / Uno.UI application found.", Color.Red);
				return 2;
			}
			Console.WriteLine("Application found.", Color.LightGreen);

			Console.WriteLineFormatted("Configuration url is {0}.", Color.Gray, new Formatter(unoConfigPath, Color.Aqua));

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

                Console.WriteLineFormatted("Starting assembly is {0}.", Color.Gray, new Formatter(config.mainAssembly, Color.Aqua));

                if (config.assemblies is null || config.assemblies is { Length: 0 })
                {
                    Console.WriteLine("No assemblies found. That's odd.", Color.Red);
                    return 1;
                }

                Console.WriteLineFormatted("{0} assemblies found. Downloading assemblies to read metadata...", Color.Gray, new Formatter(config.assemblies.Length, Color.Aqua));

                var tasks = config.assemblies
                    .Select(a => GetAssemblyDetails(new Uri(config.assembliesPath, a)))
                    .ToArray();

                var assemblyDetails = (await Task.WhenAll(tasks))
                    .OrderBy(d => d.name);

                var table = new ConsoleTable("Name", "Version", "File Version", "Framework");

				(string name, string version, string fileVersion, string targetFramework) mainAssemblyDetails = default;

				foreach (var assemblyDetail in assemblyDetails)
                {
                    table.AddRow(assemblyDetail.name, assemblyDetail.version, assemblyDetail.fileVersion, assemblyDetail.targetFramework);

					if(assemblyDetail.name.Equals(config.mainAssembly))
                    {
                        mainAssemblyDetails = assemblyDetail;
                    }
                }

				Console.WriteLine();

                WriteTable(table);

				if(mainAssemblyDetails.name is { })
                {
					Console.WriteLineFormatted("{0} version is {1}", Color.Gray, new Formatter(mainAssemblyDetails.name, Color.Aqua), new Formatter(mainAssemblyDetails.version, Color.Aqua));
                }

                var uno = assemblyDetails.FirstOrDefault(d => d.name.Equals("Uno.UI"));
                if (uno is { })
                {
                    Console.WriteLineFormatted("Uno.UI version is {0}", Color.Gray, new Formatter(uno.version, Color.Aqua));
                }
                else
                {
                    Console.WriteLine("Unable to identify the version of Uno.UI on this application. Maybe this application is only using the Uno bootstrapper.", Color.Orange);
                }

                return 0;
            }
            catch (Exception ex)
			{
				Console.Error.WriteLine($"Unable to read uno config: {ex}", Color.Red);
				return -1;
			}
		}

        private static void WriteTable(ConsoleTable table)
        {
            var writer = new StringWriter();
            table.Options.OutputTo = writer;
            table.Write(Format.Minimal);

            var alternator = new ColorAlternatorFactory().GetAlternator(1, Color.Aqua, Color.LightBlue);
            var isHeader = 2;

            foreach (var line in writer.ToString().Split(Environment.NewLine))
            {
                if (isHeader-- > 0)
                {
                    Console.WriteLine(line, Color.White);
                }
                else
                {
                    Console.WriteLineAlternating(line, alternator);
                }
            }
        }
    }
}
