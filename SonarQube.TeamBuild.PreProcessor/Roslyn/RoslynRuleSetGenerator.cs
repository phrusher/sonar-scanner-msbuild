﻿//-----------------------------------------------------------------------
// <copyright file="RoslynRuleSetGenerator.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using SonarQube.Common;

namespace SonarQube.TeamBuild.PreProcessor.Roslyn.Model
{
    public class RoslynRuleSetGenerator
    {
        private const string SONARANALYZER_PARTIAL_REPO_KEY = "sonaranalyzer-{0}";
        private const string ROSLYN_REPOSITORY_PREFIX = "roslyn.";
        private readonly IDictionary<string, string> serverSettings;

        public RoslynRuleSetGenerator(IDictionary<string, string> serverSettings)
        {
            if (serverSettings == null)
            {
                throw new ArgumentNullException(nameof(serverSettings));
            }
            this.serverSettings = serverSettings;
        }

        public RuleSet Generate(IEnumerable<ActiveRule> activeRules, IEnumerable<string> inactiveRules, string language)
        {
            if (activeRules == null || !activeRules.Any())
            {
                throw new ArgumentNullException("activeRules");
            }
            if (inactiveRules == null)
            {
                throw new ArgumentNullException(nameof(inactiveRules));
            }
            if (language == null)
            {
                throw new ArgumentNullException(nameof(language));
            }

            Dictionary<string, List<ActiveRule>> activeRulesByPartialRepoKey = ActiveRoslynRulesByPartialRepoKey(activeRules, language);
            Dictionary<string, List<string>> inactiveRulesByRepoKey = GetInactiveRulesByRepoKey(inactiveRules);

            RuleSet ruleSet = new RuleSet();

            ruleSet.Name = "Rules for SonarQube";
            ruleSet.Description = "This rule set was automatically generated from SonarQube";
            ruleSet.ToolsVersion = "14.0";

            foreach (KeyValuePair<string, List<ActiveRule>> entry in activeRulesByPartialRepoKey)
            {
                Rules rules = new Rules();
                string repoKey = entry.Value.First().RepoKey;
                rules.AnalyzerId = MandatoryPropertyValue(AnalyzerIdPropertyKey(entry.Key));
                rules.RuleNamespace = MandatoryPropertyValue(RuleNamespacePropertyKey(entry.Key));

                // add active rules
                rules.RuleList = entry.Value.Select(r => new Rule(r.RuleKey, "Warning")).ToList();

                // add other
                List<string> otherRules;
                if (inactiveRulesByRepoKey.TryGetValue(repoKey, out otherRules))
                {
                    rules.RuleList.AddRange(otherRules.Select(r => new Rule(r, "None")).ToList());
                }
                ruleSet.Rules.Add(rules);
            }

            return ruleSet;
        }

        private static Dictionary<string, List<string>> GetInactiveRulesByRepoKey(IEnumerable<string> inactiveRules)
        {
            Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
            foreach (string r in inactiveRules)
            {
                string key;
                string repo;
                ParseRuleKey(r, out repo, out key);
                AddDict(dict, repo, key);
            }
            return dict;
        }

        private static void ParseRuleKey(string keyWithRepo, out string repo, out string key)
        {
            int pos = keyWithRepo.IndexOf(':');
            repo = keyWithRepo.Substring(0, pos);
            key = keyWithRepo.Substring(pos+1);
        }

        private static Dictionary<string, List<ActiveRule>> ActiveRoslynRulesByPartialRepoKey(IEnumerable<ActiveRule> activeRules, string language)
        {
            Dictionary<string, List<ActiveRule>> rulesByPartialRepoKey = new Dictionary<string, List<ActiveRule>>();

            foreach (ActiveRule activeRule in activeRules)
            {
                if (activeRule.RepoKey.StartsWith(ROSLYN_REPOSITORY_PREFIX))
                {
                    String pluginKey = activeRule.RepoKey.Substring(ROSLYN_REPOSITORY_PREFIX.Length);
                    AddDict(rulesByPartialRepoKey, pluginKey, activeRule);
                }
                else if ("csharpsquid".Equals(activeRule.RepoKey) || "vbnet".Equals(activeRule.RepoKey))
                {
                    AddDict(rulesByPartialRepoKey, string.Format(SONARANALYZER_PARTIAL_REPO_KEY, language), activeRule);
                }
            }

            return rulesByPartialRepoKey;
        }

        private static void AddDict<T>(Dictionary<string, List<T>> dict, string key, T value)
        {
            List<T> list;
            if(!dict.TryGetValue(key, out list))
            {
                list = new List<T>();
                dict.Add(key, list);
            }
            list.Add(value);
        }

        private static string AnalyzerIdPropertyKey(string partialRepoKey)
        {
            return partialRepoKey + ".analyzerId";
        }

        private static string RuleNamespacePropertyKey(string partialRepoKey)
        {
            return partialRepoKey + ".ruleNamespace";
        }

        private string MandatoryPropertyValue(String propertyKey)
        {
            if (propertyKey == null)
            {
                throw new ArgumentNullException("propertyKey");
            }
            if (!serverSettings.ContainsKey(propertyKey))
            {
                throw new ArgumentException("key doesn't exist: " + propertyKey);
            }
            return serverSettings[propertyKey];
        }
    }
}
