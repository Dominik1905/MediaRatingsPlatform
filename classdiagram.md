classDiagram
direction LR

class Program {
  +Main(args)
}

class HttpServer {
  -HttpListener _listener
  +HttpServer(prefix)
  +Start()
  -Listen()
}

class Router {
  <<static>>
  +Handle(context)
}

class UserController {
  <<static>>
  +Handle(context, method, path)
}

class MediaController {
  <<static>>
  -DatabaseService dbService
  +Handle(context, method, path)
  -HandlePost(context)
  -HandleGet(context, path)
  -HandlePut(context, path)
  -HandleDelete(context, path)
  -WriteJson(context, statusCode, json)
}

class AuthHelper {
  <<static>>
  +GetUserFromRequest(context) User?
}

class AuthService {
  <<static>>
  -DatabaseService dbService
  +Register(user) bool
  +Login(username, password) string?
  +ValidateJwtToken(token) ClaimsPrincipal?
  -GenerateJwtToken(user) string
}

class DatabaseService {
  -string connectionString
  +AddUser(user)
  +GetUserByUsername(username) User?
  +InsertMedia(media)
  +GetMediaById(id) Media?
  +SearchMediaByTitle(titlePart) List~Media~
  +UpdateMedia(media)
  +DeleteMedia(id, userId)
  +AddFavorite(userId, mediaId)
  +RemoveFavorite(userId, mediaId)
  +GetFavoritesByUserId(userId) List~Media~
  +InsertRating(rating)
  +... weitere DB-Methoden ...
}

class User {
  +int Id
  +string Username
  +string PasswordHash
}

class Media {
  +int Id
  +string Title
  +string Description
  +MediaType Type
  +int ReleaseYear
  +string Genre
  +int AgeRestriction
  +int CreatedByUserId
  +List~Rating~ Ratings
  +List~string~ LikedByUsers
}

class Rating {
  +int Id
  +int MediaId
  +int UserId
  +int Stars
  +string Comment
  +bool Confirmed
  +DateTime Timestamp
  +int Likes
  +List~string~ LikedByUsers
}

class MediaType {
  <<enum>>
  Movie
  Series
  Game
}

Program --> HttpServer : creates
HttpServer --> Router : calls Handle()
Router --> UserController : routes /api/users
Router --> MediaController : routes /api/media

UserController --> AuthService : login/register/validate
UserController --> AuthHelper : (optional) token checks
UserController --> DatabaseService : read/write data

MediaController --> AuthHelper : auth user
MediaController --> DatabaseService : CRUD + queries

AuthHelper --> AuthService : validate token
AuthHelper --> DatabaseService : load user by username

AuthService --> DatabaseService : user lookup / insert

DatabaseService --> User
DatabaseService --> Media
DatabaseService --> Rating

Media --> Rating : contains
Media --> MediaType : uses
