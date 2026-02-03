namespace LibApp.Application.Entities.Books.Update;

public class UpdateBookCommand(IRepository<Book> bookRepo)
    : ICreateOrUpdateCommand<UpdateBookRequest, BasicCreateDeleteResponse>
{
    public async Task<BasicCreateDeleteResponse> Execute(UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var books = await bookRepo.Get(x => x.Id.Value == request.Id.Value, cancellationToken);
        var book = books.FirstOrDefault();

        if (book == null)
            throw new BookNotFoundException();

        book.Title = request.Title;
        book.Author = request.Author;
        book.Year = request.Year;
        book.Publisher = request.Publisher;

        await bookRepo.Update(book, cancellationToken);
        return new BasicCreateDeleteResponse("Ok", "Book updated successfully.");
    }
}