
using System.Data;
using System.Threading.Tasks;
using Dapper;
using hrmsAPI.MODEL.Payroll;
using Npgsql;

namespace hrmsAPI.DAL.Payroll
{
    public class SalaryConfigurationDAL
    {
        private readonly string _connectionString;

        public SalaryConfigurationDAL(
            Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("HrmsDatabase");
        }

        // 1. Employee List with Search, Filters and Pagination
        public async Task<SalaryConfigurationListResponse>
            GetSalaryConfigurations(SalaryConfigurationFilterModel model)
        {
            var response = new SalaryConfigurationListResponse();

            var parameters = new
            {
                p_search = model.Search,
                p_designation_id = model.DesignationId,
                p_salary_status_id = model.SalaryStatusId,
                p_page_size = model.PageSize,
                p_offset = (model.PageNumber - 1) * model.PageSize
            };

            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                response.TotalRecords =
                    await con.QueryFirstAsync<int>(
                        @"SELECT payroll.fn_count_salary_configuration_employees(
                            @p_search,
                            @p_designation_id,
                            @p_salary_status_id
                        );",
                        parameters
                    );

                response.Data =
                    (await con.QueryAsync<SalaryConfigurationListModel>(
                        @"SELECT *
                          FROM payroll.fn_get_salary_configuration_employees(
                              @p_search,
                              @p_designation_id,
                              @p_salary_status_id,
                              @p_page_size,
                              @p_offset
                          );",
                        parameters
                    )).AsList();
            }

            response.PageNumber = model.PageNumber;
            response.PageSize = model.PageSize;

            return response;
        }

        // 2. Dashboard Summary
        public async Task<SalaryConfigurationSummaryModel>
            GetSalaryConfigurationSummary()
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                var result =
                    await con.QueryFirstOrDefaultAsync<SalaryConfigurationSummaryModel>(
                        @"SELECT *
                          FROM payroll.fn_get_salary_configuration_summary();"
                    );

                return result ?? new SalaryConfigurationSummaryModel
                {
                    TotalEmployees = 0,
                    PendingDraft = 0,
                    Locked = 0
                };
            }
        }

        // 3. Designation, Salary Status and Pay Matrix Lookups
        public async Task<SalaryConfigurationLookupResponse>
            GetSalaryConfigurationLookups()
        {
            var response = new SalaryConfigurationLookupResponse();

            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                response.Designations =
                    (await con.QueryAsync<SalaryDesignationLookupModel>(
                        @"SELECT *
                          FROM payroll.fn_get_salary_designation_lookup();"
                    )).AsList();

                response.SalaryStatuses =
                    (await con.QueryAsync<SalaryStatusLookupModel>(
                        @"SELECT *
                          FROM payroll.fn_get_salary_status_lookup();"
                    )).AsList();

                response.PayMatrices =
                    (await con.QueryAsync<PayMatrixLookupModel>(
                        @"SELECT *
                          FROM payroll.fn_get_salary_pay_matrix_lookup();"
                    )).AsList();
            }

            return response;
        }

        // 4. Employee Salary Configuration By Employee ID
        public async Task<SalaryConfigurationDetailModel?>
            GetSalaryConfigurationById(long employeeId)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                return await con.QueryFirstOrDefaultAsync<SalaryConfigurationDetailModel>(
                    @"SELECT *
                      FROM payroll.fn_get_salary_configuration_by_id(
                          @p_employee_id
                      );",
                    new
                    {
                        p_employee_id = employeeId
                    }
                );
            }
        }

        // 5. Update Existing Draft Salary Configuration
        public async Task<long> UpdateSalaryHeaderDraft(
       long salaryHeaderId,
       UpdateSalaryHeaderDraftModel model,
       long updatedBy)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                var parameters = new DynamicParameters();

                parameters.Add(
                    "p_salary_header_id",
                    salaryHeaderId,
                    DbType.Int64);

                parameters.Add(
                    "p_effective_from",
                    model.EffectiveFrom.Date,
                    DbType.Date);

                parameters.Add(
                    "p_effective_to",
                    model.EffectiveTo.HasValue
                        ? (object)model.EffectiveTo.Value.Date
                        : null,
                    DbType.Date);

                parameters.Add(
                    "p_next_increment_date",
                    model.NextIncrementDate.HasValue
                        ? (object)model.NextIncrementDate.Value.Date
                        : null,
                    DbType.Date);

                parameters.Add(
                    "p_pay_matrix_id",
                    model.PayMatrixId,
                    DbType.Int64);

                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.CamelCase
                };

                string componentsJson =
                    System.Text.Json.JsonSerializer.Serialize(
                        model.Components,
                        jsonOptions);

                parameters.Add(
                    "p_components",
                    componentsJson,
                    DbType.String);

                parameters.Add(
                    "p_updated_by",
                    updatedBy,
                    DbType.Int64);

                return await con.ExecuteScalarAsync<long>(
                    @"SELECT payroll.fn_update_salary_header_draft(
                @p_salary_header_id,
                @p_effective_from,
                @p_effective_to,
                @p_next_increment_date,
                @p_pay_matrix_id,
                CAST(@p_components AS JSONB),
                @p_updated_by
            );",
                    parameters);
            }
        }

        public async Task<long> SaveSalaryHeaderDraft(
      PostSalaryHeaderDraftModel model,
      long createdBy)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                var parameters = new DynamicParameters();

                parameters.Add("p_employee_id", model.EmployeeId);
                parameters.Add("p_contract_id", model.ContractId);

                parameters.Add(
                    "p_effective_from",
                    model.EffectiveFrom.Date,
                    DbType.Date);

                parameters.Add(
                    "p_effective_to",
                    model.EffectiveTo.HasValue
                        ? (object)model.EffectiveTo.Value.Date
                        : null,
                    DbType.Date);

                parameters.Add(
                    "p_next_increment_date",
                    model.NextIncrementDate.HasValue
                        ? (object)model.NextIncrementDate.Value.Date
                        : null,
                    DbType.Date);

                parameters.Add("p_pay_matrix_id", model.PayMatrixId);

                // Convert PascalCase C# properties to camelCase JSON
                var jsonOptions = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy =
                        System.Text.Json.JsonNamingPolicy.CamelCase
                };

                string componentsJson =
                    System.Text.Json.JsonSerializer.Serialize(
                        model.Components,
                        jsonOptions);

                parameters.Add(
                    "p_components",
                    componentsJson,
                    DbType.String);

                parameters.Add("p_created_by", createdBy);

                return await con.ExecuteScalarAsync<long>(
                    @"SELECT payroll.fn_save_salary_header_draft(
                @p_employee_id,
                @p_contract_id,
                @p_effective_from,
                @p_effective_to,
                @p_next_increment_date,
                @p_pay_matrix_id,
                CAST(@p_components AS JSONB),
                @p_created_by
            );",
                    parameters);
            }
        }
        public async Task<long> LockSalaryHeader(
    long salaryHeaderId,
    long updatedBy)
        {
            using (NpgsqlConnection con =
                new NpgsqlConnection(_connectionString))
            {
                await con.OpenAsync();

                return await con.ExecuteScalarAsync<long>(
                    @"SELECT payroll.fn_lock_salary_header(
                @p_salary_header_id,
                @p_updated_by
            );",
                    new
                    {
                        p_salary_header_id = salaryHeaderId,
                        p_updated_by = updatedBy
                    });
            }
        }
    }
}
