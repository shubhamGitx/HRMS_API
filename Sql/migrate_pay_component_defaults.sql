-- Run this migration before deploying the updated API/UI.
-- It persists the default rules and makes the API validate the agreed priority bands.
BEGIN;

ALTER TABLE payroll.m_pay_components
    ADD COLUMN IF NOT EXISTS default_value numeric(14,2),
    ADD COLUMN IF NOT EXISTS default_base_component_code varchar(30),
    ADD COLUMN IF NOT EXISTS default_max_cap numeric(14,2),
    ADD COLUMN IF NOT EXISTS default_formula text,
    ADD COLUMN IF NOT EXISTS is_matrix_linked boolean NOT NULL DEFAULT FALSE;

-- Repair existing rows created with component/calculation codes but without their lookup IDs.
UPDATE payroll.m_pay_components pc
SET component_type_id = COALESCE(pc.component_type_id, ct.component_type_id),
    calc_type_id = COALESCE(pc.calc_type_id, calc.calc_type_id)
FROM payroll.m_component_types ct
CROSS JOIN payroll.m_calculation_types calc
WHERE UPPER(ct.code) = UPPER(pc.component_type)
  AND UPPER(calc.code) = UPPER(pc.calc_type)
  AND (pc.component_type_id IS NULL OR pc.calc_type_id IS NULL);

-- The project's matrix-paid base salary component is BASIC.
UPDATE payroll.m_pay_components
SET is_matrix_linked = TRUE,
    default_value = NULL
WHERE UPPER(code) = 'BASIC'
  AND UPPER(component_type) = 'EARNING'
  AND UPPER(calc_type) = 'FIXED';

-- Preserve the existing statutory formulas as master defaults so Configure Salary reads them from data.
UPDATE payroll.m_pay_components
SET default_formula = CASE UPPER(code)
        WHEN 'EPF_EMP' THEN 'MIN(BASIC, 15000) * 0.12'
        WHEN 'ESIC_EMP' THEN 'IF(BASIC <= 21000, BASIC * 0.0075, 0)'
        WHEN 'EPF_ER' THEN 'MIN(BASIC, 15000) * 0.13'
    END
WHERE UPPER(code) IN ('EPF_EMP', 'ESIC_EMP', 'EPF_ER')
  AND UPPER(calc_type) = 'FORMULA'
  AND default_formula IS NULL;

-- Remove the previous signatures so PostgreSQL cannot choose an obsolete overload.
DROP FUNCTION IF EXISTS payroll.fn_insert_pay_component(text, text, bigint, bigint, integer, boolean, boolean);
DROP FUNCTION IF EXISTS payroll.fn_update_pay_component(bigint, text, bigint, bigint, integer, boolean, boolean);
DROP FUNCTION IF EXISTS payroll.fn_get_pay_components(text, bigint, bigint, boolean, integer, integer);
DROP FUNCTION IF EXISTS payroll.fn_get_pay_component_by_id(bigint);

CREATE OR REPLACE FUNCTION payroll.fn_insert_pay_component(
    p_code text,
    p_name text,
    p_component_type_id bigint,
    p_calc_type_id bigint,
    p_calc_priority integer,
    p_default_value numeric,
    p_default_base_component_code text,
    p_default_max_cap numeric,
    p_default_formula text,
    p_is_matrix_linked boolean,
    p_is_prorated boolean,
    p_is_taxable boolean,
    p_is_active boolean
)
RETURNS bigint
LANGUAGE plpgsql
AS $BODY$
DECLARE
    v_component_id bigint;
    v_component_type_code text;
    v_calc_type_code text;
