
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace hrmsAPI.MODEL.Payroll
{
    // Employee listing with salary configuration details
    public class SalaryConfigurationListModel
    {
        public long EmployeeId { get; set; }
        public long? SalaryConfigurationId { get; set; }

        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }

        public long? OfficeId { get; set; }
        public string OfficeName { get; set; }

        public long? DesignationId { get; set; }
        public string DesignationName { get; set; }

        public long? PayMatrixId { get; set; }
        public string PayLevel { get; set; }
        public string PayCell { get; set; }
        public decimal? BasicPay { get; set; }

        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }

        public long? SalaryStatusId { get; set; }
        public string SalaryStatus { get; set; }
    }

    // Search, filters and pagination


    public class SalaryConfigurationFilterModel
    {
        public string? Search { get; set; }

        public long? DesignationId { get; set; }

        public long? SalaryStatusId { get; set; }

        [DefaultValue(1)]
        public int PageNumber { get; set; } = 1;

        [DefaultValue(10)]
        public int PageSize { get; set; } = 10;
    }

    // Paginated employee list response
    public class SalaryConfigurationListResponse
    {
        public int TotalRecords { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }

        public List<SalaryConfigurationListModel> Data { get; set; }
    }

    // Dashboard summary cards
    public class SalaryConfigurationSummaryModel
    {
        public int TotalEmployees { get; set; }
        public int PendingDraft { get; set; }
        public int Locked { get; set; }
    }

    // Designation dropdown
    public class SalaryDesignationLookupModel
    {
        public long DesignationId { get; set; }
        public string DesignationName { get; set; }
    }

    // Salary status dropdown
    public class SalaryStatusLookupModel
    {
        public long SalaryStatusId { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
    }

    // Existing Pay Matrix dropdown
    public class PayMatrixLookupModel
    {
        public long PayMatrixId { get; set; }
        public string PayLevel { get; set; }
        public int PayCell { get; set; }
        public decimal BasicPay { get; set; }
    }
    // All dropdowns in one response
    public class SalaryConfigurationLookupResponse
    {
        public List<SalaryDesignationLookupModel> Designations { get; set; }
        public List<SalaryStatusLookupModel> SalaryStatuses { get; set; }
        public List<PayMatrixLookupModel> PayMatrices { get; set; }
    }

    // Individual employee salary configuration details
    public class SalaryConfigurationDetailModel
    {
        public long EmployeeId { get; set; }
        public long? SalaryConfigurationId { get; set; }

        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }

        public long? OfficeId { get; set; }
        public string OfficeName { get; set; }

        public long? DesignationId { get; set; }
        public string DesignationName { get; set; }

        public long? PayMatrixId { get; set; }
        public string PayLevel { get; set; }
        public string PayCell { get; set; }
        public decimal? BasicPay { get; set; }

        public DateTime? ContractStartDate { get; set; }
        public DateTime? ContractEndDate { get; set; }

        public long? SalaryStatusId { get; set; }
        public string SalaryStatus { get; set; }
    }
    

        public class UpdateSalaryHeaderDraftModel
        {
            public DateTime EffectiveFrom { get; set; }

            public DateTime? EffectiveTo { get; set; }

            public DateTime? NextIncrementDate { get; set; }

            public long PayMatrixId { get; set; }

            public List<UpdateSalaryComponentModel> Components { get; set; }
        }

    public class UpdateSalaryComponentModel
    {
        public long ComponentId { get; set; }

        public int Priority { get; set; }

        public string RuleInput { get; set; }

        public decimal MonthlyAmount { get; set; }
    }

    public class PostSalaryHeaderDraftModel
    {
        public long EmployeeId { get; set; }

        public long ContractId { get; set; }

        public DateTime EffectiveFrom { get; set; }

        public DateTime? EffectiveTo { get; set; }

        public DateTime? NextIncrementDate { get; set; }

        public long PayMatrixId { get; set; }

        public List<SalaryComponentInputModel> Components { get; set; }
    }

    public class SalaryComponentInputModel
    {
        public long ComponentId { get; set; }

        public int Priority { get; set; }

        public string RuleInput { get; set; }

        public decimal MonthlyAmount { get; set; }
    }

}
