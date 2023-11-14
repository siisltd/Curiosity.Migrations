--CURIOSITY:Dependencies=1.0, 2.0

CREATE TABLE public.background_processor_requests1
(
    id bigserial NOT NULL,
    CONSTRAINT background_processor_requests_pkey1 PRIMARY KEY (id)
);

ALTER TABLE public.background_processor_requests1
    OWNER to %USER%;