BEGIN
    SELECT code INTO v_component_type_code
    FROM payroll.m_component_types
    WHERE component_type_id = p_component_type_id AND is_active = TRUE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Component Type ID.' USING ERRCODE = '23503';
    END IF;

    SELECT code INTO v_calc_type_code
    FROM payroll.m_calculation_types
    WHERE calc_type_id = p_calc_type_id AND is_active = TRUE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Calculation Type ID.' USING ERRCODE = '23503';
    END IF;

    IF (v_component_type_code = 'EARNING' AND p_calc_priority NOT BETWEEN 1 AND 9)
       OR (v_component_type_code = 'DEDUCTION' AND p_calc_priority NOT BETWEEN 10 AND 89)
       OR (v_component_type_code = 'EMPLOYER_CONTRIB' AND p_calc_priority < 90) THEN
        RAISE EXCEPTION 'Priority must be 1–9 for earnings, 10–89 for deductions, and 90 or higher for employer contributions.' USING ERRCODE = '22023';
    END IF;
    IF p_default_value < 0 OR p_default_max_cap < 0 THEN
        RAISE EXCEPTION 'Default value and maximum cap must be zero or greater.' USING ERRCODE = '22023';
    END IF;

    IF v_calc_type_code = 'FIXED' THEN
        IF p_is_matrix_linked AND p_default_value IS NOT NULL THEN
            RAISE EXCEPTION 'Matrix-linked fixed components must not have a default amount.' USING ERRCODE = '22023';
        ELSIF NOT p_is_matrix_linked AND p_default_value IS NULL THEN
            RAISE EXCEPTION 'A fixed component needs a default amount unless it is matrix-linked.' USING ERRCODE = '22023';
        END IF;
        IF p_default_base_component_code IS NOT NULL OR p_default_formula IS NOT NULL OR p_default_max_cap IS NOT NULL THEN
            RAISE EXCEPTION 'Fixed components do not accept a percentage base, formula, or maximum cap.' USING ERRCODE = '22023';
        END IF;
    ELSIF v_calc_type_code = 'PERCENTAGE' THEN
        IF p_is_matrix_linked OR p_default_value IS NULL OR NULLIF(TRIM(p_default_base_component_code), '') IS NULL THEN
            RAISE EXCEPTION 'Percentage components need a percentage and base component code, and cannot be matrix-linked.' USING ERRCODE = '22023';
        END IF;
        IF p_default_formula IS NOT NULL THEN
            RAISE EXCEPTION 'Percentage components do not accept a formula expression.' USING ERRCODE = '22023';
        END IF;
    ELSIF v_calc_type_code = 'FORMULA' THEN
        IF p_is_matrix_linked OR NULLIF(TRIM(p_default_formula), '') IS NULL THEN
            RAISE EXCEPTION 'Formula components need a formula expression and cannot be matrix-linked.' USING ERRCODE = '22023';
        END IF;
        IF p_default_value IS NOT NULL OR p_default_base_component_code IS NOT NULL THEN
            RAISE EXCEPTION 'Formula components do not accept a default value or percentage base component.' USING ERRCODE = '22023';
        END IF;
    END IF;

    INSERT INTO payroll.m_pay_components (
        code, name, component_type, calc_type, component_type_id, calc_type_id, calc_priority,
        default_value, default_base_component_code, default_max_cap, default_formula,
        is_matrix_linked, is_prorated, is_taxable, is_active
    ) VALUES (
        UPPER(TRIM(p_code)), TRIM(p_name), v_component_type_code, v_calc_type_code,
        p_component_type_id, p_calc_type_id, p_calc_priority,
        CASE WHEN p_is_matrix_linked THEN NULL ELSE p_default_value END,
        NULLIF(UPPER(TRIM(p_default_base_component_code)), ''), p_default_max_cap,
        NULLIF(TRIM(p_default_formula), ''), p_is_matrix_linked,
        p_is_prorated, p_is_taxable, p_is_active
    ) RETURNING component_id INTO v_component_id;

    RETURN v_component_id;
END;
$BODY$;

