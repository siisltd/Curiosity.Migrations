CREATE TABLE public.background_processor_requests
(
    id bigserial NOT NULL,
    created timestamp(6) without time zone DEFAULT timezone('UTC'::text, now()) NOT NULL,
    time_zone_id character varying(50) NOT NULL,
    processor_name varchar(50),
    type int NOT NULL,
    state int NOT NULL,
    start_processing timestamp(6) without time zone,
    finish_processing timestamp(6) without time zone,
    user_id bigint NOT NULL,
    project_id bigint,
    data_binary bytea,
    culture varchar(15),
    log text,
    params_data_json text,
    result_data_json text,
    CONSTRAINT background_processor_requests_pkey PRIMARY KEY (id)
);

ALTER TABLE public.background_processor_requests
    OWNER to %USER%;