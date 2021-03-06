﻿using System.Threading.Tasks;
using CSC.CSClassroom.Model.Assignments;
using CSC.CSClassroom.Repository;

namespace CSC.CSClassroom.Service.Assignments.QuestionLoaders
{
	/// <summary>
	/// Loads a program question.
	/// </summary>
	public class ProgramQuestionLoader : CodeQuestionLoader<ProgramQuestion>
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public ProgramQuestionLoader(DatabaseContext dbContext, ProgramQuestion question) 
			: base(dbContext, question)
		{
		}

		/// <summary>
		/// Loads the program question.
		/// </summary>
		protected override async Task LoadCodeQuestionImplAsync()
		{
			await LoadCollectionAsync(q => q.Tests);
		}
	}
}
