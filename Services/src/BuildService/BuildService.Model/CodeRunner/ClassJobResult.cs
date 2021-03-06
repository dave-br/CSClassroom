﻿namespace CSC.BuildService.Model.CodeRunner
{
	/// <summary>
	/// The result of a class job.
	/// </summary>
	public class ClassJobResult : CodeJobResult
	{
		/// <summary>
		/// The defintion of the compiled class, if the class compiled successfully.
		/// </summary>
		public ClassDefinition ClassDefinition { get; set; }
	}
}
