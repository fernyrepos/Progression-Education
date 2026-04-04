namespace ProgressionEducation
{
    public interface IClassDialog
    {
        public ClassAssignmentsManager AssignmentsManager { get; }
        public TeacherRole TeacherRole { get; }
        public StudentRole StudentRole { get; }
        public ClassCandidatePool CandidatePool { get; }
    }
}