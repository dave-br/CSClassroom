﻿using CSC.CSClassroom.Model.Classrooms;
using CSC.CSClassroom.Model.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSC.CSClassroom.Model.Projects.ServiceResults
{
    /// <summary>
    /// Information about one student's project checkpoint, offered to the SubmissionController
    /// as a candidate for downloading
    /// </summary>
    public class CheckpointDownloadCandidateResult
    {
        public Section Section { get; }

        public IList<UserDownloadCandidateResult> Users { get; }

        public CheckpointDownloadCandidateResult(Section section, IList<UserDownloadCandidateResult> users)
        {
            Section = section;
            Users = users;
        }
    }

    public class UserDownloadCandidateResult
    {
        // TODO: If I don't end up using user id anywhere, maybe just change this to firstname, lastname
        public User User { get; }

        /// <summary>
        /// Did the student actually turn in a commit for this checkpoint?
        /// </summary>
        public bool Submitted { get;  }

        public UserDownloadCandidateResult(User user, bool submitted)
        {
            User = user;
            Submitted = submitted;
        }
    }
}
