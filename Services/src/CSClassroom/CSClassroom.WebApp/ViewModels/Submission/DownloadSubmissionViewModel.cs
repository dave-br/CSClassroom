﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace CSC.CSClassroom.WebApp.ViewModels.Submission
{
	/// <summary>
	/// The master view model for the download submission page, used for downloading
	/// Eclipse projects and flat file lists from student project repos
	/// </summary>
	public class DownloadSubmissionViewModel
	{
		/// <summary>
		/// TODO: Determines type of form to render
		/// </summary>
		public int IndexForSectionStudentsView {get; set; }

		[Display
		(
			Name = "Components to download",
			Description = "Select which components of the student submissions to include in the download."
		)]
		public DownloadFormat Format { get; set; }

		[Display
		(
			Name = "Include unsubmitted code",
			Description = "Check this to include the latest commit from students who did not turn in their code.  Uncheck this to skip downloading those students' code."
		)]
		public bool IncludeUnsubmitted { get; set; }

		[Display
		(
			Name = "Sections",
			Description = "Select the sections to download."
		)]
		public List<SectionsAndStudents> SectionsAndStudents { get; set; }

		/// <summary>
		///  The section originally active from the submissions view when the user
		///  clicked download.  This informs the default options to be selected
		/// </summary>
		public SectionInfo CurrentSection { get; set; }

		/// <summary>
		/// The submit button to finally initiate the download
		/// </summary>
		public string DownloadSubmitButton { get; set; }
	}

	/// <summary>
	/// The category of submission components to download
	/// </summary>
	public enum DownloadFormat
	{
		[Display(Name = "Flat file list")]
		Flat,

		[Display(Name = "Eclipse projects")]
		Eclipse,

		[Display(Name = "All")]
		All,
	}

	/// <summary>
	/// Info on the "current section", which is what the user had clicked on before
	/// choosing to Download.  This is used to populate the default section option
	/// from the Download page.
	/// </summary>
	public class SectionInfo
	{
		public string Name { get; set; }
		public int Index { get; set; }
	}

    /// <summary>
    ///  Info for a single student to download
    /// </summary>
    public class StudentToDownload
	{
		[Display
		(
			Name = "IsSelected",
			Description = "Download?"
		)]
		public Boolean Selected { get; set; }

		/// <summary>
		/// The unique ID for the user.
		/// </summary>
		//public int Id { get; set; }

		[Display(Name = "Last Name")]
		public string LastName { get; set; }

		[Display(Name = "First Name")]
		public string FirstName { get; set; }

		[Display(Name = "Submitted?")]
		public Boolean Submitted { get; set; }
	}

	/// <summary>
	/// Info on each section to download, including the students within
	/// that section to download.
	/// </summary>
	public class SectionsAndStudents
	{
		private const int c_maxStudentsToDisplayInSelectionLink = 3;

		/// <summary>
		///  Name of section selected for download
		/// </summary>
		public SelectListItem SectionName { get; set; }

        /// <summary>
        ///  Students selected by user to download
        /// </summary>
        [Display
		(
			Name = "Students",
			Description = "Select the students to download."
		)]
		public List<StudentToDownload> SelectedStudents { get; set; }

		/// <summary>
		///  The submit button (rendered in link style) to display the form controls to select a student
		/// </summary>
		public string SectionsAndStudentsSubmitButton { get; set; }

        /// <summary>
        /// Text to use in the link to edit the list of students to download for this section
        /// </summary>
        public string getStudentSummaryDisplay(bool includeUnsubmitted)
        {
			// Decides if downloading a student is allowable on the basis of the existence of their submission
			Func<StudentToDownload, bool> allowable = (student => includeUnsubmitted || student.Submitted);

			// True if all allowable students are selected
			bool downloadAll = !SelectedStudents.Any(student => allowable(student) && !student.Selected);

            if (downloadAll)
            {
                return "Students: All" + (includeUnsubmitted ? "" : "(except unsubmitted)");
            }

            int numStudents = SelectedStudents.Count(student => allowable(student) && student.Selected);
            if (numStudents == 0)
            {
                // TODO: This should be caught earlier and should cause the checkbox to be unchecked
                // and this link to be disabled / hidden
                return "Students: None";
            }

            string ret = "Students: ";
            int numStudentsAppended = 0;
            foreach (StudentToDownload student in SelectedStudents)
            {
                if (!student.Selected || !allowable(student))
                {
                    continue;
                }

                if (numStudentsAppended > 0)
                {
                    ret += "; ";
                }

                ret += getStudentName(student);
                numStudentsAppended++;
                if (numStudentsAppended == c_maxStudentsToDisplayInSelectionLink)
                {
                    break;
                }
            }

            int remaining = numStudentsAppended - c_maxStudentsToDisplayInSelectionLink;
            if (remaining > 0)
            {
                ret += "; and " + remaining + " more";
            }

            return ret;
        }

        private string getStudentName(StudentToDownload student)
		{
			return student.LastName + ", " + student.FirstName;
		}

	}
}
