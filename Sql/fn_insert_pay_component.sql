CREATE OR REPLACE FUNCTION payroll.fn_insert_pay_component(
    p_code text,
    p_name text,
    p_component_type_id bigint,
    p_calc_type_id bigint,
    p_calc_priority integer,
    p_is_prorated boolean,
    p_is_taxable boolean
)
RETURNS bigint
LANGUAGE plpgsql
AS $BODY$
DECLARE
    v_component_id bigint;
    v_component_type_code text;
    v_calc_type_code text;
BEGIN
    SELECT code
    INTO v_component_type_code
    FROM payroll.m_component_types
    WHERE component_type_id = p_component_type_id
      AND is_active = TRUE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Component Type ID.'
            USING ERRCODE = '23503';
    END IF;

    SELECT code
    INTO v_calc_type_code
    FROM payroll.m_calculation_types
    WHERE calc_type_id = p_calc_type_id
      AND is_active = TRUE;

    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Calculation Type ID.'
            USING ERRCODE = '23503';
    END IF;

    INSERT INTO payroll.m_pay_components (
        code,
        name,
        component_type,
        calc_type,
        calc_priority,
        is_prorated,
        is_taxable,
        is_active
    )
    VALUES (
        UPPER(TRIM(p_code)),
        TRIM(p_name),
        v_component_type_code,
        v_calc_type_code,
        p_calc_priority,
        p_is_prorated,
        p_is_taxable,
        TRUE
    )
    RETURNING component_id INTO v_component_id;

    RETURN v_component_id;
END;
$BODY$;