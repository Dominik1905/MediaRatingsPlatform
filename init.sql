-- Optional: Nur wenn erlaubt
-- CREATE EXTENSION IF NOT EXISTS pg_trgm;

-- DROP: alte quoted UND unquoted Varianten weg (idempotent)
DROP TABLE IF EXISTS public."UserLikedRatings" CASCADE;
DROP TABLE IF EXISTS public."UserLikedMedia" CASCADE;
DROP TABLE IF EXISTS public."Ratings" CASCADE;
DROP TABLE IF EXISTS public."Media" CASCADE;
DROP TABLE IF EXISTS public."Users" CASCADE;

DROP TABLE IF EXISTS public.userlikedratings CASCADE;
DROP TABLE IF EXISTS public.userlikedmedia CASCADE;
DROP TABLE IF EXISTS public.ratings CASCADE;
DROP TABLE IF EXISTS public.media CASCADE;
DROP TABLE IF EXISTS public.users CASCADE;

-- USERS
CREATE TABLE public.users (
                              id            INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                              username      VARCHAR(100) NOT NULL,
                              passwordhash  VARCHAR(255) NOT NULL
);

ALTER TABLE public.users
    ADD CONSTRAINT uq_users_username UNIQUE (username);

CREATE UNIQUE INDEX ux_users_username_lower
    ON public.users (LOWER(username));

-- MEDIA
CREATE TABLE public.media (
                              id              INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                              title           VARCHAR(255) NOT NULL,
                              description     TEXT NOT NULL,
                              type            INT NOT NULL CHECK (type IN (0,1,2)),
                              releaseyear     INT NOT NULL CHECK (releaseyear BETWEEN 1800 AND 2100),
                              genre           VARCHAR(100) NOT NULL,
                              agerestriction  INT NOT NULL CHECK (agerestriction >= 0),
                              createdbyuserid INT NOT NULL REFERENCES public.users(id) ON DELETE RESTRICT
);

CREATE INDEX ix_media_createdby ON public.media (createdbyuserid);
CREATE INDEX ix_media_type      ON public.media (type);
CREATE INDEX ix_media_genre     ON public.media (genre);
CREATE INDEX ix_media_year      ON public.media (releaseyear);
CREATE INDEX ix_media_age       ON public.media (agerestriction);

-- (Optional) Trigram-Index nur wenn pg_trgm aktiviert ist
-- CREATE INDEX ix_media_title_trgm ON public.media USING GIN (title gin_trgm_ops);

-- RATINGS
CREATE TABLE public.ratings (
                                id        INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                                mediaid   INT NOT NULL REFERENCES public.media(id) ON DELETE CASCADE,
                                userid    INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
                                stars     INT NOT NULL CHECK (stars BETWEEN 1 AND 5),
                                comment   TEXT NULL,
                                confirmed BOOLEAN NOT NULL DEFAULT FALSE,
                                timestamp TIMESTAMP NOT NULL DEFAULT NOW(),
                                CONSTRAINT uq_ratings_media_user UNIQUE (mediaid, userid)
);

CREATE INDEX ix_ratings_media ON public.ratings (mediaid);
CREATE INDEX ix_ratings_user  ON public.ratings (userid);

-- FAVORITES
CREATE TABLE public.userlikedmedia (
                                       userid  INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
                                       mediaid INT NOT NULL REFERENCES public.media(id) ON DELETE CASCADE,
                                       PRIMARY KEY (userid, mediaid)
);

CREATE INDEX ix_ulm_media ON public.userlikedmedia (mediaid);

-- LIKES
CREATE TABLE public.userlikedratings (
                                         userid   INT NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
                                         ratingid INT NOT NULL REFERENCES public.ratings(id) ON DELETE CASCADE,
                                         PRIMARY KEY (userid, ratingid)
);

CREATE INDEX ix_ulr_rating ON public.userlikedratings (ratingid);
