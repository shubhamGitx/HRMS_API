
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using HRMSAPI.BAL.Payroll;
using hrmsAPI.MODEL.Payroll;

namespace hrmsAPI.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    public class PayComponentsController : ControllerBase
    {
        private readonly PayComponentsBAL _bal;

        public PayComponentsController(PayComponentsBAL bal)
        {
            _bal = bal;
        }

        // =====================================================
        // 1. GET ALL PAY COMPONENTS
        // GET: api/PayComponents
        // Supports Search, ID Filters and Pagination
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetPayComponents(
            [FromQuery] string search = null,
            [FromQuery] long? componentTypeId = null,
            [FromQuery] long? calcTypeId = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            try
            {
                PayComponentFilterModel model =
                    new PayComponentFilterModel();

                model.Search = search;
                model.ComponentTypeId = componentTypeId;
                model.CalcTypeId = calcTypeId;
                model.IsActive = isActive;
                model.PageNumber = pageNumber;
                model.PageSize = pageSize;

                PayComponentListResponse result =
                    await _bal.GetPayComponents(model);

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching pay components."
                });
            }
        }

        // =====================================================
        // 2. GET COMPONENT TYPE AND CALCULATION TYPE DROPDOWNS
        // GET: api/PayComponents/lookups
        // =====================================================
        [HttpGet("masterdropdown")]
        public async Task<IActionResult> GetPayComponentLookups()
        {
            try
            {
                PayComponentLookupResponseModel result =
                    await _bal.GetPayComponentLookups();

                return Ok(new
                {
                    success = true,
                    message = "Dropdown data fetched successfully.",
                    data = result
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching dropdown data."
                });
            }
        }

        // =====================================================
        // 3. GET PAY COMPONENT BY ID
        // GET: api/PayComponents/1
        // =====================================================
        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetPayComponentById(long id)
        {
            try
            {
                GetPayComponentModel result =
                    await _bal.GetPayComponentById(id);

                if (result == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Pay component not found."
                    });
                }

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching pay component."
                });
            }
        }

        // =====================================================
        // 4. GET DASHBOARD SUMMARY
        // GET: api/PayComponents/summary
        // =====================================================
        [HttpGet("summary")]
        public async Task<IActionResult> GetPayComponentSummary()
        {
            try
            {
                PayComponentSummaryModel result =
                    await _bal.GetPayComponentSummary();

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching pay component summary."
                });
            }
        }

        // =====================================================
        // 5. ADD NEW PAY COMPONENT
        // POST: api/PayComponents
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> InsertPayComponent(
            [FromBody] PostPayComponentModel model)
        {
            try
            {
                PayComponentResponseModel result =
                    await _bal.InsertPayComponent(model);

                if (!result.Success)
                    return BadRequest(result);

                return Ok(result);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23505")
            {
                return Conflict(new
                {
                    success = false,
                    message = "Component code already exists."
                });
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23503")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid component type ID or calculation type ID."
                });
            }
            catch (PostgresException ex)
                when (ex.SqlState == "22023")
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.MessageText
                });
            }
            catch (PostgresException ex) when (ex.ConstraintName == "ck_basic_matrix_definition")
            {
                return BadRequest(new { success = false, message = "Only BASIC can be matrix-linked. BASIC must be a fixed earning with no default amount." });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error adding pay component."
                });
            }
        }

        // =====================================================
        // 6. UPDATE PAY COMPONENT
        // PUT: api/PayComponents/1
        // =====================================================
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdatePayComponent(
            long id,
            [FromBody] UpdatePayComponentModel model)
        {
            try
            {
                PayComponentResponseModel result =
                    await _bal.UpdatePayComponent(id, model);

                if (!result.Success)
                {
                    if (result.Message == "Pay component not found.")
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (PostgresException ex)
                when (ex.SqlState == "23503")
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid component type ID or calculation type ID."
                });
            }
            catch (PostgresException ex)
                when (ex.SqlState == "22023")
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.MessageText
                });
            }
            catch (PostgresException ex) when (ex.ConstraintName == "ck_basic_matrix_definition")
            {
                return BadRequest(new { success = false, message = "Only BASIC can be matrix-linked. BASIC must be a fixed earning with no default amount." });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating pay component."
                });
            }
        }

        // =====================================================
        // 7. DEACTIVATE PAY COMPONENT
        // PATCH: api/PayComponents/1/deactivate
        // =====================================================
        [HttpPatch("{id:long}/deactivate")]
        public async Task<IActionResult> DeactivatePayComponent(long id)
        {
            try
            {
                PayComponentResponseModel result =
                    await _bal.ChangePayComponentStatus(id, false);

                if (!result.Success)
                {
                    if (result.Message == "Pay component not found.")
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error deactivating pay component."
                });
            }
        }

        // =====================================================
        // 8. ACTIVATE PAY COMPONENT
        // PATCH: api/PayComponents/1/activate
        // =====================================================
        [HttpPatch("{id:long}/activate")]
        public async Task<IActionResult> ActivatePayComponent(long id)
        {
            try
            {
                PayComponentResponseModel result =
                    await _bal.ChangePayComponentStatus(id, true);

                if (!result.Success)
                {
                    if (result.Message == "Pay component not found.")
                        return NotFound(result);

                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (PostgresException ex) when (ex.ConstraintName == "ck_basic_matrix_definition")
            {
                return BadRequest(new { success = false, message = "Only BASIC can be matrix-linked. BASIC must be a fixed earning with no default amount." });
            }
            catch (Exception)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error activating pay component."
                });
            }
        }
    }
}
