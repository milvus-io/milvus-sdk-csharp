using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.DotNet.Interactive;
using InteractiveKernel = Microsoft.DotNet.Interactive.Kernel;

public static class Settings
{
    private const string DefaultConfigFile = "config/settings.json";
    private const string Endpoint = "endpoint";
    private const string Port = "port";
    private const string Username = "username";
    private const string Password = "password";

    // Prompt user for milvus Endpoint URL
    public static async Task<string> AskEndpoint(string configFile = DefaultConfigFile)
    {
        var (endpoint, port, userName, password) = ReadSettings(configFile);

        // If needed prompt user for Azure endpoint
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            endpoint = await InteractiveKernel.GetInputAsync("Please enter your milvus endpoint");
        }

        WriteSettings(configFile,endpoint, port, userName, password);

        return endpoint;
    }

    public static async Task<int> AskPort(string configFile = DefaultConfigFile)
    {
        var (endpoint, port, userName, password) = ReadSettings(configFile);

        if (port == 0)
        {
            port = int.Parse(await InteractiveKernel.GetInputAsync("Please enter your milvus port"));
        }

        WriteSettings(configFile,endpoint, port, userName, password);

        return port;
    }

    // Prompt user for milvus username
    public static async Task<string> AskUsername(string configFile = DefaultConfigFile)
    {
        var (endpoint, port, userName, password) = ReadSettings(configFile);

        if (string.IsNullOrWhiteSpace(userName))
        {
            userName = await InteractiveKernel.GetInputAsync("Please enter your milvus username");            
        }

        WriteSettings(configFile,endpoint, port, userName, password);

        return userName;
    }

    // Prompt user for password
    public static async Task<string> AskPassword(string configFile = DefaultConfigFile)
    {
        var (endpoint, port, userName, password) = ReadSettings(configFile);

        if (string.IsNullOrWhiteSpace(password))
        {            
            password = await InteractiveKernel.GetInputAsync("Please enter your milvus username");
        }

        WriteSettings(configFile,endpoint, port, userName, password);

        return userName;
    }

    // Load settings from file
    public static (string endpoint, int port, string username, string password)
        LoadFromFile(string configFile = DefaultConfigFile)
    {
        if (!File.Exists(DefaultConfigFile))
        {
            Console.WriteLine("Configuration not found: " + DefaultConfigFile);
            Console.WriteLine("\nPlease run the Setup Notebook (0-AI-settings.ipynb) to configure your milvus backend first.\n");
            throw new Exception("Configuration not found, please setup the notebooks first using 00.Settings.ipynb");
        }

        try
        {
            var config = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(DefaultConfigFile));
            string endpoint = config[Endpoint];
            int port = int.Parse(config[Port]);
            string username = config[Username];
            string password = config[Password];

            return (endpoint, port, username, password);
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
            return ("", 0, "", "");
        }
    }

    // Delete settings file
    public static void Reset(string configFile = DefaultConfigFile)
    {
        if (!File.Exists(configFile)) { return; }

        try
        {
            File.Delete(configFile);
            Console.WriteLine("Settings deleted. Run the notebook again to configure your AI backend.");
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }
    }

    // Read and return settings from file
    private static (string endpoint, int port, string username, string password)
        ReadSettings(string configFile)
    {
        // Save the preference set in the notebook
        string endpoint = "";
        int port = 0;
        string username = "";
        string password = "";

        try
        {
            if (File.Exists(configFile))
            {
                (endpoint, port, username, password) = LoadFromFile(configFile);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }

        return (endpoint, port, username, password);
    }

    // Write settings to file
    private static void WriteSettings(
        string configFile, string endpoint, int port, string username, string password)
    {
        try
        {
            var data = new Dictionary<string, string>
                {
                    { Endpoint, endpoint },
                    { Port, port.ToString() },
                    { Username, username },
                    { Password,password }
                };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(configFile, JsonSerializer.Serialize(data, options));

        }
        catch (Exception e)
        {
            Console.WriteLine("Something went wrong: " + e.Message);
        }
    }
}
