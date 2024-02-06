﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using PLang.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLang.Utils.Tests
{
	[TestClass()]
	public class GoalHelperTests
	{
		[TestMethod()]
		public void IsSetupTest()
		{
			Assert.IsFalse(GoalHelper.IsSetup("/", "/Start"));
			Assert.IsFalse(GoalHelper.IsSetup("/", "/File.goal"));

			Assert.IsTrue(GoalHelper.IsSetup("/", "/Setup.goal"));
			Assert.IsTrue(GoalHelper.IsSetup("/", "/SETUP"));
			Assert.IsTrue(GoalHelper.IsSetup("/", "/setup"));
		}

		[TestMethod()]
		public void GetAppNameTest()
		{
			string myApp = GoalHelper.GetAppName("apps/MyApp");
			Assert.AreEqual("MyApp", myApp);

			string myApp2 = GoalHelper.GetAppName("/apps/MyApp2");
			Assert.AreEqual("MyApp2", myApp2);

			string myApp3 = GoalHelper.GetAppName("/apps/MyApp3/Start");
			Assert.AreEqual("MyApp3", myApp3);

			string myApp4 = GoalHelper.GetAppName("/APPS/MyApp4/DoStuff");
			Assert.AreEqual("MyApp4", myApp4);
		}

		[TestMethod()]
		public void GetGoalNameTest()
		{
			var goalName = GoalHelper.GetGoalPath("apps/MyApp/");
			Assert.AreEqual("Start", goalName);

			var goalName1 = GoalHelper.GetGoalPath("apps/MyApp");
			Assert.AreEqual("Start", goalName1);

			var goalName2 = GoalHelper.GetGoalPath("apps/MyApp/DoStuff");
			Assert.AreEqual("DoStuff", goalName2);

			var goalName3 = GoalHelper.GetGoalPath("apps/MyApp/DoStuff/EvenMoreStuff");
			Assert.AreEqual("DoStuff" + Path.DirectorySeparatorChar + "EvenMoreStuff", goalName3);

			var goalName4 = GoalHelper.GetGoalPath("apps/MyApp/DoStuff/");
			Assert.AreEqual("DoStuff", goalName4);
		}
	}
}