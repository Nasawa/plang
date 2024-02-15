﻿using CsvHelper.Configuration;
using Microsoft.Data.Sqlite;
using PLang.Building.Model;
using PLang.Interfaces;
using PLang.Models;
using PLang.Utils;
using System.Data;
using System.Drawing.Text;
using System.Reflection;
using static PLang.Runtime.Startup.ModuleLoader;

namespace PLang.Exceptions.AskUser
{
	public abstract class AskUserException : Exception
	{
		protected Func<object[], Task> Callback { get; set; }
		public abstract Task InvokeCallback(object value);
		public AskUserException(string question) : base(question)
		{
			this.Callback = (obj) => { return Task.CompletedTask; };
		}
		public AskUserException(string question, Func<object[], Task> callback) : base(question)
		{
			this.Callback = callback;
		}

		protected static Func<object[], Task> CreateAdapter(Delegate? callback)
		{
			if (callback == null) { return (obj) => { return Task.CompletedTask; }; }
			return async args =>
			{
				var result = callback.DynamicInvoke(args) as Task;
				if (result != null)
				{
					await result;
				}
			};
		}

	}

	public class AskUserWebserver : AskUserException
	{
		public int StatusCode { get; private set; }
		public AskUserWebserver(string question, int statusCode = 500, Func<object?, Task>? callback = null) : base(question, callback)
		{
			StatusCode = statusCode;
		}

		public override async Task InvokeCallback(object value)
		{
			return;
		}
	}

	public class AskUserConsole : AskUserException
	{
		public AskUserConsole(string question, Func<object?, Task>? callback = null) : base(question, callback)
		{
		}
		public override async Task InvokeCallback(object value)
		{
			await Callback.Invoke([value]);			
		}
	}

	public class AskUserDbConnectionString : AskUserException
	{
		private readonly string typeFullName;
		private readonly bool setAsDefaultForApp;
		private readonly bool keepHistoryEventSourcing;
		private readonly string dataSourceName;
		string regexToExtractDatabaseNameFromConnectionString;
		private readonly bool keepHistory;
		private readonly bool isDefault;

		public AskUserDbConnectionString(string dataSourceName, string typeFullName,
			string regexToExtractDatabaseNameFromConnectionString, bool keepHistory, bool isDefault, string question, 
				Func<string, string, string, string, bool, bool, Task> callback) : base(question, CreateAdapter(callback))
		{
			this.dataSourceName = dataSourceName;
			this.regexToExtractDatabaseNameFromConnectionString = regexToExtractDatabaseNameFromConnectionString;
			this.keepHistory = keepHistory;
			this.isDefault = isDefault;
			this.typeFullName = typeFullName;

		}

		public override async Task InvokeCallback(object answer)
		{
			if (Callback == null) return;

			await Callback.Invoke([dataSourceName, typeFullName, regexToExtractDatabaseNameFromConnectionString, answer, keepHistory, isDefault]);

		}
	}

	public class AskUserDatabaseType : AskUserException
	{
		private readonly ILlmService aiService;
		private readonly bool setAsDefaultForApp;
		private readonly bool keepHistoryEventSourcing;
		private readonly string supportedDbTypes;
		private readonly string dataSourceName;

		public AskUserDatabaseType(ILlmService aiService, bool setAsDefaultForApp, bool keepHistoryEventSourcing, 
				string supportedDbTypes, string dataSourceName, string question, 
				Func<string, string, string, string, string, bool, bool, Task> callback) : base(question, CreateAdapter(callback))
		{
			this.aiService = aiService;
			this.setAsDefaultForApp = setAsDefaultForApp;
			this.keepHistoryEventSourcing = keepHistoryEventSourcing;
			this.supportedDbTypes = supportedDbTypes;
			this.dataSourceName = dataSourceName;
		}

		private record DatabaseTypeResponse(string typeFullName, string nugetCommand,
				string regexToExtractDatabaseNameFromConnectionString, string dataSourceConnectionStringExample);

