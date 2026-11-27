CREATE OR REPLACE FUNCTION payroll.fn_lock_salary_header(
	p_salary_header_id bigint,
	p_updated_by bigint)
    RETURNS bigint
    LANGUAGE 'plpgsql'
    COST 100
    VOLATILE PARALLEL UNSAFE
AS $BODY$
DECLARE
    v_employee_id bigint;
    v_header payroll.t_employee_salary_headers%ROWTYPE;
    v_draft_status_id bigint;
    v_locked_status_id bigint;
    v_previous_header_id bigint;
    v_previous_effective_from date;
BEGIN
    IF p_salary_header_id IS NULL OR p_salary_header_id <= 0 THEN
        RAISE EXCEPTION 'Invalid Salary Header ID.';
    END IF;

    IF p_updated_by IS NULL OR p_updated_by <= 0 THEN
        RAISE EXCEPTION 'Invalid Updated By user ID.';
    END IF;

    SELECT h.employee_id
    INTO v_employee_id
    FROM payroll.t_employee_salary_headers h
    WHERE h.salary_header_id = p_salary_header_id;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Salary header not found.';
    END IF;

    -- Serialize version changes made by this function for the same employee.
    PERFORM 1
    FROM employee.employee_master em
    WHERE em.employee_id = v_employee_id
    FOR NO KEY UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Employee not found.';
    END IF;

    -- Read the draft again after acquiring the locks.
    SELECT h.*
    INTO v_header
    FROM payroll.t_employee_salary_headers h
    WHERE h.salary_header_id = p_salary_header_id
      AND h.employee_id = v_employee_id
    FOR UPDATE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Salary header not found.';
    END IF;

    SELECT
        (SELECT ss.salary_status_id
         FROM payroll.m_salary_status ss
         WHERE UPPER(ss.code) = 'DRAFT'),
        (SELECT ss.salary_status_id
         FROM payroll.m_salary_status ss
         WHERE UPPER(ss.code) = 'LOCKED')
    INTO v_draft_status_id, v_locked_status_id;

    IF v_draft_status_id IS NULL OR v_locked_status_id IS NULL THEN
        RAISE EXCEPTION 'DRAFT and LOCKED salary statuses must be configured.';
    END IF;

    IF v_header.salary_status_id = v_locked_status_id THEN
        RETURN p_salary_header_id;
    END IF;

    IF v_header.salary_status_id IS DISTINCT FROM v_draft_status_id THEN
        RAISE EXCEPTION 'Only draft salary headers can be locked.';
    END IF;

    IF v_header.effective_from IS NULL
       OR v_header.effective_to < v_header.effective_from THEN
        RAISE EXCEPTION 'The salary version has invalid effective dates.';
    END IF;

    IF NOT EXISTS (
        SELECT 1
        FROM payroll.t_employee_salary_components sc
        WHERE sc.salary_header_id = p_salary_header_id
    ) THEN
        RAISE EXCEPTION 'Add at least one salary component before locking.';
    END IF;

    SELECT h.salary_header_id, h.effective_from
    INTO v_previous_header_id, v_previous_effective_from
    FROM payroll.t_employee_salary_headers h
    WHERE h.employee_id = v_employee_id
      AND h.salary_status_id = v_locked_status_id
    ORDER BY h.effective_from DESC NULLS FIRST, h.salary_header_id DESC
    LIMIT 1;

    IF v_previous_header_id IS NOT NULL THEN
        IF v_previous_effective_from IS NULL
           OR v_header.effective_from <= v_previous_effective_from THEN
            RAISE EXCEPTION
                'The new effective date must be later than the latest locked version.';
        END IF;

        UPDATE payroll.t_employee_salary_headers
        SET effective_to = v_header.effective_from - 1,
            updated_by = p_updated_by,
            updated_on = CURRENT_TIMESTAMP
        WHERE salary_header_id = v_previous_header_id
          AND (effective_to IS NULL OR effective_to >= v_header.effective_from);
    END IF;

    UPDATE payroll.t_employee_salary_headers
    SET salary_status_id = v_locked_status_id,
        updated_by = p_updated_by,
        updated_on = CURRENT_TIMESTAMP
    WHERE salary_header_id = p_salary_header_id;

    RETURN p_salary_header_id;
END;
$BODY$;

ALTER FUNCTION payroll.fn_lock_salary_header(bigint, bigint)
    OWNER TO postgres;