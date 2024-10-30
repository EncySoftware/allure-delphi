using System;
using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary> Build helper </summary>
public static class BuildUtils {

    /// <summary> Converting variant to configuration </summary>
    /// <param name="variant"> Example Release_x64 </param>
    /// <returns> Release/Debug configuration </returns>
    public static string Configuration(string variant) {
        string config = variant.Substring(0, variant.IndexOf("_"));
        return config.Equals("debug", StringComparison.CurrentCultureIgnoreCase) ? "Debug" : "Release";
    }

    /// <summary> Returns the parsed json root node without comments </summary>
    public static JsonNode? GetRootNode(string jsonFilePath) {
        var jData = File.ReadAllText(jsonFilePath);
        var jOpts = new JsonDocumentOptions() {
            CommentHandling = JsonCommentHandling.Skip
        };
        return JsonNode.Parse(jData, null, jOpts);
    }

    /// <summary>
    /// Reads a list of projects from configFiles
    /// </summary>
    /// <param name="configFiles"> Json configuration file paths </param>
    public static HashSet<string> GetProjectsList(string[] configFiles) {
        var resultList = new HashSet<string>();
        foreach (var config in configFiles) {
            if (!File.Exists(config)) continue;
            var configDir = Path.GetDirectoryName(config) + "";
            var projs = GetRootNode(config)?["projects"]?.AsArray();
            foreach (var jproj in projs ?? new()) {
                var projPath = Path.GetFullPath(Path.Combine(configDir, jproj!.ToString()));
                if (File.Exists(projPath) && !resultList.Contains(projPath))
                    resultList.Add(projPath);
            }
        }
        return resultList;
    }
}