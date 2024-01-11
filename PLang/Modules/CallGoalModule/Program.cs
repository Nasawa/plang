﻿using Org.BouncyCastle.Asn1;
using PLang.Building.Model;
using PLang.Building.Parsers;
using PLang.Exceptions;
using PLang.Interfaces;
using PLang.Runtime;
using PLang.Utils;
using System.ComponentModel;
using System.IO.Compression;
using System.Net;

namespace PLang.Modules.CallGoalModule
{
	[Description("Call another Goal, when ! is prefixed, e.g. !RenameFile")]
	public class Program : BaseProgram
	{
		private readonly IPseudoRuntime pseudoRuntime;
		private readonly IEngine engine;
		private readonly VariableHelper variableHelper;
		private readonly IPLangFileSystem fileSystem;
		private readonly PrParser prParser;

		public Program(IPseudoRuntime pseudoRuntime, IEngine engine, VariableHelper variableHelper, IPLangFileSystem fileSystem, PrParser prParser) : base()
		{
			this.pseudoRuntime = pseudoRuntime;
			this.engine = engine;
			this.variableHelper = variableHelper;
			this.fileSystem = fileSystem;
			this.prParser = prParser;
		}

		public new Goal Goal { get; set; }

		public async Task RunGoal(string goalName, Dictionary<string, object>? parameters = null, bool waitForExecution = true, int delayWhenNotWaitingInMilliseconds = 0)
		{
			if (goalName == null)
			{
				throw new Exception($"Could not find goal to call from step: {goalStep.Text}");
			}
			if (Goal == null) Goal = base.Goal;
			if (goalName.Contains("."))
			{
				ValidateAppInstall(goalName);

			}
			if (waitForExecution)
			{
				await pseudoRuntime.RunGoal(engine, context, Goal.RelativeAppStartupFolderPath, goalName, variableHelper.LoadVariables(parameters), Goal);
			}
			else
			{
				var newContext = new PLangAppContext();
				foreach (var item in context)
				{
					newContext.Add(item.Key, item.Value);
				}

				pseudoRuntime.RunGoal(engine, newContext, Goal.RelativeAppStartupFolderPath, goalName, variableHelper.LoadVariables(parameters), Goal);
				if (delayWhenNotWaitingInMilliseconds > 0)
				{
					await Task.Delay(delayWhenNotWaitingInMilliseconds);
				}
			}
		}

		private void ValidateAppInstall(string goalName)
		{
			string appName = goalName.Substring(0, goalName.IndexOf("."));
			if (!fileSystem.Directory.Exists(Path.Join("apps", appName)))
			{
				string zipPath = Path.Join(fileSystem.RootDirectory, "apps", appName, appName + ".zip");
				using (var client = new HttpClient())
				{
					try
					{
						client.DefaultRequestHeaders.UserAgent.ParseAdd("plang v0.1");
						using (var s = client.GetStreamAsync($"https://raw.githubusercontent.com/PLangHQ/apps/main/{appName}/{appName}.zip"))
						{
							fileSystem.Directory.CreateDirectory(Path.Join("apps", appName));
							using (var fs = new FileStream(zipPath, FileMode.OpenOrCreate))
							{
								s.Result.CopyTo(fs);
							}
						}

						if (fileSystem.File.Exists(zipPath))
						{
							ZipFile.ExtractToDirectory(zipPath, Path.Join(fileSystem.RootDirectory, "apps", appName), true);
							prParser.ForceLoadAllGoals();
						}
						
					}
					catch (Exception ex)
					{
						throw new RuntimeException($"Could not find app {appName} at https://github.com/PLangHQ/apps/. You must put {appName} folder into the apps folder before calling it.");
					}
				}


			}
		}
	}


}

