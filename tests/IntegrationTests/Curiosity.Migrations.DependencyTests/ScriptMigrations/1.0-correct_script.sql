CREATE TABLE public.background_processor_requests
(
    id bigserial NOT NULL,
    CONSTRAINT background_processor_requests_pkey PRIMARY KEY (id)
);

ALTER TABLE public.background_processor_requests
    OWNER to %USER%;