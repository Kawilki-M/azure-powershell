﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using Microsoft.Azure.Commands.StorageSync.Evaluation;
using Microsoft.Azure.Commands.StorageSync.Evaluation.Validations;
using Xunit;
using Moq;
using Microsoft.Azure.Commands.StorageSync.Evaluation.Validations.SystemValidations;

namespace Microsoft.Azure.Commands.StorageSync.Test.UnitTests
{
    public class OSVersionValidationTest
    {
        [Fact]
		[Trait(Category.AcceptanceType, Category.CheckIn)]  
        public void WhenOsVersionIsSupportedValidationResultIsSuccessful()
        {
            // Prepare
            string aValidOSVersion = "1.0";
            uint aValidOSSku = 0;
            List<string> validOsVersions = new List<string>() { aValidOSVersion };
            List<uint> validOsSkus = new List<uint>() { aValidOSSku };
            var configurationMockFactory = new Moq.Mock<IConfiguration>();
            configurationMockFactory.Setup(configuration => configuration.ValidOsVersions()).Returns(validOsVersions);
            configurationMockFactory.Setup(configuration => configuration.ValidOsSKU()).Returns(validOsSkus);

            var powershellCommandRunnerMockFactory = new Moq.Mock<IPowershellCommandRunner>();
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.AddScript(It.IsAny<string>())).Verifiable();

            PSObject getCimInstanceResult = new PSObject();
            getCimInstanceResult.Members.Add(new PSNoteProperty("version", "1.0.123"));
            getCimInstanceResult.Members.Add(new PSNoteProperty("OperatingSystemSKU", aValidOSSku));
            Collection<PSObject> commandResults = new Collection<PSObject>
            {
                getCimInstanceResult
            };
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.Invoke()).Returns(commandResults);

            // Exercise
            OSVersionValidation osVersionValidation = new OSVersionValidation(configurationMockFactory.Object);
            IValidationResult validationResult = osVersionValidation.ValidateUsing(powershellCommandRunnerMockFactory.Object);

            // Verify
            Assert.StrictEqual<Result>(Result.Success, validationResult.Result);
        }

        [Fact]
		[Trait(Category.AcceptanceType, Category.CheckIn)]  
        public void WhenOsVersionIsNotSupportedValidationResultIsError()
        {
            // Prepare
            string aValidOSVersion = "1.0";
            uint aValidOSSku = 0;
            List<string> validOsVersions = new List<string>() { aValidOSVersion };
            List<uint> validOsSkus = new List<uint>() { aValidOSSku };
            var configurationMockFactory = new Moq.Mock<IConfiguration>();
            configurationMockFactory.Setup(configuration => configuration.ValidOsVersions()).Returns(validOsVersions);
            configurationMockFactory.Setup(configuration => configuration.ValidOsSKU()).Returns(validOsSkus);

            var powershellCommandRunnerMockFactory = new Moq.Mock<IPowershellCommandRunner>();
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.AddScript(It.IsAny<string>())).Verifiable();

            PSObject getCimInstanceResult = new PSObject();

            getCimInstanceResult.Members.Add(new PSNoteProperty("version", "2.0"));
            getCimInstanceResult.Members.Add(new PSNoteProperty("OperatingSystemSKU", aValidOSSku));

            Collection<PSObject> commandResults = new Collection<PSObject>
            {
                getCimInstanceResult
            };
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.Invoke()).Returns(commandResults);

            // Exercise
            OSVersionValidation osVersionValidation = new OSVersionValidation(configurationMockFactory.Object);
            IValidationResult validationResult = osVersionValidation.ValidateUsing(powershellCommandRunnerMockFactory.Object);

            // Verify
            Assert.StrictEqual<Result>(Result.Fail, validationResult.Result);
        }

        [Fact]
        [Trait(Category.AcceptanceType, Category.CheckIn)]
        public void WhenOsEditionIsNotSupportedValidationResultIsError()
        {
            // Prepare
            string aValidOSVersionPrefix = "1.0";
            uint aValidOSSku = 0;
            List<string> validOsVersions = new List<string>() { aValidOSVersionPrefix };
            List<uint> validOsSkus = new List<uint>() { aValidOSSku };
            var configurationMockFactory = new Moq.Mock<IConfiguration>();
            configurationMockFactory.Setup(configuration => configuration.ValidOsVersions()).Returns(validOsVersions);
            configurationMockFactory.Setup(configuration => configuration.ValidOsSKU()).Returns(validOsSkus);

            var powershellCommandRunnerMockFactory = new Moq.Mock<IPowershellCommandRunner>();
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.AddScript(It.IsAny<string>())).Verifiable();

            PSObject getCimInstanceResult = new PSObject();
            getCimInstanceResult.Members.Add(new PSNoteProperty("version", $"{aValidOSVersionPrefix}.123")); // valid version
            getCimInstanceResult.Members.Add(new PSNoteProperty("OperatingSystemSKU", aValidOSSku + 1)); // invalid edition

            Collection<PSObject> commandResults = new Collection<PSObject>
            {
                getCimInstanceResult
            };
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.Invoke()).Returns(commandResults);

            // Exercise
            OSVersionValidation osVersionValidation = new OSVersionValidation(configurationMockFactory.Object);
            IValidationResult validationResult = osVersionValidation.ValidateUsing(powershellCommandRunnerMockFactory.Object);

            // Verify
            Assert.StrictEqual<Result>(Result.Fail, validationResult.Result);
        }

        [Fact]
		[Trait(Category.AcceptanceType, Category.CheckIn)]  
        public void WhenCommandFailsToRunValidationResultIsUnavailable()
        {
            // Prepare
            string aValidOSVersion = "valid_os_version";
            List<string> validOsVersions = new List<string>() { aValidOSVersion };
            IConfiguration configuration = MockFactory.ConfigurationWithValidOSVersions(validOsVersions);

            var powershellCommandRunnerMockFactory = new Moq.Mock<IPowershellCommandRunner>();
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.AddScript(It.IsAny<string>())).Verifiable();

            PSObject getCimInstanceResult = new PSObject();
            string anInvalidOSVersion = "invalid_os_version";
            PSMemberInfo versionMember = new PSNoteProperty("version", anInvalidOSVersion);
            getCimInstanceResult.Members.Add(versionMember);
            Collection<PSObject> commandResults = new Collection<PSObject>
            {
                getCimInstanceResult
            };
            powershellCommandRunnerMockFactory.Setup(powershellCommandRunner => powershellCommandRunner.Invoke()).Throws(new Exception());

            // Exercise
            OSVersionValidation osVersionValidation = new OSVersionValidation(configuration);
            IValidationResult validationResult = osVersionValidation.ValidateUsing(powershellCommandRunnerMockFactory.Object);

            // Verify
            Assert.StrictEqual<Result>(Result.Unavailable, validationResult.Result);
        }
    }
}
