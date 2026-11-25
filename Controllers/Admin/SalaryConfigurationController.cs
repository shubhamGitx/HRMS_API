
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HRMSAPI.BAL.Payroll;
using hrmsAPI.MODEL.Payroll;

namespace hrmsAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class SalaryConfigurationController : ControllerBase
    {
        private readonly SalaryConfigurationBAL _bal;

        public SalaryConfigurationController(
            SalaryConfigurationBAL bal)
        {
            _bal = bal;
        }

        // 1. Employee List with Search, Filters and Pagination
        // GET: api/SalaryConfiguration
        [HttpGet]
        public async Task<IActionResult> GetSalaryConfigurations(
            [FromQuery] SalaryConfigurationFilterModel model)
        {
            try
            {
                var result = await _bal.GetSalaryConfigurations(model);

                return Ok(new
                {
                    Success = true,
                    Message = "Employee salary configuration list fetched successfully.",
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while fetching employee records."
                });
            }
        }

        // 2. Dashboard Summary
        // GET: api/SalaryConfiguration/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSalaryConfigurationSummary()
        {
            try
            {
                var result =
                    await _bal.GetSalaryConfigurationSummary();

                return Ok(new
                {
                    Success = true,
                    Message = "Salary configuration summary fetched successfully.",
                    Data = result
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while fetching salary summary."
                });
            }
        }

        // 3. Dropdown Lookups
        // GET: api/SalaryConfiguration/lookups
        [HttpGet("lookups")]
        public async Task<IActionResult> GetSalaryConfigurationLookups()
        {
            try
            {
                var result =
                    await _bal.GetSalaryConfigurationLookups();

                return Ok(new
                {
                    Success = true,
                    Message = "Salary configuration dropdowns fetched successfully.",
                    Data = result
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while fetching dropdown data."
                });
            }
        }

        // 4. Employee Salary Configuration Details
        // GET: api/SalaryConfiguration/{id}
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetSalaryConfigurationById(long id)
        {
            try
            {
                var result =
                    await _bal.GetSalaryConfigurationById(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        Success = false,
                        Message = "Employee salary configuration record not found."
                    });
                }

                return Ok(new
                {
                    Success = true,
                    Message = "Employee salary configuration details fetched successfully.",
                    Data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message = "An error occurred while fetching employee details."
                });
            }
        }

        // 5. Update Existing Draft Salary Configuration
        // PUT: api/SalaryConfiguration/salary-headers/{salaryHeaderId}
        [HttpPost("salary-headers")]
        public async Task<IActionResult> SaveSalaryHeaderDraft(
    [FromBody] PostSalaryHeaderDraftModel model)
        {
            try
            {
                // Temporary testing user ID
                long createdBy = 1;

                var result = await _bal.SaveSalaryHeaderDraft(
                    model,
                    createdBy);

                return Ok(new
                {
                    Success = true,
                    Message =
                        "Salary configuration saved as draft successfully.",
                    Data = new
                    {
                        SalaryHeaderId = result,
                        Status = "DRAFT"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message =
                        "An error occurred while saving salary configuration.",
                    Error = ex.Message
                });
            }
        }
        [HttpPut("salary-headers/{salaryHeaderId:long}")]
        public async Task<IActionResult> UpdateSalaryHeaderDraft(
    long salaryHeaderId,
    [FromBody] UpdateSalaryHeaderDraftModel model)
        {
            try
            {
                // Temporary testing user ID
                long updatedBy = 1;

                var result = await _bal.UpdateSalaryHeaderDraft(
                    salaryHeaderId,
                    model,
                    updatedBy);

                return Ok(new
                {
                    Success = true,
                    Message =
                        "Salary configuration draft updated successfully.",
                    Data = new
                    {
                        SalaryHeaderId = result,
                        Status = "DRAFT"
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message =
                        "An error occurred while updating salary configuration.",
                    Error = ex.Message
                });
            }
        }
        [HttpPut("salary-headers/{salaryHeaderId:long}/lock")]
        public async Task<IActionResult> LockSalaryHeader(
    long salaryHeaderId)
        {
            try
            {
                // Temporary testing user ID
                long updatedBy = 1;

                var result = await _bal.LockSalaryHeader(
                    salaryHeaderId,
                    updatedBy);

                return Ok(new
                {
                    Success = true,
                    Message =
                        "Salary configuration confirmed and locked successfully.",
                    Data = new
                    {
                        SalaryHeaderId = result,
                        Status = "LOCKED",
                        IsPayrollEligible = true
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Success = false,
                    Message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Success = false,
                    Message =
                        "An error occurred while locking salary configuration.",
                    Error = ex.Message
                });
            }
        }

    }
}