CREATE OR REPLACE FUNCTION payroll.fn_update_pay_component(
    p_component_id bigint,
    p_name text,
    p_component_type_id bigint,
    p_calc_type_id bigint,
    p_calc_priority integer,
    p_default_value numeric,
    p_default_base_component_code text,
    p_default_max_cap numeric,
    p_default_formula text,
    p_is_matrix_linked boolean,
    p_is_prorated boolean,
    p_is_taxable boolean,
    p_is_active boolean
)
RETURNS integer
LANGUAGE plpgsql
AS $BODY$
DECLARE
    v_component_type_code text;
    v_calc_type_code text;
    v_rows integer;
BEGIN
    SELECT code INTO v_component_type_code
    FROM payroll.m_component_types
    WHERE component_type_id = p_component_type_id AND is_active = TRUE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Component Type ID.' USING ERRCODE = '23503';
    END IF;

    SELECT code INTO v_calc_type_code
    FROM payroll.m_calculation_types
    WHERE calc_type_id = p_calc_type_id AND is_active = TRUE;
    IF NOT FOUND THEN
        RAISE EXCEPTION 'Invalid or inactive Calculation Type ID.' USING ERRCODE = '23503';
    END IF;

    IF (v_component_type_code = 'EARNING' AND p_calc_priority NOT BETWEEN 1 AND 9)
       OR (v_component_type_code = 'DEDUCTION' AND p_calc_priority NOT BETWEEN 10 AND 89)
       OR (v_component_type_code = 'EMPLOYER_CONTRIB' AND p_calc_priority < 90) THEN
        RAISE EXCEPTION 'Priority must be 1–9 for earnings, 10–89 for deductions, and 90 or higher for employer contributions.' USING ERRCODE = '22023';
    END IF;
    IF p_default_value < 0 OR p_default_max_cap < 0 THEN
        RAISE EXCEPTION 'Default value and maximum cap must be zero or greater.' USING ERRCODE = '22023';
    END IF;

    IF v_calc_type_code = 'FIXED' THEN
        IF (p_is_matrix_linked AND p_default_value IS NOT NULL)
           OR (NOT p_is_matrix_linked AND p_default_value IS NULL)
           OR p_default_base_component_code IS NOT NULL
           OR p_default_formula IS NOT NULL
           OR p_default_max_cap IS NOT NULL THEN
            RAISE EXCEPTION 'Fixed components need an amount unless matrix-linked; percentage, formula, and cap fields are not applicable.' USING ERRCODE = '22023';
        END IF;
    ELSIF v_calc_type_code = 'PERCENTAGE' THEN
        IF p_is_matrix_linked OR p_default_value IS NULL OR NULLIF(TRIM(p_default_base_component_code), '') IS NULL OR p_default_formula IS NOT NULL THEN
            RAISE EXCEPTION 'Percentage components need a percentage and base component code, and cannot be matrix-linked or have a formula.' USING ERRCODE = '22023';
        END IF;
    ELSIF v_calc_type_code = 'FORMULA' THEN
        IF p_is_matrix_linked OR NULLIF(TRIM(p_default_formula), '') IS NULL OR p_default_value IS NOT NULL OR p_default_base_component_code IS NOT NULL THEN
            RAISE EXCEPTION 'Formula components need a formula expression and cannot be matrix-linked or have a default value/base component.' USING ERRCODE = '22023';
        END IF;
    END IF;

    UPDATE payroll.m_pay_components
    SET name = TRIM(p_name),
        component_type_id = p_component_type_id,
        calc_type_id = p_calc_type_id,
        component_type = v_component_type_code,
        calc_type = v_calc_type_code,
        calc_priority = p_calc_priority,
        default_value = CASE WHEN p_is_matrix_linked THEN NULL ELSE p_default_value END,
        default_base_component_code = NULLIF(UPPER(TRIM(p_default_base_component_code)), ''),
        default_max_cap = p_default_max_cap,
        default_formula = NULLIF(TRIM(p_default_formula), ''),
        is_matrix_linked = p_is_matrix_linked,
        is_prorated = p_is_prorated,
        is_taxable = p_is_taxable,
        is_active = p_is_active
    WHERE component_id = p_component_id;

    GET DIAGNOSTICS v_rows = ROW_COUNT;
    RETURN v_rows;
