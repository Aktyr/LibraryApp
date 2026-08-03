namespace WebAPI.Endpoints.MinimalAPI;

[Obsolete("This class is deprecated. Use the new endpoint classes instead.")]
public static class Endpoints
{
    private const string _authGroup = "Auth";
    private const string _booksGroup = "Books";
    private const string _borrowingGroup = "Borrowing";
    private const string _reportsGroup = "Reports";
    private const string _roomsGroup = "Rooms";
    private const string _searchGroup = "Search";
    private const string _usersGroup = "Users";
    public static void MapEndpoints(this IEndpointRouteBuilder app)
    {

        #region Auth
        var auth = app.MapGroup("/api/auth").AllowAnonymous().WithTags(_authGroup);
        auth.MapPost("/login", async (LoginRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<LoginCommand, LoginRequest, LoginResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        auth.MapPost("/register", async (RegisterRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<RegisterCommand, RegisterRequest, RegisterResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion

        #region Books
        var books = app.MapGroup("/api/books").RequireRoles(UserRole.Admin, UserRole.Librarian).WithTags(_booksGroup);
        books.MapGet("/", async (IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<GetAllBooksCommand, EmptyRequest, BookResponse>(sp, new EmptyRequest(), ct);
            return Results.Ok(result);
        }).RequireRoles(UserRole.Reader);
        books.MapGet("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetBookRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<GetBookCommand, GetBookRequest, BookResponse>(sp, request, ct);
            return Results.Ok(result);
        }).RequireRoles(UserRole.Reader);
        books.MapPost("/", async (CreateBookRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<CreateBookCommand, CreateBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        books.MapPut("/", async (UpdateBookRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<UpdateBookCommand, UpdateBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        books.MapDelete("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new DeleteBookRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<DeleteBookCommand, DeleteBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        books.MapPost("/discard", async (DiscardBookRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<DiscardBookCommand, DiscardBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        books.MapGet("/discarded", async (DateTime? fromDate, DateTime? toDate, DiscardReason? reason, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetDiscardedBooksRequest { FromDate = fromDate, ToDate = toDate, DiscardReason = reason };
            var result = await MinimalApiController.ExecuteAsync<GetDiscardedBooksCommand, GetDiscardedBooksRequest, DiscardedBookResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        books.MapPost("/undo-discard", async (UndoDiscardRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<UndoDiscardCommand, UndoDiscardRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion

        #region Borrowing
        var borrowing = app.MapGroup("/api/borrowing").RequireRoles(UserRole.Reader, UserRole.Librarian, UserRole.Admin).WithTags(_borrowingGroup);
        borrowing.MapPost("/borrow", async (BorrowBookRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<BorrowBookCommand, BorrowBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        borrowing.MapPost("/return", async (ReturnBookRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<ReturnBookCommand, ReturnBookRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        borrowing.MapPost("/extend", async (ExtendDeadlineRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<ExtendDeadlineCommand, ExtendDeadlineRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion

        #region Reports
        var reports = app.MapGroup("/api/reports").RequireRoles(UserRole.Admin, UserRole.Librarian).WithTags(_reportsGroup);
        reports.MapGet("/popular-books", async (int? topCount, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetPopularBooksRequest { TopCount = topCount ?? 10 };
            var result = await MinimalApiController.ExecuteAsync<GetPopularBooksReportCommand, GetPopularBooksRequest, BookPopularityReportResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        reports.MapGet("/user-activity", async (bool onlyWithOverdue, bool onlyActive, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetUserActivityRequest { OnlyWithOverdue = onlyWithOverdue, OnlyActive = onlyActive };
            var result = await MinimalApiController.ExecuteAsync<GetUserActivityReportCommand, GetUserActivityRequest, UserActivityReportResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion

        #region Rooms 
        var rooms = app.MapGroup("/api/rooms").RequireRoles(UserRole.Admin, UserRole.Librarian).WithTags(_roomsGroup);
        rooms.MapGet("/", async (IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<GetAllRoomsCommand, EmptyRequest, RoomResponse>(sp, new EmptyRequest(), ct);
            return Results.Ok(result);
        }).RequireRoles(UserRole.Reader);
        rooms.MapGet("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetRoomRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<GetRoomCommand, GetRoomRequest, RoomResponse>(sp, request, ct);
            return Results.Ok(result);
        }).RequireRoles(UserRole.Reader);
        rooms.MapPost("/", async (CreateRoomRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<CreateRoomCommand, CreateRoomRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        rooms.MapPut("/", async (UpdateRoomRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<UpdateRoomCommand, UpdateRoomRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        rooms.MapDelete("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new DeleteRoomRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<DeleteRoomCommand, DeleteRoomRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion

        #region Search
        app.MapGet("/api/search/books", async (string? query, string? title, string? author, int? yearFrom, int? yearTo, string? publisher, bool availableOnly, string? sortBy, bool sortDescending, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new SearchBooksRequest
            {
                Query = query,
                Title = title,
                Author = author,
                YearFrom = yearFrom,
                YearTo = yearTo,
                Publisher = publisher,
                AvailableOnly = availableOnly,
                SortBy = sortBy ?? "Title",
                SortDescending = sortDescending
            };
            var result = await MinimalApiController.ExecuteAsync<SearchBooksCommand, SearchBooksRequest, BookResponse>(sp, request, ct);
            return Results.Ok(result);
        }).AllowAnonymous().WithTags(_searchGroup);
        #endregion

        #region Users
        var users = app.MapGroup("/api/users").RequireRoles(UserRole.Admin, UserRole.Librarian).WithTags(_usersGroup);
        users.MapGet("/", async (IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<GetAllUsersCommand, EmptyRequest, UserResponse>(sp, new EmptyRequest(), ct);
            return Results.Ok(result);
        });
        users.MapGet("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetUserRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<GetUserCommand, GetUserRequest, UserResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        users.MapGet("/{id}/books", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new GetUserBooksRequest { UserId = id };
            var result = await MinimalApiController.ExecuteAsync<GetUserRoomBooksCommand, GetUserBooksRequest, BorrowResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        users.MapPost("/", async (CreateUserRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<CreateUserCommand, CreateUserRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        users.MapPut("/", async (UpdateUserRequest request, IServiceProvider sp, CancellationToken ct) =>
        {
            var result = await MinimalApiController.ExecuteAsync<UpdateUserCommand, UpdateUserRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        users.MapDelete("/{id}", async (Guid id, IServiceProvider sp, CancellationToken ct) =>
        {
            var request = new DeleteUserRequest { Id = new Id(id) };
            var result = await MinimalApiController.ExecuteAsync<DeleteUserCommand, DeleteUserRequest, BasicCreateDeleteResponse>(sp, request, ct);
            return Results.Ok(result);
        });
        #endregion
    }
}