-- Run once after migrate_pay_component_defaults.sql.
BEGIN;

-- Also covers inactive BASIC definitions and concurrent attempts to create one.
CREATE UNIQUE INDEX IF NOT EXISTS ux_pay_components_one_basic
    ON payroll.m_pay_components (upper(btrim(code)))
    WHERE upper(btrim(code)) = 'BASIC';

ALTER TABLE payroll.m_pay_components DROP CONSTRAINT IF EXISTS ck_basic_matrix_definition;
ALTER TABLE payroll.m_pay_components ADD CONSTRAINT ck_basic_matrix_definition CHECK (
    CASE WHEN upper(btrim(code)) = 'BASIC' THEN
        is_matrix_linked IS TRUE AND component_type = 'EARNING' AND calc_type = 'FIXED'
        AND default_value IS NULL
    ELSE is_matrix_linked IS FALSE END
);

-- Draft writes replace the child rows after writing the header. Inspect the
-- completed version at transaction end, including a direct request to lock it.
CREATE OR REPLACE FUNCTION payroll.check_salary_version_basic()
RETURNS trigger LANGUAGE plpgsql AS $$
DECLARE
    basic_id bigint;
    employee_rate numeric;
    basic_count bigint;
    basic_amount numeric;
BEGIN
    IF NOT EXISTS (SELECT 1 FROM payroll.m_salary_status
                   WHERE salary_status_id = NEW.salary_status_id AND upper(code) IN ('DRAFT','LOCKED')) THEN
        RETURN NULL;
    END IF;
    SELECT component_id INTO basic_id FROM payroll.m_pay_components
    WHERE upper(btrim(code)) = 'BASIC' AND is_active AND is_matrix_linked
    FOR SHARE;
    IF basic_id IS NULL THEN
        RAISE EXCEPTION 'Configure and activate BASIC as a matrix-linked fixed earning before saving this salary.'
            USING ERRCODE = 'P4501';
    END IF;

    SELECT pm.sanctioned_amount INTO employee_rate
    FROM employee.employee_master em
    JOIN payroll.m_pay_matrix pm ON pm.pay_matrix_id = em.pay_matrix_id
    WHERE em.employee_id = NEW.employee_id AND pm.pay_matrix_id = NEW.pay_matrix_id
      AND NEW.effective_from >= pm.effective_from
      AND (pm.effective_to IS NULL OR NEW.effective_from <= pm.effective_to)
    FOR SHARE OF em, pm;
    IF employee_rate IS NULL OR employee_rate <= 0 THEN
        RAISE EXCEPTION 'A valid assigned pay matrix rate is required for this employee and salary effective date.'
            USING ERRCODE = 'P4501';
    END IF;
    SELECT count(*), min(monthly_amount) INTO basic_count, basic_amount
    FROM payroll.t_employee_salary_components
    WHERE salary_header_id = NEW.salary_header_id AND component_id = basic_id;
    IF basic_count <> 1 OR basic_amount IS DISTINCT FROM employee_rate THEN
        RAISE EXCEPTION 'Salary must contain exactly one BASIC line at the employee matrix rate of %.', employee_rate
            USING ERRCODE = 'P4501';
    END IF;
    RETURN NULL;
END;
$$;

DROP TRIGGER IF EXISTS check_salary_version_basic ON payroll.t_employee_salary_headers;
CREATE CONSTRAINT TRIGGER check_salary_version_basic
AFTER INSERT OR UPDATE OF salary_status_id, pay_matrix_id, employee_id, effective_from
ON payroll.t_employee_salary_headers DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION payroll.check_salary_version_basic();

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

COMMIT;
