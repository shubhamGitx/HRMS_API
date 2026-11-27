CREATE OR REPLACE FUNCTION payroll.fn_update_salary_header_draft(
    p_salary_header_id bigint,
    p_effective_from date,
    p_effective_to date,
    p_next_increment_date date,
    p_pay_matrix_id bigint,
    p_components jsonb,
    p_updated_by bigint
)
RETURNS bigint
LANGUAGE plpgsql
AS $BODY$
DECLARE
    v_header payroll.t_employee_salary_headers%ROWTYPE;
    v_invalid_component_count bigint;
    v_has_duplicate_components boolean;
    v_gross_earnings numeric(14, 2);
    v_total_deductions numeric(14, 2);
BEGIN
    IF p_salary_header_id IS NULL OR p_salary_header_id <= 0 THEN
        RAISE EXCEPTION 'Invalid Salary Header ID.';
    END IF;

    IF p_updated_by IS NULL OR p_updated_by <= 0 THEN
        RAISE EXCEPTION 'Invalid Updated By user ID.';
    END IF;

    -- Hold the header lock until its component list has been replaced.
    SELECT sh.*
    INTO v_header
    FROM payroll.t_employee_salary_headers sh
    WHERE sh.salary_header_id = p_salary_header_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Salary header not found.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM payroll.m_salary_status ss
        WHERE ss.salary_status_id = v_header.salary_status_id
          AND UPPER(ss.code) = 'DRAFT'
    ) THEN
        RAISE EXCEPTION 'Only draft salary headers can be updated.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM employee.employee_contract_details cd
        WHERE cd.employee_id = v_header.employee_id
          AND cd.contract_id = v_header.contract_id
    ) THEN
        RAISE EXCEPTION 'Invalid employee or contract.';
    END IF;

    IF p_effective_from IS NULL THEN
        RAISE EXCEPTION 'Effective From date is required.';
    END IF;

    IF p_effective_to < p_effective_from THEN
        RAISE EXCEPTION 'Effective To cannot be earlier than Effective From.';
    END IF;

    IF p_next_increment_date < p_effective_from THEN
        RAISE EXCEPTION 'Next Increment Date cannot be earlier than Effective From.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM payroll.m_pay_matrix pm
        WHERE pm.pay_matrix_id = p_pay_matrix_id
          AND p_effective_from >= pm.effective_from
          AND (pm.effective_to IS NULL OR p_effective_from <= pm.effective_to)
    ) THEN
        RAISE EXCEPTION 'Invalid Pay Matrix for the selected effective date.';
    END IF;

    IF jsonb_typeof(p_components) IS DISTINCT FROM 'array' THEN
        RAISE EXCEPTION 'Salary components must be provided as a JSON array.';
    END IF;

    IF jsonb_array_length(p_components) = 0 THEN
        RAISE EXCEPTION 'At least one salary component is required.';
    END IF;

    SELECT
        COUNT(*) FILTER (
            WHERE pc.component_id IS NULL OR c.priority IS NULL OR c.priority <= 0
               OR c."monthlyAmount" IS NULL OR c."monthlyAmount" < 0
        ),
        COUNT(*) > COUNT(DISTINCT c."componentId"),
        COALESCE(SUM(c."monthlyAmount") FILTER (WHERE UPPER(ct.code) = 'EARNING'), 0),
        COALESCE(SUM(c."monthlyAmount") FILTER (WHERE UPPER(ct.code) = 'DEDUCTION'), 0)
    INTO v_invalid_component_count, v_has_duplicate_components,
         v_gross_earnings, v_total_deductions
    FROM jsonb_to_recordset(p_components) AS c(
        "componentId" bigint, priority integer, "monthlyAmount" numeric
    )
    LEFT JOIN payroll.m_pay_components pc ON pc.component_id = c."componentId"
    LEFT JOIN payroll.m_component_types ct ON ct.component_type_id = pc.component_type_id;

    IF v_invalid_component_count > 0 THEN
        RAISE EXCEPTION 'Invalid salary component details.';
    END IF;

    IF v_has_duplicate_components THEN
        RAISE EXCEPTION 'Duplicate salary components are not allowed.';
    END IF;

    UPDATE payroll.t_employee_salary_headers
    SET effective_from = p_effective_from,
        effective_to = p_effective_to,
        next_increment_date = p_next_increment_date,
        pay_matrix_id = p_pay_matrix_id,
        gross_earnings = v_gross_earnings,
        total_deductions = v_total_deductions,
        net_payable = v_gross_earnings - v_total_deductions,
        updated_by = p_updated_by,
        updated_on = CURRENT_TIMESTAMP
    WHERE salary_header_id = p_salary_header_id;

    DELETE FROM payroll.t_employee_salary_components
    WHERE salary_header_id = p_salary_header_id;

    INSERT INTO payroll.t_employee_salary_components (
        salary_header_id, component_id, priority, rule_input, monthly_amount
    )
    SELECT p_salary_header_id, c."componentId", c.priority, c."ruleInput", c."monthlyAmount"
    FROM jsonb_to_recordset(p_components) AS c(
        "componentId" bigint, priority integer, "ruleInput" text, "monthlyAmount" numeric
    );

    RETURN p_salary_header_id;
END;
$BODY$;
