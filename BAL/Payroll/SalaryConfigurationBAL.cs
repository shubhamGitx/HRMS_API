
using System;
using System.Threading.Tasks;
using hrmsAPI.DAL.Payroll;
using hrmsAPI.MODEL.Payroll;

namespace HRMSAPI.BAL.Payroll
{
    public class SalaryConfigurationBAL
    {
        private readonly SalaryConfigurationDAL _dal;

        public SalaryConfigurationBAL(SalaryConfigurationDAL dal)
        {
            _dal = dal;
        }

        // 1. Employee List with Search, Filters and Pagination
        public async Task<SalaryConfigurationListResponse>
            GetSalaryConfigurations(SalaryConfigurationFilterModel model)
        {
            if (model == null)
            {
                model = new SalaryConfigurationFilterModel();
            }

            if (model.PageNumber <= 0)
            {
                model.PageNumber = 1;
            }

            if (model.PageSize <= 0)
            {
                model.PageSize = 10;
            }

            if (model.PageSize > 100)
            {
                model.PageSize = 100;
            }

            if (model.DesignationId.HasValue &&
                model.DesignationId.Value <= 0)
            {
                throw new ArgumentException("Invalid Designation ID.");
            }

            if (model.SalaryStatusId.HasValue &&
                model.SalaryStatusId.Value <= 0)
            {
                throw new ArgumentException("Invalid Salary Status ID.");
            }

            if (string.IsNullOrWhiteSpace(model.Search))
            {
                model.Search = null;
            }
            else
            {
                model.Search = model.Search.Trim();
            }

            return await _dal.GetSalaryConfigurations(model);
        }

        // 2. Dashboard Summary
        public async Task<SalaryConfigurationSummaryModel>
            GetSalaryConfigurationSummary()
        {
            return await _dal.GetSalaryConfigurationSummary();
        }

        // 3. Dropdown Lookups
        public async Task<SalaryConfigurationLookupResponse>
            GetSalaryConfigurationLookups()
        {
            return await _dal.GetSalaryConfigurationLookups();
        }

        // 4. Employee Salary Configuration By ID
        public async Task<SalaryConfigurationDetailModel?>
            GetSalaryConfigurationById(long employeeId)
        {
            if (employeeId <= 0)
            {
                throw new ArgumentException("Invalid Employee ID.");
            }

            return await _dal.GetSalaryConfigurationById(employeeId);
        }

        // 5. Update Existing Draft Salary Configuration
        public async Task<long> UpdateSalaryHeaderDraft(
            long salaryHeaderId,
            UpdateSalaryHeaderDraftModel model,
            long updatedBy)
        {
            if (salaryHeaderId <= 0)
            {
                throw new ArgumentException("Invalid Salary Header ID.");
            }

            if (model == null)
            {
                throw new ArgumentException(
                    "Salary configuration data is required.");
            }

            if (updatedBy <= 0)
            {
                throw new ArgumentException("Invalid Updated By user ID.");
            }

            if (model.EffectiveFrom == DateTime.MinValue)
            {
                throw new ArgumentException(
                    "Effective From date is required.");
            }

            if (model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
            {
                throw new ArgumentException(
                    "Effective To cannot be earlier than Effective From.");
            }

            if (model.PayMatrixId <= 0)
            {
                throw new ArgumentException(
                    "Please select a valid Pay Matrix.");
            }

            if (model.Components == null || model.Components.Count == 0)
            {
                throw new ArgumentException(
                    "At least one salary component is required.");
            }

            if (model.Components.Exists(x =>
                x == null ||
                x.ComponentId <= 0 ||
                x.Priority <= 0 ||
                x.MonthlyAmount < 0))
            {
                throw new ArgumentException(
                    "Invalid salary component details.");
            }

            return await _dal.UpdateSalaryHeaderDraft(
                salaryHeaderId,
                model,
                updatedBy);
        }

        public async Task<long> SaveSalaryHeaderDraft(
            PostSalaryHeaderDraftModel model,
            long createdBy)
        {
            if (model == null)
            {
                throw new ArgumentException(
                    "Salary configuration data is required.");
            }

            if (model.EmployeeId <= 0)
            {
                throw new ArgumentException("Invalid Employee ID.");
            }

            if (model.ContractId <= 0)
            {
                throw new ArgumentException("Invalid Contract ID.");
            }

            if (createdBy <= 0)
            {
                throw new ArgumentException("Invalid Created By user ID.");
            }

            if (model.EffectiveFrom == DateTime.MinValue)
            {
                throw new ArgumentException(
                    "Effective From date is required.");
            }

            if (model.EffectiveTo.HasValue &&
                model.EffectiveTo.Value.Date < model.EffectiveFrom.Date)
            {
                throw new ArgumentException(
                    "Effective To cannot be earlier than Effective From.");
            }

            if (model.NextIncrementDate.HasValue &&
                model.NextIncrementDate.Value.Date < model.EffectiveFrom.Date)
            {
                throw new ArgumentException(
                    "Next Increment Date cannot be earlier than Effective From.");
            }

            if (model.PayMatrixId <= 0)
            {
                throw new ArgumentException("Please select a valid Pay Matrix.");
            }

            if (model.Components == null || model.Components.Count == 0)
            {
                throw new ArgumentException(
                    "At least one salary component is required.");
            }

            if (model.Components.Exists(x =>
                x == null ||
                x.ComponentId <= 0 ||
                x.Priority <= 0 ||
                x.MonthlyAmount < 0))
            {
                throw new ArgumentException(
                    "Invalid salary component details.");
            }

            // Prevent duplicate components
            if (model.Components
                .GroupBy(x => x.ComponentId)
                .Any(g => g.Count() > 1))
            {
                throw new ArgumentException(
                    "Duplicate salary components are not allowed.");
            }

            return await _dal.SaveSalaryHeaderDraft(
                model,
                createdBy);
        }
        public async Task<long> LockSalaryHeader(
    long salaryHeaderId,
    long updatedBy)
        {
            if (salaryHeaderId <= 0)
            {
                throw new ArgumentException(
                    "Invalid Salary Header ID.");
            }

            if (updatedBy <= 0)
            {
                throw new ArgumentException(
                    "Invalid Updated By user ID.");
            }

            return await _dal.LockSalaryHeader(
                salaryHeaderId,
                updatedBy);
        }
    }
}