		public override async Task InvokeCallback(object answer)
		{
			var system = @$"Map user request

If user provides a full data source connection, return {{error:explainWhyConnectionStringShouldNotBeInCodeMax100Characters}}.

typeFullName: from database types provided, is the type.FullName for IDbConnection in c# for this database type for .net 7
dataSourceName: give name to the datasource if not defined by user 
nugetCommand: nuget package name, for running ""nuget install ...""
dataSourceConnectionStringExample: create an example of a connection string for this type of databaseType
regexToExtractDatabaseNameFromConnectionString: generate regex to extract the database name from a connection string from user selected databaseType
";
			string assistant = $"## database types ##\r\n{supportedDbTypes}\r\n## database types ##";

			var promptMessage = new List<LlmMessage>();
			promptMessage.Add(new LlmMessage("system", system));
			promptMessage.Add(new LlmMessage("assistant", assistant));
			promptMessage.Add(new LlmMessage("user", answer.ToString()));

			var llmRequest = new LlmRequest("AskUserDatabaseType", promptMessage);
			llmRequest.scheme = TypeHelper.GetJsonSchema(typeof(DatabaseTypeResponse));

			var result = await aiService.Query<DatabaseTypeResponse>(llmRequest);

			if (result == null) throw new Exception("Could not use LLM to format your answer");
			if (Callback == null) return;

			await Callback.Invoke([
				 result.typeFullName,
				this.dataSourceName,
				result.nugetCommand,
				result.dataSourceConnectionStringExample,
				result.regexToExtractDatabaseNameFromConnectionString,
				keepHistoryEventSourcing,
				setAsDefaultForApp]);
			
		}
	}
	public class AskUserSqliteName : AskUserException
	{
		private readonly string rootPath;

		public AskUserSqliteName(string rootPath, string question, Func<string, string, string, string, bool, bool, Task> callback) : base(question, CreateAdapter(callback))
		{
			this.rootPath = rootPath;
		}

		public override async Task InvokeCallback(object answer)
		{
			if (Callback == null) return;

			var dbName = answer.ToString()!.Replace(" ", "_").Replace(".sqlite", "");
			string dbPath = "." + Path.DirectorySeparatorChar + ".db" + Path.DirectorySeparatorChar + dbName + ".sqlite";
			string dbAbsolutePath = Path.Join(rootPath, dbPath);

			await Callback.Invoke([
				dbName.ToString(), typeof(SqliteConnection).FullName!, dbName.ToString() + ".sqlite", $"Data Source={dbAbsolutePath};Version=3;", true, false]);

		}
	}
	public class AskUserDataSourceNameExists : AskUserException
	{
		private readonly ILlmService aiService;
		private readonly string typeFullName;
		private readonly string dataSourceName;
		private readonly string nugetCommand;
		private readonly string dataSourceConnectionStringExample;
		private readonly string regexToExtractDatabaseNameFromConnectionString;
		private readonly bool keepHistoryEventSourcing;
		private readonly bool isDefault;

		public AskUserDataSourceNameExists(ILlmService aiService, string typeFullName, string dataSourceName, string nugetCommand,
			string dataSourceConnectionStringExample, string regexToExtractDatabaseNameFromConnectionString,
			bool keepHistoryEventSourcing, bool isDefault, string message, Func<string, string, string, string, string, bool, bool, Task> callback) : base(message, CreateAdapter(callback))
		{
			this.aiService = aiService;
			this.typeFullName = typeFullName;
			this.dataSourceName = dataSourceName;
			this.nugetCommand = nugetCommand;
			this.dataSourceConnectionStringExample = dataSourceConnectionStringExample;
			this.regexToExtractDatabaseNameFromConnectionString = regexToExtractDatabaseNameFromConnectionString;
			this.keepHistoryEventSourcing = keepHistoryEventSourcing;
			this.isDefault = isDefault;
		}

		private record MethodResponse(string typeFullName, string dataSourceName, string dataSourceConnectionStringExample, string nugetCommand, string regexToExtractDatabaseNameFromConnectionString, bool keepHistoryEventSourcing, bool isDefault = false);
		public override async Task InvokeCallback(object answer)
		{
			if (Callback == null) return;

			string assistant = @$"These are previously defined properties by the user, use them if not otherwise defined by user.
## previously defined ##
typeFullName: {typeFullName}
dataSourceName: {dataSourceName}
nugetCommand: {nugetCommand}
dataSourceConnectionStringExample: {dataSourceConnectionStringExample}
regexToExtractDatabaseNameFromConnectionString: {regexToExtractDatabaseNameFromConnectionString}
isDefault: {isDefault}
keepHistoryEventSourcing: {keepHistoryEventSourcing}
## previously defined ##
";

			var promptMessage = new List<LlmMessage>();
			promptMessage.Add(new LlmMessage("system", "Map user request"));
			promptMessage.Add(new LlmMessage("assistant", assistant));
			promptMessage.Add(new LlmMessage("user", answer.ToString()!));


			var llmRequest = new LlmRequest("AskUserDatabaseType", promptMessage);
			var result = await aiService.Query<MethodResponse>(llmRequest);
			if (result == null) return;

			await Callback.Invoke(new object[] {
				 result.typeFullName, result.dataSourceName, result.nugetCommand, result.dataSourceConnectionStringExample,
				result.regexToExtractDatabaseNameFromConnectionString, result.keepHistoryEventSourcing, result.isDefault});

		}
	}
}
