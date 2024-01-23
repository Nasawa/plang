﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using NSubstitute;
using PLang.Services.OpenAi;
using PLang.Utils;
using PLangTests;
using System.Runtime.CompilerServices;
using static PLang.Modules.BaseBuilder;

namespace PLang.Modules.CallGoalModule.Tests
{
	[TestClass()]
	public class BuilderTests : BasePLangTest
	{
		Builder builder;

		[TestInitialize]
		public void Init()
		{
			base.Initialize();

			settings.Get(typeof(OpenAiService), "Global_AIServiceKey", Arg.Any<string>(), Arg.Any<string>()).Returns(Environment.GetEnvironmentVariable("OpenAIKey"));
			var llmService = new OpenAiService(settings, logger, cacheHelper, context);

			typeHelper = new TypeHelper(fileSystem, settings);

			builder = new Builder();
			builder.InitBaseBuilder("PLang.Modules.CallGoalModule", fileSystem, llmService, typeHelper, memoryStack, context, variableHelper, logger);

		}

		private void SetupResponse(string stepText, [CallerMemberName] string caller = "", Type? type = null)
		{
			var llmService = GetLlmService(stepText, caller, type);
			if (llmService == null) return;

			builder = new Builder();
			builder.InitBaseBuilder("PLang.Modules.CallGoalModule", fileSystem, llmService, typeHelper, memoryStack, context, variableHelper, logger);
		}

		[DataTestMethod]
		[DataRow("call !Process.Image name=%full_name%, %address%")]
		public async Task RunGoal_Test(string text)
		{
			SetupResponse(text);

			var step = new Building.Model.GoalStep();
			step.Text = text;			

			var instruction = await builder.Build(step);
			var gf = instruction.Action as GenericFunction;

			Store(text, instruction.LlmQuestion.RawResponse);
			
			Assert.AreEqual("RunGoal", gf.FunctionName);
			Assert.AreEqual("goalName", gf.Parameters[0].Name);
			Assert.AreEqual("Process.Image", gf.Parameters[0].Value);
			Assert.AreEqual("parameters", gf.Parameters[1].Name);

			var dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(gf.Parameters[1].Value.ToString());
			Assert.AreEqual("%full_name%", dict["name"]);
			Assert.AreEqual("%address%", dict["address"]);

		}


		[DataTestMethod]
		[DataRow("call !RunReporting, dont wait, delay for 3 sec")]
		public async Task RunGoal2_Test(string text)
		{
			SetupResponse(text);
			var step = new Building.Model.GoalStep();
			step.Text = text;

			var instruction = await builder.Build(step);
			var gf = instruction.Action as GenericFunction;

			Store(text, instruction.LlmQuestion.RawResponse);

			Assert.AreEqual("RunGoal", gf.FunctionName);
			Assert.AreEqual("goalName", gf.Parameters[0].Name);
			Assert.AreEqual("RunReporting", gf.Parameters[0].Value);
		
			Assert.AreEqual("waitForExecution", gf.Parameters[1].Name);
			Assert.AreEqual(false, gf.Parameters[1].Value);
			Assert.AreEqual("delayWhenNotWaitingInMilliseconds", gf.Parameters[2].Name);
			Assert.AreEqual((long) 3*1000, gf.Parameters[2].Value);

		}


	}
}