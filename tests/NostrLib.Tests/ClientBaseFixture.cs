using System.Reflection;
using System.Text.Json;
using Xunit.Abstractions;

namespace NostrLib.Tests
{
    public abstract class ClientBaseFixture
    {
        private static string _assemblyLocation = typeof(ClientBaseFixture).GetTypeInfo().Assembly.Location;

        protected static readonly string BaseUrl = ConfigVariable("BASE_URL");

        public ITestOutputHelper Output { get; }

        public ClientBaseFixture(ITestOutputHelper testOutputHelper)
        {
            _assemblyLocation = typeof(ClientBaseFixture).GetTypeInfo().Assembly.Location;

            AssertSettingsAvailable();

            Output = testOutputHelper;
        }

        private static void AssertSettingsAvailable()
        {
            Assert.NotNull(BaseUrl);
        }

        /// <summary>
        /// Retrieve the config variable from the environment,
        /// or app.config if the environment doesn't specify it.
        /// </summary>
        /// <param name="variableName">The name of the environment variable to get.</param>
        /// <returns></returns>
        private static string ConfigVariable(string variableName)
        {
            string? retval = null;
            //this is here to allow us to have a config that isn't committed to source control, but still allows the project to build
            var configFile = Environment.GetEnvironmentVariable("NOSTRLIB_CONFIG_FILE_NAME") ?? "testing_keys.json";
            try
            {
                var location = Path.GetFullPath(_assemblyLocation);
                var pathComponents = location.Split(Path.DirectorySeparatorChar).ToList();
                var componentsCount = pathComponents.Count;
                var keyPath = "";
                while (componentsCount > 0)
                {
                    keyPath = Path.Combine(new[] { Path.DirectorySeparatorChar.ToString() }
                        .Concat(pathComponents.Take(componentsCount)
                            .Concat(new[] { configFile }))
                        .ToArray());
                    if (File.Exists(keyPath))
                    {
                        break;
                    }

                    componentsCount--;
                }

                var values = JsonSerializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(keyPath)) ?? new();
                retval = values[variableName];
            }
            catch
            {
                //This is OK, it just doesn't exist.. no big deal.
            }

            return string.IsNullOrWhiteSpace(retval) ? Environment.GetEnvironmentVariable(variableName) ?? string.Empty : retval;
        }
    }
}
