
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace hrmsAPI.MODEL.Payroll
{
    // 1. POST - Add Pay Component
    public class PostPayComponentModel
    {
        [Required]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Invalid Component Type ID.")]
        public long ComponentTypeId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Invalid Calculation Type ID.")]
        public long CalcTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Calculation priority must be greater than zero.")]
        public int CalcPriority { get; set; }

        public bool IsProrated { get; set; }

        public bool IsTaxable { get; set; }
    }

    // 2. GET - Pay Component Details
    public class GetPayComponentModel
    {
        public long ComponentId { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public long ComponentTypeId { get; set; }

        // Name fetched through JOIN with component type master
        public string ComponentTypeName { get; set; }

        public long CalcTypeId { get; set; }

        // Name fetched through JOIN with calculation type master
        public string CalcTypeName { get; set; }

        public int CalcPriority { get; set; }

        public bool IsProrated { get; set; }

        public bool IsTaxable { get; set; }

        public bool IsActive { get; set; }
    }

    // 3. PUT - Update Pay Component
    public class UpdatePayComponentModel
    {
        [Required]
        public string Name { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Invalid Component Type ID.")]
        public long ComponentTypeId { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = "Invalid Calculation Type ID.")]
        public long CalcTypeId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Calculation priority must be greater than zero.")]
        public int CalcPriority { get; set; }

        public bool IsProrated { get; set; }

        public bool IsTaxable { get; set; }
    }

    // 4. GET - Search and Filter
    public class PayComponentFilterModel
    {
        public string Search { get; set; }

        public long? ComponentTypeId { get; set; }

        public long? CalcTypeId { get; set; }

        public bool? IsActive { get; set; }

        public int PageNumber { get; set; } = 1;

        public int PageSize { get; set; } = 10;
    }

    // 5. GET - List Response
    public class PayComponentListResponse
    {
        public int TotalRecords { get; set; }

        public int PageNumber { get; set; }

        public int PageSize { get; set; }

        public List<GetPayComponentModel> Data { get; set; }
    }

    // 6. GET - Dashboard Summary
    public class PayComponentSummaryModel
    {
        public int TotalComponents { get; set; }

        public int ActiveComponents { get; set; }

        public int InactiveComponents { get; set; }

        public int Earnings { get; set; }

        public int Deductions { get; set; }

        public int EmployerContributions { get; set; }
    }

    // 7. Common API Response
    public class PayComponentResponseModel
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public long ComponentId { get; set; }
    }

    // 8. Component Type Dropdown Model
    public class ComponentTypeLookupModel
    {
        public long ComponentTypeId { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }

    // 9. Calculation Type Dropdown Model
    public class CalculationTypeLookupModel
    {
        public long CalcTypeId { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }

    // 10. Combined Dropdown Response Model
    public class PayComponentLookupResponseModel
    {
        public List<ComponentTypeLookupModel> ComponentTypes { get; set; }

        public List<CalculationTypeLookupModel> CalculationTypes { get; set; }
    }
}
