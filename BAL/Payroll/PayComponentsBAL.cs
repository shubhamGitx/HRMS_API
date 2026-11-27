
using System;
using System.Threading.Tasks;
using hrmsAPI.DAL.Payroll;
using hrmsAPI.MODEL.Payroll;

namespace HRMSAPI.BAL.Payroll
{
    public class PayComponentsBAL
    {
        private readonly PayComponentsDAL _dal;

        public PayComponentsBAL(PayComponentsDAL dal)
        {
            _dal = dal;
        }

        // =====================================================
        // 1. GET ALL PAY COMPONENTS WITH SEARCH AND FILTER
        // =====================================================
        public async Task<PayComponentListResponse> GetPayComponents(
            PayComponentFilterModel model)
        {
            if (model == null)
                throw new ArgumentException("Invalid request.");

            // Default pagination
            if (model.PageNumber <= 0)
                model.PageNumber = 1;

            if (model.PageSize <= 0)
                model.PageSize = 10;

            if (model.PageSize > 100)
                model.PageSize = 100;

            // Validate Component Type ID
            if (model.ComponentTypeId.HasValue &&
                model.ComponentTypeId.Value <= 0)
            {
                throw new ArgumentException(
                    "Invalid component type ID.");
            }

            // Validate Calculation Type ID
            if (model.CalcTypeId.HasValue &&
                model.CalcTypeId.Value <= 0)
            {
                throw new ArgumentException(
                    "Invalid calculation type ID.");
            }

            if (!string.IsNullOrWhiteSpace(model.Search))
                model.Search = model.Search.Trim();

            return await _dal.GetPayComponents(model);
        }

        // =====================================================
        // 2. GET PAY COMPONENT BY ID
        // =====================================================
        public async Task<GetPayComponentModel> GetPayComponentById(
            long componentId)
        {
            if (componentId <= 0)
                throw new ArgumentException("Invalid component ID.");

            return await _dal.GetPayComponentById(componentId);
        }

        // =====================================================
        // 3. INSERT NEW PAY COMPONENT
        // =====================================================
        public async Task<PayComponentResponseModel> InsertPayComponent(
            PostPayComponentModel model)
        {
            string validationMessage =
                ValidatePostPayComponent(model);

            if (validationMessage != null)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = validationMessage
                };
            }

            // Normalize input values
            model.Code = model.Code.Trim().ToUpper();
            model.Name = model.Name.Trim();
            model.DefaultBaseComponentCode = string.IsNullOrWhiteSpace(model.DefaultBaseComponentCode)
                ? null
                : model.DefaultBaseComponentCode.Trim().ToUpperInvariant();
            model.DefaultFormula = string.IsNullOrWhiteSpace(model.DefaultFormula)
                ? null
                : model.DefaultFormula.Trim();

            long componentId =
                await _dal.InsertPayComponent(model);

            return new PayComponentResponseModel
            {
                Success = true,
                ComponentId = componentId,
                Message = "Pay component added successfully."
            };
        }

        // =====================================================
        // 4. UPDATE PAY COMPONENT
        // =====================================================
        public async Task<PayComponentResponseModel> UpdatePayComponent(
            long componentId,
            UpdatePayComponentModel model)
        {
            if (componentId <= 0)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = "Invalid component ID."
                };
            }

            string validationMessage =
                ValidateUpdatePayComponent(model);

            if (validationMessage != null)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = validationMessage
                };
            }

            model.Name = model.Name.Trim();
            model.DefaultBaseComponentCode = string.IsNullOrWhiteSpace(model.DefaultBaseComponentCode)
                ? null
                : model.DefaultBaseComponentCode.Trim().ToUpperInvariant();
            model.DefaultFormula = string.IsNullOrWhiteSpace(model.DefaultFormula)
                ? null
                : model.DefaultFormula.Trim();

            int rowsAffected =
                await _dal.UpdatePayComponent(componentId, model);

            if (rowsAffected == 0)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = "Pay component not found."
                };
            }

            return new PayComponentResponseModel
            {
                Success = true,
                ComponentId = componentId,
                Message = "Pay component updated successfully."
            };
        }

        // =====================================================
        // 5. ACTIVATE / DEACTIVATE PAY COMPONENT
        // =====================================================
        public async Task<PayComponentResponseModel>
            ChangePayComponentStatus(
                long componentId,
                bool isActive)
        {
            if (componentId <= 0)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = "Invalid component ID."
                };
            }

            int rowsAffected =
                await _dal.ChangePayComponentStatus(
                    componentId, isActive);

            if (rowsAffected == 0)
            {
                return new PayComponentResponseModel
                {
                    Success = false,
                    Message = "Pay component not found."
                };
            }

            return new PayComponentResponseModel
            {
                Success = true,
                ComponentId = componentId,
                Message = isActive
                    ? "Pay component activated successfully."
                    : "Pay component deactivated successfully."
            };
        }

        // =====================================================
        // 6. GET DASHBOARD SUMMARY
        // =====================================================
        public async Task<PayComponentSummaryModel>
            GetPayComponentSummary()
        {
            return await _dal.GetPayComponentSummary();
        }

        // =====================================================
        // 7. GET COMPONENT TYPE AND CALCULATION TYPE DROPDOWNS
        // =====================================================
        public async Task<PayComponentLookupResponseModel>
            GetPayComponentLookups()
        {
            return await _dal.GetPayComponentLookups();
        }

        // =====================================================
        // 8. VALIDATION - POST
        // =====================================================
        private string ValidatePostPayComponent(
            PostPayComponentModel model)
        {
            if (model == null)
                return "Request body is required.";

            if (string.IsNullOrWhiteSpace(model.Code))
                return "Component code is required.";

            if (model.Code.Trim().Length > 30)
                return "Component code cannot exceed 30 characters.";

            if (string.IsNullOrWhiteSpace(model.Name))
                return "Component name is required.";

            if (model.Name.Trim().Length > 100)
                return "Component name cannot exceed 100 characters.";

            if (model.ComponentTypeId <= 0)
                return "Valid component type ID is required.";

            if (model.CalcTypeId <= 0)
                return "Valid calculation type ID is required.";

            if (model.CalcPriority <= 0)
                return "Calculation priority must be greater than zero.";

            if ((model.DefaultValue.HasValue && model.DefaultValue.Value < 0) ||
                (model.DefaultMaxCap.HasValue && model.DefaultMaxCap.Value < 0))
                return "Default values and maximum caps must be zero or greater.";

            return null;
        }

        // =====================================================
        // 9. VALIDATION - UPDATE
        // =====================================================
        private string ValidateUpdatePayComponent(
            UpdatePayComponentModel model)
        {
            if (model == null)
                return "Request body is required.";

            if (string.IsNullOrWhiteSpace(model.Name))
                return "Component name is required.";

            if (model.Name.Trim().Length > 100)
                return "Component name cannot exceed 100 characters.";

            if (model.ComponentTypeId <= 0)
                return "Valid component type ID is required.";

            if (model.CalcTypeId <= 0)
                return "Valid calculation type ID is required.";

            if (model.CalcPriority <= 0)
                return "Calculation priority must be greater than zero.";

            if ((model.DefaultValue.HasValue && model.DefaultValue.Value < 0) ||
                (model.DefaultMaxCap.HasValue && model.DefaultMaxCap.Value < 0))
                return "Default values and maximum caps must be zero or greater.";

            return null;
        }
    }
}
