namespace LibApp.Application.Entities.Books;

public class DeleteBookCommand(IRepository<Book> bookRepo)
    : IDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault() ?? throw new BookNotFoundException();

        await bookRepo.Remove(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book deleted successfully.");
    }
}