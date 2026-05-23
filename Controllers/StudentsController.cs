using Microsoft.AspNetCore.Mvc;
using UniversityTasksDbFirstApi.Data;
using UniversityTasksDbFirstApi.DTOs;
using Microsoft.EntityFrameworkCore;

namespace UniversityTasksDbFirstApi.Controllers
{
    [ApiController]
    [Route("api/students")]
    public class StudentsController : ControllerBase
    {
        private readonly UniversityTasksDbContext _context;

        public StudentsController(UniversityTasksDbContext context)
        {
            _context = context;
        }

        [HttpGet("{idStudent}/dashboard")]
        public async Task<IActionResult> GetDashboard(int idStudent)
        {
            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.Enrollments).ThenInclude(e => e.Course)
                .Include(s => s.Submissions).ThenInclude(sub => sub.Assignment)
                .FirstOrDefaultAsync(s => s.StudentId == idStudent);

            if (student == null)
                return NotFound($"Student {idStudent} not found");

            var dto = new StudentDashboardDto
            {
                StudentId = student.StudentId,
                IndexNumber = student.IndexNumber,
                FullName = student.FullName,
                IsActive = student.IsActive,
                Enrollments = student.Enrollments.Select(e => new EnrollmentDto
                {
                    EnrollmentId = e.EnrollmentId,
                    CourseName = e.Course.Name,
                    CourseCode = e.Course.Code,
                    Status = e.Status,
                    EnrolledAt = e.EnrolledAt

                }).ToList(),
                Submissions = student.Submissions.Select(sub => new SubmissionDto
                {
                    SubmissionId = sub.SubmissionId,
                    StudentFullName = student.FullName,
                    AssignmentTitle = sub.Assignment.Title,
                    RepositoryUrl = sub.RepositoryUrl,
                    Status = sub.Status,
                    Score = sub.Score,
                    Feedback = sub.Feedback,
                    SubmittedAt = sub.SubmittedAt

                }).ToList()
            };

            return Ok(dto);
        }
    }
}
