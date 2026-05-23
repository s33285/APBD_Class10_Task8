using Microsoft.EntityFrameworkCore;
using UniversityTasksDbFirstApi.Data;
using UniversityTasksDbFirstApi.DTOs;
using UniversityTasksDbFirstApi.Models;

namespace UniversityTasksDbFirstApi.Services
{
    public class SubmissionService
    {

        private readonly UniversityTasksDbContext _context;

        public SubmissionService(UniversityTasksDbContext context)
        {
            _context = context;
        }

        public async Task<(Submission? submission, string? error, int statusCode)> CreateSubmissionAsync(CreateSubmissionDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RepositoryUrl) || !dto.RepositoryUrl.StartsWith("https://"))
                return (null, "RepositoryUrl must not be blank and must start with https://", 400);

            var student = await _context.Students.FirstOrDefaultAsync(s => s.StudentId == dto.StudentId);
            if (student == null)
                return (null, "Student not found.", 404);
            if (!student.IsActive)
                return (null, "Student is not active.", 400);

            var assignment = await _context.Assignments
                .Include(a => a.Course)
                .FirstOrDefaultAsync(a => a.AssignmentId == dto.AssignmentId);
            if (assignment == null)
                return (null, "Assignment not found.", 404);
            if (!assignment.IsPublished)
                return (null, "Assignment is not published.", 400);

            var enrolled = await _context.Enrollments.AnyAsync(e =>
                e.StudentId == dto.StudentId &&
                e.CourseId == assignment.CourseId &&
                (e.Status == "Active" || e.Status == "Completed"));
            if (!enrolled)
                return (null, "Student is not enrolled in the course for this assignment.", 400);

            var duplicate = await _context.Submissions.AnyAsync(s =>
                s.AssignmentId == dto.AssignmentId && s.StudentId == dto.StudentId);
            if (duplicate)
                return (null, "Student has already submitted this assignment.", 409);

            var now = DateTime.UtcNow;
            var status = assignment.IsOverdue(now) ? "Late" : "Submitted";

            var submission = new Submission
            {
                AssignmentId = dto.AssignmentId,
                StudentId = dto.StudentId,
                RepositoryUrl = dto.RepositoryUrl,
                SubmittedAt = now,
                Status = status
            };

            _context.Submissions.Add(submission);
            await _context.SaveChangesAsync();

            return (submission, null, 201);
        }

        public async Task<(bool success, string? error, int statusCode)> GradeSubmissionAsync(int id, GradeSubmissionDto dto)
        {
            var submission = await _context.Submissions
                .Include(s => s.Assignment)
                .FirstOrDefaultAsync(s => s.SubmissionId == id);

            if (submission == null)
                return (false, "Submission not found.", 404);
            if (dto.Score < 0)
                return (false, "Score cannot be negative.", 400);
            if (dto.Score > submission.Assignment.MaxPoints)
                return (false, $"Score cannot exceed max points ({submission.Assignment.MaxPoints}).", 400);

            submission.Score = dto.Score;
            submission.Feedback = dto.Feedback;
            submission.Status = "Graded";

            await _context.SaveChangesAsync();
            return (true, null, 200);
        }

        public async Task<(bool success, string? error, int statusCode)> DeleteSubmissionAsync(int id)
        {
            var submission = await _context.Submissions.FirstOrDefaultAsync(s => s.SubmissionId == id);
            if (submission == null)
                return (false, "Submission not found.", 404);
            if (submission.Status == "Graded")
                return (false, "Cannot delete a graded submission.", 400);

            _context.Submissions.Remove(submission);
            await _context.SaveChangesAsync();
            return (true, null, 204);
        }
    }
}
