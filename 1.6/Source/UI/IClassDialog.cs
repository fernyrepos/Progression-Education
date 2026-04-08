namespace ProgressionEducation;

public interface IClassDialog
{
    public ClassAssignmentsManager AssignmentsManager { get; }
    public ClassCandidatePool CandidatePool { get; }
    public StudentRole StudentRole { get; }
    public TeacherRole TeacherRole { get; }
}