END;
$BODY$;

-- The API list/detail responses now include the defaults needed by Configure Salary.
CREATE FUNCTION payroll.fn_get_pay_components(
    p_search text,
    p_component_type_id bigint,
    p_calc_type_id bigint,
    p_is_active boolean,
    p_page_size integer,
    p_offset integer
)
RETURNS TABLE(
    "ComponentId" bigint,
    "Code" character varying,
    "Name" character varying,
    "ComponentTypeId" bigint,
    "ComponentTypeName" character varying,
    "CalcTypeId" bigint,
    "CalcTypeName" character varying,
    "CalcPriority" integer,
    "IsProrated" boolean,
    "IsTaxable" boolean,
    "IsActive" boolean,
    "DefaultValue" numeric,
    "DefaultBaseComponentCode" character varying,
    "DefaultMaxCap" numeric,
    "DefaultFormula" text,
    "IsMatrixLinked" boolean
)
LANGUAGE plpgsql
AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        pc.component_id,
        pc.code::varchar,
        pc.name::varchar,
        pc.component_type_id,
        ct.name::varchar,
        pc.calc_type_id,
        calc.name::varchar,
        pc.calc_priority,
        pc.is_prorated,
        pc.is_taxable,
        pc.is_active,
        pc.default_value,
        pc.default_base_component_code::varchar,
        pc.default_max_cap,
        pc.default_formula,
        pc.is_matrix_linked
    FROM payroll.m_pay_components pc
    INNER JOIN payroll.m_component_types ct
        ON ct.component_type_id = pc.component_type_id
    INNER JOIN payroll.m_calculation_types calc
        ON calc.calc_type_id = pc.calc_type_id
    WHERE (COALESCE(TRIM(p_search), '') = ''
           OR pc.code ILIKE '%' || TRIM(p_search) || '%'
           OR pc.name ILIKE '%' || TRIM(p_search) || '%')
      AND (p_component_type_id IS NULL OR pc.component_type_id = p_component_type_id)
      AND (p_calc_type_id IS NULL OR pc.calc_type_id = p_calc_type_id)
      AND (p_is_active IS NULL OR pc.is_active = p_is_active)
    ORDER BY pc.component_id DESC
    LIMIT GREATEST(COALESCE(p_page_size, 10), 1)
    OFFSET GREATEST(COALESCE(p_offset, 0), 0);
END;
$BODY$;

CREATE FUNCTION payroll.fn_get_pay_component_by_id(p_component_id bigint)
RETURNS TABLE(
    "ComponentId" bigint,
    "Code" character varying,
    "Name" character varying,
    "ComponentTypeId" bigint,
    "ComponentTypeName" character varying,
    "CalcTypeId" bigint,
    "CalcTypeName" character varying,
    "CalcPriority" integer,
    "IsProrated" boolean,
    "IsTaxable" boolean,
    "IsActive" boolean,
    "DefaultValue" numeric,
    "DefaultBaseComponentCode" character varying,
    "DefaultMaxCap" numeric,
    "DefaultFormula" text,
    "IsMatrixLinked" boolean
)
LANGUAGE plpgsql
AS $BODY$
BEGIN
    RETURN QUERY
    SELECT
        pc.component_id,
        pc.code::varchar,
        pc.name::varchar,
        pc.component_type_id,
        ct.name::varchar,
        pc.calc_type_id,
        calc.name::varchar,
        pc.calc_priority,
        pc.is_prorated,
        pc.is_taxable,
        pc.is_active,
        pc.default_value,
        pc.default_base_component_code::varchar,
        pc.default_max_cap,
        pc.default_formula,
        pc.is_matrix_linked
    FROM payroll.m_pay_components pc
    INNER JOIN payroll.m_component_types ct
        ON ct.component_type_id = pc.component_type_id
    INNER JOIN payroll.m_calculation_types calc
        ON calc.calc_type_id = pc.calc_type_id
    WHERE pc.component_id = p_component_id;
END;
$BODY$;

COMMIT;
