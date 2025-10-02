-- Tabelle für Benutzer
CREATE TABLE Users (
    Id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Username VARCHAR(100) NOT NULL,
    PasswordHash VARCHAR(255) NOT NULL
);

-- Tabelle für Medien
CREATE TABLE Media (
    Id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Title VARCHAR(255) NOT NULL,
    Description TEXT NOT NULL,
    Type INT NOT NULL, -- entspricht MediaType Enum (0=Movie,1=Series,2=Game)
    ReleaseYear INT NOT NULL,
    Genre VARCHAR(100) NOT NULL,
    AgeRestriction INT NOT NULL,
    CreatedByUserId INT NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(Id)
);

-- Tabelle für Bewertungen
CREATE TABLE Ratings (
    Id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    MediaId INT NOT NULL,
    UserId INT NOT NULL,
    Stars INT NOT NULL CHECK (Stars BETWEEN 1 AND 5),
    Comment TEXT NOT NULL,
    Confirmed BOOLEAN NOT NULL,
    Timestamp TIMESTAMP NOT NULL,
    FOREIGN KEY (MediaId) REFERENCES Media(Id),
    FOREIGN KEY (UserId) REFERENCES Users(Id)
);

-- Many-to-Many Tabelle: User ↔ LikedMedia
CREATE TABLE UserLikedMedia (
    UserId INT NOT NULL,
    MediaId INT NOT NULL,
    PRIMARY KEY (UserId, MediaId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (MediaId) REFERENCES Media(Id)
);

-- Many-to-Many Tabelle: User ↔ LikedRatings
CREATE TABLE UserLikedRatings (
    UserId INT NOT NULL,
    RatingId INT NOT NULL,
    PRIMARY KEY (UserId, RatingId),
    FOREIGN KEY (UserId) REFERENCES Users(Id),
    FOREIGN KEY (RatingId) REFERENCES Ratings(Id)
);
