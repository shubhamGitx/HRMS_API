-- FUNCTION: payroll.fn_get_salary_configuration_by_id(bigint)

-- DROP FUNCTION IF EXISTS payroll.fn_get_salary_configuration_by_id(bigint);

CREATE OR REPLACE FUNCTION payroll.fn_get_salary_configuration_by_id(
	p_employee_id bigint)
    RETURNS TABLE("EmployeeId" bigint, "SalaryConfigurationId" bigint, "EmployeeCode" character varying, "EmployeeName" character varying, "OfficeId" bigint, "OfficeName" character varying, "DesignationId" bigint, "DesignationName" character varying, "PayMatrixId" bigint, "PayLevel" character varying, "PayCell" character varying, "BasicPay" numeric, "ContractStartDate" date, "ContractEndDate" date, "SalaryStatusId" bigint, "SalaryStatus" character varying, "VersionNo" integer, "EffectiveFrom" date, "EffectiveTo" date, "NextIncrementDate" date, "ComponentsJson" text) 
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
    ROWS 1000

AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        em.employee_id::bigint,
        sh.salary_header_id::bigint,
        em.contract_no::varchar,
        em.emp_name::varchar,
        om.office_id::bigint,
        om.office_name::varchar,
        dm.designation_id::bigint,
        dm.designation_name::varchar,
        pm.pay_matrix_id::bigint,
        pm.level_code::varchar,
        pm.cell_no::varchar,
        pm.sanctioned_amount::numeric,
        ecd.contract_start_date::date,
        ecd.contract_end_date::date,
        ss.salary_status_id::bigint,
        ss.name::varchar,
        sh.version_no::integer,
        sh.effective_from::date,
        sh.effective_to::date,
        sh.next_increment_date::date,
        COALESCE(components.items, '[]'::jsonb)::text
    FROM employee.employee_master em

    LEFT JOIN LATERAL (
        SELECT h.*
        FROM payroll.t_employee_salary_headers h
        WHERE h.employee_id = em.employee_id
        ORDER BY h.created_on DESC, h.salary_header_id DESC
        LIMIT 1
    ) sh ON TRUE

    LEFT JOIN LATERAL (
        SELECT cd.contract_start_date, cd.contract_end_date
        FROM employee.employee_contract_details cd
        WHERE cd.employee_id = em.employee_id
          AND (sh.contract_id IS NULL OR cd.contract_id = sh.contract_id)
        ORDER BY cd.contract_start_date DESC, cd.contract_id DESC
        LIMIT 1
    ) ecd ON TRUE

    LEFT JOIN master.office_master om
        ON om.office_id = em.office_id AND om.lang_code = 1

    LEFT JOIN master.designation_master dm
        ON dm.designation_id = em.designation_id AND dm.lang_code = 1

    LEFT JOIN payroll.m_salary_status ss
        ON ss.salary_status_id = sh.salary_status_id

    LEFT JOIN payroll.m_pay_matrix pm
        ON pm.pay_matrix_id = CASE WHEN upper(ss.code) = 'LOCKED'
                                   THEN sh.pay_matrix_id ELSE em.pay_matrix_id END

    LEFT JOIN LATERAL (
        SELECT jsonb_agg(
            jsonb_build_object(
                'componentId', sc.component_id,
                'priority', sc.priority,
                'ruleInput', sc.rule_input,
                'monthlyAmount', sc.monthly_amount,
                'componentCode', pc.code,
                'componentName', pc.name,
                'componentTypeCode', ct.code
            )
            ORDER BY sc.priority, sc.component_id
        ) AS items
        FROM payroll.t_employee_salary_components sc
        LEFT JOIN payroll.m_pay_components pc
            ON pc.component_id = sc.component_id
        LEFT JOIN payroll.m_component_types ct
            ON ct.component_type_id = pc.component_type_id
        WHERE sc.salary_header_id = sh.salary_header_id
    ) components ON TRUE

    WHERE em.employee_id = p_employee_id;
END;
$BODY$;

ALTER FUNCTION payroll.fn_get_salary_configuration_by_id(bigint)
    OWNER TO postgres;

