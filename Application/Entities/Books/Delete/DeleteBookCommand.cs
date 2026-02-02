namespace LibApp.Application.Entities.Books.Delete;

public class DeleteBookCommand(IRepository<Book> bookRepo)
    : IDeleteCommand<DeleteBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(DeleteBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault();

        if (book == null)
        {
            return new BasicCreateDeleteResponse("Error", "Book not found.");
        }

        await bookRepo.Remove(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book deleted successfully.");
    }
}