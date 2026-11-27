
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Dapper;
using Npgsql;
using hrmsAPI.MODEL.Payroll;

namespace hrmsAPI.DAL.Payroll
{
    public class PayComponentsDAL
    {
        private readonly string connectionString;

        public PayComponentsDAL(IConfiguration configuration)
        {
            connectionString =
                configuration.GetConnectionString("HrmsDatabase");
        }

        // =====================================================
        // 1. GET ALL PAY COMPONENTS WITH SEARCH AND FILTER
        // =====================================================
        public async Task<PayComponentListResponse> GetPayComponents(
            PayComponentFilterModel model)
        {
            PayComponentListResponse response =
                new PayComponentListResponse();

            var parameters = new
            {
                p_search = model.Search,
                p_component_type_id = model.ComponentTypeId,
                p_calc_type_id = model.CalcTypeId,
                p_is_active = model.IsActive,
                p_page_size = model.PageSize,
                p_offset = (model.PageNumber - 1) * model.PageSize
            };

            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                await con.OpenAsync();

                response.TotalRecords =
                    await con.QueryFirstAsync<int>(
                        @"SELECT payroll.fn_count_pay_components(
                            @p_search,
                            @p_component_type_id,
                            @p_calc_type_id,
                            @p_is_active
                        );",
                        parameters);

                response.Data =
                    (await con.QueryAsync<GetPayComponentModel>(
                        @"SELECT * FROM payroll.fn_get_pay_components(
                            @p_search,
                            @p_component_type_id,
                            @p_calc_type_id,
                            @p_is_active,
                            @p_page_size,
                            @p_offset
                        );",
                        parameters)).AsList();
            }

            response.PageNumber = model.PageNumber;
            response.PageSize = model.PageSize;

            return response;
        }

        // =====================================================
        // 2. GET PAY COMPONENT BY ID
        // =====================================================
        public async Task<GetPayComponentModel> GetPayComponentById(
            long componentId)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                return await con.QueryFirstOrDefaultAsync<GetPayComponentModel>(
                    @"SELECT * FROM payroll.fn_get_pay_component_by_id(
                        @p_component_id
                    );",
                    new
                    {
                        p_component_id = componentId
                    });
            }
        }

        // =====================================================
        // 3. INSERT NEW PAY COMPONENT
        // =====================================================
        public async Task<long> InsertPayComponent(
            PostPayComponentModel model)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                return await con.QueryFirstAsync<long>(
                    @"SELECT payroll.fn_insert_pay_component(
                        @p_code,
                        @p_name,
                        @p_component_type_id,
                        @p_calc_type_id,
                        @p_calc_priority,
                        @p_default_value,
                        @p_default_base_component_code,
                        @p_default_max_cap,
                        @p_default_formula,
                        @p_is_matrix_linked,
                        @p_is_prorated,
                        @p_is_taxable,
                        @p_is_active
                    );",
                    new
                    {
                        p_code = model.Code,
                        p_name = model.Name,
                        p_component_type_id = model.ComponentTypeId,
                        p_calc_type_id = model.CalcTypeId,
                        p_calc_priority = model.CalcPriority,
                        p_default_value = model.DefaultValue,
                        p_default_base_component_code = model.DefaultBaseComponentCode,
                        p_default_max_cap = model.DefaultMaxCap,
                        p_default_formula = model.DefaultFormula,
                        p_is_matrix_linked = model.IsMatrixLinked,
                        p_is_prorated = model.IsProrated,
                        p_is_taxable = model.IsTaxable,
                        p_is_active = model.IsActive
                    });
            }
        }

        // =====================================================
        // 4. UPDATE PAY COMPONENT
        // =====================================================
        public async Task<int> UpdatePayComponent(
            long componentId,
            UpdatePayComponentModel model)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                return await con.QueryFirstAsync<int>(
                    @"SELECT payroll.fn_update_pay_component(
                        @p_component_id,
                        @p_name,
                        @p_component_type_id,
                        @p_calc_type_id,
                        @p_calc_priority,
                        @p_default_value,
                        @p_default_base_component_code,
                        @p_default_max_cap,
                        @p_default_formula,
                        @p_is_matrix_linked,
                        @p_is_prorated,
                        @p_is_taxable,
                        @p_is_active
                    );",
                    new
                    {
                        p_component_id = componentId,
                        p_name = model.Name,
                        p_component_type_id = model.ComponentTypeId,
                        p_calc_type_id = model.CalcTypeId,
                        p_calc_priority = model.CalcPriority,
                        p_default_value = model.DefaultValue,
                        p_default_base_component_code = model.DefaultBaseComponentCode,
                        p_default_max_cap = model.DefaultMaxCap,
                        p_default_formula = model.DefaultFormula,
                        p_is_matrix_linked = model.IsMatrixLinked,
                        p_is_prorated = model.IsProrated,
                        p_is_taxable = model.IsTaxable,
                        p_is_active = model.IsActive
                    });
            }
        }

        // =====================================================
        // 5. ACTIVATE / DEACTIVATE PAY COMPONENT
        // =====================================================
        public async Task<int> ChangePayComponentStatus(
            long componentId,
            bool isActive)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                return await con.QueryFirstAsync<int>(
                    @"SELECT payroll.fn_change_pay_component_status(
                        @p_component_id,
                        @p_is_active
                    );",
                    new
                    {
                        p_component_id = componentId,
                        p_is_active = isActive
                    });
            }
        }

        // =====================================================
        // 6. GET DASHBOARD SUMMARY
        // =====================================================
        public async Task<PayComponentSummaryModel>
            GetPayComponentSummary()
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                return await con.QueryFirstAsync<PayComponentSummaryModel>(
                    @"SELECT * FROM payroll.fn_get_pay_component_summary();");
            }
        }

        // =====================================================
        // 7. GET COMPONENT TYPE AND CALCULATION TYPE DROPDOWNS
        // =====================================================
        public async Task<PayComponentLookupResponseModel>
            GetPayComponentLookups()
        {
            PayComponentLookupResponseModel response =
                new PayComponentLookupResponseModel();

            using (NpgsqlConnection con =
                new NpgsqlConnection(connectionString))
            {
                await con.OpenAsync();

                response.ComponentTypes =
                    (await con.QueryAsync<ComponentTypeLookupModel>(
                        @"SELECT * FROM payroll.fn_get_component_types();"
                    )).AsList();

                response.CalculationTypes =
                    (await con.QueryAsync<CalculationTypeLookupModel>(
                        @"SELECT * FROM payroll.fn_get_calculation_types();"
                    )).AsList();
            }

            return response;
        }
    }
}
