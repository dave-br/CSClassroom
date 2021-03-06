﻿using System;
using System.Collections.Generic;
using System.Text;
using CSC.CSClassroom.Model.Classrooms;
using CSC.CSClassroom.Model.Assignments;
using CSC.CSClassroom.Model.Assignments.ServiceResults;
using CSC.CSClassroom.Model.Users;

namespace CSC.CSClassroom.Service.Assignments.AssignmentScoring
{
	/// <summary>
	/// Generates results for a single user/assignment group.
	/// </summary>
	public interface IAssignmentGroupResultGenerator
	{
		/// <summary>
		/// Returns an assignment group result, for a given user and assignment group.
		/// </summary>
		AssignmentGroupResult GetAssignmentGroupResult(
			string assignmentGroupName,
			IList<Assignment> assignments,
			Section section,
			User user,
			IList<UserQuestionSubmission> submissions,
			bool admin);
	}
}
