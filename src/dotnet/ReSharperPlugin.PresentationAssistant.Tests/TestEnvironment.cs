﻿using System.Threading;
using JetBrains.Application.BuildScript.Application.Zones;
using JetBrains.ReSharper.TestFramework;
using JetBrains.TestFramework;
using JetBrains.TestFramework.Application.Zones;
using NUnit.Framework;

[assembly: Apartment(ApartmentState.STA)]

namespace ReSharperPlugin.PresentationAssistant.Tests
{
  [ZoneDefinition]
  public interface IPresentationAssistantTestZone : ITestsEnvZone, IRequire<PsiFeatureTestZone>
  {
  }

  [SetUpFixture]
  public class TestEnvironment : ExtensionTestEnvironmentAssembly<IPresentationAssistantTestZone>
  {
  }
}
