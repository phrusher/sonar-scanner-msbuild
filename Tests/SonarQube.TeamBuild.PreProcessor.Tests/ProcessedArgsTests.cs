﻿//-----------------------------------------------------------------------
// <copyright file="ProcessedArgsTests.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarQube.Common;
using System;
using TestUtilities;

namespace SonarQube.TeamBuild.PreProcessor.Tests
{
    [TestClass]
    public class ProcessedArgsTests
    {
        #region Tests

        [TestMethod]
        public void ProcArgs_GetSetting()
        {
            // 0. Setup
            ListPropertiesProvider cmdLineProps = new ListPropertiesProvider();
            cmdLineProps.AddProperty("cmd.key.1", "cmd value 1");
            cmdLineProps.AddProperty("shared.key.1", "shared cmd value");

            ListPropertiesProvider fileProps = new ListPropertiesProvider();
            fileProps.AddProperty("file.key.1", "file value 1");
            fileProps.AddProperty("shared.key.1", "shared file value");

            ProcessedArgs args = new ProcessedArgs("key", "branch", "ver", true, cmdLineProps, fileProps);

            // 1. Throws on missing value
            AssertException.Expects<InvalidOperationException>(() => args.GetSetting("missing.property"));

            // 2. Returns existing values
            Assert.AreEqual("cmd value 1", args.GetSetting("cmd.key.1"));
            Assert.AreEqual("file value 1", args.GetSetting("file.key.1"));

            // 3. Precedence - command line properties should win
            Assert.AreEqual("shared cmd value", args.GetSetting("shared.key.1"));

            // 4. Preprocessor only settings
            Assert.AreEqual(true, args.InstallLoaderTargets);
        }

        [TestMethod]
        public void ProcArgs_TryGetSetting()
        {
            // 0. Setup
            ListPropertiesProvider cmdLineProps = new ListPropertiesProvider();
            cmdLineProps.AddProperty("cmd.key.1", "cmd value 1");
            cmdLineProps.AddProperty("shared.key.1", "shared cmd value");

            ListPropertiesProvider fileProps = new ListPropertiesProvider();
            fileProps.AddProperty("file.key.1", "file value 1");
            fileProps.AddProperty("shared.key.1", "shared file value");

            ProcessedArgs args = new ProcessedArgs("key", "branch", "ver", false, cmdLineProps, fileProps);

            // 1. Missing key -> null
            string result;
            Assert.IsFalse(args.TryGetSetting("missing.property", out result), "Expecting false when the specified key does not exist");
            Assert.IsNull(result, "Expecting the value to be null when the specified key does not exist");

            // 2. Returns existing values
            Assert.IsTrue(args.TryGetSetting("cmd.key.1", out result));
            Assert.AreEqual("cmd value 1", result);

            // 3. Precedence - command line properties should win
            Assert.AreEqual("shared cmd value", args.GetSetting("shared.key.1"));

            // 4. Preprocessor only settings
            Assert.AreEqual(false, args.InstallLoaderTargets);
        }

        [TestMethod]
        public void ProcArgs_GetSettingOrDefault()
        {
            // 0. Setup
            ListPropertiesProvider cmdLineProps = new ListPropertiesProvider();
            cmdLineProps.AddProperty("cmd.key.1", "cmd value 1");
            cmdLineProps.AddProperty("shared.key.1", "shared cmd value");

            ListPropertiesProvider fileProps = new ListPropertiesProvider();
            fileProps.AddProperty("file.key.1", "file value 1");
            fileProps.AddProperty("shared.key.1", "shared file value");

            ProcessedArgs args = new ProcessedArgs("key", "name", "ver", true, cmdLineProps, fileProps);

            // 1. Missing key -> default returned
            string result = args.GetSetting("missing.property", "default value");
            Assert.AreEqual("default value", result);

            // 2. Returns existing values
            result = args.GetSetting("file.key.1", "default value");
            Assert.AreEqual("file value 1", result);

            // 3. Precedence - command line properties should win
            Assert.AreEqual("shared cmd value", args.GetSetting("shared.key.1", "default ValueType"));

            // 4. Preprocessor only settings
            Assert.AreEqual(true, args.InstallLoaderTargets);
        }

        [TestMethod]
        public void ProcArgs_CmdLinePropertiesOverrideFileSettings()
        {
            // Checks command line properties override those from files

            // Arrange
            // The set of command line properties to supply
            ListPropertiesProvider cmdLineProperties = new ListPropertiesProvider();
            cmdLineProperties.AddProperty("shared.key1", "cmd line value1 - should override server value");
            cmdLineProperties.AddProperty("cmd.line.only", "cmd line value4 - only on command line");
            cmdLineProperties.AddProperty("xxx", "cmd line value XXX - lower case");
            cmdLineProperties.AddProperty(SonarProperties.HostUrl, "http://host");

            // The set of file properties to supply
            ListPropertiesProvider fileProperties = new ListPropertiesProvider();
            fileProperties.AddProperty("shared.key1", "file value1 - should be overridden");
            fileProperties.AddProperty("file.only", "file value3 - only in file");
            fileProperties.AddProperty("XXX", "file line value XXX - upper case");

            // Act
            ProcessedArgs args = new ProcessedArgs("key", "branch", "version", false, cmdLineProperties, fileProperties);

            AssertExpectedValue("shared.key1", "cmd line value1 - should override server value", args);
            AssertExpectedValue("cmd.line.only", "cmd line value4 - only on command line", args);
            AssertExpectedValue("file.only", "file value3 - only in file", args);
            AssertExpectedValue("xxx", "cmd line value XXX - lower case", args);
            AssertExpectedValue("XXX", "file line value XXX - upper case", args);
            AssertExpectedValue(SonarProperties.HostUrl, "http://host", args);
        }

        #endregion

        #region Checks

        private static void AssertExpectedValue(string key, string expectedValue, ProcessedArgs args)
        {
            string actualValue;
            bool found = args.TryGetSetting(key, out actualValue);

            Assert.IsTrue(found, "Expected setting was not found. Key: {0}", key);
            Assert.AreEqual(expectedValue, actualValue, "Setting does not have the expected value. Key: {0}", key);
        }

        #endregion
    }
}
