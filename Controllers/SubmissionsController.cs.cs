using Microsoft.AspNetCore.Mvc;
using UniversityTasksDbFirstApi.Data;
using UniversityTasksDbFirstApi.DTOs;
using UniversityTasksDbFirstApi.Services;
using Microsoft.EntityFrameworkCore;

namespace UniversityTasksDbFirstApi.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    public class SubmissionsController : ControllerBase
    {
        private readonly SubmissionService _service;
        private readonly UniversityTasksDbContext _context;

        public SubmissionsController(SubmissionService service, UniversityTasksDbContext context)
        {
            _service = service;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSubmission([FromBody] CreateSubmissionDto dto)
        {
            var (submission, error, statusCode) = await _service.CreateSubmissionAsync(dto);

            if (error != null)
            {
                return statusCode switch
                {
                    404 => NotFound(error),
                    409 => Conflict(error),
                    _ => BadRequest(error)
                };
            }

            var created = await _context.Submissions
                .AsNoTracking()
                .Include(s => s.Student)
                .Include(s => s.Assignment)
                .FirstAsync(s => s.SubmissionId == submission!.SubmissionId);

            var result = new SubmissionDto
            {
                SubmissionId = created.SubmissionId,
                StudentFullName = created.Student.FullName,
                AssignmentTitle = created.Assignment.Title,
                RepositoryUrl = created.RepositoryUrl,
                Status = created.Status,
                Score = created.Score,
                Feedback = created.Feedback,
                SubmittedAt = created.SubmittedAt
            };

            return CreatedAtAction(nameof(CreateSubmission), new { id = result.SubmissionId }, result);
        }

        [HttpPut("{idSubmission}/grade")]
        public async Task<IActionResult> GradeSubmission(int idSubmission, [FromBody] GradeSubmissionDto dto)
        {
            var (success, error, statusCode) = await _service.GradeSubmissionAsync(idSubmission, dto);

            if (!success)
            {
                return statusCode switch
                {
                    404 => NotFound(error),
                    _ => BadRequest(error)
                };
            }

            return Ok("Submission graded successfully.");
        }

        [HttpDelete("{idSubmission}")]
        public async Task<IActionResult> DeleteSubmission(int idSubmission)
        {
            var (success, error, statusCode) = await _service.DeleteSubmissionAsync(idSubmission);

            if (!success)
            {
                return statusCode switch
                {
                    404 => NotFound(error),
                    _ => BadRequest(error)
                };
            }

            return NoContent();
        }
    }
}
